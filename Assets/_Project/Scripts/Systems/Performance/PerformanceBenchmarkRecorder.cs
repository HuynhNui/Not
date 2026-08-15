#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using _Project.Scripts.Core.GameLoop;
using _Project.Scripts.Data.Balance;
using _Project.Scripts.Gameplay.Player;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using RuntimeEnemySpawnerSystem = _Project.Scripts.Systems.EnemySpawnerSystem.EnemySpawnerSystem;
using RuntimeGateSystem = _Project.Scripts.Systems.GateSystem.GateSystem;
using RuntimePoolSystem = _Project.Scripts.Systems.PoolSystem.PoolSystem;
using RuntimePoolDiagnostics = _Project.Scripts.Systems.PoolSystem.PoolDiagnostics;

namespace _Project.Scripts.Systems.Performance
{
    public sealed class PerformanceBenchmarkRecorder : MonoBehaviour
    {
        private const string DirectoryName = "PerformanceBenchmark";
        private const string RequestFileName = "benchmark_request.json";
        private const string RawFileName = "Performance_Benchmark_Raw.csv";
        private const string SummaryFileName = "Performance_Benchmark_Summary.csv";
        private const string MetadataFileName = "Performance_Benchmark_Metadata.json";
        private const string CompletionFileName = "benchmark_complete.marker";
        private const string FailureFileName = "benchmark_failed.marker";

        private enum CaptureState
        {
            MenuWarmup,
            MenuMeasurement,
            GameplayWarmup,
            GameplayMeasurement,
            Completed,
            Failed
        }

        private readonly FrameTiming[] _frameTimingBuffer = new FrameTiming[1];
        private readonly List<float> _windowFrameMilliseconds = new List<float>(128);
        private readonly List<ScenarioAccumulator> _scenarios = new List<ScenarioAccumulator>(6);

        private PerformanceBenchmarkRequest _request;
        private GameManager _gameManager;
        private RuntimeEnemySpawnerSystem _enemySpawner;
        private RuntimeGateSystem _gateSystem;
        private PlayerController _playerController;
        private RuntimePoolSystem _poolSystem;
        private StreamWriter _rawWriter;
        private CaptureState _state;
        private string _outputDirectory;
        private float _captureElapsedSeconds;
        private float _gameplayElapsedSeconds;
        private float _stateElapsedSeconds;
        private float _sampleElapsedSeconds;
        private int _sampleIndex;
        private int _gcCollectionBaseline;
        private int _lastPressureNodeIndex = -1;
        private string _lastPressureLabel = "N/A";
        private bool _frameTimingCpuObserved;
        private bool _frameTimingGpuObserved;
        private double _lastCpuFrameMilliseconds;
        private double _lastGpuFrameMilliseconds;
        private double _windowCpuFrameTotal;
        private int _windowCpuFrameCount;
        private double _windowGpuFrameTotal;
        private int _windowGpuFrameCount;
        private double _windowMainThreadTotal;
        private int _windowMainThreadCount;
        private double _windowRenderThreadTotal;
        private int _windowRenderThreadCount;
        private long _windowGcAllocatedBytes;
        private int _windowGcAllocatedFrameCount;

        private ProfilerRecorder _mainThreadRecorder;
        private ProfilerRecorder _renderThreadRecorder;
        private ProfilerRecorder _gcAllocatedRecorder;
        private ProfilerRecorder _drawCallsRecorder;
        private ProfilerRecorder _batchesRecorder;
        private ProfilerRecorder _setPassRecorder;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            string requestPath = Path.Combine(Application.persistentDataPath, DirectoryName, RequestFileName);
            if (!File.Exists(requestPath))
            {
                return;
            }

            GameObject recorderObject = new GameObject(nameof(PerformanceBenchmarkRecorder));
            DontDestroyOnLoad(recorderObject);
            recorderObject.AddComponent<PerformanceBenchmarkRecorder>();
        }

        private void Awake()
        {
            string requestPath = Path.Combine(Application.persistentDataPath, DirectoryName, RequestFileName);
            try
            {
                string json = File.ReadAllText(requestPath);
                _request = JsonUtility.FromJson<PerformanceBenchmarkRequest>(json);
                File.Delete(requestPath);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Performance benchmark request could not be loaded: {exception.Message}");
                Destroy(gameObject);
                return;
            }

            if (_request == null || !_request.enabled)
            {
                Destroy(gameObject);
                return;
            }

            _request.Validate();
        }

