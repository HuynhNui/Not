using System;

namespace _Project.Scripts.Systems.UISystem
{
    /// <summary>
    /// Deterministic inactivity state machine. The owning UI supplies unscaled time and presentation callbacks.
    /// </summary>
    public sealed class UIInactivityTimeoutController
    {
        private readonly float _warningAfterSeconds;
        private readonly float _graceDurationSeconds;

        private float _idleElapsedSeconds;
        private float _graceRemainingSeconds;
        private int _lastReportedGraceSeconds = -1;
        private bool _warningVisible;
        private bool _timeoutRaised;

        public UIInactivityTimeoutController(float warningAfterSeconds, float graceDurationSeconds)
        {
            _warningAfterSeconds = Math.Max(0f, warningAfterSeconds);
            _graceDurationSeconds = Math.Max(0f, graceDurationSeconds);
            _graceRemainingSeconds = _graceDurationSeconds;
        }

        public event Action<bool> WarningVisibilityChanged;
        public event Action<int> GraceCountdownChanged;
        public event Action TimedOut;

        public bool IsMonitoring { get; private set; }
        public bool IsWarningVisible => _warningVisible;
        public float IdleElapsedSeconds => _idleElapsedSeconds;
        public float GraceRemainingSeconds => _graceRemainingSeconds;

        public void StartMonitoring()
        {
            bool warningWasVisible = _warningVisible;
            IsMonitoring = true;
            _timeoutRaised = false;
            _idleElapsedSeconds = 0f;
            _graceRemainingSeconds = _graceDurationSeconds;
            _lastReportedGraceSeconds = -1;
            _warningVisible = false;

            if (warningWasVisible)
            {
                WarningVisibilityChanged?.Invoke(false);
            }
        }

        public void StopMonitoring()
        {
            bool warningWasVisible = _warningVisible;
            IsMonitoring = false;
            _timeoutRaised = false;
            _idleElapsedSeconds = 0f;
            _graceRemainingSeconds = _graceDurationSeconds;
            _lastReportedGraceSeconds = -1;
            _warningVisible = false;

            if (warningWasVisible)
            {
                WarningVisibilityChanged?.Invoke(false);
            }
        }

        public void RegisterActivity()
        {
            if (!IsMonitoring)
            {
                return;
            }

            bool warningWasVisible = _warningVisible;
            _idleElapsedSeconds = 0f;
            _graceRemainingSeconds = _graceDurationSeconds;
            _lastReportedGraceSeconds = -1;
            _warningVisible = false;

            if (warningWasVisible)
            {
                WarningVisibilityChanged?.Invoke(false);
            }
        }

        public void Advance(float unscaledDeltaTime)
        {
            if (!IsMonitoring || _timeoutRaised)
            {
                return;
            }

            float safeDeltaTime = Math.Max(0f, unscaledDeltaTime);
            if (!_warningVisible)
            {
                _idleElapsedSeconds += safeDeltaTime;
                if (_idleElapsedSeconds < _warningAfterSeconds)
                {
                    return;
                }

                float graceOverflow = _idleElapsedSeconds - _warningAfterSeconds;
                _warningVisible = true;
                _graceRemainingSeconds = _graceDurationSeconds;
                _lastReportedGraceSeconds = -1;
                WarningVisibilityChanged?.Invoke(true);
                ReportGraceCountdown();

                if (graceOverflow > 0f)
                {
                    ConsumeGrace(graceOverflow);
                }

                return;
            }

            ConsumeGrace(safeDeltaTime);
        }

        private void ConsumeGrace(float elapsedSeconds)
        {
            _graceRemainingSeconds = Math.Max(0f, _graceRemainingSeconds - elapsedSeconds);
            ReportGraceCountdown();

            if (_graceRemainingSeconds > 0f)
            {
                return;
            }

            RaiseTimeoutOnce();
        }

        private void ReportGraceCountdown()
        {
            int wholeSeconds = Math.Max(0, (int)Math.Ceiling(_graceRemainingSeconds));
            if (wholeSeconds == _lastReportedGraceSeconds)
            {
                return;
            }

            _lastReportedGraceSeconds = wholeSeconds;
            GraceCountdownChanged?.Invoke(wholeSeconds);
        }

        private void RaiseTimeoutOnce()
        {
            if (_timeoutRaised)
            {
                return;
            }

            _timeoutRaised = true;
            IsMonitoring = false;
            _warningVisible = false;
            WarningVisibilityChanged?.Invoke(false);
            TimedOut?.Invoke();
        }
    }
}
