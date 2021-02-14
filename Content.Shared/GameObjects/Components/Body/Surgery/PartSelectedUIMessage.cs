using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Surgery
{
    [Serializable, NetSerializable]
    public class PartSelectedUIMessage : BoundUserInterfaceMessage
    {
        public PartSelectedUIMessage(EntityUid id)
        {
            Id = id;
        }

        public EntityUid Id { get; }
    }
}
