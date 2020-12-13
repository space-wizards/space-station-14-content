﻿using System;
using Content.Server.Utility;
using Content.Shared.Actions;
using Content.Shared.GameObjects.Components.Mobs;
using Content.Shared.Utility;
using JetBrains.Annotations;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;

namespace Content.Server.Actions
{
    /// <summary>
    /// Just shows a popup message.asd
    /// </summary>
    [UsedImplicitly]
    public class DebugInstant : IInstantAction, IInstantItemAction
    {
        public string Message { get; private set; }
        public float Cooldown { get; private set; }

        public void ExposeData(ObjectSerializer serializer)
        {
            serializer.DataField(this, x => x.Message, "message", "Instant action used.");
            serializer.DataField(this, x => x.Cooldown, "cooldown", 0);
        }

        public void DoInstantAction(InstantItemActionEventArgs args)
        {
            args.Performer.PopupMessageEveryone(Message);
            if (Cooldown > 0)
            {
                args.PerformerActionsComponent.Cooldown(args.ActionType, args.Item, Cooldowns.SecondsFromNow(Cooldown));
            }
        }

        public void DoInstantAction(InstantActionEventArgs args)
        {
            args.Performer.PopupMessageEveryone(Message);
            args.PerformerActionsComponent.Cooldown(args.ActionType, Cooldowns.SecondsFromNow(Cooldown));
        }
    }
}