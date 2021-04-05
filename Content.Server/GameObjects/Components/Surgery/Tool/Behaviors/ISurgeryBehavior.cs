using Content.Shared.GameObjects.Components.Surgery.Surgeon;
using Content.Shared.GameObjects.Components.Surgery.Target;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Surgery.Tool.Behaviors
{
    [ImplicitDataDefinitionForInheritors]
    public interface ISurgeryBehavior
    {
        bool CanPerform(SurgeonComponent surgeon, SurgeryTargetComponent target);

        void OnBeginPerformDelay(SurgeonComponent surgeon, SurgeryTargetComponent target);

        bool Perform(SurgeonComponent surgeon, SurgeryTargetComponent target);
    }
}
