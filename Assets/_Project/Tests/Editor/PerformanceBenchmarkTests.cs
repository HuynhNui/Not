using System.Collections.Generic;
using _Project.Scripts.Systems.Performance;
using NUnit.Framework;

namespace TrueGate.EditorTests
{
    public sealed class PerformanceBenchmarkTests
    {
        [Test]
        public void Statistics_ConstantFrameTimeProducesExpectedFps()
        {
            List<float> frames = new List<float>();
            for (int index = 0; index < 120; index++)
            {
                frames.Add(1000f / 60f);
            }

            PerformanceFrameStatistics statistics = PerformanceStatistics.Calculate(frames);

            Assert.That(statistics.FrameCount, Is.EqualTo(120));
            Assert.That(statistics.AverageFps, Is.EqualTo(60d).Within(0.01d));
            Assert.That(statistics.MedianFps, Is.EqualTo(60d).Within(0.01d));
            Assert.That(statistics.OnePercentLowFps, Is.EqualTo(60d).Within(0.01d));
            Assert.That(statistics.MinimumFps, Is.EqualTo(60d).Within(0.01d));
        }

        [Test]
        public void Statistics_OnePercentLowUsesSlowestFrameTimeMean()
        {
            List<float> frames = new List<float>();
            for (int index = 0; index < 99; index++)
            {
                frames.Add(1000f / 60f);
            }

            frames.Add(100f);
            PerformanceFrameStatistics statistics = PerformanceStatistics.Calculate(frames);

            Assert.That(statistics.OnePercentLowFps, Is.EqualTo(10d).Within(0.001d));
            Assert.That(statistics.MinimumFps, Is.EqualTo(10d).Within(0.001d));
            Assert.That(statistics.MaximumFrameMilliseconds, Is.EqualTo(100d).Within(0.001d));
        }

        [Test]
        public void RequestValidationClampsDurationsAndSanitizesRunId()
        {
            PerformanceBenchmarkRequest request = new PerformanceBenchmarkRequest
            {
                runId = " run/01 ",
                gameplayWarmupSeconds = 10f,
                gameplayDurationSeconds = 5f,
                sampleIntervalSeconds = 0f,
                startingProjectileCount = 0,
                startingSquadSize = 0
            };

            request.Validate();

            Assert.That(request.runId, Is.EqualTo("run_01"));
            Assert.That(request.gameplayDurationSeconds, Is.EqualTo(11f));
            Assert.That(request.sampleIntervalSeconds, Is.EqualTo(0.25f));
            Assert.That(request.startingProjectileCount, Is.EqualTo(1));
            Assert.That(request.startingSquadSize, Is.EqualTo(1));
        }
    }
}
