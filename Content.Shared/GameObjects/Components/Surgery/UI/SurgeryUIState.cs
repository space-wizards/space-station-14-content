using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Surgery.UI
{
    [Serializable, NetSerializable]
    public class SurgeryUIState : BoundUserInterfaceState
    {
        public SurgeryUIState(EntityUid[] entities)
        {
            Entities = entities;
        }

        public EntityUid[] Entities { get; }
    }
}
