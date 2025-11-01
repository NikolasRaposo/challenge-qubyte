using UnityEngine;

namespace Enemies.BossStates
{
    /// <summary>
    /// The boss has been defeated. This is a final state.
    /// </summary>
    public class BossDefeatedState : BossState
    {
        public BossDefeatedState(BossContext bossContext) : base(bossContext) {}

        public override void Enter() {
            if (BossContext.EnableDebugLogs) Debug.LogWarning($"[DefeatedState] Entered. Boss is defeated. Stopping NavAgent and firing event.");
            // Stop everything
            BossContext.StopAllCoroutines();
            navAgent.isStopped = true;

            // Play animation and fire event
            animator.SetTrigger(BossContext.Defeated);
            BossContext.InvokeOnBossDefeated();
        }
    }
}
