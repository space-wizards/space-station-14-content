using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Content.Client.GameObjects.Components;
using Content.Client.State;
using Content.Client.UserInterface;
using Content.Client.UserInterface.Stylesheets;
using Content.Client.Utility;
using Content.Shared.GameObjects.EntitySystemMessages;
using Content.Shared.GameObjects.Verbs;
using Content.Shared.GameTicking;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.GameObjects;
using Robust.Client.GameObjects.EntitySystems;
using Robust.Client.Graphics;
using Robust.Client.Graphics.Drawing;
using Robust.Client.Interfaces.GameObjects.Components;
using Robust.Client.Interfaces.Input;
using Robust.Client.Interfaces.ResourceManagement;
using Robust.Client.Interfaces.State;
using Robust.Client.Interfaces.UserInterface;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.Utility;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Input;
using Robust.Shared.Input.Binding;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Timing;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Client.GameObjects.EntitySystems
{
    [UsedImplicitly]
    public sealed class VerbSystem : SharedVerbSystem, IResettingEntitySystem
    {
        private class ContextMenuComparer<T> : IEqualityComparer<(T, IEnumerable<T>)>
        {
            bool IEqualityComparer<(T, IEnumerable<T>)>.Equals((T, IEnumerable<T>) x, (T, IEnumerable<T>) y)
            {
                return x.Item1.Equals(y.Item1) && Enumerable.SequenceEqual(x.Item2.OrderBy(t => t), y.Item2.OrderBy(t => t));
            }

            int IEqualityComparer<(T, IEnumerable<T>)>.GetHashCode((T, IEnumerable<T>) obj)
            {
                var hash = EqualityComparer<T>.Default.GetHashCode(obj.Item1);
                foreach (var element in obj.Item2)
                {
                    hash ^= EqualityComparer<T>.Default.GetHashCode(element);
                }
                return hash;
            }
        }

        [Dependency] private readonly IStateManager _stateManager = default!;
        [Dependency] private readonly IPlayerManager _playerManager = default!;
        [Dependency] private readonly IInputManager _inputManager = default!;
        [Dependency] private readonly IItemSlotManager _itemSlotManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly IUserInterfaceManager _userInterfaceManager = default!;

        private ContextPopup _contextPopup;
        private ContextPopup _contextPopupExt;

        private VerbPopup _currentVerbListRoot;
        private VerbPopup _currentGroupList;

        private EntityUid _currentEntity;

        private bool IsAnyContextMenuOpen => _contextPopup != null || _currentVerbListRoot != null || _contextPopupExt != null;

        private bool _playerCanSeeThroughContainers;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeNetworkEvent<VerbSystemMessages.VerbsResponseMessage>(FillEntityPopup);
            SubscribeNetworkEvent<PlayerContainerVisibilityMessage>(HandleContainerVisibilityMessage);

            IoCManager.InjectDependencies(this);

            CommandBinds.Builder
                .Bind(ContentKeyFunctions.OpenContextMenu,
                    new PointerInputCmdHandler(OnOpenContextMenu))
                .Register<VerbSystem>();
        }

        public override void Shutdown()
        {
            CommandBinds.Unregister<VerbSystem>();
            base.Shutdown();
        }

        public void Reset()
        {
            _playerCanSeeThroughContainers = false;
        }

        private void HandleContainerVisibilityMessage(PlayerContainerVisibilityMessage ev)
        {
            _playerCanSeeThroughContainers = ev.CanSeeThrough;
        }

        public void OpenContextMenu(IEntity entity, ScreenCoordinates screenCoordinates)
        {
            if (_currentVerbListRoot != null)
            {
                CloseVerbMenu();
            }

            _currentEntity = entity.Uid;
            _currentVerbListRoot = new VerbPopup();
            _userInterfaceManager.ModalRoot.AddChild(_currentVerbListRoot);
            _currentVerbListRoot.OnPopupHide += CloseVerbMenu;

            if (!entity.Uid.IsClientSide())
            {

                _currentVerbListRoot.List.AddChild(new Label { Text = Loc.GetString("Waiting on Server...") });
                RaiseNetworkEvent(new VerbSystemMessages.RequestVerbsMessage(_currentEntity));
            }

            var box = UIBox2.FromDimensions(screenCoordinates.Position, (1, 1));
            _currentVerbListRoot.Open(box);
        }

        public bool CanSeeOnContextMenu(IEntity entity)
        {
            if (!entity.TryGetComponent(out SpriteComponent sprite) || !sprite.Visible)
            {
                return false;
            }

            if (entity.GetAllComponents<IShowContextMenu>().Any(s => !s.ShowContextMenu(entity)))
            {
                return false;
            }

            if (!_playerCanSeeThroughContainers &&
                entity.TryGetContainer(out var container) &&
                !container.ShowContents)
            {
                return false;
            }

            return true;
        }

        private bool OnOpenContextMenu(in PointerInputCmdHandler.PointerInputCmdArgs args)
        {
            if (IsAnyContextMenuOpen)
            {
                CloseAllMenus();
                return true;
            }

            if (_stateManager.CurrentState is not GameScreenBase)
            {
                return false;
            }

            var mapCoordinates = args.Coordinates.ToMap(EntityManager);
            var playerEntity = _playerManager.LocalPlayer?.ControlledEntity;

            if (playerEntity == null || !TryGetContextEntities(playerEntity, mapCoordinates, out var entities))
            {
                return false;
            }

            // Objects are grouped based on their current appearance. For example a weapon without a
            // magazine with a sprite layer showing that is different from the same weapon
            // with a magazine in it.
            var entitySpriteStates = new Dictionary<(string, IEnumerable<string>), List<IEntity>>(new ContextMenuComparer<string>());
            foreach (var entity in entities)
            {
                if (!CanSeeOnContextMenu(entity))
                {
                    continue;
                }

                if (entity.Prototype != null && entity.TryGetComponent(out ISpriteComponent sprite))
                {
                    var currentState = (entity.Prototype.ID, sprite.AllLayers.Where(e => e.Visible).Select(s => s.RsiState.Name));
                    if (!entitySpriteStates.ContainsKey(currentState))
                    {
                        entitySpriteStates.Add(currentState, new List<IEntity>() { entity });
                    }
                    else
                    {
                        entitySpriteStates[currentState].Add(entity);
                    }
                }
            }

            if (entitySpriteStates.Count == 0)
            {
                return false;
            }

            var orderedStates = entitySpriteStates.ToList();
            orderedStates.Sort((x, y) => string.CompareOrdinal(x.Value.First().Prototype.Name, y.Value.First().Prototype.Name));

            _contextPopup = new ContextPopup();
            _contextPopup.OnPopupHide += CloseAllMenus;
            var debugEnabled = _userInterfaceManager.DebugMonitors.Visible;

            var contextElements = new List<ContextElement>();
            foreach (var (_, vEntity) in orderedStates)
            {
                if (vEntity.Count > 1)
                {
                    var multiContextElement = new MultiContextElement(this, vEntity, debugEnabled);
                    contextElements.Add(multiContextElement);
                    _contextPopup.AddElement(multiContextElement, true);
                }
                else
                {
                    var singleContextElement = new SingleContextElement(this, vEntity.First(), debugEnabled, true);
                    contextElements.Add(singleContextElement);
                    _contextPopup.AddElement(singleContextElement, true);
                }
            }

            var maxSizeX = contextElements.Max(e => e.Margin.CombinedMinimumSize.X);
            foreach (var element in contextElements)
            {
                element.Margin.CustomMinimumSize = (maxSizeX, element.Margin.CombinedMinimumSize.Y);
            }

            _userInterfaceManager.ModalRoot.AddChild(_contextPopup);

            var size = _contextPopup.List.CombinedMinimumSize;
            var box = UIBox2.FromDimensions(_userInterfaceManager.MousePositionScaled, size);
            _contextPopup.Open(box);

            return true;
        }

        private void OnContextButtonPressed(IEntity entity)
        {
            OpenContextMenu(entity, new ScreenCoordinates(_userInterfaceManager.MousePositionScaled));
        }

        private void FillEntityPopup(VerbSystemMessages.VerbsResponseMessage msg)
        {
            if (_currentEntity != msg.Entity || !EntityManager.TryGetEntity(_currentEntity, out var entity))
            {
                return;
            }

            DebugTools.AssertNotNull(_currentVerbListRoot);

            var buttons = new Dictionary<string, List<ListedVerbData>>();
            var groupIcons = new Dictionary<string, SpriteSpecifier>();

            var vBox = _currentVerbListRoot.List;
            vBox.DisposeAllChildren();

            // Local variable so that scope capture ensures this is the correct value.
            var curEntity = _currentEntity;

            foreach (var data in msg.Verbs)
            {
                var list = buttons.GetOrNew(data.Category);

                if (data.CategoryIcon != null && !groupIcons.ContainsKey(data.Category))
                {
                    groupIcons.Add(data.Category, data.CategoryIcon);
                }

                list.Add(new ListedVerbData(data.Text, !data.Available, data.Key, entity.ToString(), () =>
                {
                    RaiseNetworkEvent(new VerbSystemMessages.UseVerbMessage(curEntity, data.Key));
                    CloseAllMenus();
                }, data.Icon));
            }

            var user = GetUserEntity();
            //Get verbs, component dependent.
            foreach (var (component, verb) in VerbUtility.GetVerbs(entity))
            {
                if (!VerbUtility.VerbAccessChecks(user, entity, verb))
                {
                    continue;
                }

                var verbData = verb.GetData(user, component);

                if (verbData.IsInvisible)
                    continue;

                var list = buttons.GetOrNew(verbData.Category);

                if (verbData.CategoryIcon != null && !groupIcons.ContainsKey(verbData.Category))
                {
                    groupIcons.Add(verbData.Category, verbData.CategoryIcon);
                }

                list.Add(new ListedVerbData(verbData.Text, verbData.IsDisabled, verb.ToString(), entity.ToString(),
                    () => verb.Activate(user, component), verbData.Icon));
            }

            //Get global verbs. Visible for all entities regardless of their components.
            foreach (var globalVerb in VerbUtility.GetGlobalVerbs(Assembly.GetExecutingAssembly()))
            {
                if (!VerbUtility.VerbAccessChecks(user, entity, globalVerb))
                {
                    continue;
                }

                var verbData = globalVerb.GetData(user, entity);

                if (verbData.IsInvisible)
                    continue;

                var list = buttons.GetOrNew(verbData.Category);

                if (verbData.CategoryIcon != null && !groupIcons.ContainsKey(verbData.Category))
                {
                    groupIcons.Add(verbData.Category, verbData.CategoryIcon);
                }

                list.Add(new ListedVerbData(verbData.Text, verbData.IsDisabled, globalVerb.ToString(),
                    entity.ToString(),
                    () => globalVerb.Activate(user, entity), verbData.Icon));
            }

            if (buttons.Count > 0)
            {
                var first = true;
                foreach (var (category, verbs) in buttons)
                {
                    if (string.IsNullOrEmpty(category))
                        continue;

                    if (!first)
                    {
                        vBox.AddChild(new PanelContainer
                        {
                            CustomMinimumSize = (0, 2),
                            PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#333") }
                        });
                    }

                    first = false;

                    groupIcons.TryGetValue(category, out var icon);

                    vBox.AddChild(CreateCategoryButton(category, verbs, icon));
                }

                if (buttons.ContainsKey(""))
                {
                    buttons[""].Sort((a, b) => string.Compare(a.Text, b.Text, StringComparison.CurrentCulture));

                    foreach (var verb in buttons[""])
                    {
                        if (!first)
                        {
                            vBox.AddChild(new PanelContainer
                            {
                                CustomMinimumSize = (0, 2),
                                PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#333") }
                            });
                        }

                        first = false;

                        vBox.AddChild(CreateVerbButton(verb));
                    }
                }
            }
            else
            {
                var panel = new PanelContainer();
                panel.AddChild(new Label { Text = Loc.GetString("No verbs!") });
                vBox.AddChild(panel);
            }
        }

        private VerbButton CreateVerbButton(ListedVerbData data)
        {
            var button = new VerbButton
            {
                Text = Loc.GetString(data.Text),
                Disabled = data.Disabled
            };

            if (data.Icon != null)
            {
                button.Icon = data.Icon.Frame0();
            }

            if (!data.Disabled)
            {
                button.OnPressed += _ =>
                {
                    CloseAllMenus();
                    try
                    {
                        data.Action.Invoke();
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS("verb", "Exception in verb {0} on {1}:\n{2}", data.VerbName, data.OwnerName, e);
                    }
                };
            }

            return button;
        }

        private Control CreateCategoryButton(string text, List<ListedVerbData> verbButtons, SpriteSpecifier icon)
        {
            verbButtons.Sort((a, b) => string.Compare(a.Text, b.Text, StringComparison.CurrentCulture));

            return new VerbGroupButton(this, verbButtons, icon)
            {
                Text = Loc.GetString(text),
            };
        }

        private void CloseVerbMenu()
        {
            _currentVerbListRoot?.Dispose();
            _currentVerbListRoot = null;
            _currentEntity = EntityUid.Invalid;
        }

        private void CloseContextPopup()
        {
            _contextPopup?.Dispose();
            _contextPopup = null;
        }

        private void CloseAllMenus()
        {
            CloseVerbMenu();
            CloseContextPopup();
            CloseContextPopupExt();
            CloseGroupMenu();
        }

        private void CloseGroupMenu()
        {
            _currentGroupList?.Dispose();
            _currentGroupList = null;
        }

        private void CloseContextPopupExt()
        {
            _contextPopupExt?.Dispose();
            _contextPopupExt = null;
        }

        private IEntity GetUserEntity()
        {
            return _playerManager.LocalPlayer.ControlledEntity;
        }

        private abstract class ContextElement : Control
        {
            protected readonly VerbSystem VSystem;
            protected readonly bool ShowUid;
            protected readonly bool IsRoot;
            public MarginContainer Margin { get; set; }

            public ContextElement(VerbSystem system, bool showUid, bool isRoot)
            {
                VSystem = system;
                ShowUid = showUid;
                IsRoot = isRoot;

                MouseFilter = MouseFilterMode.Stop;

                Margin = new MarginContainer
                {
                    MarginLeftOverride = 4,
                    MarginRightOverride = 4,
                };
            }

            protected override void Draw(DrawingHandleScreen handle)
            {
                base.Draw(handle);

                if (UserInterfaceManager.CurrentlyHovered == this)
                {
                    handle.DrawRect(PixelSizeBox, Color.DarkSlateGray);
                }
            }

            protected void ContextKeyBindDown(GUIBoundKeyEventArgs args, IEntity entity, bool isSingle = true)
            {
                if (args.Function == ContentKeyFunctions.OpenContextMenu && isSingle)
                {
                    VSystem.OnContextButtonPressed(entity);
                    return;
                }

                if (args.Function == ContentKeyFunctions.ExamineEntity && isSingle)
                {
                    Get<ExamineSystem>().DoExamine(entity);
                    return;
                }

                if (args.Function == EngineKeyFunctions.Use ||
                    args.Function == ContentKeyFunctions.Point ||
                    args.Function == ContentKeyFunctions.TryPullObject ||
                    args.Function == ContentKeyFunctions.MovePulledObject)
                {
                    // TODO: Remove an entity from the menu when it is deleted
                    if (entity.Deleted)
                    {
                        VSystem.CloseAllMenus();
                        return;
                    }

                    var inputSys = VSystem.EntitySystemManager.GetEntitySystem<InputSystem>();

                    var func = args.Function;
                    var funcId = VSystem._inputManager.NetworkBindMap.KeyFunctionID(args.Function);

                    var message = new FullInputCmdMessage(VSystem._gameTiming.CurTick, VSystem._gameTiming.TickFraction, funcId,
                        BoundKeyState.Down, entity.Transform.Coordinates, args.PointerLocation, entity.Uid);

                    // client side command handlers will always be sent the local player session.
                    var session = VSystem._playerManager.LocalPlayer.Session;
                    inputSys.HandleInputCommand(session, func, message);

                    VSystem.CloseAllMenus();
                    return;
                }

                if (VSystem._itemSlotManager.OnButtonPressed(args, entity))
                {
                    VSystem.CloseAllMenus();
                }
            }
        }

        private sealed class SingleContextElement : ContextElement
        {
            private readonly IEntity _entity;
            private InteractionOutlineComponent _outline;
            private SpriteComponent _sprite;

            private bool _drawOutline = false;
            private int _oldDrawDepth;

            public SingleContextElement(VerbSystem system, IEntity entity, bool showUid, bool isRoot) : base(system, showUid, isRoot)
            {
                _entity = entity;

                var text = Loc.GetString(ShowUid ? $"{entity.Name} ({entity.Uid})" : entity.Name);
                Margin.AddChild(new Label { Text = text });

                var control = new HBoxContainer
                {
                    SeparationOverride = 6,
                    Children =
                    {
                        new LayoutContainer
                        {
                            Children =
                            {
                                new SpriteView { Sprite = _entity.GetComponent<ISpriteComponent>() },
                            }
                        },
                        Margin
                    }
                };
                AddChild(control);

                if (_entity.TryGetComponent(out _sprite))
                {
                    _oldDrawDepth = _sprite.DrawDepth;
                }
                _outline = _entity.GetComponentOrNull<InteractionOutlineComponent>();
            }

            protected override void MouseEntered()
            {
                base.MouseEntered();

                if (VSystem._currentGroupList != null)
                {
                    VSystem.CloseGroupMenu();
                }

                if (IsRoot && VSystem._contextPopupExt != null)
                {
                    VSystem.CloseContextPopupExt();
                }

                if (_entity != null && !_entity.Deleted)
                {
                    var localPlayer = VSystem._playerManager.LocalPlayer;
                    if (localPlayer != null && localPlayer.ControlledEntity != null)
                    {
                        _sprite.DrawDepth = (int) Shared.GameObjects.DrawDepth.HighlightedItems;
                        _outline?.OnMouseEnter(localPlayer.InRangeUnobstructed(_entity, ignoreInsideBlocker: true));
                    }
                }
                _drawOutline = true;
            }

            protected override void Draw(DrawingHandleScreen handle)
            {
                base.Draw(handle);
                if (_entity != null && !_entity.Deleted && _drawOutline)
                {
                    var localPlayer = VSystem._playerManager.LocalPlayer;
                    if (localPlayer != null && localPlayer.ControlledEntity != null)
                    {
                        _outline?.OnMouseEnter(localPlayer.InRangeUnobstructed(_entity, ignoreInsideBlocker: true));
                    }
                }
            }

            protected override void MouseExited()
            {
                base.MouseExited();
                if (_entity != null && !_entity.Deleted)
                {
                    _sprite.DrawDepth = _oldDrawDepth;
                    _outline?.OnMouseLeave();
                }
                _drawOutline = false;
            }

            protected override void KeyBindDown(GUIBoundKeyEventArgs args)
            {
                base.KeyBindDown(args);
                ContextKeyBindDown(args, _entity);
            }
        }

        private sealed class MultiContextElement : ContextElement
        {
            private readonly List<IEntity> _entities;
            private CancellationTokenSource _openCancel;
            private static readonly TimeSpan HoverDelay = TimeSpan.FromSeconds(0.2);

            public MultiContextElement(VerbSystem system, IEnumerable<IEntity> entities, bool showUid) : base(system, showUid, true)
            {
                _entities = new(entities);

                var entity = _entities.First();

                var labelCount = new Label
                {
                    Text = Loc.GetString(_entities.Count.ToString()),
                    StyleClasses = { StyleNano.StyleClassContextMenuCount },
                };
                LayoutContainer.SetAnchorPreset(labelCount, LayoutContainer.LayoutPreset.BottomRight);
                LayoutContainer.SetGrowHorizontal(labelCount, LayoutContainer.GrowDirection.Begin);
                LayoutContainer.SetGrowVertical(labelCount, LayoutContainer.GrowDirection.Begin);

                var text = showUid ? $"{entity.Name} (---)" : entity.Name;
                Margin.AddChild(new Label { Text = Loc.GetString(text) } );

                var control = new HBoxContainer
                {
                    SeparationOverride = 6,
                    Children =
                    {
                        new LayoutContainer
                        {
                            Children =
                            {
                                new SpriteView { Sprite = entity.GetComponent<ISpriteComponent>() },
                                labelCount
                            }
                        },
                        Margin,
                        new TextureRect
                        {
                            Texture = IoCManager.Resolve<IResourceCache>().GetTexture("/Textures/Interface/VerbIcons/group.svg.96dpi.png"),
                            Stretch = TextureRect.StretchMode.KeepCentered,
                        }
                    }
                };
                AddChild(control);
            }

            protected override void KeyBindDown(GUIBoundKeyEventArgs args)
            {
                base.KeyBindDown(args);
                // TODO: Get the next entity if the first one is unavailable (deleted / taken / out of range).
                // TODO: Edit similar entities at the same time ?
                ContextKeyBindDown(args, _entities.First(), false);
            }

            protected override void MouseEntered()
            {
                base.MouseEntered();

                _openCancel = new CancellationTokenSource();

                Timer.Spawn(HoverDelay, () =>
                {
                    if (VSystem._currentGroupList != null)
                    {
                        VSystem.CloseGroupMenu();
                    }

                    if (VSystem._contextPopupExt != null)
                    {
                        VSystem.CloseContextPopupExt();
                    }

                    VSystem._contextPopupExt = new ContextPopup();
                    foreach (var entity in _entities)
                    {
                        if (!entity.Deleted)
                        {
                            VSystem._contextPopupExt.AddElement(new SingleContextElement(VSystem, entity, ShowUid, false), true);
                        }
                    }

                    UserInterfaceManager.ModalRoot.AddChild(VSystem._contextPopupExt);

                    var size = VSystem._contextPopupExt.List.CombinedMinimumSize;
                    VSystem._contextPopupExt.Open(UIBox2.FromDimensions(GlobalPosition + (Width, 0), size));
                }, _openCancel.Token);
            }

            protected override void MouseExited()
            {
                base.MouseExited();

                _openCancel?.Cancel();
                _openCancel = null;
            }
        }

        private sealed class ContextPopup : Popup
        {
            private const int MaxItemsBeforeScroll = 10;
            private int _contextElementCount = 0;

            public VBoxContainer List { get; }

            public ContextPopup()
            {
                AddChild(new ScrollContainer
                {
                    HScrollEnabled = false,
                    Children = { new PanelContainer
                    {
                        Children = { (List = new VBoxContainer()) },
                        PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#111E") }
                    }}
                });
            }

            private void AddSeparation()
            {
                List.AddChild(new PanelContainer
                {
                    CustomMinimumSize = (0, 2),
                    PanelOverride = new StyleBoxFlat { BackgroundColor = Color.FromHex("#333") }
                });
            }

            public void AddElement(SingleContextElement e, bool addSeparation)
            {
                List.AddChild(e);
                if (addSeparation)
                {
                    AddSeparation();
                }
                _contextElementCount++;
            }

            public void AddElement(MultiContextElement e, bool addSeparation)
            {
                List.AddChild(e);
                if (addSeparation)
                {
                    AddSeparation();
                }
                _contextElementCount++;
            }

            protected override Vector2 CalculateMinimumSize()
            {
                var size = base.CalculateMinimumSize();
                size.Y = _contextElementCount > MaxItemsBeforeScroll ? MaxItemsBeforeScroll * 32 + MaxItemsBeforeScroll * 2 : size.Y;
                return size;
            }
        }

        private sealed class VerbPopup : Popup
        {
            public VBoxContainer List { get; }

            public VerbPopup()
            {
                AddChild(new PanelContainer
                {
                    Children = {(List = new VBoxContainer())},
                    PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#111E")}
                });
            }
        }

        private sealed class VerbButton : BaseButton
        {
            private readonly Label _label;
            private readonly TextureRect _icon;

            public Texture Icon
            {
                get => _icon.Texture;
                set => _icon.Texture = value;
            }

            public string Text
            {
                get => _label.Text;
                set => _label.Text = value;
            }

            public VerbButton()
            {
                AddChild(new HBoxContainer
                {
                    Children =
                    {
                        (_icon = new TextureRect
                        {
                            CustomMinimumSize = (32, 32),
                            Stretch = TextureRect.StretchMode.KeepCentered
                        }),
                        (_label = new Label()),
                        // Padding
                        new Control {CustomMinimumSize = (8, 0)}
                    }
                });
            }

            protected override void Draw(DrawingHandleScreen handle)
            {
                base.Draw(handle);

                if (DrawMode == DrawModeEnum.Hover)
                {
                    handle.DrawRect(PixelSizeBox, Color.DarkSlateGray);
                }
            }
        }

        private sealed class VerbGroupButton : Control
        {
            private static readonly TimeSpan HoverDelay = TimeSpan.FromSeconds(0.2);

            private readonly VerbSystem _system;
            public List<ListedVerbData> VerbButtons { get; }
            private readonly Label _label;
            private readonly TextureRect _icon;

            private CancellationTokenSource _openCancel;

            public string Text
            {
                get => _label.Text;
                set => _label.Text = value;
            }

            public Texture Icon
            {
                get => _icon.Texture;
                set => _icon.Texture = value;
            }

            public VerbGroupButton(VerbSystem system, List<ListedVerbData> verbButtons, SpriteSpecifier icon)
            {
                _system = system;
                VerbButtons = verbButtons;

                MouseFilter = MouseFilterMode.Stop;

                AddChild(new HBoxContainer
                {
                    Children =
                    {
                        (_icon = new TextureRect
                        {
                            CustomMinimumSize = (32, 32),
                            Stretch = TextureRect.StretchMode.KeepCentered
                        }),

                        (_label = new Label
                        {
                            SizeFlagsHorizontal = SizeFlags.FillExpand
                        }),

                        // Padding
                        new Control {CustomMinimumSize = (8, 0)},

                        new TextureRect
                        {
                            Texture = IoCManager.Resolve<IResourceCache>()
                                .GetTexture("/Textures/Interface/VerbIcons/group.svg.96dpi.png"),
                            Stretch = TextureRect.StretchMode.KeepCentered,
                        }
                    }
                });

                if (icon != null)
                {
                    _icon.Texture = icon.Frame0();
                }
            }

            protected override void Draw(DrawingHandleScreen handle)
            {
                base.Draw(handle);

                if (this == UserInterfaceManager.CurrentlyHovered)
                {
                    handle.DrawRect(PixelSizeBox, Color.DarkSlateGray);
                }
            }

            protected override void MouseEntered()
            {
                base.MouseEntered();

                _openCancel = new CancellationTokenSource();

                Timer.Spawn(HoverDelay, () =>
                {
                    if (_system._currentGroupList != null)
                    {
                        _system.CloseGroupMenu();
                    }

                    var popup = _system._currentGroupList = new VerbPopup();

                    var first = true;
                    foreach (var verb in VerbButtons)
                    {
                        if (!first)
                        {
                            popup.List.AddChild(new PanelContainer
                            {
                                CustomMinimumSize = (0, 2),
                                PanelOverride = new StyleBoxFlat {BackgroundColor = Color.FromHex("#333")}
                            });
                        }

                        first = false;

                        popup.List.AddChild(_system.CreateVerbButton(verb));
                    }

                    UserInterfaceManager.ModalRoot.AddChild(popup);
                    popup.Open(UIBox2.FromDimensions(GlobalPosition + (Width, 0), (1, 1)), GlobalPosition);
                }, _openCancel.Token);
            }

            protected override void MouseExited()
            {
                base.MouseExited();

                _openCancel?.Cancel();
                _openCancel = null;
            }
        }

        private sealed class ListedVerbData
        {
            public string Text { get; }
            public bool Disabled { get; }
            public string VerbName { get; }
            public string OwnerName { get; }
            public SpriteSpecifier Icon { get; }
            public Action Action { get; }

            public ListedVerbData(string text, bool disabled, string verbName, string ownerName,
                Action action, SpriteSpecifier icon)
            {
                Text = text;
                Disabled = disabled;
                VerbName = verbName;
                OwnerName = ownerName;
                Action = action;
                Icon = icon;
            }
        }
    }
}
