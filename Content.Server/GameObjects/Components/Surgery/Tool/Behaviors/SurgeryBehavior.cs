using Content.Shared.GameObjects.Components.Surgery.Surgeon;
using Content.Shared.GameObjects.Components.Surgery.Target;

namespace Content.Server.GameObjects.Components.Surgery.Tool.Behaviors
{
    public abstract class SurgeryBehavior : ISurgeryBehavior
    {
        public abstract bool CanPerform(SurgeonComponent surgeon, SurgeryTargetComponent target);

        public abstract bool Perform(SurgeonComponent surgeon, SurgeryTargetComponent target);
    }
}
