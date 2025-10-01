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
[BepInAutoPlugin(id: "com.demojameson.silksong.betterascendantgrip", name: "Better Ascendant's Grip", version: "1.0.0")]
public partial class BetterAscendantGripPlugin : BaseUnityPlugin {
    private static ManualLogSource logger;
    private static ConfigEntry<bool> enabled;
    private Harmony harmony;

    private void Awake() {
        logger = Logger;
        enabled = Config.Bind("General",
            "Stop Falling Immediately",
            true,
            "Whether to enable stop Falling Immediately");
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
}