        private void Start()
        {
            if (_request == null)
            {
                return;
            }

            _gameManager = FindAnyObjectByType<GameManager>(FindObjectsInactive.Include);
            _enemySpawner = FindAnyObjectByType<RuntimeEnemySpawnerSystem>(FindObjectsInactive.Include);
            _gateSystem = FindAnyObjectByType<RuntimeGateSystem>(FindObjectsInactive.Include);
            _playerController = FindAnyObjectByType<PlayerController>(FindObjectsInactive.Include);
            _poolSystem = FindAnyObjectByType<RuntimePoolSystem>(FindObjectsInactive.Include);

            if (_gameManager == null || _enemySpawner == null || _playerController == null)
            {
                Fail("Required gameplay systems were not found after scene load.");
                return;
            }

            _outputDirectory = Path.Combine(
                Application.persistentDataPath,
                DirectoryName,
                _request.runId);
            Directory.CreateDirectory(_outputDirectory);
            DeleteMarkerFiles();
            CreateScenarios();
            StartProfilerRecorders();
            OpenRawWriter();
            _gcCollectionBaseline = GetGcCollectionCount();
            _state = CaptureState.MenuWarmup;
        }

        private void Update()
        {
            if (_state == CaptureState.Completed || _state == CaptureState.Failed || _request == null)
            {
                return;
            }

            float deltaSeconds = Mathf.Max(0f, Time.unscaledDeltaTime);
            if (deltaSeconds <= 0f)
            {
                return;
            }

            _captureElapsedSeconds += deltaSeconds;
            _stateElapsedSeconds += deltaSeconds;
            _sampleElapsedSeconds += deltaSeconds;

            if (_state == CaptureState.GameplayWarmup || _state == CaptureState.GameplayMeasurement)
            {
                _gameplayElapsedSeconds += deltaSeconds;
            }

            CaptureFrame(deltaSeconds);

            if (_sampleElapsedSeconds >= _request.sampleIntervalSeconds)
            {
                WriteRawSample();
                _sampleElapsedSeconds = 0f;
            }

            AdvanceStateIfNeeded();
        }

        private void OnDestroy()
        {
            if (_gameManager != null)
            {
                _gameManager.RunEnded -= HandleUnexpectedRunEnd;
            }

            _rawWriter?.Dispose();
            DisposeProfilerRecorders();
        }

        private void CaptureFrame(float deltaSeconds)
        {
            float frameMilliseconds = deltaSeconds * 1000f;
            long gcAllocatedBytes = RecorderValue(_gcAllocatedRecorder);
            double mainThreadMilliseconds = NanosecondsToMilliseconds(RecorderValue(_mainThreadRecorder));
            double renderThreadMilliseconds = NanosecondsToMilliseconds(RecorderValue(_renderThreadRecorder));

            FrameTimingManager.CaptureFrameTimings();
            uint timingCount = FrameTimingManager.GetLatestTimings(1, _frameTimingBuffer);
            if (timingCount > 0)
            {
                FrameTiming timing = _frameTimingBuffer[0];
                if (timing.cpuFrameTime > 0d)
                {
                    _lastCpuFrameMilliseconds = timing.cpuFrameTime;
                    _frameTimingCpuObserved = true;
                }

                if (timing.gpuFrameTime > 0d)
                {
                    _lastGpuFrameMilliseconds = timing.gpuFrameTime;
                    _frameTimingGpuObserved = true;
                }
            }

            _windowFrameMilliseconds.Add(frameMilliseconds);
            AccumulateWindowCounter(_frameTimingCpuObserved, _lastCpuFrameMilliseconds, ref _windowCpuFrameTotal, ref _windowCpuFrameCount);
            AccumulateWindowCounter(_frameTimingGpuObserved, _lastGpuFrameMilliseconds, ref _windowGpuFrameTotal, ref _windowGpuFrameCount);
            AccumulateWindowCounter(_mainThreadRecorder.Valid, mainThreadMilliseconds, ref _windowMainThreadTotal, ref _windowMainThreadCount);
            AccumulateWindowCounter(_renderThreadRecorder.Valid, renderThreadMilliseconds, ref _windowRenderThreadTotal, ref _windowRenderThreadCount);

            if (_gcAllocatedRecorder.Valid && gcAllocatedBytes >= 0)
            {
                _windowGcAllocatedBytes += gcAllocatedBytes;
                _windowGcAllocatedFrameCount++;
            }

            ScenarioAccumulator scenario = CurrentScenario();
            scenario?.RecordFrame(
                frameMilliseconds,
                gcAllocatedBytes,
                _gcAllocatedRecorder.Valid,
                mainThreadMilliseconds,
                _mainThreadRecorder.Valid,
                renderThreadMilliseconds,
                _renderThreadRecorder.Valid);
        }

