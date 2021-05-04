using Content.Shared.GameObjects.Components.Surgery.Operation.Step.Conditional;
using Content.Shared.GameObjects.Components.Surgery.Target;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Surgery.Operation.Step
{
    [DataDefinition]
    public class OperationStep
    {
        [field: DataField("id", required: true)]
        public string Id { get; init; } = string.Empty;

        [field: DataField("conditional")]
        public IOperationStepConditional? Conditional { get; init; }

        public SurgeryStepPrototype Step(IPrototypeManager prototypeManager)
        {
            return prototypeManager.Index<SurgeryStepPrototype>(Id);
        }

        public bool Necessary(SurgeryTargetComponent target)
        {
            return Conditional?.Necessary(target) ?? true;
        }
    }
}
