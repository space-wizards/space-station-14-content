#nullable enable
using System.Collections.Immutable;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Body.Surgery.Operations
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

        [field: DataField("steps")]
        public ImmutableList<string> Steps { get; } = ImmutableList<string>.Empty;
    }
}
