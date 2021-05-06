using System.Collections.Generic;
using System.Threading.Tasks;
using Content.Server.Utility;
using Content.Shared.GameObjects.Components.Body;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Surgery.Operation;
using Content.Shared.GameObjects.Components.Surgery.Surgeon;
using Content.Shared.GameObjects.Components.Surgery.Target;
using Content.Shared.GameObjects.Components.Surgery.UI;
using Content.Shared.GameObjects.EntitySystems;
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

        private SharedSurgerySystem SurgerySystem => EntitySystem.Get<SharedSurgerySystem>();

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

        private void DoPopups(IEntity surgeon, IEntity target, SurgeryOperationPrototype operation)
        {
            if (SurgeonIsTarget(surgeon, target))
            {
                if (target.TryGetComponent(out IBodyPart? part) &&
                    part.Body != null)
                {
                    var id = "surgery-prepare-start-self-surgeon-popup";
                    target.PopupMessage(surgeon, Loc.GetString(id,
                        ("item", Owner),
                        ("zone", target),
                        ("procedure", operation.Name)));

                    id = "surgery-prepare-start-self-outsider-popup";
                    part.Body.Owner.PopupMessageOtherClients(Loc.GetString(id,
                        ("user", surgeon),
                        ("item", Owner),
                        ("part", target),
                        ("procedure", operation.Name)),
                        except: part.Body.Owner);
                }
                else
                {
                    var id = "surgery-prepare-start-self-no-zone-surgeon-popup";
                    target.PopupMessage(surgeon, Loc.GetString(id,
                        ("item", Owner),
                        ("procedure", operation.Name)));

                    id = "surgery-prepare-start-self-no-zone-outsider-popup";
                    target.PopupMessage(surgeon, Loc.GetString(id,
                        ("user", surgeon),
                        ("item", Owner)));
                }
            }
            else
            {
                if (target.TryGetComponent(out IBodyPart? part) &&
                    part.Body != null)
                {
                    var id = "surgery-prepare-start-surgeon-popup";
                    part.Body.Owner.PopupMessage(surgeon, Loc.GetString(id,
                        ("item", Owner),
                        ("target", part.Body.Owner),
                        ("zone", part.Owner),
                        ("procedure", operation.Name)));

                    id = "surgery-prepare-start-target-popup";
                    surgeon.PopupMessage(part.Body.Owner, Loc.GetString(id,
                        ("user", surgeon),
                        ("item", Owner),
                        ("zone", part.Owner)));

                    id = "surgery-prepare-start-outsider-popup";
                    surgeon.PopupMessageOtherClients(Loc.GetString(id,
                        ("user", surgeon),
                        ("item", Owner),
                        ("target", part.Body.Owner),
                        ("zone", target)),
                        except: part.Body.Owner);
                }
                else
                {
                    var id = "surgery-prepare-start-no-zone-surgeon-popup";
                    target.PopupMessage(surgeon, Loc.GetString(id,
                        ("item", Owner),
                        ("target", target),
                        ("procedure", operation.Name)));

                    id = "surgery-prepare-start-no-zone-target-popup";
                    surgeon.PopupMessage(target, Loc.GetString(id,
                        ("user", surgeon),
                        ("item", Owner)));

                    id = "surgery-prepare-start-no-zone-outsider-popup";
                    surgeon.PopupMessageOtherClients(Loc.GetString(id,
                        ("user", surgeon),
                        ("item", Owner),
                        ("target", target)),
                        except: target);
                }
            }
        }

        private bool SurgeonIsTarget(IEntity surgeon, IEntity target)
        {
            if (surgeon == target)
            {
                return true;
            }

            if (target.TryGetComponent(out IBodyPart? part) &&
                surgeon.TryGetComponent(out IBody? body) &&
                part.Body == body)
            {
                return true;
            }

            return false;
        }

        public bool TryUse(SurgeonComponent surgeon, SurgeryTargetComponent target, SurgeryOperationPrototype operation)
        {
            if (!SurgerySystem.TryStartSurgery(surgeon, target, operation))
            {
                return false;
            }

            DoPopups(surgeon.Owner, target.Owner, operation);
            return true;
        }

        private void OnUIMessage(ServerBoundUserInterfaceMessage message)
        {
            switch (message.Message)
            {
                case SurgeryOpPartSelectUIMsg msg:
                    if (!Owner.EntityManager.TryGetEntity(msg.Part, out var targetEntity))
                    {
                        _sawmill.Warning(
                            $"Client {message.Session} sent {nameof(SurgeryOpPartSelectUIMsg)} with an invalid target entity id: {msg.Part}");
                        return;
                    }

                    if (!targetEntity.TryGetComponent(out SurgeryTargetComponent? target))
                    {
                        _sawmill.Warning(
                            $"Client {message.Session} sent {nameof(SurgeryOpPartSelectUIMsg)} with an entity that has no {nameof(SurgeryTargetComponent)}: {targetEntity}");
                        return;
                    }

                    var surgeon = message.Session.AttachedEntity?.EnsureComponent<SurgeonComponent>();

                    if (surgeon == null)
                    {
                        _sawmill.Warning(
                            $"Client {message.Session} sent {nameof(SurgeryOpPartSelectUIMsg)} with no attached entity of their own.");
                        return;
                    }

                    if (!_prototypeManager.TryIndex<SurgeryOperationPrototype>(msg.OperationId, out var operation))
                    {
                        _sawmill.Warning(
                            $"Client {message.Session} sent {nameof(SurgeryOpPartSelectUIMsg)} with an invalid {nameof(SurgeryOperationPrototype)} id: {msg.OperationId}");
                        return;
                    }

                    if (operation.Hidden)
                    {
                        _sawmill.Warning(
                            $"Client {message.Session} sent {nameof(SurgeryOpPartSelectUIMsg)} that tried to start a hidden {nameof(SurgeryOperationPrototype)} with id: {msg.OperationId}");
                        return;
                    }

                    if (SurgerySystem.IsPerformingSurgeryOn(surgeon, target))
                    {
                        _sawmill.Warning($"Client {message.Session} sent {nameof(SurgeryOpPartSelectUIMsg)} to a start a {msg.OperationId} operation while already performing a {target.Operation?.ID} on {target.Owner}");
                        return;
                    }

                    TryUse(surgeon, target, operation);
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

            foreach (var (part, _) in body.Parts)
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
                SurgerySystem.StopSurgery(surgeon);
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
