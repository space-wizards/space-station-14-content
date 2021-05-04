using Content.Shared.GameObjects.Components.Surgery.Target;
using Content.Shared.GameObjects.EntitySystems;
using Robust.Shared.GameObjects;

namespace Content.Shared.GameObjects.Components.Surgery.Operation.Effect
{
    public class CauterizationEffect : IOperationEffect
    {
        public void Execute(SurgeryTargetComponent target)
        {
            if (target.Surgeon == null)
            {
                return;
            }

            EntitySystem.Get<SharedSurgerySystem>().StopSurgery(target.Surgeon, target);
        }
    }
}
