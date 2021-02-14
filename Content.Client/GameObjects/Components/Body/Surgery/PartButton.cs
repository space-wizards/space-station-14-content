using Robust.Client.UserInterface.Controls;
using Robust.Shared.GameObjects;

namespace Content.Client.GameObjects.Components.Body.Surgery
{
    public class PartButton : Button
    {
        public PartButton(EntityUid entityUid)
        {
            EntityUid = entityUid;
        }

        public EntityUid EntityUid { get; }
    }
}
