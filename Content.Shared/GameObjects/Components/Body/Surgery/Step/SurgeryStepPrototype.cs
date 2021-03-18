using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Body.Surgery.Step
{
    [Prototype("surgeryStep")]
    public class SurgeryStepPrototype : IPrototype
    {
        [DataField("id", required: true)]
        public string ID { get; [UsedImplicitly] private set; } = string.Empty;
    }
}
