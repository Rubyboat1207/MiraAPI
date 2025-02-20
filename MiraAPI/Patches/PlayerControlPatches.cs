﻿using HarmonyLib;
using MiraAPI.Hud;
using MiraAPI.Modifiers;
using MiraAPI.Roles;
using Reactor.Utilities.Extensions;

namespace MiraAPI.Patches;

/// <summary>
/// General patches for the PlayerControl class.
/// </summary>
[HarmonyPatch(typeof(PlayerControl))]
public static class PlayerControlPatches
{
    /// <summary>
    /// Adds the modifier component to the player on start.
    /// </summary>
    [HarmonyPrefix]
    [HarmonyPatch(nameof(PlayerControl.Start))]
    public static void PlayerControlStartPostfix(PlayerControl __instance)
    {
        if (__instance.gameObject.TryGetComponent<ModifierComponent>(out var comp))
        {
            comp.DestroyImmediate();
        }

        __instance.gameObject.AddComponent<ModifierComponent>();
    }

    /// <summary>
    /// Calls the OnDeath method for all active modifiers.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PlayerControl.Die))]
    public static void PlayerControlDiePostfix(PlayerControl __instance)
    {
        var modifiersComponent = __instance.GetComponent<ModifierComponent>();

        if (modifiersComponent)
        {
            modifiersComponent.ActiveModifiers.ForEach(x=>x.OnDeath());
        }
    }

    /// <summary>
    /// FixedUpdate handler for custom roles and custom buttons.
    /// </summary>
    [HarmonyPostfix]
    [HarmonyPatch(nameof(PlayerControl.FixedUpdate))]
    public static void PlayerControlFixedUpdatePostfix(PlayerControl __instance)
    {
        if (__instance.Data?.Role is ICustomRole customRole)
        {
            customRole.PlayerControlFixedUpdate(__instance);
        }

        if (!__instance.AmOwner)
        {
            return;
        }

        foreach (var button in CustomButtonManager.CustomButtons)
        {
            if (__instance.Data?.Role == null)
            {
                continue;
            }

            if (!button.Enabled(__instance.Data?.Role))
            {
                continue;
            }

            button.FixedUpdateHandler(__instance);
        }
    }
}
