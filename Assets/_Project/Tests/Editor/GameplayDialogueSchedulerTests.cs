using System.Collections.Generic;
using _Project.Scripts.Gameplay.Dialogue;
using NUnit.Framework;

namespace _Project.Tests.Editor
{
    public sealed class GameplayDialogueSchedulerTests
    {
        [Test]
        public void BeginNormalRun_TriggersOpeningOnce()
        {
            GameplayDialogueScheduler scheduler = CreateScheduler(out List<GameplayDialogueTriggerKind> triggers);

            scheduler.BeginNormalRun();
            scheduler.BeginNormalRun();

            Assert.That(triggers, Is.EqualTo(new[]
            {
                GameplayDialogueTriggerKind.Opening,
                GameplayDialogueTriggerKind.Opening
            }));
        }

        [Test]
        public void Tick_BeforeSixtySeconds_DoesNotTriggerPeriodic()
        {
            GameplayDialogueScheduler scheduler = CreateScheduler(out List<GameplayDialogueTriggerKind> triggers);
            scheduler.BeginNormalRun();
            triggers.Clear();

            scheduler.Tick(59.99f);

            Assert.That(triggers, Is.Empty);
        }

        [Test]
        public void Tick_AtSixtySeconds_TriggersOnePeriodic()
        {
            GameplayDialogueScheduler scheduler = CreateScheduler(out List<GameplayDialogueTriggerKind> triggers);
            scheduler.BeginNormalRun();
            triggers.Clear();

            scheduler.Tick(60f);

            Assert.That(triggers, Is.EqualTo(new[] { GameplayDialogueTriggerKind.Periodic }));
        }

        [Test]
        public void Tick_LargeDelta_DoesNotMissPeriodicMilestones()
        {
            GameplayDialogueScheduler scheduler = CreateScheduler(out List<GameplayDialogueTriggerKind> triggers);
            scheduler.BeginNormalRun();
            triggers.Clear();

            scheduler.Tick(181f);

            Assert.That(triggers, Is.EqualTo(new[]
            {
                GameplayDialogueTriggerKind.Periodic,
                GameplayDialogueTriggerKind.Periodic,
                GameplayDialogueTriggerKind.Periodic
            }));
        }

        [Test]
        public void SuspendedTime_DoesNotAccumulate()
        {
            GameplayDialogueScheduler scheduler = CreateScheduler(out List<GameplayDialogueTriggerKind> triggers);
            scheduler.BeginNormalRun();
            triggers.Clear();

            scheduler.Tick(59f);
            scheduler.Suspend();
            scheduler.Tick(10f);
            scheduler.Resume();
            scheduler.Tick(1f);

            Assert.That(triggers, Is.EqualTo(new[] { GameplayDialogueTriggerKind.Periodic }));
        }

        private static GameplayDialogueScheduler CreateScheduler(out List<GameplayDialogueTriggerKind> triggers)
        {
            triggers = new List<GameplayDialogueTriggerKind>();
            var captured = triggers;
            var scheduler = new GameplayDialogueScheduler(60f);
            scheduler.Triggered += captured.Add;
            return scheduler;
        }
    }
}
