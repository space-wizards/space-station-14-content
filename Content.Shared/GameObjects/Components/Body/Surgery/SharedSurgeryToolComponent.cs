#nullable enable
using Content.Shared.GameObjects.Components.Body.Surgery.Behaviors;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.GameObjects.Components.Body.Surgery
{
    public abstract class SharedSurgeryToolComponent : Component
    {
        public override string Name => "SurgeryTool";

        [DataField("behavior")]
        public ISurgeryBehavior? Behavior { get; [UsedImplicitly] private set; }

        [DataField("delay")]
        public float Delay { get; [UsedImplicitly] private set; }
    }
}
