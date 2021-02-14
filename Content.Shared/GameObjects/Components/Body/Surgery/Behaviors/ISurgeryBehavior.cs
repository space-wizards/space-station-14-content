using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Body.Surgery.Behaviors
{
    public interface ISurgeryBehavior : IExposeData
    {
        bool Perform(IEntity performer, IBodyPart part);
    }
}
