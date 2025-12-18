using CoreSystems;
using CoreSystems.Platform;
using CoreSystems.Support;
using Sandbox.ModAPI;
using VRage.Input;
using VRageMath;

namespace WeaponCore.Data.Scripts.CoreSystems.Ui
{
    internal class UiInput
    {
        internal int PreviousWheel;
        internal int CurrentWheel;
        internal int ShiftTime;
        internal int MouseMenuTime;
        internal int ReloadTime;
        internal bool MouseButtonPressed;
        internal bool InputChanged;
        internal bool MouseButtonLeftWasPressed;
        internal bool MouseButtonLeftNewPressed;
        internal bool MouseButtonLeftReleased;

        internal bool MouseButtonMenuWasPressed;
        internal bool MouseButtonMenuNewPressed;
        internal bool MouseButtonMenuReleased;

        internal bool MouseButtonRightNewPressed;
        internal bool MouseButtonRightReleased;
        internal bool MouseButtonRightWasPressed;

        internal bool ReloadKeyPressed;
        internal bool ReloadKeyReleased;

        internal bool IronSights;
        internal bool IronLock;
        internal bool WasInMenu;
        internal bool WheelForward;
        internal bool WheelBackward;
        internal bool CycleNextKeyPressed;
        internal bool CycleNextKeyReleased;
        internal bool CyclePrevKeyPressed;
        internal bool CyclePrevKeyReleased;
        internal bool ShiftReleased;
        internal bool ShiftPressed;
        internal bool LongShift;
        internal bool AltPressed;
        internal bool ControlKeyPressed;
        internal bool ActionKeyPressed;
        internal bool InfoKeyPressed;
        internal bool ControlKeyReleased;
        internal bool ActionKeyReleased;
        internal bool InfoKeyReleased;
        internal bool BlackListActive1;
        internal bool CtrlPressed;
        internal bool CtrlReleased;
        internal bool AnyKeyPressed;
        internal bool KeyPrevPressed;
        internal bool UiKeyPressed;
        internal bool UiKeyWasPressed;
        internal bool PlayerCamera;
        internal bool FirstPersonView;
        internal bool CameraBlockView;
        internal bool TurretBlockView;
        internal long CameraChannelId;
        internal bool PlayerWeapon;
        internal bool Debug = true;
        internal bool MouseShootWasOn;
        internal bool MouseShootOn;
        internal LineD AimRay;
        private uint _lastInputUpdate;
        internal readonly InputStateData ClientInputState;
        internal MyKeys ControlKey;
        internal MyKeys ActionKey;
        internal MyKeys InfoKey;
        internal MyKeys CycleNextKey;
        internal MyKeys CyclePrevKey;
        internal MyKeys ReloadKey;

        internal MyMouseButtonsEnum MouseButtonMenu;

        internal UiInput()
        {
            ClientInputState = new InputStateData();
        }

