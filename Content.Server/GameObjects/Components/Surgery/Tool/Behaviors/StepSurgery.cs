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
    public class StepSurgery : ISurgeryBehavior
    {
        private SharedSurgerySystem SurgerySystem => EntitySystem.Get<SharedSurgerySystem>();

        [field: DataField("step", customTypeSerializer: typeof(PrototypeIdSerializer<SurgeryStepPrototype>))]
        private string? StepId { get; } = default;

        public SurgeryStepPrototype? Step => StepId == null
            ? null
            : IoCManager.Resolve<IPrototypeManager>().Index<SurgeryStepPrototype>(StepId);

        public bool CanPerform(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            return StepId != null && SurgerySystem.CanAddSurgeryTag(target, StepId);
        }

        public bool Perform(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            var step = Step;

            if (step == null)
            {
                return false;
            }

            return SurgerySystem.TryAddSurgeryTag(target, step.ID);
        }

        public void OnPerformDelayBegin(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            var step = Step;

            if (step == null)
            {
                return;
            }

            var surgeonOwner = surgeon.Owner;
            var bodyOwner = target.Owner.GetComponentOrNull<IBodyPart>()?.Body?.Owner ?? target.Owner;

            surgeonOwner.PopupMessage(step.SurgeonBeginPopup(surgeonOwner, bodyOwner, target.Owner));

            if (bodyOwner != surgeonOwner)
            {
                bodyOwner.PopupMessage(step.TargetBeginPopup(surgeonOwner, bodyOwner));
            }

            surgeonOwner.PopupMessageOtherClients(step.OutsiderBeginPopup(surgeonOwner, bodyOwner, target.Owner), except: bodyOwner);
        }

        public void OnPerformSuccess(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            var step = Step;

            if (step == null)
            {
                return;
            }

            var surgeonOwner = surgeon.Owner;
            var bodyOwner = target.Owner.GetComponentOrNull<IBodyPart>()?.Body?.Owner ?? target.Owner;

            surgeonOwner.PopupMessage(step.SurgeonSuccessPopup(surgeonOwner, bodyOwner, target.Owner));

            if (bodyOwner != surgeonOwner)
            {
                bodyOwner.PopupMessage(step.TargetSuccessPopup(surgeonOwner, bodyOwner));
            }

            surgeonOwner.PopupMessageOtherClients(step.OutsiderSuccessPopup(surgeonOwner, bodyOwner, target.Owner), except: bodyOwner);
        }

        public void OnPerformFail(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            surgeon.Owner.PopupMessage("You see no useful way to do that.");
        }
    }
}
