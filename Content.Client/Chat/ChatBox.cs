﻿using Content.Client.UserInterface.Stylesheets;
using Content.Shared.Chat;
using Robust.Client.Graphics.Drawing;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Input;
using Robust.Shared.Localization;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Content.Client.Chat
{
    public class ChatBox : MarginContainer
    {
        public delegate void TextSubmitHandler(ChatBox chatBox, string text);

        public delegate void FilterToggledHandler(ChatBox chatBox, BaseButton.ButtonToggledEventArgs e);

        public HistoryLineEdit Input { get; private set; }
        public OutputPanel Contents { get; }

        // Buttons for filtering
        public Button AllButton { get; }
        public Button LocalButton { get; }
        public Button OOCButton { get; }
        public Button AdminButton { get; }
        public Button DeadButton { get;  }

        private readonly OptionButton _channelSelector;

        /// <summary>
        ///     Default formatting string for the ClientChatConsole.
        /// </summary>
        public string DefaultChatFormat { get; set; }

        public bool ReleaseFocusOnEnter { get; set; } = true;

        public bool ClearOnEnter { get; set; } = true;

        public ChatBox()
        {
            /*MarginLeft = -475.0f;
            MarginTop = 10.0f;
            MarginRight = -10.0f;
            MarginBottom = 235.0f;

            AnchorLeft = 1.0f;
            AnchorRight = 1.0f;*/
            MouseFilter = MouseFilterMode.Stop;

            AddChild(new VBoxContainer
            {
                Children =
                {
                    new PanelContainer
                    {
                        PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#25252aaa")},
                        SizeFlagsVertical = SizeFlags.FillExpand,
                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                        Children =
                        {
                            new VBoxContainer
                            {
                                Children =
                                {
                                    new MarginContainer
                                    {
                                        MarginLeftOverride = 4, MarginRightOverride = 4,
                                        SizeFlagsVertical = SizeFlags.FillExpand,
                                        Children =
                                        {
                                            (Contents = new OutputPanel())
                                        }
                                    },
                                    new PanelContainer
                                    {
                                        StyleClasses = { StyleNano.StyleClassChatSubPanel },
                                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                                        Children =
                                        {
                                            new HBoxContainer
                                            {
                                                SizeFlagsHorizontal = SizeFlags.FillExpand,
                                                SeparationOverride = 4,
                                                Children =
                                                {
                                                    (_channelSelector = new OptionButton
                                                    {
                                                        HideTriangle = true,
                                                        StyleClasses = { StyleNano.StyleClassChatFilterOptionButton },
                                                        OptionStyleClasses = { StyleNano.StyleClassChatFilterOptionButton }
                                                    }),
                                                    (Input = new HistoryLineEdit
                                                    {
                                                        SizeFlagsHorizontal = SizeFlags.FillExpand,
                                                        StyleClasses = { StyleNano.StyleClassChatLineEdit }
                                                    })
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    },
                }
            });

            _channelSelector.AddItem("Local", (int) ChatChannel.Local);
            _channelSelector.AddItem("Radio", (int) ChatChannel.Radio);
            _channelSelector.AddItem("OOC", (int) ChatChannel.OOC);

            AllButton = new Button
            {
                Text = Loc.GetString("All"),
                Name = "ALL",
                SizeFlagsHorizontal = SizeFlags.ShrinkEnd | SizeFlags.Expand,
                ToggleMode = true,
            };

            LocalButton = new Button
            {
                Text = Loc.GetString("Local"),
                Name = "Local",
                ToggleMode = true,
            };

            OOCButton = new Button
            {
                Text = Loc.GetString("OOC"),
                Name = "OOC",
                ToggleMode = true,
            };

            AdminButton = new Button
            {
                Text = Loc.GetString("Admin"),
                Name = "Admin",
                ToggleMode = true,
                Visible = false
            };

            DeadButton = new Button
            {
                Text = Loc.GetString("Dead"),
                Name = "Dead",
                ToggleMode = true,
                Visible = false
            };

            AllButton.OnToggled += OnFilterToggled;
            LocalButton.OnToggled += OnFilterToggled;
            OOCButton.OnToggled += OnFilterToggled;
            AdminButton.OnToggled += OnFilterToggled;
            DeadButton.OnToggled += OnFilterToggled;
        }

        protected override void EnteredTree()
        {
            base.EnteredTree();
            _channelSelector.OnItemSelected += OnChannelItemSelected;
            Input.OnKeyBindDown += InputKeyBindDown;
            Input.OnTextEntered += Input_OnTextEntered;
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();
            _channelSelector.OnItemSelected -= OnChannelItemSelected;
            Input.OnKeyBindDown -= InputKeyBindDown;
            Input.OnTextEntered -= Input_OnTextEntered;
        }


        protected override void KeyBindDown(GUIBoundKeyEventArgs args)
        {
            base.KeyBindDown(args);

            if (!args.CanFocus)
            {
                return;
            }

            Input.GrabKeyboardFocus();
        }

        private void InputKeyBindDown(GUIBoundKeyEventArgs args)
        {
            if (args.Function == EngineKeyFunctions.TextReleaseFocus)
            {
                Input.ReleaseKeyboardFocus();
                args.Handle();
                return;
            }
        }

        private void OnChannelItemSelected(OptionButton.ItemSelectedEventArgs args)
        {
            _channelSelector.SelectId(args.Id);

            // TODO: Change the channel
        }

        public event TextSubmitHandler TextSubmitted;

        public event FilterToggledHandler FilterToggled;

        public void AddLine(string message, ChatChannel channel, Color color)
        {
            if (Disposed)
            {
                return;
            }

            var formatted = new FormattedMessage(3);
            formatted.PushColor(color);
            formatted.AddText(message);
            formatted.Pop();
            Contents.AddMessage(formatted);
        }

        private void Input_OnTextEntered(LineEdit.LineEditEventArgs args)
        {
            // We set it there to true so it's set to false by TextSubmitted.Invoke if necessary
            ClearOnEnter = true;

            if (!string.IsNullOrWhiteSpace(args.Text))
            {
                TextSubmitted?.Invoke(this, args.Text);
            }

            if (ClearOnEnter)
            {
                Input.Clear();
            }

            if (ReleaseFocusOnEnter)
            {
                Input.ReleaseKeyboardFocus();
            }
        }

        private void OnFilterToggled(BaseButton.ButtonToggledEventArgs args)
        {
            FilterToggled?.Invoke(this, args);
        }
    }
}
