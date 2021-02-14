#nullable enable
using Content.Shared.GameObjects.Components.Body.Surgery.Behaviors;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Surgery
{
    public abstract class SharedSurgeryToolComponent : Component
    {
        public override string Name => "SurgeryTool";

        public ISurgeryBehavior? Behavior { get; [UsedImplicitly] private set; }

        public float Delay { get; [UsedImplicitly] private set; }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Behavior, "behavior", null);
            serializer.DataField(this, x => x.Delay, "delay", 0);
        }
    }
}
