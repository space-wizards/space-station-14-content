using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Surgery
{
    [Serializable, NetSerializable]
    public class SurgeryUIState : BoundUserInterfaceState
    {
        public SurgeryUIState(EntityUid[] parts)
        {
            Parts = parts;
        }

        public EntityUid[] Parts { get; }
    }
}
