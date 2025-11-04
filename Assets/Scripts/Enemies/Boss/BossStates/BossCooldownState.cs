using System.Collections;
using UnityEngine;

namespace Enemies.BossStates {
    /// <summary>
    /// The boss is waiting for a short duration after an attack.
    /// </summary>
    public class BossCooldownState : BossState {
        private Coroutine _cooldownCoroutine;

        public BossCooldownState(BossContext bossContext) : base(bossContext) {}

        public override void Enter() {
            if (BossContext.EnableDebugLogs) Debug.Log($"[CooldownState] Entered. Waiting for {BossContext.AttackCooldown}s.");
            _cooldownCoroutine = BossContext.StartCoroutine(CooldownRoutine());
        }

        public override void Exit() {
            // Ensure the coroutine is stopped if we are interrupted
            if (_cooldownCoroutine != null)
            {
                if (BossContext.EnableDebugLogs) Debug.Log($"[CooldownState] Exited. Stopping coroutine.");
                BossContext.StopCoroutine(_cooldownCoroutine);
            }
        }

        private IEnumerator CooldownRoutine() {
            yield return new WaitForSeconds(BossContext.AttackCooldown);
            if (BossContext.EnableDebugLogs) Debug.Log($"[CooldownState] Cooldown finished. Switching to ChasingState.");
            // Cooldown finished, go back to chasing
            BossContext.ChangeState(BossContext.ChasingState);
        }
    }
}
