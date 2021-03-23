using Content.Shared.GameObjects.Components.Surgery.Target;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Surgery.Operation.Effect
{
    [ImplicitDataDefinitionForInheritors]
    public interface IOperationEffect
    {
        void Execute(SurgeryTargetComponent target);
    }
}