        internal void UpdateInputState()
        {
            var s = Session.I;
            WheelForward = false;
            WheelBackward = false;
            IronSights = false;
            IronLock = false;
            AimRay = new LineD();
            CycleNextKeyPressed = false;
            CyclePrevKeyPressed = false;

            if (!s.InGridAiBlock) s.UpdateLocalAiAndCockpit();

            if (s.InGridAiBlock && !s.InMenu)
            {
                var ai = s.TrackingAi;
                var gamePadRT = MyAPIGateway.Input.IsJoystickAxisPressed(MyJoystickAxesEnum.ZRight);
                var gamePadLTRel = MyAPIGateway.Input.IsNewJoystickAxisReleased(MyJoystickAxesEnum.ZLeft);
                var gamePadLT = MyAPIGateway.Input.IsJoystickAxisPressed(MyJoystickAxesEnum.ZLeft);
                var gamePadBumpers = MyAPIGateway.Input.IsJoystickButtonPressed(MyJoystickButtonsEnum.J05) && MyAPIGateway.Input.IsJoystickButtonPressed(MyJoystickButtonsEnum.J06);

                MouseButtonPressed = MyAPIGateway.Input.IsAnyMousePressed();
                MouseButtonLeftNewPressed = MyAPIGateway.Input.IsNewLeftMousePressed();
                MouseButtonLeftReleased = MyAPIGateway.Input.IsNewLeftMouseReleased();
                MouseButtonLeftWasPressed = ClientInputState.MouseButtonLeft;

                MouseButtonRightNewPressed = MyAPIGateway.Input.IsNewRightMousePressed() || gamePadLT;
                MouseButtonRightReleased = MyAPIGateway.Input.IsNewRightMouseReleased() || gamePadLTRel;
                MouseButtonRightWasPressed = ClientInputState.MouseButtonRight;

                MouseButtonMenuNewPressed = MyAPIGateway.Input.IsNewMiddleMousePressed();
                MouseButtonMenuReleased = MyAPIGateway.Input.IsNewMiddleMouseReleased();
                MouseButtonMenuWasPressed = ClientInputState.MouseButtonMenu;

                WasInMenu = ClientInputState.InMenu;


                ClientInputState.InMenu = s.InMenu;
                PlayerWeapon = ai.AiType == Ai.AiTypes.Player;

                IronSights = PlayerWeapon && ai.OnlyWeaponComp.Rifle.GunBase.HasIronSightsActive;
                IronLock = IronSights && ai.SmartHandheld;
                if (PlayerWeapon && s.GunnerBlackList)
                {
                    ReloadKeyPressed = MyAPIGateway.Input.IsKeyPress(ReloadKey);
                    ReloadKeyReleased = MyAPIGateway.Input.IsNewKeyReleased(ReloadKey);
                    ReloadTime = ReloadKeyPressed ? ++ReloadTime : 0;

                    if (ReloadTime == 60)
                        ai.OnlyWeaponComp.CycleHandAmmo();
                    else if (ReloadTime < 60 && ReloadKeyReleased)
                    {
                        ai.OnlyWeaponComp.ForceReload();
                    }


                    ShiftReleased = MyAPIGateway.Input.IsNewKeyReleased(MyKeys.LeftShift);
                    ShiftPressed = MyAPIGateway.Input.IsKeyPress(MyKeys.LeftShift);
                }   

                if (MouseButtonMenuWasPressed)
                {
                    if (++MouseMenuTime == 90 && IronLock)
                        UpdateNonBlockControlMode(ai);
                }
                else
                    MouseMenuTime = 0;

                if (MouseButtonPressed || gamePadRT || gamePadLT)
                {
                    ClientInputState.MouseButtonLeft = MyAPIGateway.Input.IsMousePressed(MyMouseButtonsEnum.Left) || gamePadRT;
                    ClientInputState.MouseButtonMenu = MyAPIGateway.Input.IsMousePressed(MouseButtonMenu);
                    ClientInputState.MouseButtonRight = MyAPIGateway.Input.IsMousePressed(MyMouseButtonsEnum.Right) || gamePadLT;
                }
                else
                {
                    ClientInputState.MouseButtonLeft = false;
                    ClientInputState.MouseButtonMenu = false;
                    ClientInputState.MouseButtonRight = false;
                }

                if (s.MpActive)
                {
                    var shootButtonActive = ClientInputState.MouseButtonLeft || ClientInputState.MouseButtonRight || gamePadRT;

                    MouseShootWasOn = MouseShootOn;
                    if (( s.Tick - _lastInputUpdate >= 29) && shootButtonActive && !MouseShootOn)
                    {
                        _lastInputUpdate = s.Tick;
                        MouseShootOn = true;
                    }
                    else if (MouseShootOn && !shootButtonActive)
                        MouseShootOn = false;

                    InputChanged = MouseShootOn != MouseShootWasOn || WasInMenu != ClientInputState.InMenu;
                }

                ShiftReleased = MyAPIGateway.Input.IsNewKeyReleased(MyKeys.LeftShift);
                ShiftPressed = MyAPIGateway.Input.IsKeyPress(MyKeys.LeftShift);
                ControlKeyReleased = MyAPIGateway.Input.IsNewKeyReleased(ControlKey);

                if (ShiftPressed)
                {
                    ShiftTime++;
                    LongShift = ShiftTime > 59;
                }
                else
                {
                    if (LongShift) ShiftReleased = false;
                    ShiftTime = 0;
                    LongShift = false;
                }

                AltPressed = MyAPIGateway.Input.IsAnyAltKeyPressed() || gamePadBumpers;
                CtrlPressed = MyAPIGateway.Input.IsKeyPress(MyKeys.Control);
                CtrlReleased = MyAPIGateway.Input.IsNewKeyReleased(MyKeys.Control);
                KeyPrevPressed = AnyKeyPressed;
                AnyKeyPressed = MyAPIGateway.Input.IsAnyKeyPress();
                UiKeyWasPressed = UiKeyPressed;
                UiKeyPressed = CtrlPressed || AltPressed || ShiftPressed;
                PlayerCamera = MyAPIGateway.Session.IsCameraControlledObject;
                var cameraController = MyAPIGateway.Session.CameraController;
                FirstPersonView = PlayerCamera && cameraController.IsInFirstPersonView;
                TurretBlockView = cameraController is IMyLargeTurretBase;
                CameraBlockView = !PlayerCamera && !FirstPersonView && s.ActiveCameraBlock != null && s.ActiveCameraBlock.IsActive && s.ActiveCameraBlock.IsWorking;
                if (CameraBlockView && s.ActiveCameraBlock != null)
                    CameraChannelId = s.CameraChannelMappings[s.ActiveCameraBlock];
                else CameraChannelId = 0;

                if ((!UiKeyPressed && !UiKeyWasPressed) || !AltPressed && CtrlPressed && !FirstPersonView)
                {
                    PreviousWheel = MyAPIGateway.Input.PreviousMouseScrollWheelValue();
                    CurrentWheel = MyAPIGateway.Input.MouseScrollWheelValue();
                }


            }
            else if (!s.InMenu)
            {
                CtrlPressed = MyAPIGateway.Input.IsKeyPress(MyKeys.Control);
                ControlKeyPressed = MyAPIGateway.Input.IsKeyPress(ControlKey);
                CameraChannelId = 0;
                MouseMenuTime = 0;
                if (CtrlPressed && ControlKeyPressed && GetAimRay(s, out AimRay) && Debug)
                {
                    DsDebugDraw.DrawLine(AimRay, Color.Red, 0.1f);
                }
            }

            if (!s.InMenu && s.InGridAiBlock)
            {
                ActionKeyReleased = MyAPIGateway.Input.IsNewKeyReleased(ActionKey);
                ActionKeyPressed = MyAPIGateway.Input.IsKeyPress(ActionKey);
                var altCheck = InfoKey == MyKeys.Decimal;
                InfoKeyReleased =  MyAPIGateway.Input.IsNewKeyReleased(InfoKey) || altCheck && MyAPIGateway.Input.IsNewKeyReleased(MyKeys.Delete);
                if (ActionKeyPressed || InfoKeyReleased)
                {
                    if (!BlackListActive1)
                        BlackList1(true);

                    if (ActionKeyPressed && s.CanChangeHud)
                    {
                        var evenTicks = s.Tick % 2 == 0;
                        if (evenTicks)
                        {

                            if (MyAPIGateway.Input.IsKeyPress(MyKeys.Up))
                            {
                                s.Settings.ClientConfig.HudPos.Y += 0.01f;
                                s.Settings.VersionControl.UpdateClientCfgFile();
                            }
                            else if (MyAPIGateway.Input.IsKeyPress(MyKeys.Down))
                            {
                                s.Settings.ClientConfig.HudPos.Y -= 0.01f;
                                s.Settings.VersionControl.UpdateClientCfgFile();
                            }
                            else if (MyAPIGateway.Input.IsKeyPress(MyKeys.Left))
                            {
                                s.Settings.ClientConfig.HudPos.X -= 0.01f;
                                s.Settings.VersionControl.UpdateClientCfgFile();
                            }
                            else if (MyAPIGateway.Input.IsKeyPress(MyKeys.Right))
                            {
                                s.Settings.ClientConfig.HudPos.X += 0.01f;
                                s.Settings.VersionControl.UpdateClientCfgFile();
                            }

                        }

                        if (s.Tick10)
                        {
                            if (MyAPIGateway.Input.IsKeyPress(MyKeys.Add))
                            {
                                s.HudUi.NeedsUpdate = true;
                                s.Settings.ClientConfig.HudScale = MathHelper.Clamp(s.Settings.ClientConfig.HudScale + 0.01f, 0.1f, 10f);
                                s.Settings.VersionControl.UpdateClientCfgFile();
                            }
                            else if (MyAPIGateway.Input.IsKeyPress(MyKeys.Subtract))
                            {
                                s.HudUi.NeedsUpdate = true;
                                s.Settings.ClientConfig.HudScale = MathHelper.Clamp(s.Settings.ClientConfig.HudScale - 0.01f, 0.1f, 10f);
                                s.Settings.VersionControl.UpdateClientCfgFile();
                            }
                        }
                    }

                    if (InfoKeyReleased)
                    {
                        var set = s.Settings;
                        if (set.ClientConfig.MinimalHud && !set.ClientConfig.AdvancedMode || set.ClientConfig.AdvancedMode || s.MinimalHudOverride)
                        {
                            if (set.ClientConfig.MinimalHud && !set.ClientConfig.HideReload)
                            {
                                s.HudUi.NeedsUpdate = true;
                                set.ClientConfig.HideReload = true;
                            }
                            else
                            {
                                set.ClientConfig.MinimalHud = !set.ClientConfig.MinimalHud;
                                set.ClientConfig.HideReload = false;
                            }
                            s.ShowLocalNotify($"WC Top Hud: {(set.ClientConfig.MinimalHud ? "Minimal" : "Full")}", 2000, "Red", true);
                            s.ShowLocalNotify($"WC Right Info Panel: {(set.ClientConfig.HideReload ? "Off" : "On")}", 2000, "Red");
                            set.VersionControl.UpdateClientCfgFile();
                        }
                        else if (!set.ClientConfig.AdvancedMode)
                        {
                            s.MinimalHudOverride = true;
                        }
                    }


                }
            }
            else
            {
                ActionKeyPressed = false;
                ActionKeyReleased = false;
                InfoKeyPressed = false;
                InfoKeyReleased = false;
            }

            if (s.MpActive && !s.InGridAiBlock)
            {
                if (ClientInputState.InMenu || ClientInputState.MouseButtonRight || ClientInputState.MouseButtonMenu || ClientInputState.MouseButtonRight)
                {
                    ClientInputState.InMenu = false;
                    ClientInputState.MouseButtonLeft = false;
                    ClientInputState.MouseButtonMenu = false;
                    ClientInputState.MouseButtonRight = false;
                    InputChanged = true;
                }
            }

            if (CurrentWheel != PreviousWheel && CurrentWheel > PreviousWheel)
                WheelForward = true;
            else if (s.UiInput.CurrentWheel != s.UiInput.PreviousWheel)
                WheelBackward = true;

            if (MyAPIGateway.Input.IsKeyPress(CycleNextKey) && CycleNextKeyReleased)
            {
                CycleNextKeyPressed = true;
                CycleNextKeyReleased = false;
            }
            if (MyAPIGateway.Input.IsNewKeyReleased(CycleNextKey)) CycleNextKeyReleased = true;

            if (MyAPIGateway.Input.IsKeyPress(CyclePrevKey) && CyclePrevKeyReleased)
            {
                CyclePrevKeyPressed = true;
                CyclePrevKeyReleased = false;
            }
            if (MyAPIGateway.Input.IsNewKeyReleased(CyclePrevKey)) CyclePrevKeyReleased = true;

            if (MyAPIGateway.Input.IsJoystickButtonPressed(MyJoystickButtonsEnum.J05) && MyAPIGateway.Input.IsJoystickButtonPressed(MyJoystickButtonsEnum.J06))
            {
                if (MyAPIGateway.Input.IsJoystickButtonPressed(MyJoystickButtonsEnum.J10) && CycleNextKeyReleased)
                {
                    CycleNextKeyPressed = true;
                    CycleNextKeyReleased = false;
                }
                if (MyAPIGateway.Input.IsNewJoystickButtonReleased(MyJoystickButtonsEnum.J10)) CycleNextKeyReleased = true;

                if (MyAPIGateway.Input.IsJoystickButtonPressed(MyJoystickButtonsEnum.J09) && CyclePrevKeyReleased)
                {
                    CyclePrevKeyPressed = true;
                    CyclePrevKeyReleased = false;
                }
                if (MyAPIGateway.Input.IsNewJoystickButtonReleased(MyJoystickButtonsEnum.J09)) CyclePrevKeyReleased = true;
            }

            if (!ActionKeyPressed && BlackListActive1)
                BlackList1(false);
        }

