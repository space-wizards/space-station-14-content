using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Surgery.Behaviors
{
    public abstract class SurgeryBehavior : ISurgeryBehavior
    {
        public virtual void ExposeData(ObjectSerializer serializer) { }

        public abstract bool Perform(IEntity performer, IBodyPart part);
    }
}
