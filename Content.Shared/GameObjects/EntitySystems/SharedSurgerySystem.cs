using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Content.Shared.GameObjects.Components.Surgery.Operation;
using Content.Shared.GameObjects.Components.Surgery.Operation.Messages;
using Content.Shared.GameObjects.Components.Surgery.Operation.Step;
using Content.Shared.GameObjects.Components.Surgery.Surgeon;
using Content.Shared.GameObjects.Components.Surgery.Surgeon.Messages;
using Content.Shared.GameObjects.Components.Surgery.Target;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared.GameObjects.EntitySystems
{
    public class SharedSurgerySystem : EntitySystem
    {
        public const string SurgeryLogId = "surgery";

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            ValidateOperations();

            SubscribeLocalEvent<SurgeryTargetComponent, ComponentRemove>(HandleTargetComponentRemoved);
            SubscribeLocalEvent<SurgeonComponent, SurgeonStartedOperationMessage>(HandleSurgeonStartedOperation);
            SubscribeLocalEvent<SurgeonComponent, SurgeonStoppedOperationMessage>(HandleSurgeonStoppedOperation);
            SubscribeLocalEvent<SurgeryTargetComponent, OperationEndedMessage>(HandleOperationEnded);
        }

        private void HandleTargetComponentRemoved(EntityUid uid, SurgeryTargetComponent target, ComponentRemove args)
        {
            if (target.Surgeon == null || target.Operation == null)
            {
                return;
            }

            StopSurgery(target.Surgeon, target);
        }

        private void HandleSurgeonStartedOperation(EntityUid uid, SurgeonComponent surgeon, SurgeonStartedOperationMessage args)
        {
            args.Target.Surgeon = EntityManager.GetEntity(uid).GetComponent<SurgeonComponent>();
            args.Target.Operation = args.Operation;
        }

        private void HandleSurgeonStoppedOperation(EntityUid uid, SurgeonComponent surgeon, SurgeonStoppedOperationMessage args)
        {
            surgeon.SurgeryCancellation?.Cancel();
            surgeon.SurgeryCancellation = null;
            surgeon.Target = null;
            surgeon.Slot = null;

            args.OldTarget.Surgeon = null;
        }

        private void HandleOperationEnded(EntityUid uid, SurgeryTargetComponent target, OperationEndedMessage args)
        {
            target.Surgeon = null;
            target.Operation = null;
        }

        private void ValidateOperations()
        {
            foreach (var operation in _prototypeManager.EnumeratePrototypes<SurgeryOperationPrototype>())
            {
                foreach (var step in operation.Steps)
                {
                    if (!_prototypeManager.HasIndex<SurgeryStepPrototype>(step.Id))
                    {
                        throw new PrototypeLoadException(
                            $"Invalid {nameof(SurgeryStepPrototype)} found in surgery operation with id {operation.ID}: No step found with id {step}");
                    }
                }
            }
        }

        public CancellationTokenSource StartSurgery(
            SurgeonComponent surgeon,
            SurgeryTargetComponent target,
            SurgeryOperationPrototype operation)
        {
            StopSurgery(surgeon);

            surgeon.Target = target;

            var cancellation = new CancellationTokenSource();
            surgeon.SurgeryCancellation = cancellation;

            var message = new SurgeonStartedOperationMessage(target, operation);
            RaiseLocalEvent(surgeon.Owner.Uid, message);

            return cancellation;
        }

        public bool TryStartSurgery(
            SurgeonComponent surgeon,
            SurgeryTargetComponent target,
            SurgeryOperationPrototype operation,
            [NotNullWhen(true)] out CancellationTokenSource? token)
        {
            if (surgeon.Target != null)
            {
                token = null;
                return false;
            }

            token = StartSurgery(surgeon, target, operation);
            return true;
        }

        public bool TryStartSurgery(
            SurgeonComponent surgeon,
            SurgeryTargetComponent target,
            SurgeryOperationPrototype operation)
        {
            return TryStartSurgery(surgeon, target, operation, out _);
        }

        /// <summary>
        ///     Tries to stop the surgery that the surgeon is performing.
        /// </summary>
        /// <returns>True if stopped, false otherwise even if no surgery was underway.</returns>
        public bool StopSurgery(SurgeonComponent surgeon)
        {
            if (surgeon.Target == null)
            {
                return false;
            }

            var oldTarget = surgeon.Target;
            surgeon.Target = null;

            var message = new SurgeonStoppedOperationMessage(oldTarget);
            RaiseLocalEvent(surgeon.Owner.Uid, message);

            return true;
        }

        public bool StopSurgery(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            if (surgeon.Target != target)
            {
                return false;
            }

            return StopSurgery(surgeon);
        }
    }
}
