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

        public override void Enter()
        {
            if (BossContext.EnableDebugLogs) Debug.Log($"[RecoveringState] Entered. Playing 'GetUp' animation.");
            animator.SetTrigger(BossContext.GetUp);
        }
        
        public override void Exit()
        {
            if (BossContext.EnableDebugLogs) Debug.Log($"[RecoveringState] Exited.");
            
            if (BossContext.stunVFX != null)
            {
                BossContext.stunVFX.SetActive(false);
            }
            if (BossContext.StatusEffectAudioSource != null)
            {
                BossContext.StatusEffectAudioSource.Stop();
                BossContext.StatusEffectAudioSource.loop = false;
            }
        }
        
        /// <summary>
        /// Chamado pelo Animation Event "OnAttackAnimationFinished"
        /// no FINAL do clipe de animação "GetUp".
        /// </summary>
        public override void OnAnimationFinished()
        {
            if (BossContext.EnableDebugLogs) Debug.Log($"[RecoveringState] 'GetUp' animation finished. Switching to ChasingState.");
            BossContext.ChangeState(BossContext.ChasingState);
        }
    }
}
