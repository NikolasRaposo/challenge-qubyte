using UnityEngine;
using UnityEngine.AI;
namespace Enemies
{
    /// <summary>
    /// Abstract base class for all boss states.
    /// Provides shared references and default implementations for state methods.
    /// </summary>
    public abstract class BossState
    {
        // Protected references to the boss's components and context
        protected readonly BossContext BossContext;
        protected readonly NavMeshAgent navAgent;
        protected readonly Animator animator;

        /// <summary>
        /// Constructor to pass in the boss context.
        /// </summary>
        /// <param name="bossContext">The main CapeloboBoss script (the context).</param>
        protected BossState(BossContext bossContext)
        {
            this.BossContext = bossContext;
            this.navAgent = bossContext.NavAgent;
            this.animator = bossContext.Animator;
        }

        /// <summary>
        /// Called once when the state machine transitions *into* this state.
        /// </summary>
        public virtual void Enter() {}

        /// <summary>
        /// Called every frame by the boss's Update() method while this state is active.
        /// </summary>
        public virtual void Tick() {}

        /// <summary>
        /// Called once when the state machine transitions *out of* this state.
        /// </summary>
        public virtual void Exit() {}

        /// <summary>
        /// Called when the boss's TakeDamage() method is triggered.
        /// Default behavior is to play an invulnerable flash.
        /// </summary>
        public virtual void OnTakeDamage()
        {
            if (BossContext.EnableDebugLogs) Debug.Log($"[State:{GetType().Name}] OnTakeDamage (Default: Boss is invulnerable. Flashing.)");
            
            // Default behavior: flash if not already flashing
            if (!BossContext.IsFlashing)
            {
                BossContext.StartCoroutine(BossContext.InvulnerableFlashRoutine());
            }
        }

        /// <summary>
        /// Called when an animation event triggers OnAttackAnimationFinished().
        /// </summary>
        public virtual void OnAnimationFinished() {}
    }
}