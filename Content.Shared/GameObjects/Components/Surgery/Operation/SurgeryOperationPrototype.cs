using System.Collections.Immutable;
using Content.Shared.GameObjects.Components.Surgery.Operation.Effect;
using Content.Shared.GameObjects.Components.Surgery.Operation.Step;
using Content.Shared.GameObjects.Components.Surgery.Operation.Step.Serializers;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Surgery.Operation
{
    [Prototype("surgeryOperation")]
    public class SurgeryOperationPrototype : IPrototype
    {
        [field: DataField("id", required: true)]
        public string ID { get; } = string.Empty;

        [field: DataField("name")]
        public string Name { get; } = string.Empty;

        [field: DataField("description")]
        public string Description { get; } = string.Empty;

        [field: DataField("steps", customTypeSerializer: typeof(OperationStepImmutableListSerializer))]
        public ImmutableList<OperationStep> Steps { get; } = ImmutableList<OperationStep>.Empty;

        [field: DataField("effect", serverOnly: true)]
        public IOperationEffect? Effect { get; }

        [field: DataField("hidden")]
        public bool Hidden { get; }
    }
}
