#nullable enable
using System.Collections.Immutable;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.GameObjects.Components.Body.Surgery.Operations
{
    [Prototype("surgeryOperation")]
    public class SurgeryOperationPrototype : IPrototype, IIndexedPrototype, IExposeData
    {
        public string ID { get; [UsedImplicitly] private set; } = string.Empty;

        public string Name { get; [UsedImplicitly] private set; } = string.Empty;

        public string Description { get; [UsedImplicitly] private set; } = string.Empty;

        public ImmutableList<string> Steps { get; [UsedImplicitly] private set; } = ImmutableList<string>.Empty;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var reader = YamlObjectSerializer.NewReader(mapping);
            ((IExposeData) this).ExposeData(reader);
        }

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.ID, "id", string.Empty);
            serializer.DataField(this, x => x.Name, "name", string.Empty);
            serializer.DataField(this, x => x.Description, "description", string.Empty);
            serializer.DataField(this, x => x.Steps, "steps", ImmutableList<string>.Empty);
        }
    }
}
