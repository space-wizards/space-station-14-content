using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using YamlDotNet.RepresentationModel;

namespace Content.Shared.GameObjects.Components.Body.Surgery.Step
{
    [Prototype("surgeryStep")]
    public class SurgeryStepPrototype : IPrototype, IIndexedPrototype, IExposeData
    {
        public string ID { get; [UsedImplicitly] private set; } = string.Empty;

        public void LoadFrom(YamlMappingNode mapping)
        {
            var reader = YamlObjectSerializer.NewReader(mapping);
            ((IExposeData) this).ExposeData(reader);
        }

        void IExposeData.ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.ID, "id", string.Empty);
        }
    }
}
