#nullable enable
using System.Collections.Immutable;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Body.Surgery.Operations
{
    [Prototype("surgeryOperation")]
    public class SurgeryOperationPrototype : IPrototype
    {
        [field: DataField("id", required: true)]
        public string ID { get; [UsedImplicitly] private set; } = string.Empty;

        [field: DataField("name")]
        public string Name { get; [UsedImplicitly] private set; } = string.Empty;

        [field: DataField("description")]
        public string Description { get; [UsedImplicitly] private set; } = string.Empty;

        [field: DataField("steps")]
        public ImmutableList<string> Steps { get; [UsedImplicitly] private set; } = ImmutableList<string>.Empty;
    }
}
