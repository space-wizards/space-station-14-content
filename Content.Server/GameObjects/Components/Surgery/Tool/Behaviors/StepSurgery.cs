using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Surgery.Operation.Step;
using Content.Shared.GameObjects.Components.Surgery.Surgeon;
using Content.Shared.GameObjects.Components.Surgery.Target;
using Content.Shared.GameObjects.EntitySystems;
using Content.Shared.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.GameObjects.Components.Surgery.Tool.Behaviors
{
    public class StepSurgery : SurgeryBehavior
    {
        private SharedSurgerySystem SurgerySystem => EntitySystem.Get<SharedSurgerySystem>();

        [field: DataField("step", customTypeSerializer: typeof(PrototypeIdSerializer<SurgeryStepPrototype>))]
        private string? StepId { get; } = default;

        public SurgeryStepPrototype? Step => StepId == null
            ? null
            : IoCManager.Resolve<IPrototypeManager>().Index<SurgeryStepPrototype>(StepId);

        public override bool CanPerform(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            return StepId != null && SurgerySystem.CanAddSurgeryTag(target, StepId);
        }

        public override void OnBeginPerformDelay(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            var step = Step;

            if (step == null)
            {
                return;
            }

            var surgeonOwner = surgeon.Owner;
            var targetOwner = target.Owner.GetComponentOrNull<IBodyPart>()?.Body?.Owner ?? target.Owner;

            surgeonOwner.PopupMessage(step.SurgeonBeginPopup(surgeonOwner, targetOwner, targetOwner));

            if (targetOwner != surgeonOwner)
            {
                targetOwner.PopupMessage(step.TargetBeginPopup(surgeonOwner, targetOwner));
            }

            surgeonOwner.PopupMessageOtherClients(step.OutsiderBeginPopup(surgeonOwner, targetOwner, targetOwner), except: targetOwner);
        }

        public override bool Perform(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            var step = Step;

            if (step == null)
            {
                return false;
            }

            if (!SurgerySystem.TryAddSurgeryTag(target, step.ID))
            {
                surgeon.Owner.PopupMessage("You see no useful way to do that.");
                return false;
            }

            var surgeonOwner = surgeon.Owner;
            var targetOwner = target.Owner.GetComponentOrNull<IBodyPart>()?.Body?.Owner ?? target.Owner;

            surgeonOwner.PopupMessage(step.SurgeonSuccessPopup(surgeonOwner, targetOwner, targetOwner));

            if (targetOwner != surgeonOwner)
            {
                targetOwner.PopupMessage(step.TargetSuccessPopup(surgeonOwner, targetOwner));
            }

            surgeonOwner.PopupMessageOtherClients(step.OutsiderSuccessPopup(surgeonOwner, targetOwner, targetOwner), except: targetOwner);

            return true;
        }
    }
}