        private void AdvanceStateIfNeeded()
        {
            switch (_state)
            {
                case CaptureState.MenuWarmup when _stateElapsedSeconds >= _request.menuWarmupSeconds:
                    TransitionTo(CaptureState.MenuMeasurement);
                    break;

                case CaptureState.MenuMeasurement when _stateElapsedSeconds >= _request.menuMeasurementSeconds:
                    StartGameplayBenchmark();
                    break;

                case CaptureState.GameplayWarmup when _gameplayElapsedSeconds >= _request.gameplayWarmupSeconds:
                    TransitionTo(CaptureState.GameplayMeasurement);
                    break;

                case CaptureState.GameplayMeasurement when _gameplayElapsedSeconds >= _request.gameplayDurationSeconds:
                    Complete();
                    break;
            }
        }

        private void StartGameplayBenchmark()
        {
            PlayerRunStartStats startStats = new PlayerRunStartStats(
                _request.startingDamage,
                _request.startingFireRate,
                _request.startingMaxHp,
                _request.startingProjectileCount,
                _request.startingSquadSize);

            if (!_gameManager.TryStartPerformanceBenchmarkRun(startStats, _request.benchmarkProfileId))
            {
                Fail("GameManager refused to start the performance benchmark run.");
                return;
            }

            _gameManager.RunEnded -= HandleUnexpectedRunEnd;
            _gameManager.RunEnded += HandleUnexpectedRunEnd;

            if (_request.noGateBaseline && _gateSystem != null)
            {
                _gateSystem.SetSpawningEnabled(false);
            }

            if (_request.invulnerable)
            {
                _playerController.SetGateIncomingDamageMultiplier(0f);
            }

            _gameplayElapsedSeconds = 0f;
            TransitionTo(CaptureState.GameplayWarmup);
        }

