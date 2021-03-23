using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Surgery.Operation;
using Content.Shared.GameObjects.Components.Surgery.Surgeon;
using Content.Shared.GameObjects.Components.Surgery.Target;
using Content.Shared.GameObjects.Components.Surgery.UI;
using Content.Shared.Interfaces;
using Content.Shared.Interfaces.GameObjects.Components;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.ViewVariables;
using static Content.Shared.GameObjects.EntitySystems.SharedSurgerySystem;

namespace Content.Server.GameObjects.Components.Surgery.Tool
{
    [RegisterComponent]
    public class SurgeryDrapesComponent : Component, IAfterInteract
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        public override string Name => "SurgeryDrapes";

        private ISawmill _sawmill = default!;

        [ViewVariables] private BoundUserInterface? UserInterface => Owner.GetUIOrNull(SurgeryUIKey.Key);

        public override void Initialize()
        {
            base.Initialize();

            _sawmill = Logger.GetSawmill(SurgeryLogId);
        }

        protected override void Startup()
        {
            base.Startup();

            if (UserInterface != null)
            {
                UserInterface.OnReceiveMessage += OnUIMessage;
            }
        }

        private void OnUIMessage(ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case SurgeryOperationSelectedUIMessage msg:
                    if (!Owner.EntityManager.TryGetEntity(msg.Target, out var targetEntity))
                    {
                        _sawmill.Warning(
                            $"Client {message.Session} sent {nameof(SurgeryOperationSelectedUIMessage)} with an invalid target entity id: {msg.Target}");
                        return;
                    }

                    if (!targetEntity.TryGetComponent(out SurgeryTargetComponent? target))
                    {
                        _sawmill.Warning(
                            $"Client {message.Session} sent {nameof(SurgeryOperationSelectedUIMessage)} with an entity that has no {nameof(SurgeryTargetComponent)}: {targetEntity}");
                        return;
                    }

                    var surgeon = message.Session.AttachedEntity?.EnsureComponent<SurgeonComponent>();

                    if (surgeon == null)
                    {
                        _sawmill.Warning(
                            $"Client {message.Session} sent {nameof(SurgeryOperationSelectedUIMessage)} with no attached entity of their own.");
                        return;
                    }

                    if (!_prototypeManager.TryIndex<SurgeryOperationPrototype>(msg.OperationId, out var operation))
                    {
                        _sawmill.Warning(
                            $"Client {message.Session} sent {nameof(SurgeryOperationSelectedUIMessage)} with an invalid {nameof(SurgeryOperationPrototype)} id: {msg.OperationId}");
                        return;
                    }

                    if (!surgeon.TryStartSurgery(target, operation))
                    {
                        _sawmill.Warning($"Client {message.Session} sent {nameof(SurgeryOperationSelectedUIMessage)} to a start a {msg.OperationId} operation while already performing a {target.Operation?.ID} on {target.Owner}");
                        return;
                    }

                    var surgeonOwner = surgeon.Owner;
                    var targetOwner = target.Owner;

                    if (surgeonOwner == targetOwner)
                    {
                        if (targetOwner.TryGetComponent(out IBodyPart? part) &&
                            part.Body != null)
                        {
                            var id = "surgery-prepare-start-self-surgeon-popup";
                            targetOwner.PopupMessage(surgeonOwner, Loc.GetString(id,
                                ("item", Owner),
                                ("zone", targetOwner),
                                ("procedure", operation.Name)));

                            id = "surgery-prepare-start-self-outsider-popup";
                            part.Body.Owner.PopupMessageOtherClients(Loc.GetString(id,
                                ("user", surgeonOwner),
                                ("item", Owner),
                                ("part", targetOwner),
                                ("procedure", operation.Name)), except: surgeonOwner);
                        }
                        else
                        {
                            var id = "surgery-prepare-start-self-no-zone-surgeon-popup";
                            targetOwner.PopupMessage(surgeonOwner, Loc.GetString(id,
                                ("item", Owner),
                                ("procedure", operation.Name)));

                            id = "surgery-prepare-start-self-no-zone-outsider-popup";
                            targetOwner.PopupMessage(surgeonOwner, Loc.GetString(id,
                                ("user", surgeonOwner),
                                ("item", Owner)));
                        }
                    }
                    else
                    {
                        if (targetOwner.TryGetComponent(out IBodyPart? part) &&
                            part.Body != null)
                        {
                            var id = "surgery-prepare-start-surgeon-popup";
                            part.Body.Owner.PopupMessage(surgeonOwner, Loc.GetString(id,
                                ("item", Owner),
                                ("target", part.Body.Owner),
                                ("zone", part.Owner),
                                ("procedure", operation.Name)));

                            id = "surgery-prepare-start-target-popup";
                            surgeonOwner.PopupMessage(part.Body.Owner, Loc.GetString(id,
                                ("user", surgeonOwner),
                                ("item", Owner),
                                ("zone", part.Owner)));

                            id = "surgery-prepare-start-outsider-popup";
                            surgeonOwner.PopupMessageOtherClients(Loc.GetString(id,
                                ("user", surgeonOwner),
                                ("item", Owner),
                                ("target", part.Body.Owner),
                                ("zone", targetOwner)), except: targetOwner);
                        }
                        else
                        {
                            var id = "surgery-prepare-start-no-zone-surgeon-popup";
                            targetOwner.PopupMessage(surgeonOwner, Loc.GetString(id,
                                ("item", Owner),
                                ("target", targetOwner),
                                ("procedure", operation.Name)));

                            id = "surgery-prepare-start-no-zone-target-popup";
                            surgeonOwner.PopupMessage(targetOwner, Loc.GetString(id,
                                ("user", surgeonOwner),
                                ("item", Owner)));

                            id = "surgery-prepare-start-no-zone-outsider-popup";
                            surgeonOwner.PopupMessageOtherClients(Loc.GetString(id,
                                ("user", surgeonOwner),
                                ("item", Owner),
                                ("target", targetOwner)), except: targetOwner);
                        }
                    }

                    break;
            }
        }

        private void OpenUI(IPlayerSession session)
        {
            UserInterface?.Open(session);
        }

        private void UpdateUI(IBody body)
        {
            if (UserInterface == null)
            {
                return;
            }

            var parts = new List<EntityUid>();

            foreach (var part in body.Parts.Values)
            {
                if (part.Owner.TryGetComponent(out SurgeryTargetComponent? surgery))
                {
                    parts.Add(surgery.Owner.Uid);
                }
            }

            var state = new SurgeryUIState(parts.ToArray());
            UserInterface.SetState(state);
        }

        // TODO SURGERY: Add surgery for dismembered limbs
        async Task<bool> IAfterInteract.AfterInteract(AfterInteractEventArgs eventArgs)
        {
            var target = eventArgs.Target;
            if (target == null)
            {
                return false;
            }

            var user = eventArgs.User;
            if (user.TryGetComponent(out SurgeonComponent? surgeon) &&
                surgeon.Target?.Owner == target)
            {
                surgeon.StopSurgery();
                return true;
            }

            if (!user.TryGetComponent(out IActorComponent? actor) ||
                !target.TryGetComponent(out IBody? body))
            {
                return false;
            }

            OpenUI(actor.playerSession);
            UpdateUI(body);

            return true;
        }
    }
}
