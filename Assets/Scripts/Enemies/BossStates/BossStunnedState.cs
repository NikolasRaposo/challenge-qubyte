using UnityEngine;

namespace Enemies.BossStates
{
    /// <summary>
    /// The boss has been hit while taunting and is now stunned.
    /// Waits for the animation to finish via an event.
    /// </summary>
    public class BossStunnedState : BossState
    {
        public BossStunnedState(BossContext bossContext) : base(bossContext) {}

        public override void Enter() {
            if (BossContext.EnableDebugLogs) Debug.Log($"[StunnedState] Entered. Playing Stun animation and waiting for it to finish.");
            animator.SetTrigger(BossContext.Stunned);
            BossContext.VFX.ShowStunEffect();
            BossContext.Audio.PlayStunLoop();
        }

        public override void Exit() {
            if (BossContext.EnableDebugLogs) Debug.Log($"[StunnedState] Exited.");
        }
        
        /// <summary>
        /// This is called by the Animation Event "OnAttackAnimationFinished"
        /// at the end of the "Stunned" (caindo) animation clip.
        /// </summary>
        public override void OnAnimationFinished() {
            if (BossContext.EnableDebugLogs) Debug.Log($"[StunnedState] Stun ANIMATION finished. Resetting attack count. Switching to RecoveringState.");
            BossContext.CurrentAttackCount = 0;
            BossContext.ChangeState(BossContext.RecoveringState);
        }
    }
}