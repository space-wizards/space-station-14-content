using Content.Shared.GameObjects.Components.Body.Part;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Body.Surgery.Behaviors
{
    [ImplicitDataDefinitionForInheritors]
    public interface ISurgeryBehavior
    {
        bool Perform(IEntity surgeon, IBodyPart part);
    }
}
