using System.Collections;
using UnityEngine;
namespace Enemies.BossStates {
    /// <summary>
    /// The boss is taunting and is vulnerable to damage.
    /// </summary>
    public class BossTauntingState : BossState {
        private Coroutine _tauntCoroutine;
        private Coroutine _vulnerabilityFlashCoroutine;
        public BossTauntingState(BossContext bossContext) : base(bossContext) {}

        public override void Enter() {
            if (BossContext.EnableDebugLogs) Debug.LogWarning($"[TauntingState] Entered. Boss is VULNERABLE for {BossContext.TauntDuration}s.");
            BossContext.FacePlayer();
            animator.SetTrigger(BossContext.Taunt);
            BossContext.Audio.PlayTaunt();
            _tauntCoroutine = BossContext.StartCoroutine(TauntRoutine());
            BossContext.VFX.StartVulnerableLoop();
        }

        public override void Exit() {
            if (BossContext.EnableDebugLogs) Debug.Log($"[TauntingState] Exited.");
            if (_tauntCoroutine != null) {
                BossContext.StopCoroutine(_tauntCoroutine);
            }
            BossContext.VFX.StopAllFlashes();
            animator.ResetTrigger(BossContext.Taunt);
        }

        /// <summary>
        /// Failsafe timer.
        /// </summary>
        private IEnumerator TauntRoutine() {
            yield return new WaitForSeconds(BossContext.TauntDuration);
            if (BossContext.EnableDebugLogs) Debug.Log($"[TauntingState] Taunt duration expired. Player did not hit. Switching to ChasingState.");
            BossContext.ChangeState(BossContext.ChasingState);
        }

        /// <summary>
        /// This state has special logic for TakeDamage.
        /// </summary>
        public override void OnTakeDamage() {
            if (BossContext.EnableDebugLogs) Debug.LogWarning($"[TauntingState] OnTakeDamage: SUCCESS! Boss was hit while vulnerable.");
            BossContext.Audio.PlayVulnerableHit();
            BossContext.CurrentHealth--;
            if (BossContext.EnableDebugLogs) Debug.Log($"[TauntingState] Health remaining: {BossContext.CurrentHealth}");
            BossContext.StopAllCoroutines();
            BossContext.VFX.StopAllFlashes();
            if (BossContext.CurrentHealth <= 0) {
                if (BossContext.EnableDebugLogs) Debug.Log($"[TauntingState] Boss defeated. Switching to DefeatedState.");
                BossContext.ChangeState(BossContext.DefeatedState);
            } else {
                if (BossContext.EnableDebugLogs) Debug.Log($"[TauntingState] Boss hit. Switching to StunnedState.");
                BossContext.ChangeState(BossContext.StunnedState);
            }
        }
    }
}