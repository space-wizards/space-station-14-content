using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Surgery.Surgeon
{
    [Serializable, NetSerializable]
    public class SurgeonComponentState : ComponentState
    {
        public SurgeonComponentState(EntityUid? target, string? slot, EntityUid? mechanism) : base(ContentNetIDs.SURGEON)
        {
            Target = target;
            Slot = slot;
            Mechanism = mechanism;
        }

        public EntityUid? Target { get; }

        public string? Slot { get; }

        public EntityUid? Mechanism { get; }
    }
}
