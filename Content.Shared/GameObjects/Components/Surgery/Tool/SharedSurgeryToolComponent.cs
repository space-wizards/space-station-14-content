#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Surgery.Tool
{
    public abstract class SharedSurgeryToolComponent : Component
    {
        public override string Name => "SurgeryTool";

        [DataField("delay")]
        public float Delay { get; } = default!;
    }
}
