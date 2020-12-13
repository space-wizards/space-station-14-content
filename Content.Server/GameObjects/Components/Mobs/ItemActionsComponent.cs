﻿using System.Collections.Generic;
using System.Linq;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Mobs
{
    /// <summary>
    /// Should be used only on items. This is not required in order to give an item
    /// actions, it's just one way to do it (the other way is by implementing IItemActionsEquipped or explicitly calling
    /// SharedActionsComponent methods on the user from a different component on your item).
    ///
    /// Currently all it does is grant specific item actions when picked up (they will
    /// be revoked automatically by SharedActionsComponent when dropped). This could possibly evolve
    /// to make it easier to use for granting item actions.
    /// </summary>
    [RegisterComponent]
    public class ItemActionsComponent : Component, IEquippedHand, IEquipped
    {
        public override string Name => "ItemActions";

        /// <summary>
        /// List of ItemActionTypes that will be auto-granted when this item is picked up.
        /// </summary>
        public IEnumerable<ItemActionType> AutoGrantActions => _autoGrantActions;
        private List<ItemActionType> _autoGrantActions;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _autoGrantActions,"autoGrantActions", new List<ItemActionType>());
        }

        public void EquippedHand(EquippedHandEventArgs eventArgs)
        {
            Grant(eventArgs.User);
        }

        public void Equipped(EquippedEventArgs eventArgs)
        {
            Grant(eventArgs.User);
        }

        private void Grant(IEntity user)
        {
            if (!user.TryGetComponent<SharedActionsComponent>(out var actionsComponent)) return;
            foreach (var actionType in AutoGrantActions)
            {
                actionsComponent.Grant(actionType, Owner, true);
            }
        }
    }
}