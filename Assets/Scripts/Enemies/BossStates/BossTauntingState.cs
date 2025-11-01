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
            _tauntCoroutine = BossContext.StartCoroutine(TauntRoutine());
            _vulnerabilityFlashCoroutine = BossContext.StartCoroutine(BossContext.VulnerableFlashLoop());
        }

        public override void Exit() {
            if (BossContext.EnableDebugLogs) Debug.Log($"[TauntingState] Exited.");
            if (_tauntCoroutine != null) {
                BossContext.StopCoroutine(_tauntCoroutine);
            }
            if (_vulnerabilityFlashCoroutine != null) {
                BossContext.StopCoroutine(_vulnerabilityFlashCoroutine);
            }
            BossContext.ResetFlashMaterial();
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
            BossContext.CurrentHealth--;
            if (BossContext.EnableDebugLogs) Debug.Log($"[TauntingState] Health remaining: {BossContext.CurrentHealth}");
            BossContext.StopAllCoroutines();
            BossContext.ResetFlashMaterial();
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