using Content.Shared.GameObjects.Components.Surgery.Target;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Surgery.Operation.Step.Conditional
{
    [ImplicitDataDefinitionForInheritors]
    public interface IOperationStepConditional
    {
        bool Necessary(SurgeryTargetComponent target);
    }
}