        private void HandleUnexpectedRunEnd()
        {
            Fail($"The benchmark run ended early at {_gameplayElapsedSeconds.ToString("0.###", CultureInfo.InvariantCulture)} seconds.");
        }

        private void TransitionTo(CaptureState nextState)
        {
            _state = nextState;
            _stateElapsedSeconds = 0f;
            ResetWindow();
        }

        private ScenarioAccumulator CurrentScenario()
        {
            if (_state == CaptureState.MenuMeasurement)
            {
                return _scenarios[0];
            }

            if (_state != CaptureState.GameplayMeasurement)
            {
                return null;
            }

            if (_gameplayElapsedSeconds >= _request.gameplayDurationSeconds)
            {
                for (int index = _scenarios.Count - 1; index >= 1; index--)
                {
                    ScenarioAccumulator finalScenario = _scenarios[index];
                    if (finalScenario.EndSeconds > finalScenario.StartSeconds)
                    {
                        return finalScenario;
                    }
                }
            }

            for (int index = 1; index < _scenarios.Count; index++)
            {
                ScenarioAccumulator scenario = _scenarios[index];
                if (_gameplayElapsedSeconds >= scenario.StartSeconds
                    && _gameplayElapsedSeconds < scenario.EndSeconds)
                {
                    return scenario;
                }
            }

            return null;
        }

        private void CreateScenarios()
        {
            _scenarios.Clear();
            _scenarios.Add(new ScenarioAccumulator("Main Menu Idle", 0f, _request.menuMeasurementSeconds));
            _scenarios.Add(new ScenarioAccumulator("Early Run", _request.gameplayWarmupSeconds, Math.Min(60f, _request.gameplayDurationSeconds)));
            _scenarios.Add(new ScenarioAccumulator("60-180 Seconds", 60f, Math.Min(180f, _request.gameplayDurationSeconds)));
            _scenarios.Add(new ScenarioAccumulator("180-300 Seconds", 180f, Math.Min(300f, _request.gameplayDurationSeconds)));
            _scenarios.Add(new ScenarioAccumulator("Heavy Combat", 300f, Math.Min(420f, _request.gameplayDurationSeconds)));
            _scenarios.Add(new ScenarioAccumulator("Long Run / Stress", 420f, _request.gameplayDurationSeconds));
        }

        private void OpenRawWriter()
        {
            string path = Path.Combine(_outputDirectory, RawFileName);
            _rawWriter = new StreamWriter(path, false, new UTF8Encoding(false), 65536);
            _rawWriter.WriteLine(
                "run_id,sample_index,capture_elapsed_sec,gameplay_elapsed_sec,state,scenario,included_in_summary," +
                "window_frames,avg_fps,avg_frame_ms,p95_frame_ms,p99_frame_ms,max_frame_ms," +
                "cpu_frame_ms,gpu_frame_ms,main_thread_ms,render_thread_ms,gc_alloc_bytes,gc_collections," +
                "total_allocated_mb,total_reserved_mb,mono_used_mb,mono_heap_mb," +
                "enemy_count,visible_enemy_count,active_projectile_count,active_gate_count,squad_size," +
                "pool_active_count,pool_inactive_count,pressure_phase,spawn_per_second,enemy_cap," +
                "minimum_visible,threat_budget,gate_phase,gate_cadence_sec,draw_calls,batches,setpass_calls," +
                "target_frame_rate,vsync_count,quality_level,battery_temperature_c,battery_level");
        }

        private void WriteRawSample()
        {
            if (_rawWriter == null || _windowFrameMilliseconds.Count == 0)
            {
                return;
            }

            PerformanceFrameStatistics frameStats = PerformanceStatistics.Calculate(_windowFrameMilliseconds);
            ScenarioAccumulator scenario = CurrentScenario();
            bool included = scenario != null;

            long totalAllocated = Profiler.GetTotalAllocatedMemoryLong();
            long totalReserved = Profiler.GetTotalReservedMemoryLong();
            long monoUsed = Profiler.GetMonoUsedSizeLong();
            long monoHeap = Profiler.GetMonoHeapSizeLong();
            int gcCollections = GetGcCollectionCount() - _gcCollectionBaseline;

            int enemyCount = _enemySpawner != null ? _enemySpawner.ActiveEnemyCount : 0;
            int visibleEnemyCount = _enemySpawner != null ? _enemySpawner.VisibleEnemyCount : 0;
            RuntimePoolDiagnostics poolDiagnostics = _poolSystem != null
                ? _poolSystem.CaptureDiagnostics()
                : default;
            int activeGateCount = _gateSystem != null ? _gateSystem.ActiveGateCount : 0;
            int squadSize = _playerController != null ? _playerController.CurrentSquadCount : 0;

            scenario?.RecordSample(
                totalAllocated,
                totalReserved,
                enemyCount,
                poolDiagnostics.ActiveProjectiles,
                activeGateCount,
                poolDiagnostics.TotalInstances,
                gcCollections);

            string[] values =
            {
                Escape(_request.runId),
                _sampleIndex.ToString(CultureInfo.InvariantCulture),
                F(_captureElapsedSeconds),
                IsGameplayState() ? F(_gameplayElapsedSeconds) : "N/A",
                _state.ToString(),
                Escape(included ? scenario.Name : "Warm-up"),
                included ? "1" : "0",
                frameStats.FrameCount.ToString(CultureInfo.InvariantCulture),
                F(frameStats.AverageFps),
                F(frameStats.AverageFrameMilliseconds),
                F(frameStats.P95FrameMilliseconds),
                F(frameStats.P99FrameMilliseconds),
                F(frameStats.MaximumFrameMilliseconds),
                AverageOrUnavailable(_windowCpuFrameTotal, _windowCpuFrameCount),
                AverageOrUnavailable(_windowGpuFrameTotal, _windowGpuFrameCount),
                AverageOrUnavailable(_windowMainThreadTotal, _windowMainThreadCount),
                AverageOrUnavailable(_windowRenderThreadTotal, _windowRenderThreadCount),
                _windowGcAllocatedFrameCount > 0 ? _windowGcAllocatedBytes.ToString(CultureInfo.InvariantCulture) : "N/A",
                gcCollections.ToString(CultureInfo.InvariantCulture),
                BytesToMegabytes(totalAllocated),
                BytesToMegabytes(totalReserved),
                BytesToMegabytes(monoUsed),
                BytesToMegabytes(monoHeap),
                enemyCount.ToString(CultureInfo.InvariantCulture),
                visibleEnemyCount.ToString(CultureInfo.InvariantCulture),
                poolDiagnostics.ActiveProjectiles.ToString(CultureInfo.InvariantCulture),
                activeGateCount.ToString(CultureInfo.InvariantCulture),
                squadSize.ToString(CultureInfo.InvariantCulture),
                poolDiagnostics.ActiveInstances.ToString(CultureInfo.InvariantCulture),
                poolDiagnostics.InactiveInstances.ToString(CultureInfo.InvariantCulture),
                Escape(GetPressurePhase()),
                _enemySpawner != null ? F(_enemySpawner.CurrentRawSpawnPerSecond) : "N/A",
                _enemySpawner != null ? _enemySpawner.CurrentMaxActiveEnemies.ToString(CultureInfo.InvariantCulture) : "N/A",
                _enemySpawner != null ? _enemySpawner.CurrentMinimumVisibleEnemies.ToString(CultureInfo.InvariantCulture) : "N/A",
                _enemySpawner != null ? F(_enemySpawner.CurrentThreatBudget) : "N/A",
                Escape(_gateSystem != null ? _gateSystem.CurrentPhaseId : "N/A"),
                _gateSystem != null ? F(_gateSystem.GateCadenceSeconds) : "N/A",
                RecorderValueOrUnavailable(_drawCallsRecorder),
                RecorderValueOrUnavailable(_batchesRecorder),
                RecorderValueOrUnavailable(_setPassRecorder),
                Application.targetFrameRate.ToString(CultureInfo.InvariantCulture),
                QualitySettings.vSyncCount.ToString(CultureInfo.InvariantCulture),
                Escape(QualitySettings.names[QualitySettings.GetQualityLevel()]),
                "N/A",
                F(SystemInfo.batteryLevel)
            };

            _rawWriter.WriteLine(string.Join(",", values));
            _sampleIndex++;
            if (_sampleIndex % 10 == 0)
            {
                _rawWriter.Flush();
            }

            if (_request.invulnerable && IsGameplayState())
            {
                _playerController?.SetGateIncomingDamageMultiplier(0f);
            }

            ResetWindow();
        }

        private void Complete()
        {
            if (_windowFrameMilliseconds.Count > 0)
            {
                WriteRawSample();
            }

            _state = CaptureState.Completed;
            _rawWriter?.Flush();
            _rawWriter?.Dispose();
            _rawWriter = null;
            WriteSummary();
            WriteMetadata();
            File.WriteAllText(
                Path.Combine(_outputDirectory, CompletionFileName),
                DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            Debug.Log($"Performance benchmark complete: {_outputDirectory}");
        }

        private void Fail(string reason)
        {
            _state = CaptureState.Failed;
            _rawWriter?.Flush();
            _rawWriter?.Dispose();
            _rawWriter = null;

            string root = string.IsNullOrWhiteSpace(_outputDirectory)
                ? Path.Combine(Application.persistentDataPath, DirectoryName)
                : _outputDirectory;
            Directory.CreateDirectory(root);
            File.WriteAllText(Path.Combine(root, FailureFileName), reason ?? "Unknown failure.");
            Debug.LogError($"Performance benchmark failed: {reason}");
        }

        private void WriteSummary()
        {
            string path = Path.Combine(_outputDirectory, SummaryFileName);
            using StreamWriter writer = new StreamWriter(path, false, new UTF8Encoding(false));
            writer.WriteLine(
                "run_id,scenario,duration_sec,frame_count,avg_fps,median_fps,one_percent_low_fps,min_fps," +
                "avg_frame_ms,p95_frame_ms,p99_frame_ms,max_frame_ms,peak_total_allocated_mb," +
                "peak_total_reserved_mb,avg_gc_alloc_bytes,total_gc_alloc_bytes,gc_collections," +
                "avg_main_thread_ms,avg_render_thread_ms,peak_enemy_count,peak_projectile_count," +
                "peak_gate_count,peak_pool_count");

            for (int index = 0; index < _scenarios.Count; index++)
            {
                writer.WriteLine(_scenarios[index].ToCsv(_request.runId));
            }
        }

        private void WriteMetadata()
        {
            BenchmarkMetadata metadata = new BenchmarkMetadata
            {
                runId = _request.runId,
                sourceCommit = _request.sourceCommit,
                unityVersion = Application.unityVersion,
                applicationVersion = Application.version,
                platform = Application.platform.ToString(),
                developmentBuild = Debug.isDebugBuild,
                autoconnectProfiler = _request.autoconnectProfiler,
                deepProfiling = _request.deepProfiling,
                scriptingBackend = ScriptingBackend(),
                deviceModel = SystemInfo.deviceModel,
                operatingSystem = SystemInfo.operatingSystem,
                processorType = SystemInfo.processorType,
                graphicsDeviceName = SystemInfo.graphicsDeviceName,
                graphicsMemoryMb = SystemInfo.graphicsMemorySize,
                systemMemoryMb = SystemInfo.systemMemorySize,
                screenWidth = Screen.width,
                screenHeight = Screen.height,
                refreshRateHz = (float)Screen.currentResolution.refreshRateRatio.value,
                targetFrameRate = Application.targetFrameRate,
                vSyncCount = QualitySettings.vSyncCount,
                qualityLevel = QualitySettings.names[QualitySettings.GetQualityLevel()],
                noGateBaseline = _request.noGateBaseline,
                invulnerable = _request.invulnerable,
                menuWarmupSeconds = _request.menuWarmupSeconds,
                menuMeasurementSeconds = _request.menuMeasurementSeconds,
                gameplayWarmupSeconds = _request.gameplayWarmupSeconds,
                gameplayDurationSeconds = _request.gameplayDurationSeconds,
                sampleIntervalSeconds = _request.sampleIntervalSeconds,
                frameIntervalSource = "Time.unscaledDeltaTime",
                frameTimingCpuAvailable = _frameTimingCpuObserved,
                frameTimingGpuAvailable = _frameTimingGpuObserved,
                mainThreadRecorderAvailable = _mainThreadRecorder.Valid,
                renderThreadRecorderAvailable = _renderThreadRecorder.Valid,
                gcAllocatedRecorderAvailable = _gcAllocatedRecorder.Valid,
                drawCallsRecorderAvailable = _drawCallsRecorder.Valid,
                batchesRecorderAvailable = _batchesRecorder.Valid,
                setPassRecorderAvailable = _setPassRecorder.Valid,
                utcCompleted = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture)
            };

            File.WriteAllText(
                Path.Combine(_outputDirectory, MetadataFileName),
                JsonUtility.ToJson(metadata, true),
                new UTF8Encoding(false));
        }

        private void StartProfilerRecorders()
        {
            _mainThreadRecorder = TryStartRecorder(ProfilerCategory.Internal, "Main Thread");
            _renderThreadRecorder = TryStartRecorder(ProfilerCategory.Internal, "Render Thread");
            _gcAllocatedRecorder = TryStartRecorder(ProfilerCategory.Memory, "GC Allocated In Frame");
            _drawCallsRecorder = TryStartRecorder(ProfilerCategory.Render, "Draw Calls Count");
            _batchesRecorder = TryStartRecorder(ProfilerCategory.Render, "Batches Count");
            _setPassRecorder = TryStartRecorder(ProfilerCategory.Render, "SetPass Calls Count");
        }

        private static ProfilerRecorder TryStartRecorder(ProfilerCategory category, string counterName)
        {
            try
            {
                ProfilerRecorder recorder = ProfilerRecorder.StartNew(category, counterName, 1);
                if (recorder.Valid)
                {
                    return recorder;
                }

                recorder.Dispose();
            }
            catch (Exception)
            {
                // Counter availability differs by platform and build configuration.
            }

            return default;
        }

        private void DisposeProfilerRecorders()
        {
            DisposeRecorder(ref _mainThreadRecorder);
            DisposeRecorder(ref _renderThreadRecorder);
            DisposeRecorder(ref _gcAllocatedRecorder);
            DisposeRecorder(ref _drawCallsRecorder);
            DisposeRecorder(ref _batchesRecorder);
            DisposeRecorder(ref _setPassRecorder);
        }

        private static void DisposeRecorder(ref ProfilerRecorder recorder)
        {
            if (recorder.Valid)
            {
                recorder.Dispose();
            }

            recorder = default;
        }

        private string GetPressurePhase()
        {
            if (_enemySpawner == null || !IsGameplayState())
            {
                return "N/A";
            }

            RunPressureConfig config = _enemySpawner.PressureConfig;
            IReadOnlyList<RunPressureNode> nodes = config != null ? config.Nodes : null;
            if (nodes == null || nodes.Count == 0)
            {
                return "default-pressure";
            }

            int nodeIndex = 0;
            for (int index = 1; index < nodes.Count; index++)
            {
                if (_gameplayElapsedSeconds < nodes[index].TimeSeconds)
                {
                    break;
                }

                nodeIndex = index;
            }

            if (nodeIndex == _lastPressureNodeIndex)
            {
                return _lastPressureLabel;
            }

            _lastPressureNodeIndex = nodeIndex;
            string end = nodeIndex + 1 < nodes.Count
                ? F(nodes[nodeIndex + 1].TimeSeconds)
                : "plus";
            _lastPressureLabel = $"node_{F(nodes[nodeIndex].TimeSeconds)}_{end}";
            return _lastPressureLabel;
        }

        private bool IsGameplayState()
        {
            return _state == CaptureState.GameplayWarmup || _state == CaptureState.GameplayMeasurement;
        }

        private void ResetWindow()
        {
            _windowFrameMilliseconds.Clear();
            _windowCpuFrameTotal = 0d;
            _windowCpuFrameCount = 0;
            _windowGpuFrameTotal = 0d;
            _windowGpuFrameCount = 0;
            _windowMainThreadTotal = 0d;
            _windowMainThreadCount = 0;
            _windowRenderThreadTotal = 0d;
            _windowRenderThreadCount = 0;
            _windowGcAllocatedBytes = 0L;
            _windowGcAllocatedFrameCount = 0;
        }

        private void DeleteMarkerFiles()
        {
            DeleteIfExists(Path.Combine(_outputDirectory, CompletionFileName));
            DeleteIfExists(Path.Combine(_outputDirectory, FailureFileName));
        }

        private static void DeleteIfExists(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        private static int GetGcCollectionCount()
        {
            return GC.CollectionCount(0) + GC.CollectionCount(1) + GC.CollectionCount(2);
        }

        private static long RecorderValue(ProfilerRecorder recorder)
        {
            return recorder.Valid ? recorder.LastValue : -1L;
        }

        private static string RecorderValueOrUnavailable(ProfilerRecorder recorder)
        {
            return recorder.Valid
                ? Math.Max(0L, recorder.LastValue).ToString(CultureInfo.InvariantCulture)
                : "N/A";
        }

        private static double NanosecondsToMilliseconds(long nanoseconds)
        {
            return nanoseconds >= 0 ? nanoseconds / 1_000_000d : 0d;
        }

        private static void AccumulateWindowCounter(
            bool available,
            double value,
            ref double total,
            ref int count)
        {
            if (!available || value < 0d)
            {
                return;
            }

            total += value;
            count++;
        }

        private static string AverageOrUnavailable(double total, int count)
        {
            return count > 0 ? F(total / count) : "N/A";
        }

        private static string BytesToMegabytes(long bytes)
        {
            return F(Math.Max(0L, bytes) / (1024d * 1024d));
        }

        private static string F(double value)
        {
            return double.IsNaN(value) || double.IsInfinity(value)
                ? "N/A"
                : value.ToString("0.###", CultureInfo.InvariantCulture);
        }

        private static string Escape(string value)
        {
            string safe = value ?? string.Empty;
            if (!safe.Contains(",") && !safe.Contains("\"") && !safe.Contains("\n"))
            {
                return safe;
            }

            return $"\"{safe.Replace("\"", "\"\"")}\"";
        }

