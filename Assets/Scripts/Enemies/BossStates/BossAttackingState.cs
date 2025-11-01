using UnityEngine;

namespace Enemies.BossStates
{
    /// <summary>
    /// The boss is performing an attack animation.
    /// Logic is triggered on Enter(), and transition happens via Animation Event.
    /// </summary>
    public class BossAttackingState : BossState {
        public BossAttackingState(BossContext bossContext) : base(bossContext) {}

        public override void Enter() {
            BossContext.FacePlayer();
            
            bool isPunch = Random.Range(0, 2) == 0;
            string attackName = isPunch ? "PunchAttack" : "SpinAttack";
            int attackTrigger = isPunch ? BossContext.PunchAttack : BossContext.SpinAttack;
            
            if (BossContext.EnableDebugLogs) Debug.Log($"[AttackingState] Entered. Performing attack: {attackName}");
            
            animator.SetTrigger(attackTrigger);
        }

        /// <summary>
        /// This is called by the Animation Event via the boss.
        /// </summary>
        public override void OnAnimationFinished()
        {
            // If we've attacked enough, go to Taunt
            if (BossContext.CurrentAttackCount >= BossContext.AttacksBeforeTaunt)
            {
                if (BossContext.EnableDebugLogs) Debug.Log($"[AttackingState] Attack finished. Attack count ({BossContext.CurrentAttackCount}) reached limit. Switching to TauntingState.");
                BossContext.ChangeState(BossContext.TauntingState);
            }
            // Otherwise, go to Cooldown
            else
            {
                if (BossContext.EnableDebugLogs) Debug.Log($"[AttackingState] Attack finished. Attack count ({BossContext.CurrentAttackCount}). Switching to CooldownState.");
                BossContext.ChangeState(BossContext.CooldownState);
            }
        }
    }
}
