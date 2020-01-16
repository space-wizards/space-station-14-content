﻿using System;
using System.Runtime;
using Content.Client.GameObjects.Components;
using Content.Client.Interfaces;
using Content.Shared.Preferences;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Localization;
using Robust.Shared.Maths;

namespace Content.Client.UserInterface
{
    public class HumanoidProfileEditorPanel : Control
    {
        private static readonly StyleBoxFlat HighlightedStyle = new StyleBoxFlat
        {
            BackgroundColor = new Color(47, 47, 53),
            ContentMarginTopOverride = 10,
            ContentMarginBottomOverride = 10,
            ContentMarginLeftOverride = 10,
            ContentMarginRightOverride = 10
        };

        private readonly LineEdit _ageEdit;

        private readonly LineEdit _nameEdit;
        private readonly IClientPreferencesManager _preferencesManager;
        private readonly Button _saveButton;
        private readonly Button _sexFemaleButton;
        private readonly Button _sexMaleButton;

        private bool _isDirty;
        public int CharacterSlot;
        public HumanoidCharacterProfile? Profile;

        public HumanoidProfileEditorPanel(ILocalizationManager localization,
            IClientPreferencesManager preferencesManager)
        {
            Profile = (HumanoidCharacterProfile?) preferencesManager.Preferences.SelectedCharacter;
            CharacterSlot = preferencesManager.Preferences.SelectedCharacterIndex;
            _preferencesManager = preferencesManager;

            var margin = new MarginContainer
            {
                MarginTopOverride = 10,
                MarginBottomOverride = 10,
                MarginLeftOverride = 10,
                MarginRightOverride = 10
            };
            AddChild(margin);

            var vBox = new VBoxContainer();
            margin.AddChild(vBox);

            var middleContainer = new HBoxContainer
            {
                SeparationOverride = 10
            };
            vBox.AddChild(middleContainer);

            var leftColumn = new VBoxContainer();
            middleContainer.AddChild(leftColumn);

            #region Randomize

            {
                var panel = HighlightedContainer();
                var randomizeEverythingButton = new Button
                {
                    Text = localization.GetString("Randomize everything"),
                    Disabled = true,
                    ToolTip = "Not yet implemented!"
                };
                panel.AddChild(randomizeEverythingButton);
                leftColumn.AddChild(panel);
            }

            #endregion Randomize

            var middleColumn = new VBoxContainer();
            leftColumn.AddChild(middleColumn);

            #region Name

            {
                var panel = HighlightedContainer();
                var hBox = new HBoxContainer
                {
                    SizeFlagsVertical = SizeFlags.FillExpand
                };
                var nameLabel = new Label {Text = localization.GetString("Name:")};
                _nameEdit = new LineEdit
                {
                    CustomMinimumSize = (270, 0),
                    SizeFlagsVertical = SizeFlags.ShrinkCenter
                };
                _nameEdit.OnTextChanged += args =>
                {
                    Profile = Profile?.WithName(args.Text);
                    IsDirty = true;
                };
                var nameRandomButton = new Button
                {
                    Text = localization.GetString("Randomize"),
                    Disabled = true,
                    ToolTip = "Not implemented yet!"
                };
                hBox.AddChild(nameLabel);
                hBox.AddChild(_nameEdit);
                hBox.AddChild(nameRandomButton);
                panel.AddChild(hBox);
                middleColumn.AddChild(panel);
            }

            #endregion Name

            var sexAndAgeRow = new HBoxContainer
            {
                SeparationOverride = 10
            };
            middleColumn.AddChild(sexAndAgeRow);

            #region Sex

            {
                var panel = HighlightedContainer();
                var hBox = new HBoxContainer();
                var sexLabel = new Label {Text = localization.GetString("Sex:")};

                var sexButtonGroup = new ButtonGroup();

                _sexMaleButton = new Button
                {
                    Text = localization.GetString("Male"),
                    Group = sexButtonGroup
                };
                _sexMaleButton.OnPressed += args =>
                {
                    Profile = Profile?.WithSex(Sex.Male);
                    IsDirty = true;
                };
                _sexFemaleButton = new Button
                {
                    Text = localization.GetString("Female"),
                    Group = sexButtonGroup
                };
                _sexFemaleButton.OnPressed += args =>
                {
                    Profile = Profile?.WithSex(Sex.Female);
                    IsDirty = true;
                };
                hBox.AddChild(sexLabel);
                hBox.AddChild(_sexMaleButton);
                hBox.AddChild(_sexFemaleButton);
                panel.AddChild(hBox);
                sexAndAgeRow.AddChild(panel);
            }

            #endregion Sex

            #region Age

            {
                var panel = HighlightedContainer();
                var hBox = new HBoxContainer();
                var ageLabel = new Label {Text = localization.GetString("Age:")};
                _ageEdit = new LineEdit {CustomMinimumSize = (40, 0)};
                _ageEdit.OnTextChanged += args =>
                {
                    if (!int.TryParse(args.Text, out var newAge))
                        return;
                    Profile = Profile?.WithAge(newAge);
                    IsDirty = true;
                };
                hBox.AddChild(ageLabel);
                hBox.AddChild(_ageEdit);
                panel.AddChild(hBox);
                sexAndAgeRow.AddChild(panel);
            }

            #endregion Age

            var rightColumn = new VBoxContainer();
            middleContainer.AddChild(rightColumn);

            #region Import/Export

            {
                var panelContainer = HighlightedContainer();
                var hBox = new HBoxContainer();
                var importButton = new Button
                {
                    Text = localization.GetString("Import"),
                    Disabled = true,
                    ToolTip = "Not yet implemented!"
                };
                var exportButton = new Button
                {
                    Text = localization.GetString("Export"),
                    Disabled = true,
                    ToolTip = "Not yet implemented!"
                };
                hBox.AddChild(importButton);
                hBox.AddChild(exportButton);
                panelContainer.AddChild(hBox);
                rightColumn.AddChild(panelContainer);
            }

            #endregion Import/Export

            #region Save

            {
                var panel = HighlightedContainer();
                _saveButton = new Button
                {
                    Text = localization.GetString("Save"),
                    SizeFlagsHorizontal = SizeFlags.ShrinkCenter
                };
                _saveButton.OnPressed += args =>
                {
                    IsDirty = false;
                    _preferencesManager.UpdateCharacter(Profile.Value, CharacterSlot);
                    OnProfileChanged?.Invoke(Profile.Value);
                };
                panel.AddChild(_saveButton);
                rightColumn.AddChild(panel);
            }

            #endregion Save

            #region Hair

            {
                var panel = HighlightedContainer();
                panel.SizeFlagsHorizontal = SizeFlags.None;
                var hairHBox = new HBoxContainer();

                var hairPicker = new HairStylePicker();
                hairPicker.Populate();

                hairPicker.OnHairStylePicked += newStyle =>
                {
                    if (!Profile.HasValue)
                        return;
                    Profile = Profile.Value.WithCharacterAppearance(
                        Profile.Value.Appearance.WithHairStyleName(newStyle));
                    IsDirty = true;
                };

                hairPicker.OnHairColorPicked += newColor =>
                {
                    if (!Profile.HasValue)
                        return;
                    Profile = Profile?.WithCharacterAppearance(
                        Profile.Value.Appearance.WithHairColor(newColor));
                    IsDirty = true;
                };

                var facialHairPicker = new FacialHairStylePicker();
                facialHairPicker.Populate();

                facialHairPicker.OnHairStylePicked += newStyle =>
                {
                    if (!Profile.HasValue)
                        return;
                    Profile = Profile.Value.WithCharacterAppearance(
                        Profile.Value.Appearance.WithFacialHairStyleName(newStyle));
                    IsDirty = true;
                };

                facialHairPicker.OnHairColorPicked += newColor =>
                {
                    if (!Profile.HasValue)
                        return;
                    Profile = Profile?.WithCharacterAppearance(
                        Profile.Value.Appearance.WithFacialHairColor(newColor));
                    IsDirty = true;
                };

                hairHBox.AddChild(hairPicker);
                hairHBox.AddChild(facialHairPicker);

                panel.AddChild(hairHBox);
                vBox.AddChild(panel);
            }

            #endregion Hair

            UpdateControls();
        }

        private bool IsDirty
        {
            get => _isDirty;
            set
            {
                _isDirty = value;
                UpdateSaveButton();
            }
        }

        private static Control HighlightedContainer()
        {
            return new PanelContainer
            {
                PanelOverride = HighlightedStyle
            };
        }

        private void UpdateSexControls()
        {
            if (Profile.Value.Sex == Sex.Male)
                _sexMaleButton.Pressed = true;
            else
                _sexFemaleButton.Pressed = true;
        }

        private void UpdateSaveButton()
        {
            _saveButton.Disabled = !IsDirty;
        }

        public void UpdateControls()
        {
            if (!Profile.HasValue) return;
            _nameEdit.Text = Profile?.Name;
            UpdateSexControls();
            _ageEdit.Text = Profile?.Age.ToString();
            UpdateSaveButton();
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (!disposing) return;
            //TODO FIXME still need this?
        }

        public event Action<HumanoidCharacterProfile> OnProfileChanged;
    }
}