        private static string ScriptingBackend()
        {
#if ENABLE_IL2CPP
            return "IL2CPP";
#else
            return "Mono";
#endif
        }

        [Serializable]
        private sealed class BenchmarkMetadata
        {
            public string runId;
            public string sourceCommit;
            public string unityVersion;
            public string applicationVersion;
            public string platform;
            public bool developmentBuild;
            public bool autoconnectProfiler;
            public bool deepProfiling;
            public string scriptingBackend;
            public string deviceModel;
            public string operatingSystem;
            public string processorType;
            public string graphicsDeviceName;
            public int graphicsMemoryMb;
            public int systemMemoryMb;
            public int screenWidth;
            public int screenHeight;
            public float refreshRateHz;
            public int targetFrameRate;
            public int vSyncCount;
            public string qualityLevel;
            public bool noGateBaseline;
            public bool invulnerable;
            public float menuWarmupSeconds;
            public float menuMeasurementSeconds;
            public float gameplayWarmupSeconds;
            public float gameplayDurationSeconds;
            public float sampleIntervalSeconds;
            public string frameIntervalSource;
            public bool frameTimingCpuAvailable;
            public bool frameTimingGpuAvailable;
            public bool mainThreadRecorderAvailable;
            public bool renderThreadRecorderAvailable;
            public bool gcAllocatedRecorderAvailable;
            public bool drawCallsRecorderAvailable;
            public bool batchesRecorderAvailable;
            public bool setPassRecorderAvailable;
            public string utcCompleted;
        }

