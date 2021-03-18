#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Body.Surgery
{
    public abstract class SharedSurgeryToolComponent : Component
    {
        public override string Name => "SurgeryTool";

        [field: DataField("delay")]
        public float Delay { get; }
    }
}
