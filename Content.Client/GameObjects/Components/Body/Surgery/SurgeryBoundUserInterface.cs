#nullable enable
using System.Collections.Generic;
using Content.Shared.GameObjects.Components.Body.Part;
using Content.Shared.GameObjects.Components.Body.Surgery;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.ViewVariables;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.GameObjects.Components.Body.Surgery
{
    [UsedImplicitly]
    public class SurgeryBoundUserInterface : BoundUserInterface
    {
        [Dependency] private readonly IEntityManager _entityManager = default!;

        [ViewVariables] private SurgeryWindow? _window;

        public SurgeryBoundUserInterface(ClientUserInterfaceComponent owner, object uiKey) : base(owner, uiKey) { }

        protected override void Open()
        {
            base.Open();
            _window = new SurgeryWindow();
            _window.OnClose += Close;
            _window.OpenCentered();
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            if (_window == null || state is not SurgeryUIState uiState)
            {
                return;
            }

            foreach (var button in _window.PartButtons)
            {
                button.OnPressed -= OnPressed;
            }

            var parts = new List<IBodyPart>();

            foreach (var id in uiState.Parts)
            {
                if (!_entityManager.TryGetEntity(id, out var entity))
                {
                    continue;
                }

                if (!entity.TryGetComponent(out IBodyPart? part))
                {
                    continue;
                }

                parts.Add(part);
            }

            _window.UpdateParts(parts);
        }

        private void OnPressed(ButtonEventArgs args)
        {
            var button = (PartButton) args.Button;
            var msg = new PartSelectedUIMessage(button.EntityUid);
            SendMessage(msg);
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _window?.Dispose();
            }
        }
    }
}
