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
            BossContext.StopAllCoroutines();
            navAgent.isStopped = true;
            BossContext.Audio.PlayDefeated();
            animator.SetTrigger(BossContext.Defeated);
            BossContext.InvokeOnBossDefeated();
        }
    }
}
