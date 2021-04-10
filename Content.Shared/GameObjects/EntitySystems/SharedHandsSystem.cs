using Content.Shared.GameObjects.Components.Items;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Players;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using System;

namespace Content.Shared.GameObjects.EntitySystems
{
    public abstract class SharedHandsSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<EntRemovedFromContainerMessage>(HandleContainerModified);
            SubscribeLocalEvent<EntInsertedIntoContainerMessage>(HandleContainerModified);

            SubscribeLocalEvent<SwapHandsMessage>(HandleSwapHands);
            SubscribeNetworkEvent<SwapHandsMessage>(HandleSwapHands);

            SubscribeLocalEvent<DropMessage>(HandleDrop);
            SubscribeNetworkEvent<DropMessage>(HandleDrop);
        }

        private void HandleSwapHands(SwapHandsMessage msg, EntitySessionEventArgs eventArgs)
        {
            var entity = eventArgs.SenderSession?.AttachedEntity;

            if (entity == null || !entity.TryGetComponent(out SharedHandsComponent? hands))
                return;

            hands.SwapHands();
        }

        private void HandleDrop(DropMessage msg, EntitySessionEventArgs eventArgs)
        {
            var entity = eventArgs.SenderSession?.AttachedEntity;

            if (entity == null || !entity.TryGetComponent(out SharedHandsComponent? hands))
                return;

            hands.TryDropActiveHand(msg.DropTarget);
        }

        protected abstract void HandleContainerModified(ContainerModifiedMessage args);
    }

    [Serializable, NetSerializable]
    public class SwapHandsMessage : EntityEventArgs
    {
    }

    [Serializable, NetSerializable]
    public class DropMessage : EntityEventArgs
    {
        public EntityCoordinates DropTarget { get; }

        public DropMessage(EntityCoordinates dropTarget)
        {
            DropTarget = dropTarget;
        }
    }
}
