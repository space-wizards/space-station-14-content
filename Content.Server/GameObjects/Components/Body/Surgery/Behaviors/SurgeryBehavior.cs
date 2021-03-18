using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.GameObjects;

namespace Content.Server.GameObjects.Components.Body.Surgery.Behaviors
{
    public abstract class SurgeryBehavior : ISurgeryBehavior
    {
        public abstract bool Perform(IEntity surgeon, IBodyPart part);
    }
}
