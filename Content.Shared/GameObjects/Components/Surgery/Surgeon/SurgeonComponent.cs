using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Content.Shared.GameObjects.Components.Body.Slot;
using Content.Shared.GameObjects.Components.Surgery.Operation;
using Content.Shared.GameObjects.Components.Surgery.Surgeon.ComponentMessages;
using Content.Shared.GameObjects.Components.Surgery.Surgeon.Messages;
using Content.Shared.GameObjects.Components.Surgery.Target;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Shared.GameObjects.Components.Surgery.Surgeon
{
    [RegisterComponent]
    public class SurgeonComponent : Component
    {
        public override string Name => "Surgeon";
        public override uint? NetID => ContentNetIDs.SURGEON;

        private SurgeryTargetComponent? _target;

        public SurgeryTargetComponent? Target
        {
            get => _target;
            private set
            {
                if (_target == value)
                {
                    return;
                }

                var old = _target;
                _target = value;

                if (value == null)
                {
                    DebugTools.AssertNotNull(old);

                    SurgeryCancellation?.Cancel();
                    SurgeryCancellation = null;
                    BodyPartSlot = null;

                    var message = new SurgeonStoppedOperationMessage(old!);
                    Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, message);
                }
                else
                {
                    SurgeryCancellation = new CancellationTokenSource();
                }
            }
        }

        public CancellationTokenSource? SurgeryCancellation { get; private set; }

        public BodyPartSlot? BodyPartSlot { get; private set; }

        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not SurgeonComponentState state)
            {
                return;
            }

            _target = state.Target == null
                ? null
                : Owner.EntityManager.GetEntity(state.Target.Value).EnsureComponent<SurgeryTargetComponent>();
        }

        public bool TryStartSurgery(
            SurgeryTargetComponent target,
            SurgeryOperationPrototype operation,
            [NotNullWhen(true)] out CancellationTokenSource? token)
        {
            if (_target != null)
            {
                token = null;
                return false;
            }

            token = StartSurgery(target, operation);
            return true;
        }

        public bool TryStartSurgery(SurgeryTargetComponent target, SurgeryOperationPrototype operation)
        {
            return TryStartSurgery(target, operation, out _);
        }

        public CancellationTokenSource StartSurgery(SurgeryTargetComponent target, SurgeryOperationPrototype operation)
        {
            StopSurgery();

            Target = target;

            var message = new SurgeonStartedOperationMessage(target, operation);
            Owner.EntityManager.EventBus.RaiseLocalEvent(Owner.Uid, message);

            var compMessage = new SurgeonStartedOperationComponentMessage(this, target, operation);
            target.Owner.SendMessage(this, compMessage);

            return SurgeryCancellation!;
        }

        /// <summary>
        ///     Tries to stop the surgery that this surgeon is performing.
        /// </summary>
        /// <returns>True if stopped, false otherwise even if no surgery was underway.</returns>
        public bool StopSurgery()
        {
            if (Target == null)
            {
                return false;
            }

            Target = null;
            return true;
        }

        public bool StopSurgery(SurgeryTargetComponent target)
        {
            if (Target != target)
            {
                return false;
            }

            return StopSurgery();
        }
    }
}