        internal bool GetAimRay(Session s, out LineD ray)
        {
            if (s.LocalCharacter != null)
            {
                ray = new LineD(s.PlayerPos, s.PlayerPos + (s.LocalCharacter.WorldMatrix.Forward * 1000000));
                return true;
            }
            ray = new LineD();
            return false;
        }

        private void BlackList1(bool activate)
        {
            var upKey = MyAPIGateway.Input.GetControl(MyKeys.Up);
            var downKey = MyAPIGateway.Input.GetControl(MyKeys.Down);
            var leftKey = MyAPIGateway.Input.GetControl(MyKeys.Left);
            var rightkey = MyAPIGateway.Input.GetControl(MyKeys.Right);
            var addKey = MyAPIGateway.Input.GetControl(MyKeys.Add);
            var subKey = MyAPIGateway.Input.GetControl(MyKeys.Subtract);
            var actionKey = MyAPIGateway.Input.GetControl(MyKeys.NumPad0);
            var controlKey = MyAPIGateway.Input.GetControl(ControlKey);
            var detailKey = MyAPIGateway.Input.GetControl(MyKeys.Decimal);

            if (upKey != null)
            {
                Session.I.CustomBlackListRequestBecauseKeenIsBrainDead(upKey.GetGameControlEnum().String, Session.I.PlayerId, !activate);
            }
            if (downKey != null)
            {
                Session.I.CustomBlackListRequestBecauseKeenIsBrainDead(downKey.GetGameControlEnum().String, Session.I.PlayerId, !activate);
            }
            if (leftKey != null)
            {
                Session.I.CustomBlackListRequestBecauseKeenIsBrainDead(leftKey.GetGameControlEnum().String, Session.I.PlayerId, !activate);
            }
            if (rightkey != null)
            {
                Session.I.CustomBlackListRequestBecauseKeenIsBrainDead(rightkey.GetGameControlEnum().String, Session.I.PlayerId, !activate);
            }
            if (addKey != null)
            {
                Session.I.CustomBlackListRequestBecauseKeenIsBrainDead(addKey.GetGameControlEnum().String, Session.I.PlayerId, !activate);
            }
            if (subKey != null)
            {
                Session.I.CustomBlackListRequestBecauseKeenIsBrainDead(subKey.GetGameControlEnum().String, Session.I.PlayerId, !activate);
            }

            if (actionKey != null)
            {
                Session.I.CustomBlackListRequestBecauseKeenIsBrainDead(actionKey.GetGameControlEnum().String, Session.I.PlayerId, !activate);
            }

            if (controlKey != null)
            {
                Session.I.CustomBlackListRequestBecauseKeenIsBrainDead(controlKey.GetGameControlEnum().String, Session.I.PlayerId, !activate);
            }

            if (detailKey != null)
            {
                Session.I.CustomBlackListRequestBecauseKeenIsBrainDead(detailKey.GetGameControlEnum().String, Session.I.PlayerId, !activate);
            }
            BlackListActive1 = activate;
        }

        private void UpdateNonBlockControlMode(Ai ai)
        {
            var notPainter = ai.OnlyWeaponComp.Data.Repo.Values.Set.Overrides.Control != ProtoWeaponOverrides.ControlModes.Painter;
            var newValue = notPainter ? 2 : 0;

            Weapon.WeaponComponent.RequestSetValue(ai.OnlyWeaponComp, "ControlModes", newValue, Session.I.PlayerId);
        }
    }
}
