using UnityEngine;
namespace Enemies.BossStates
{
    /// <summary>
    /// O chefe está se levantando após ser atordoado.
    /// Toca a animação "GetUp" e espera ela terminar.
    /// </summary>
    public class BossRecoveringState : BossState
    {
        public BossRecoveringState(BossContext bossContext) : base(bossContext) {}

        public override void Enter() {
            if (BossContext.EnableDebugLogs) Debug.Log($"[RecoveringState] Entered. Playing 'GetUp' animation.");
            animator.SetTrigger(BossContext.GetUp);
            BossContext.Audio.PlayGetUp();
        }
        
        public override void Exit() {
            if (BossContext.EnableDebugLogs) Debug.Log($"[RecoveringState] Exited.");
            BossContext.VFX.HideStunEffect();
            BossContext.Audio.StopStatusLoop();
        }
        
        /// <summary>
        /// Chamado pelo Animation Event "OnAttackAnimationFinished"
        /// no FINAL do clipe de animação "GetUp".
        /// </summary>
        public override void OnAnimationFinished() {
            if (BossContext.EnableDebugLogs) Debug.Log($"[RecoveringState] 'GetUp' animation finished. Switching to ChasingState.");
            BossContext.ChangeState(BossContext.ChasingState);
        }
    }
}