        private sealed class ScenarioAccumulator
        {
            private readonly List<float> _frameMilliseconds = new List<float>(4096);
            private long _totalGcAllocatedBytes;
            private int _gcAllocatedFrameCount;
            private double _mainThreadTotalMilliseconds;
            private int _mainThreadFrameCount;
            private double _renderThreadTotalMilliseconds;
            private int _renderThreadFrameCount;
            private long _peakTotalAllocatedBytes;
            private long _peakTotalReservedBytes;
            private int _peakEnemyCount;
            private int _peakProjectileCount;
            private int _peakGateCount;
            private int _peakPoolCount;
            private int _firstGcCollectionCount = -1;
            private int _lastGcCollectionCount;

            public string Name { get; }
            public float StartSeconds { get; }
            public float EndSeconds { get; }

            public ScenarioAccumulator(string name, float startSeconds, float endSeconds)
            {
                Name = name;
                StartSeconds = startSeconds;
                EndSeconds = Math.Max(startSeconds, endSeconds);
            }

            public void RecordFrame(
                float frameMilliseconds,
                long gcAllocatedBytes,
                bool gcAvailable,
                double mainThreadMilliseconds,
                bool mainThreadAvailable,
                double renderThreadMilliseconds,
                bool renderThreadAvailable)
            {
                _frameMilliseconds.Add(frameMilliseconds);

                if (gcAvailable && gcAllocatedBytes >= 0)
                {
                    _totalGcAllocatedBytes += gcAllocatedBytes;
                    _gcAllocatedFrameCount++;
                }

                if (mainThreadAvailable)
                {
                    _mainThreadTotalMilliseconds += mainThreadMilliseconds;
                    _mainThreadFrameCount++;
                }

                if (renderThreadAvailable)
                {
                    _renderThreadTotalMilliseconds += renderThreadMilliseconds;
                    _renderThreadFrameCount++;
                }
            }

