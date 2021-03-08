﻿#nullable enable
using JetBrains.Annotations;
using NFluidsynth;
using Robust.Client.AutoGenerated;
using Robust.Client.Console;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.IoC;

namespace Content.Client.UserInterface.AdminMenu.Tabs.AdminTab
{
    [GenerateTypedNameReferences]
    [UsedImplicitly]
    public partial class TeleportWindow : SS14Window
    {
        private IPlayerSession? _selectedSession;

        protected override void EnteredTree()
        {
            SubmitButton.OnPressed += SubmitButtonOnOnPressed;
            PlayerList.OnSelectionChanged += OnListOnOnSelectionChanged;
        }

        private void OnListOnOnSelectionChanged(IPlayerSession? obj)
        {
            _selectedSession = obj;
            SubmitButton.Disabled = _selectedSession == null;
        }

        private void SubmitButtonOnOnPressed(BaseButton.ButtonEventArgs obj)
        {
            if (_selectedSession == null)
                return;
            // Execute command
            IoCManager.Resolve<IClientConsoleHost>().ExecuteCommand(
                $"tpto \"{_selectedSession.Name}\"");
        }
    }
}
