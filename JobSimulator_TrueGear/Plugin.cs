using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using OwlchemyVR;
using OwlchemyVR2;
using System.Threading;
using UnityEngine.Events;
using MyTrueGear;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using UnityEngine;

namespace JobSimulator_TrueGear
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BasePlugin
    {
        internal static new ManualLogSource Log;

        private static TrueGearMod _TrueGear = null;

        private static UnityEngine.Vector3 leftHandPos = new UnityEngine.Vector3();
        private static UnityEngine.Vector3 rightHandPos = new UnityEngine.Vector3();

        private static float lastAmt = 0;

        private static bool canEat = true;
        private static bool canDrink = true;
        private static bool canPullLever = true;
        private static bool canFireExtinguisher = true;
        private static bool canLeftHandDispensing = true;
        private static bool canRightHandDispensing = true;

        public override void Load()
        {
            // Plugin startup logic
            Log = base.Log;

            Harmony.CreateAndPatchAll(typeof(Plugin));

            _TrueGear = new TrueGearMod();

            _TrueGear.Play("FireExtinguisher");

            Log.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
        }

        private static bool isLeftRomanCandleInHand = false;
        private static bool isRightRomanCandleInHand = false;

        private static bool isLeftFuseInHand = false;
        private static bool isRightFuseInHand = false;

        [HarmonyPostfix, HarmonyPatch(typeof(InteractionHandController), "GrabGrabbable")]
        private static void InteractionHandController_GrabGrabbable_Postfix(InteractionHandController __instance, GrabbableItem grabbableItem)
        {
            if (__instance.handController.SteamVRController.Handedness == Handedness.Left)
            {
                if (grabbableItem.name.Contains("FireworkRomanCandle"))
                {
                    if (!grabbableItem.name.Contains("Fuse"))
                    {
                        isLeftRomanCandleInHand = true;
                    }
                    else
                    {
                        isLeftFuseInHand = true;
                    }
                }
                Log.LogInfo("----------------------------------------------------");
                Log.LogInfo("LeftHandPickupItem");
                _TrueGear.Play("LeftHandPickupItem");

            }
            else
            {
                if (grabbableItem.name.Contains("FireworkRomanCandle") && !grabbableItem.name.Contains("Fuse"))
                {
                    if (!grabbableItem.name.Contains("Fuse"))
                    {
                        isRightRomanCandleInHand = true;
                    }
                    else
                    {
                        isRightFuseInHand = true;
                    }
                }                
                Log.LogInfo("----------------------------------------------------");
                Log.LogInfo("RightHandPickupItem");
                _TrueGear.Play("RightHandPickupItem");

            }

            Log.LogInfo(__instance.handController.SteamVRController.Handedness);
            Log.LogInfo(grabbableItem.name);
            Log.LogInfo(grabbableItem.transform.parent.name);
            Log.LogInfo(grabbableItem.transform.parent.parent.name);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(InteractionHandController), "ReleaseCurrGrabbable")]
        private static void InteractionHandController_ReleaseCurrGrabbable_Postfix(InteractionHandController __instance)
        {
            if (__instance.handController.SteamVRController.Handedness == Handedness.Left)
            {
                isLeftRomanCandleInHand = false;
                isLeftFuseInHand = false;
                Log.LogInfo("----------------------------------------------------");
                Log.LogInfo("LeftHandReleaseItem");
                _TrueGear.Play("LeftHandReleaseItem");
                leftSparklingName = null;
                Log.LogInfo("StopLeftHandSparkling");
                _TrueGear.StopLeftHandSparkling();
            }
            else
            {
                isRightRomanCandleInHand = false;
                isRightFuseInHand = false;
                Log.LogInfo("----------------------------------------------------");
                Log.LogInfo("RightHandReleaseItem");
                _TrueGear.Play("RightHandReleaseItem");
                rightSparklingName = null;
                Log.LogInfo("StopRightHandSparkling");
                _TrueGear.StopRightHandSparkling();
            }
            Log.LogInfo(__instance.handController.SteamVRController.Handedness);
        }

       
        [HarmonyPostfix, HarmonyPatch(typeof(HeadController), "DoBiteSoundForWorldItem")]
        private static void HeadController_DoBiteSoundForWorldItem_Postfix(HeadController __instance)
        {
            if (!canEat)
            {
                return;
            }
            canEat = false;
            new Il2CppSystem.Threading.Timer((Il2CppSystem.Threading.TimerCallback)EatTimerCallBack, null, 200, Timeout.Infinite);
            Log.LogInfo("----------------------------------------------------");
            Log.LogInfo("Eat");
            _TrueGear.Play("Eat");
        }
        private static void EatTimerCallBack(object o)
        {
            canEat = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HeadController), "ItemPouredIntoMouth")]
        private static void HeadController_ItemPouredIntoMouth_Postfix(HeadController __instance, ParticleCollectionZone zone, WorldItemData itemData)
        {
            if (!canDrink)
            {
                return;
            }
            canDrink = false;
            new Il2CppSystem.Threading.Timer((Il2CppSystem.Threading.TimerCallback)DrinkTimerCallBack, null, 200, Timeout.Infinite);
            Log.LogInfo("----------------------------------------------------");
            Log.LogInfo("Drink");
            Log.LogInfo(itemData.name);
            Log.LogInfo(__instance.isBlowing);
            _TrueGear.Play("Drink");
        }
        private static void DrinkTimerCallBack(object o)
        {
            canDrink = true;
        }


        [HarmonyPostfix, HarmonyPatch(typeof(InteractionHandController), "Update")]
        private static void InteractionHandController_Update_Postfix(InteractionHandController __instance)
        {
            if (__instance.handController.SteamVRController.Handedness == Handedness.Left)
            {
                leftHandPos = __instance.gameObject.transform.position;
            }
            else if (__instance.handController.SteamVRController.Handedness == Handedness.Right)
            {
                rightHandPos = __instance.gameObject.transform.position;
            }
        }


        [HarmonyPostfix, HarmonyPatch(typeof(MechanicalPushButtonController), "Awake")]
        private static void MechanicalPushButtonController_Awake_Postfix(MechanicalPushButtonController __instance)
        {
            __instance.OnButtonPressed.RemoveListener((UnityAction<MechanicalPushButtonController>)OnButtonPressed);
            __instance.OnButtonPressed.AddListener((UnityAction<MechanicalPushButtonController>)OnButtonPressed);
        }

        private static void OnButtonPressed(MechanicalPushButtonController instance)
        {
            float leftDis = UnityEngine.Vector3.Distance(leftHandPos, instance.transform.position);
            float rightDis = UnityEngine.Vector3.Distance(rightHandPos, instance.transform.position);
            if (leftDis < rightDis && leftDis < 0.19f)
            {
                Log.LogInfo("----------------------------------------------------");
                Log.LogInfo("LeftHandPressButton");
                _TrueGear.Play("LeftHandPressButton");
            }
            else if (rightDis < 0.19f)
            {
                Log.LogInfo("----------------------------------------------------");
                Log.LogInfo("RightHandPressButton");
                _TrueGear.Play("RightHandPressButton");
            }
            Log.LogInfo(leftDis);
            Log.LogInfo(rightDis);
        }

        private static float lastLever = 0;

        [HarmonyPostfix, HarmonyPatch(typeof(LeverController), "Update")]
        private static void LeverController_Update_Postfix(LeverController __instance)
        {
            try
            {
                if (__instance.hinge.grabbable == null)
                {
                    return;
                }
               
                if (lastLever == __instance.hinge.angle || __instance.hinge.angle != 65)
                {
                    lastLever = __instance.hinge.angle;
                    return;
                }
                if (__instance.hinge.grabbable.currInteractableHand.HandController.SteamVRController.Handedness == Handedness.Left)
                {
                    Log.LogInfo("----------------------------------------------------");
                    Log.LogInfo("LeftHandPullLever6");
                    _TrueGear.Play("LeftHandPullLever6");
                }
                else
                {
                    Log.LogInfo("----------------------------------------------------");
                    Log.LogInfo("RightHandPullLever6");
                    _TrueGear.Play("RightHandPullLever6");
                }
                lastLever = __instance.hinge.angle;
            }
            catch
            {

            }
        }
        private static void PullLeverTimerCallBack(object o)
        {
            canPullLever = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BlenderBodyController), "UpdateSpinSpeed")]
        private static void BlenderBodyController_UpdateSpinSpeed_Postfix(BlenderBodyController __instance, float speed, float powerRatio)
        {
            try
            {
                if (!canPullLever)
                {
                    return;
                }
                canPullLever = false;
                new Il2CppSystem.Threading.Timer((Il2CppSystem.Threading.TimerCallback)PullLeverTimerCallBack, null, 90, Timeout.Infinite);
                int power = (int)(powerRatio * 6.5);
                if (__instance.connectedBase.leverGrab.currInteractableHand.HandController.SteamVRController.Handedness == Handedness.Left)
                {
                    Log.LogInfo("----------------------------------------------------");
                    Log.LogInfo("LeftHandPullLever" + power);
                    _TrueGear.Play("LeftHandPullLever" + power);
                }
                else
                {
                    Log.LogInfo("----------------------------------------------------");
                    Log.LogInfo("RightHandPullLever" + power);
                    _TrueGear.Play("RightHandPullLever" + power);
                }
                Log.LogInfo(speed);
                Log.LogInfo(powerRatio);

               
            }
            catch
            {

            }
        }



        [HarmonyPrefix, HarmonyPatch(typeof(ToasterController), "TrayLocked")]
        private static void ToasterController_TrayLocked_Prefix(ToasterController __instance, GrabbableSlider slider, bool isInitial)
        {
            Log.LogInfo("----------------------------------------------------");
            Log.LogInfo("TrayLocked");
            Log.LogInfo(isInitial);
            if (UnityEngine.Vector3.Distance(leftHandPos, slider.transform.position) < UnityEngine.Vector3.Distance(rightHandPos, slider.transform.position))
            {
                Log.LogInfo("LeftHandToasterTrayLocked");
                _TrueGear.Play("LeftHandToasterTrayLocked");
            }
            else
            {
                Log.LogInfo("RightHandToasterTrayLocked");
                _TrueGear.Play("RightHandToasterTrayLocked");
            }
            Log.LogInfo(UnityEngine.Vector3.Distance(leftHandPos, slider.transform.position));
            Log.LogInfo(UnityEngine.Vector3.Distance(rightHandPos, slider.transform.position));
        }


        [HarmonyPostfix, HarmonyPatch(typeof(WashStationController), "WaterToggle")]
        private static void WashStationController_WaterToggle_Postfix(WashStationController __instance)
        {
            Log.LogInfo("----------------------------------------------------");
            if (UnityEngine.Vector3.Distance(leftHandPos, __instance.waterHinge.transform.position) < UnityEngine.Vector3.Distance(rightHandPos, __instance.waterHinge.transform.position))
            {
                Log.LogInfo("LeftHandSwitchFaucet");
                _TrueGear.Play("LeftHandSwitchFaucet");
            }
            else
            {
                Log.LogInfo("RightHandSwitchFaucet");
                _TrueGear.Play("RightHandSwitchFaucet");
            }
            Log.LogInfo(__instance.waterHinge.normalizedAngleToActivateAt);
            Log.LogInfo(__instance.waterHinge.normalizedAngleToResetAt);
            Log.LogInfo(UnityEngine.Vector3.Distance(leftHandPos, __instance.waterHinge.transform.position));
            Log.LogInfo(UnityEngine.Vector3.Distance(rightHandPos, __instance.waterHinge.transform.position));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(WashStationController), "DispenseSoap")]
        private static void WashStationController_DispenseSoap_Postfix(WashStationController __instance)
        {
            Log.LogInfo("----------------------------------------------------");
            if (UnityEngine.Vector3.Distance(leftHandPos, __instance.soapDispenser.tipLocation.position) < UnityEngine.Vector3.Distance(rightHandPos, __instance.soapDispenser.tipLocation.position))
            {
                Log.LogInfo("LeftHandDispenseSoap");
                _TrueGear.Play("LeftHandDispenseSoap");
            }
            else
            {
                Log.LogInfo("RightHandDispenseSoap");
                _TrueGear.Play("RightHandDispenseSoap");
            }
            Log.LogInfo(__instance.soapDispenser.canPour);
            Log.LogInfo(UnityEngine.Vector3.Distance(leftHandPos, __instance.soapDispenser.tipLocation.position));
            Log.LogInfo(UnityEngine.Vector3.Distance(rightHandPos, __instance.soapDispenser.tipLocation.position));
        }


        [HarmonyPostfix, HarmonyPatch(typeof(ButtonController), "Awake")]
        private static void ButtonController_Awake_Postfix(ButtonController __instance)
        {
            __instance.OnAction.RemoveListener((UnityAction)(() => ButtonAction(__instance)));
            __instance.OnAction.AddListener((UnityAction)(() => ButtonAction(__instance)));
        }

        private static void ButtonAction(ButtonController instance)
        {
            Log.LogInfo("----------------------------------------------------");
            Log.LogInfo("ButtonAction");
            float leftDis = UnityEngine.Vector3.Distance(leftHandPos, instance.transform.position);
            float rightDis = UnityEngine.Vector3.Distance(rightHandPos, instance.transform.position);
            if (leftDis < rightDis && leftDis < 0.19f)
            {
                Log.LogInfo("LeftHandPressButton");
                _TrueGear.Play("LeftHandPressButton");
            }
            else if (rightDis < 0.19f)
            {
                Log.LogInfo("RightHandPressButton");
                _TrueGear.Play("RightHandPressButton");
            }
            Log.LogInfo(UnityEngine.Vector3.Distance(leftHandPos, instance.transform.position));
            Log.LogInfo(UnityEngine.Vector3.Distance(rightHandPos, instance.transform.position));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BoomboxController), "Ejectbutton_OnButtonPushed")]
        private static void BoomboxController_Ejectbutton_OnButtonPushed_Postfix(BoomboxController __instance, PushableButton obj)
        {
            Log.LogInfo("----------------------------------------------------");
            Log.LogInfo("Ejectbutton_OnButtonPushed");
            float leftDis = UnityEngine.Vector3.Distance(leftHandPos, obj.transform.position);
            float rightDis = UnityEngine.Vector3.Distance(rightHandPos, obj.transform.position);
            if (leftDis < rightDis && leftDis < 0.19f)
            {
                Log.LogInfo("LeftHandPressButton");
                _TrueGear.Play("LeftHandPressButton");
            }
            else if (rightDis < 0.19f)
            {
                Log.LogInfo("RightHandPressButton");
                _TrueGear.Play("RightHandPressButton");
            }
            Log.LogInfo(UnityEngine.Vector3.Distance(leftHandPos, obj.transform.position));
            Log.LogInfo(UnityEngine.Vector3.Distance(rightHandPos, obj.transform.position));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BoomboxController), "Playbutton_OnButtonPushed")]
        private static void BoomboxController_Playbutton_OnButtonPushed_Postfix(BoomboxController __instance, PushableButton obj)
        {
            Log.LogInfo("----------------------------------------------------");
            Log.LogInfo("Playbutton_OnButtonPushed");

            float leftDis = UnityEngine.Vector3.Distance(leftHandPos, obj.transform.position);
            float rightDis = UnityEngine.Vector3.Distance(rightHandPos, obj.transform.position);

            if (leftDis < rightDis && leftDis < 0.19f)
            {
                Log.LogInfo("LeftHandPressButton");
                _TrueGear.Play("LeftHandPressButton");
            }
            else if (rightDis < 0.19f)
            {
                Log.LogInfo("RightHandPressButton");
                _TrueGear.Play("RightHandPressButton");
            }
            Log.LogInfo(UnityEngine.Vector3.Distance(leftHandPos, obj.transform.position));
            Log.LogInfo(UnityEngine.Vector3.Distance(rightHandPos, obj.transform.position));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(BoomboxController), "RefreshVolume")]
        private static void BoomboxController_RefreshVolume_Postfix(BoomboxController __instance)
        {
            if (__instance.volumeKnob.GetCurrentAxisValue() == 0)
            {
                return;
            }
            Log.LogInfo("----------------------------------------------------");
            Log.LogInfo("RefreshVolume");
            Log.LogInfo(__instance.volumeKnob.GetCurrentAxisValue());
            Log.LogInfo(__instance.volumeKnob.Grabbable.currInteractableHand.handController.SteamVRController.Handedness);

        }

        [HarmonyPostfix, HarmonyPatch(typeof(CashierScanner), "ItemScanned")]
        private static void CashierScanner_ItemScanned_Postfix(CashierScanner __instance)
        {
            Log.LogInfo("----------------------------------------------------");
            Log.LogInfo("ItemScanned");
            _TrueGear.Play("ItemScanned");
        }


        [HarmonyPostfix, HarmonyPatch(typeof(StaplerController), "Click")]
        private static void StaplerController_Click_Postfix(StaplerController __instance)
        {
            Log.LogInfo("----------------------------------------------------");
            Log.LogInfo("Click");
            if (UnityEngine.Vector3.Distance(leftHandPos, __instance.topTransform.position) < UnityEngine.Vector3.Distance(rightHandPos, __instance.topTransform.position))
            {
                Log.LogInfo("LeftHandStaplerClick");
                _TrueGear.Play("LeftHandStaplerClick");
            }
            else
            {
                Log.LogInfo("RightHandStaplerClick");
                _TrueGear.Play("RightHandStaplerClick");
            }
            Log.LogInfo(UnityEngine.Vector3.Distance(leftHandPos, __instance.topTransform.position));
            Log.LogInfo(UnityEngine.Vector3.Distance(rightHandPos, __instance.topTransform.position));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GarageChainController), "ModifyYankDistance")]
        private static void GarageChainController_ModifyYankDistance_Postfix(GarageChainController __instance, float amt)
        {
            try
            {
                if (Math.Abs(lastAmt) < 0.01 && Math.Abs(amt) > 0.01)
                {
                    if (__instance.GetGrabbedControlSlider().grabbable.currInteractableHand.handController.SteamVRController.Handedness == Handedness.Left)
                    {
                        Log.LogInfo("----------------------------------------------------");
                        Log.LogInfo("LeftHandPullGarageChain");
                        _TrueGear.Play("LeftHandPullGarageChain");
                    }
                    else
                    {
                        Log.LogInfo("----------------------------------------------------");
                        Log.LogInfo("RightHandPullGarageChain");
                        _TrueGear.Play("RightHandPullGarageChain");
                    }
                }
                Log.LogInfo(amt);
                Log.LogInfo(__instance.GetGrabbedControlSlider().grabbable.currInteractableHand.handController.SteamVRController.Handedness);
                lastAmt = amt;
            }
            catch { }
        }

        [HarmonyPostfix, HarmonyPatch(typeof(FireExtinguisherController), "Update")]
        private static void FireExtinguisherController_Update_Postfix(FireExtinguisherController __instance)
        {
            if (__instance.isFiring)
            {
                if (!canFireExtinguisher)
                {
                    return;
                }
                canFireExtinguisher = false;
                new Il2CppSystem.Threading.Timer((Il2CppSystem.Threading.TimerCallback)FireExtinguisherTimerCallBack, null, 90, Timeout.Infinite);
                Log.LogInfo("----------------------------------------------------");
                Log.LogInfo("FireExtinguisher");
                _TrueGear.Play("FireExtinguisher");
            }
        }
        private static void FireExtinguisherTimerCallBack(object o)
        {
            canFireExtinguisher = true;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(GravityDispensingItem), "DoDispensing")]
        private static void GravityDispensingItem_DoDispensing_Postfix(GravityDispensingItem __instance)
        {
            if (__instance.transform.parent.name == "Sink")
            {
                return;
            }
            if (UnityEngine.Vector3.Distance(leftHandPos, __instance.tipLocation.position) < UnityEngine.Vector3.Distance(rightHandPos, __instance.tipLocation.position))
            {
                if (!canLeftHandDispensing)
                {
                    return;
                }
                canLeftHandDispensing = false;
                new Il2CppSystem.Threading.Timer((Il2CppSystem.Threading.TimerCallback)LeftHandDispensingTimeCallBack, null, 200, Timeout.Infinite);
                Log.LogInfo("----------------------------------------------------");
                Log.LogInfo("LeftHandGravityDispensing");
                _TrueGear.Play("LeftHandGravityDispensing");
            }
            else
            {
                if (!canRightHandDispensing)
                {
                    return;
                }
                canRightHandDispensing = false;
                new Il2CppSystem.Threading.Timer((Il2CppSystem.Threading.TimerCallback)RightHandDispensingTimeCallBack, null, 200, Timeout.Infinite);
                Log.LogInfo("----------------------------------------------------");
                Log.LogInfo("RightHandGravityDispensing");
                _TrueGear.Play("RightHandGravityDispensing");
            }
            Log.LogInfo(__instance.name);
            Log.LogInfo(__instance.transform.parent.name);
            Log.LogInfo(UnityEngine.Vector3.Distance(leftHandPos, __instance.tipLocation.position));
            Log.LogInfo(UnityEngine.Vector3.Distance(rightHandPos, __instance.tipLocation.position));
        }
        private static void LeftHandDispensingTimeCallBack(object o)
        {
            canLeftHandDispensing = true;
        }

        private static void RightHandDispensingTimeCallBack(object o)
        {
            canRightHandDispensing = true;
        }


        [HarmonyPostfix, HarmonyPatch(typeof(MouseController), "ClickDown")]
        private static void MouseController_ClickDown_Postfix(MouseController __instance)
        {
            if (__instance.grabbableItem.currInteractableHand.handController.SteamVRController.Handedness == Handedness.Left)
            {
                Log.LogInfo("----------------------------------------------------");
                Log.LogInfo("LeftHandMouseClickDown");
                _TrueGear.Play("LeftHandMouseClickDown");
            }
            else
            {
                Log.LogInfo("----------------------------------------------------");
                Log.LogInfo("RightHandMouseClickDown");
                _TrueGear.Play("RightHandMouseClickDown");
            }
            Log.LogInfo(__instance.grabbableItem.currInteractableHand.handController.SteamVRController.Handedness);
        }

        private static float lastStamperLever = 0;
        [HarmonyPostfix, HarmonyPatch(typeof(StamperController), "LateUpdate")]
        private static void StamperController_LateUpdate_Postfix(StamperController __instance)
        {
            try
            {
                if (lastStamperLever >= 80 || __instance.hinge.angle < 80)
                {
                    lastStamperLever = __instance.hinge.angle;
                    return;
                }
                if (__instance.leverGrabbable.currInteractableHand.HandController.SteamVRController.Handedness == Handedness.Left)
                {
                    Log.LogInfo("----------------------------------------------------");
                    Log.LogInfo("LeftHandPullLever6");
                    _TrueGear.Play("LeftHandPullLever6");
                }
                else
                {
                    Log.LogInfo("----------------------------------------------------");
                    Log.LogInfo("RightHandPullLever6");
                    _TrueGear.Play("RightHandPullLever6");
                }
                lastStamperLever = __instance.hinge.angle;
                Log.LogInfo("----------------------------------------------------");
                Log.LogInfo("LateUpdate");
                Log.LogInfo(__instance.hinge.angle);
                Log.LogInfo(__instance.leverGrabbable.currInteractableHand.HandController.SteamVRController.Handedness);
            }
            catch { }
        }

        private static int lastScratcher = -1;
        [HarmonyPostfix, HarmonyPatch(typeof(ScratcherController), "OnCollisionStay")]
        private static void ScratcherController_OnCollisionStay_Postfix(ScratcherController __instance, Collision c)
        {
            if (!c.gameObject.name.Contains("Coin") || __instance.panelsScratched == 0 || __instance.panelsScratched == 10)
            {
                return;
            }
            if (__instance.panelsScratched == lastScratcher)
            {
                lastScratcher = __instance.panelsScratched;
                return;
            }
            lastScratcher = __instance.panelsScratched;
            Log.LogInfo("----------------------------------------------------");
            float leftDis = UnityEngine.Vector3.Distance(leftHandPos, __instance.transform.position);
            float rightDis = UnityEngine.Vector3.Distance(rightHandPos, __instance.transform.position);
            if (leftDis < rightDis && leftDis < 0.19f)
            {
                Log.LogInfo("LeftHandScratchTicket");
                _TrueGear.Play("LeftHandScratchTicket");
            }
            else if(rightDis < 0.19f)
            {
                Log.LogInfo("RightHandScratchTicket");
                _TrueGear.Play("RightHandScratchTicket");
            }
            Log.LogInfo(UnityEngine.Vector3.Distance(leftHandPos, __instance.transform.position));
            Log.LogInfo(UnityEngine.Vector3.Distance(rightHandPos, __instance.transform.position));
        }

        [HarmonyPostfix, HarmonyPatch(typeof(RomanCandleController), "OnFuseDetach")]
        private static void RomanCandleController_OnFuseDetach_Postfix(RomanCandleController __instance, AttachableObject obj)
        {
            if (isLeftFuseInHand && isRightFuseInHand)
            {
                if (UnityEngine.Vector3.Distance(leftHandPos, obj.transform.position) < UnityEngine.Vector3.Distance(rightHandPos, obj.transform.position))
                {
                    Log.LogInfo("----------------------------------------------------");
                    Log.LogInfo("LeftHandFuseDetach");
                    _TrueGear.Play("LeftHandFuseDetach");
                }
                else
                {
                    Log.LogInfo("----------------------------------------------------");
                    Log.LogInfo("RightHandFuseDetach");
                    _TrueGear.Play("RightHandFuseDetach");
                }
            }
            else if (isLeftFuseInHand)
            {
                Log.LogInfo("----------------------------------------------------");
                Log.LogInfo("LeftHandFuseDetach");
                _TrueGear.Play("LeftHandFuseDetach");
            }
            else if (isRightFuseInHand)
            {
                Log.LogInfo("----------------------------------------------------");
                Log.LogInfo("RightHandFuseDetach");
                _TrueGear.Play("RightHandFuseDetach");
            }
        }


        [HarmonyPostfix, HarmonyPatch(typeof(RomanCandleBurstController), "Start")]
        private static void RomanCandleBurstController_Start_Postfix(RomanCandleBurstController __instance)
        {
            if (isLeftRomanCandleInHand && isRightRomanCandleInHand)
            {
                if (UnityEngine.Vector3.Distance(leftHandPos, __instance.transform.position) < UnityEngine.Vector3.Distance(rightHandPos, __instance.transform.position))
                {
                    Log.LogInfo("----------------------------------------------------");
                    Log.LogInfo("LeftHandRomanCandleBurst");
                    _TrueGear.Play("LeftHandRomanCandleBurst");
                }
                else
                {
                    Log.LogInfo("----------------------------------------------------");
                    Log.LogInfo("RightHandRomanCandleBurst");
                    _TrueGear.Play("RightHandRomanCandleBurst");
                }
                Log.LogInfo(UnityEngine.Vector3.Distance(leftHandPos, __instance.transform.position));
                Log.LogInfo(UnityEngine.Vector3.Distance(rightHandPos, __instance.transform.position));
            }
            else if (isLeftRomanCandleInHand)
            {
                Log.LogInfo("----------------------------------------------------");
                Log.LogInfo("LeftHandRomanCandleBurst");
                _TrueGear.Play("LeftHandRomanCandleBurst");
            }
            else if (isRightRomanCandleInHand)
            {
                Log.LogInfo("----------------------------------------------------");
                Log.LogInfo("RightHandRomanCandleBurst");
                _TrueGear.Play("RightHandRomanCandleBurst");
            }
        }


        private static string leftSparklingName = null;
        private static string rightSparklingName = null;
        [HarmonyPostfix, HarmonyPatch(typeof(SparklerController), "BeginSparkling")]
        private static void SparklerController_BeginSparkling_Postfix(SparklerController __instance, GrabbableItem item)
        {
            if (item.currInteractableHand.handController.SteamVRController.Handedness == Handedness.Left)
            {
                leftSparklingName = __instance.name;
                Log.LogInfo("----------------------------------------------------");
                Log.LogInfo("StartLeftHandSparkling");
                _TrueGear.StartLeftHandSparkling();
            }
            else if (item.currInteractableHand.handController.SteamVRController.Handedness == Handedness.Right)
            {
                rightSparklingName = __instance.name;
                Log.LogInfo("----------------------------------------------------");
                Log.LogInfo("StartRightHandSparkling");
                _TrueGear.StartRightHandSparkling();
            }

            Log.LogInfo(item.currInteractableHand.handController.SteamVRController.Handedness);
            Log.LogInfo(__instance.name);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(SparklerController), "StopHaptics")]
        private static void SparklerController_StopHaptics_Prefix(SparklerController __instance)
        {
            if (__instance.name == leftSparklingName)
            {
                leftSparklingName = null;
                Log.LogInfo("----------------------------------------------------");
                Log.LogInfo("StopLeftHandSparkling");
                _TrueGear.StopLeftHandSparkling();
            }
            else if (__instance.name == rightSparklingName)
            {
                rightSparklingName = null;
                Log.LogInfo("----------------------------------------------------");
                Log.LogInfo("StopRightHandSparkling");
                _TrueGear.StopRightHandSparkling();
            }
        }

    }
}
