using Content.Shared.GameObjects.Components.Surgery.Surgeon;
using Content.Shared.GameObjects.Components.Surgery.Target;

namespace Content.Server.GameObjects.Components.Surgery.Tool.Behaviors
{
    public abstract class SurgeryBehavior : ISurgeryBehavior
    {
        public abstract bool CanPerform(SurgeonComponent surgeon, SurgeryTargetComponent target);

        /// <summary>
        ///     Called when a delay is started to perform this behaviour.
        /// </summary>
        /// <param name="surgeon">The surgeon that will perform this behavior.</param>
        /// <param name="target">The target of the operation.</param>
        public virtual void OnBeginPerformDelay(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
        }

        public abstract bool Perform(SurgeonComponent surgeon, SurgeryTargetComponent target);
    }
}
