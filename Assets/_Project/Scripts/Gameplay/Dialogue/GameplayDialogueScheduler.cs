using System;

namespace _Project.Scripts.Gameplay.Dialogue
{
    public enum GameplayDialogueTriggerKind
    {
        Opening,
        Periodic
    }

    public sealed class GameplayDialogueScheduler
    {
        private readonly float _periodicIntervalSeconds;
        private bool _isRunActive;
        private bool _isSuppressed;
        private bool _openingTriggered;
        private float _elapsedPlayingSeconds;
        private float _nextPeriodicTrigger;

        public GameplayDialogueScheduler(float periodicIntervalSeconds = 60f)
        {
            _periodicIntervalSeconds = Math.Max(0.01f, periodicIntervalSeconds);
            Reset();
        }

        public event Action<GameplayDialogueTriggerKind> Triggered;

        public bool IsRunActive => _isRunActive;
        public bool IsSuppressed => _isSuppressed;
        public bool OpeningTriggered => _openingTriggered;
        public float ElapsedPlayingSeconds => _elapsedPlayingSeconds;
        public float NextPeriodicTrigger => _nextPeriodicTrigger;

        public void BeginNormalRun()
        {
            Reset();
            _isRunActive = true;
            TriggerOpeningIfNeeded();
        }

        public void EndRun()
        {
            Reset();
        }

        public void Suspend()
        {
            _isSuppressed = true;
        }

        public void Resume()
        {
            _isSuppressed = false;
        }

        public void Tick(float deltaTime)
        {
            if (!_isRunActive || _isSuppressed || deltaTime <= 0f)
            {
                return;
            }

            _elapsedPlayingSeconds += deltaTime;
            while (_elapsedPlayingSeconds + 0.0001f >= _nextPeriodicTrigger)
            {
                Triggered?.Invoke(GameplayDialogueTriggerKind.Periodic);
                _nextPeriodicTrigger += _periodicIntervalSeconds;
            }
        }

        public void ResetTimer()
        {
            _elapsedPlayingSeconds = 0f;
            _nextPeriodicTrigger = _periodicIntervalSeconds;
        }

        private void TriggerOpeningIfNeeded()
        {
            if (_openingTriggered || _isSuppressed)
            {
                return;
            }

            _openingTriggered = true;
            Triggered?.Invoke(GameplayDialogueTriggerKind.Opening);
        }

        private void Reset()
        {
            _isRunActive = false;
            _isSuppressed = false;
            _openingTriggered = false;
            ResetTimer();
        }
    }
}