            public void RecordSample(
                long totalAllocatedBytes,
                long totalReservedBytes,
                int enemyCount,
                int projectileCount,
                int gateCount,
                int poolCount,
                int gcCollectionCount)
            {
                _peakTotalAllocatedBytes = Math.Max(_peakTotalAllocatedBytes, totalAllocatedBytes);
                _peakTotalReservedBytes = Math.Max(_peakTotalReservedBytes, totalReservedBytes);
                _peakEnemyCount = Math.Max(_peakEnemyCount, enemyCount);
                _peakProjectileCount = Math.Max(_peakProjectileCount, projectileCount);
                _peakGateCount = Math.Max(_peakGateCount, gateCount);
                _peakPoolCount = Math.Max(_peakPoolCount, poolCount);
                if (_firstGcCollectionCount < 0)
                {
                    _firstGcCollectionCount = gcCollectionCount;
                }

                _lastGcCollectionCount = gcCollectionCount;
            }

            public string ToCsv(string runId)
            {
                if (_frameMilliseconds.Count == 0)
                {
                    return string.Join(",", new[]
                    {
                        Escape(runId), Escape(Name), "0", "0",
                        "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A",
                        "N/A", "N/A", "N/A", "N/A", "N/A", "N/A", "N/A",
                        "0", "0", "0", "0"
                    });
                }

                PerformanceFrameStatistics statistics = PerformanceStatistics.Calculate(_frameMilliseconds);
                double durationSeconds = 0d;
                for (int index = 0; index < _frameMilliseconds.Count; index++)
                {
                    durationSeconds += _frameMilliseconds[index] / 1000d;
                }

                int gcCollections = _firstGcCollectionCount >= 0
                    ? Math.Max(0, _lastGcCollectionCount - _firstGcCollectionCount)
                    : 0;

                return string.Join(",", new[]
                {
                    Escape(runId),
                    Escape(Name),
                    F(durationSeconds),
                    statistics.FrameCount.ToString(CultureInfo.InvariantCulture),
                    F(statistics.AverageFps),
                    F(statistics.MedianFps),
                    F(statistics.OnePercentLowFps),
                    F(statistics.MinimumFps),
                    F(statistics.AverageFrameMilliseconds),
                    F(statistics.P95FrameMilliseconds),
                    F(statistics.P99FrameMilliseconds),
                    F(statistics.MaximumFrameMilliseconds),
                    BytesToMegabytes(_peakTotalAllocatedBytes),
                    BytesToMegabytes(_peakTotalReservedBytes),
                    _gcAllocatedFrameCount > 0 ? F(_totalGcAllocatedBytes / (double)_gcAllocatedFrameCount) : "N/A",
                    _gcAllocatedFrameCount > 0 ? _totalGcAllocatedBytes.ToString(CultureInfo.InvariantCulture) : "N/A",
                    gcCollections.ToString(CultureInfo.InvariantCulture),
                    _mainThreadFrameCount > 0 ? F(_mainThreadTotalMilliseconds / _mainThreadFrameCount) : "N/A",
                    _renderThreadFrameCount > 0 ? F(_renderThreadTotalMilliseconds / _renderThreadFrameCount) : "N/A",
                    _peakEnemyCount.ToString(CultureInfo.InvariantCulture),
                    _peakProjectileCount.ToString(CultureInfo.InvariantCulture),
                    _peakGateCount.ToString(CultureInfo.InvariantCulture),
                    _peakPoolCount.ToString(CultureInfo.InvariantCulture)
                });
            }
        }
    }
}
#endif
