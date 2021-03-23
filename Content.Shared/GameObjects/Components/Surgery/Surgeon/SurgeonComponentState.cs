using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Surgery.Surgeon
{
    [Serializable, NetSerializable]
    public class SurgeonComponentState : ComponentState
    {
        public SurgeonComponentState(EntityUid? target) : base(ContentNetIDs.SURGEON)
        {
            Target = target;
        }

        public EntityUid? Target { get; }
    }
}
