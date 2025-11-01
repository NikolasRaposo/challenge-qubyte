using UnityEngine;

namespace Enemies.BossStates {
    /// <summary>
    /// The boss is doing nothing, waiting for the battle to start.
    /// </summary>
    public class BossIdleState : BossState {
        public BossIdleState(BossContext bossContext) : base(bossContext) {}
        public override void Enter() {
            if (BossContext.EnableDebugLogs) Debug.Log($"[IdleState] Entered. Waiting for StartBattle() call.");
            navAgent.isStopped = true;
        }
    }
}
