using UnityEngine;
namespace Enemies.BossStates
{
    /// <summary>
    /// The boss is actively chasing the player.
    /// </summary>
    public class BossChasingState : BossState
    {
        public BossChasingState(BossContext bossContext) : base(bossContext) {}

        public override void Enter()
        {
            if (BossContext.EnableDebugLogs) Debug.Log($"[ChasingState] Entered. Started chasing player.");
            navAgent.isStopped = false;
            animator.SetBool(BossContext.IsWalking, true);
        }

        public override void Tick()
        {
            if (!BossContext.PlayerTarget) return;

            navAgent.SetDestination(BossContext.PlayerTarget.position);

            // Check if we are in range to attack
            float distance = Vector3.Distance(BossContext.transform.position, BossContext.PlayerTarget.position);
            if (distance <= BossContext.AttackRange)
            {
                if (BossContext.EnableDebugLogs) Debug.Log($"[ChasingState] Player in attack range (Distance: {distance}). Switching to AttackingState.");
                BossContext.ChangeState(BossContext.AttackingState);
            }
        }

        public override void Exit()
        {
            if (BossContext.EnableDebugLogs) Debug.Log($"[ChasingState] Exited. Stopped NavAgent.");
            navAgent.isStopped = true;
            animator.SetBool(BossContext.IsWalking, false);
        }
    }
}
