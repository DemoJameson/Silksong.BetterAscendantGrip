using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Diagnostics;
using System.Reflection;

namespace Silksong.BetterAscendantGrip;

[HarmonyPatch]
[BepInAutoPlugin(id: "com.demojameson.silksong.betterascendantgrip", name: "Better Ascendant's Grip")]
public partial class BetterAscendantGripPlugin : BaseUnityPlugin {
    private static ManualLogSource logger;
    private static ConfigEntry<bool> enabled;
    private static ConfigEntry<bool> holdDownSlide;
    private Harmony harmony;

    private void Awake() {
        logger = Logger;
        enabled = Config.Bind(
            "General",
            "Stop Falling Immediately",
            true,
            "Whether to enable stop Falling Immediately");
        holdDownSlide = Config.Bind(
            "General",
            "Hold Down to Slide",
            true,
            "Whether to enable hold down to slide");
        harmony = Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly());
    }

    private void OnDestroy() {
        harmony.UnpatchSelf();
    }

    [HarmonyPatch(typeof(HeroController), nameof(HeroController.AffectedByGravity))]
    [HarmonyPrefix]
    private static void PrefixAffectedByGravity(HeroController __instance, ref bool gravityApplies) {
        if (enabled.Value && __instance.cState.wallClinging && !gravityApplies) {
            __instance.rb2d.linearVelocityY = Math.Max(0, __instance.rb2d.linearVelocityY);
        }
    }

    [HarmonyPatch(typeof(HeroController), nameof(HeroController.Update))]
    [HarmonyILManipulator]
    private static void ILUpdate(ILContext context) {
        var cursor = new ILCursor(context);
        // call class ToolItem GlobalSettings.Gameplay::get_WallClingTool()
        // callvirt instance bool ToolItemManager/ToolStatus::get_IsEquipped()
        if (cursor.TryGotoNext(MoveType.After,
                ins => ins.OpCode == OpCodes.Call && ins.Operand.ToString().EndsWith("get_WallClingTool()"))) {
            if (cursor.TryGotoNext(MoveType.After,
                    ins => ins.OpCode == OpCodes.Callvirt && ins.Operand.ToString().EndsWith("get_IsEquipped()"))) {
                cursor.Emit(OpCodes.Ldarg_0).EmitDelegate<Func<bool, HeroController, bool>>(SupportPressDownSlide);
            }
        }
    }

    private static bool SupportPressDownSlide(bool wallClingToolEquipped, HeroController heroController) {
        if (holdDownSlide.Value) {
            return wallClingToolEquipped && !heroController.inputHandler.inputActions.Down.IsPressed;
        } else {
            return wallClingToolEquipped;
        }
    }
}
