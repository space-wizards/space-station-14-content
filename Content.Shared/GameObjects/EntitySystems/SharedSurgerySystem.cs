using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Content.Shared.GameObjects.Components.Surgery;
using Content.Shared.GameObjects.Components.Surgery.Operation;
using Content.Shared.GameObjects.Components.Surgery.Operation.Messages;
using Content.Shared.GameObjects.Components.Surgery.Operation.Step;
using Content.Shared.GameObjects.Components.Surgery.Surgeon;
using Content.Shared.GameObjects.Components.Surgery.Surgeon.Messages;
using Content.Shared.GameObjects.Components.Surgery.Target;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;

namespace Content.Shared.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public class SharedSurgerySystem : EntitySystem
    {
        public const string SurgeryLogId = "surgery";

        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override void Initialize()
        {
            base.Initialize();

            ValidateOperations();

            SubscribeLocalEvent<SurgeryTargetComponent, ComponentRemove>(HandleTargetComponentRemoved);
            SubscribeLocalEvent<SurgeonComponent, SurgeonStartedOperation>(HandleSurgeonStartedOperation);
            SubscribeLocalEvent<SurgeonComponent, SurgeonStoppedOperation>(HandleSurgeonStoppedOperation);
            SubscribeLocalEvent<SurgeryTargetComponent, OperationEnded>(HandleOperationEnded);
        }

        private void HandleTargetComponentRemoved(EntityUid uid, SurgeryTargetComponent target, ComponentRemove args)
        {
            if (target.Surgeon == null || target.Operation == null)
            {
                return;
            }

            StopSurgery(target.Surgeon, target);
        }

        private void HandleSurgeonStartedOperation(EntityUid uid, SurgeonComponent surgeon, SurgeonStartedOperation args)
        {
            args.Target.Surgeon = EntityManager.GetEntity(uid).GetComponent<SurgeonComponent>();
            args.Target.Operation = args.Operation;
        }

        private void HandleSurgeonStoppedOperation(EntityUid uid, SurgeonComponent surgeon, SurgeonStoppedOperation args)
        {
            surgeon.SurgeryCancellation?.Cancel();
            surgeon.SurgeryCancellation = null;
            surgeon.Target = null;
            surgeon.Slot = null;

            args.OldTarget.Surgeon = null;
        }

        private void HandleOperationEnded(EntityUid uid, SurgeryTargetComponent target, OperationEnded args)
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

            var message = new SurgeonStartedOperation(target, operation);
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

        public bool IsPerformingSurgery(SurgeonComponent surgeon)
        {
            return surgeon.Target != null;
        }

        public bool IsPerformingSurgeryOn(SurgeonComponent surgeon, SurgeryTargetComponent target)
        {
            return surgeon.Target == target;
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

            var message = new SurgeonStoppedOperation(oldTarget);
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

        public bool CanAddSurgeryTag(SurgeryTargetComponent target, SurgeryTag tag)
        {
            if (target.Operation == null ||
                target.Operation.Steps.Count <= target.SurgeryTags.Count)
            {
                return false;
            }

            var nextStep = target.Operation.Steps[target.SurgeryTags.Count];
            if (!nextStep.Necessary(target) || nextStep.Id != tag.Id)
            {
                return false;
            }

            return true;
        }

        public bool TryAddSurgeryTag(SurgeryTargetComponent target, SurgeryTag tag)
        {
            if (!CanAddSurgeryTag(target, tag))
            {
                return false;
            }

            target.SurgeryTags.Add(tag);
            CheckCompletion(target);

            return true;
        }

        public bool TryRemoveSurgeryTag(SurgeryTargetComponent target, SurgeryTag tag)
        {
            if (target.SurgeryTags.Count == 0 ||
                target.SurgeryTags[^1] != tag)
            {
                return false;
            }

            target.SurgeryTags.RemoveAt(target.SurgeryTags.Count - 1);
            return true;
        }

        private void CheckCompletion(SurgeryTargetComponent target)
        {
            if (target.Surgeon == null ||
                target.Operation == null ||
                target.Operation.Steps.Count > target.SurgeryTags.Count)
            {
                return;
            }

            var offset = 0;

            for (var i = 0; i < target.SurgeryTags.Count; i++)
            {
                var step = target.Operation.Steps[i + offset];

                if (!step.Necessary(target))
                {
                    offset++;
                    step = target.Operation.Steps[i + offset];
                }

                var tag = target.SurgeryTags[i];

                if (tag != step.Id)
                {
                    return;
                }
            }

            target.Operation.Effect?.Execute(target.Surgeon, target);
        }
    }
}
