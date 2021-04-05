using System.Threading.Tasks;
using Content.Server.GameObjects.Components.Surgery.Tool.Behaviors;
using Content.Server.GameObjects.EntitySystems.DoAfter;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Surgery.Surgeon;
using Content.Shared.GameObjects.Components.Surgery.Target;
using Content.Shared.GameObjects.Components.Surgery.Tool;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Surgery.Tool
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSurgeryToolComponent))]
    public class SurgeryToolComponent : SharedSurgeryToolComponent, IAfterInteract
    {
        [field: DataField("behavior")]
        public ISurgeryBehavior? Behavior { get; } = default!;

        private async Task Use(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            if (Behavior == null)
            {
                return;
            }

            var doAfterSystem = EntitySystem.Get<DoAfterSystem>();

            if (Delay <= 0)
            {
                Behavior.Perform(surgeon, target);
                return;
            }

            Behavior.OnBeginPerformDelay(surgeon, target);

            var cancelToken = surgeon.SurgeryCancellation?.Token ?? default;
            var result = await doAfterSystem.DoAfter(new DoAfterEventArgs(surgeon.Owner, Delay, cancelToken, target.Owner)
            {
                BreakOnDamage = true,
                BreakOnStun = true,
                BreakOnTargetMove = true,
                BreakOnUserMove = true,
                NeedHand = true
            });

            if (result == DoAfterStatus.Finished)
            {
                Behavior.Perform(surgeon, target);
            }
        }

        // TODO SURGERY: Add surgery for dismembered limbs
        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            if (Behavior == null)
            {
                return false;
            }

            if (!eventArgs.User.TryGetComponent(out SurgeonComponent? surgeon))
            {
                return false;
            }

            if (surgeon.Target == null)
            {
                return false;
            }

            if (eventArgs.Target == null)
            {
                return false;
            }

            var target = eventArgs.Target;

            // If we are not performing surgery on a grape
            if (surgeon.Target.Owner != target)
            {
                // It might be on a body instead
                if (target.TryGetComponent(out IBody? body) &&
                    body.HasPart(surgeon.Target.Owner))
                {
                    target = surgeon.Target.Owner;
                }
                else
                {
                    return false;
                }
            }

            if (!target.TryGetComponent(out SurgeryTargetComponent? surgeryTarget))
            {
                return false;
            }

            if (!Behavior.CanPerform(surgeon, surgeryTarget))
            {
                surgeon.Owner.PopupMessage("You see no useful way to do that.");
                return false;
            }

            await Use(surgeon, surgeryTarget);
            return true;
        }
    }
}
