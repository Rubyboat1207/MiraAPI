﻿using AmongUs.GameOptions;
using HarmonyLib;
using Il2CppSystem;
using Il2CppSystem.Collections.Generic;
using MiraAPI.PluginLoading;
using MiraAPI.Roles;
using MiraAPI.Utilities.Assets;
using System.Linq;
using TMPro;
using UnityEngine;
using Object = UnityEngine.Object;

namespace MiraAPI.Patches.Roles;

[HarmonyPatch]
public static class TaskAdderPatch
{
    private static TaskFolder _rolesFolder;
    private static readonly System.Collections.Generic.Dictionary<string, string> ModsFolders = new();
    private static Scroller _scroller;

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TaskAdderGame), nameof(TaskAdderGame.Begin))]
    public static void AddRolesFolder(TaskAdderGame __instance)
    {
        GameObject inner = new("Inner");
        inner.transform.SetParent(__instance.TaskParent.transform, false);

        _scroller = __instance.TaskParent.gameObject.AddComponent<Scroller>();
        _scroller.allowX = false;
        _scroller.allowY = true;
        _scroller.Inner = inner.transform;

        GameObject hitbox = new("Hitbox")
        {
            layer = 5,
        };
        hitbox.transform.SetParent(__instance.TaskParent.transform, false);
        hitbox.transform.localScale = new Vector3(7.5f, 6.5f, 1);
        hitbox.transform.localPosition = new Vector3(2.8f, -2.2f, 0);

        var mask = hitbox.AddComponent<SpriteMask>();
        mask.sprite = MiraAssets.NextButton.LoadAsset();
        mask.alphaCutoff = 0.0f;

        var collider = hitbox.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(1f, 1f);
        collider.enabled = true;

        _scroller.ClickMask = collider;

        __instance.TaskPrefab.GetComponent<PassiveButton>().ClickMask = collider;
        __instance.RoleButton.GetComponent<PassiveButton>().ClickMask = collider;
        __instance.RootFolderPrefab.GetComponent<PassiveButton>().ClickMask = collider;
        __instance.RootFolderPrefab.gameObject.SetActive(false);

        __instance.TaskParent = inner.transform;

        _rolesFolder = Object.Instantiate(__instance.RootFolderPrefab, _scroller.Inner);
        _rolesFolder = Object.Instantiate(__instance.RootFolderPrefab, _scroller.Inner);
        _rolesFolder.gameObject.SetActive(false);
        _rolesFolder.FolderName = "Roles";
        _rolesFolder.name = "RolesFolder";

        foreach (var plugin in MiraPluginManager.Instance.RegisteredPlugins())
        {
            var newFolder = Object.Instantiate(__instance.RootFolderPrefab, _scroller.Inner);
            newFolder.FolderName = newFolder.name = plugin.PluginInfo.Metadata.Name;
            newFolder.gameObject.SetActive(false);
            _rolesFolder.SubFolders.Add(newFolder);

            if (!ModsFolders.ContainsKey(plugin.PluginInfo.Metadata.Name))
            {
                ModsFolders.Add(plugin.PluginInfo.Metadata.Name, plugin.PluginId);
            }
        }

        __instance.Root.SubFolders.Add(_rolesFolder);

        __instance.GoToRoot();
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(TaskAddButton), "Role", MethodType.Setter)]
    public static void RoleGetterPatch(TaskAddButton __instance)
    {
        if (__instance.role is ICustomRole { Team: ModdedRoleTeams.Neutral })
        {
            __instance.FileImage.color = Color.gray;
        }

        __instance.RolloverHandler.OutColor = __instance.FileImage.color;
    }

    private static void AddFileAsChildCustom(this TaskAdderGame instance, TaskFolder taskFolder, TaskAddButton item, ref float xCursor, ref float yCursor, ref float maxHeight)
    {
        item.transform.SetParent(instance.TaskParent);
        item.transform.localPosition = new Vector3(xCursor, yCursor, 0f);
        item.transform.localScale = Vector3.one;
        maxHeight = Mathf.Max(maxHeight, item.Text.bounds.size.y + 1.3f);
        xCursor += instance.fileWidth;
        if (xCursor > instance.lineWidth)
        {
            xCursor = 0f;
            yCursor -= maxHeight;
            maxHeight = 0f;
        }
        instance.ActiveItems.Add(item.transform);
    }
    

    // yes it might be crazy patching the entire method, but i tried so many other methods and only this works :cry:
    [HarmonyPrefix]
    [HarmonyPatch(typeof(TaskAdderGame), nameof(TaskAdderGame.ShowFolder))]
    public static bool ShowPatch(TaskAdderGame __instance, [HarmonyArgument(0)] TaskFolder taskFolder)
    {
        var stringBuilder = new Il2CppSystem.Text.StringBuilder(64);
        __instance.Hierarchy.Add(taskFolder);
        for (var i = 0; i < __instance.Hierarchy.Count; i++)
        {
            stringBuilder.Append(__instance.Hierarchy.ToArray()[i].FolderName);
            stringBuilder.Append("\\");
        }
        __instance.PathText.text = stringBuilder.ToString();
        for (var j = 0; j < __instance.ActiveItems.Count; j++)
        {
            Object.Destroy(__instance.ActiveItems.ToArray()[j].gameObject);
        }
        __instance.ActiveItems.Clear();
        var num = 0f;
        var num2 = 0f;
        var num3 = 0f;
        for (var k = 0; k < taskFolder.SubFolders.Count; k++)
        {
            var taskFolder2 = Object.Instantiate(taskFolder.SubFolders.ToArray()[k], __instance.TaskParent);
            var folderTransform = taskFolder2.transform;

            taskFolder2.gameObject.SetActive(true);
            taskFolder2.Parent = __instance;

            folderTransform.localPosition = new Vector3(num, num2, 0f);
            folderTransform.localScale = Vector3.one;
            num3 = Mathf.Max(num3, taskFolder2.Text.bounds.size.y + 1.3f);
            num += __instance.folderWidth;
            if (num > __instance.lineWidth)
            {
                num = 0f;
                num2 -= num3;
                num3 = 0f;
            }
            __instance.ActiveItems.Add(folderTransform);
            if (!taskFolder2 || !taskFolder2.Button)
            {
                continue;
            }

            ControllerManager.Instance.AddSelectableUiElement(taskFolder2.Button);
            if (!string.IsNullOrEmpty(__instance.restorePreviousSelectionByFolderName) && taskFolder2.FolderName.Equals(__instance.restorePreviousSelectionByFolderName))
            {
                __instance.restorePreviousSelectionFound = taskFolder2.Button;
            }
        }

        var flag = false;
        var list = new List<PlayerTask>();

        foreach (var item in taskFolder.Children)
        {
            list.Add(item);
        }

        for (var l = 0; l < list.Count; l++)
        {
            var taskAddButton = Object.Instantiate(__instance.TaskPrefab);
            taskAddButton.MyTask = list.ToArray()[l];
            if (taskAddButton.MyTask.TaskType == TaskTypes.DivertPower)
            {
                var targetSystem = taskAddButton.MyTask.Cast<DivertPowerTask>().TargetSystem;
                taskAddButton.Text.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.DivertPowerTo, DestroyableSingleton<TranslationController>.Instance.GetString(targetSystem));
            }
            else if (taskAddButton.MyTask.TaskType == TaskTypes.FixWeatherNode)
            {
                var nodeId = ((WeatherNodeTask)taskAddButton.MyTask).NodeId;
                taskAddButton.Text.text = DestroyableSingleton<TranslationController>.Instance.GetString(StringNames.FixWeatherNode, Array.Empty<Il2CppSystem.Object>()) + " " + DestroyableSingleton<TranslationController>.Instance.GetString(WeatherSwitchGame.ControlNames[nodeId],
                    Array.Empty<Il2CppSystem.Object>());
            }
            else
            {
                taskAddButton.Text.text = DestroyableSingleton<TranslationController>.Instance.GetString(taskAddButton.MyTask.TaskType);
            }
            __instance.AddFileAsChildCustom(taskFolder, taskAddButton, ref num, ref num2, ref num3);
            if (taskAddButton != null && taskAddButton.Button != null)
            {
                ControllerManager.Instance.AddSelectableUiElement(taskAddButton.Button);
                if (__instance.Hierarchy.Count != 1 && !flag)
                {
                    var component = ControllerManager.Instance.CurrentUiState.CurrentSelection.GetComponent<TaskFolder>();
                    if (component != null)
                    {
                        __instance.restorePreviousSelectionByFolderName = component.FolderName;
                    }
                    ControllerManager.Instance.SetDefaultSelection(taskAddButton.Button);
                    flag = true;
                }
            }
        }
        if (taskFolder.FolderName == "Roles")
        {
            for (var m = 0; m < DestroyableSingleton<RoleManager>.Instance.AllRoles.Length; m++)
            {
                var roleBehaviour = DestroyableSingleton<RoleManager>.Instance.AllRoles[m];
                if (roleBehaviour.Role != RoleTypes.ImpostorGhost && roleBehaviour.Role != RoleTypes.CrewmateGhost && !CustomRoleManager.CustomRoles.ContainsKey((ushort)roleBehaviour.Role))
                {
                    var taskAddButton2 = Object.Instantiate(__instance.RoleButton);
                    taskAddButton2.SafePositionWorld = __instance.SafePositionWorld;
                    taskAddButton2.Text.text = "Be_" + roleBehaviour.NiceName + ".exe";
                    __instance.AddFileAsChildCustom(_rolesFolder, taskAddButton2, ref num, ref num2, ref num3);
                    taskAddButton2.Role = roleBehaviour;
                    if (taskAddButton2 != null && taskAddButton2.Button != null)
                    {
                        ControllerManager.Instance.AddSelectableUiElement(taskAddButton2.Button);
                        if (m == 0 && __instance.restorePreviousSelectionFound != null)
                        {
                            ControllerManager.Instance.SetDefaultSelection(__instance.restorePreviousSelectionFound);
                            __instance.restorePreviousSelectionByFolderName = string.Empty;
                            __instance.restorePreviousSelectionFound = null;
                        }
                        else if (m == 0)
                        {
                            ControllerManager.Instance.SetDefaultSelection(taskAddButton2.Button);
                        }
                    }
                }
            }
        }

        if (ModsFolders.TryGetValue(taskFolder.FolderName, out var guid))
        {
            var plugin = MiraPluginManager.Instance.GetPluginByGuid(guid);
            for (var m = 0; m < plugin.CustomRoles.Count; m++)
            {
                var roleBehaviour = plugin.CustomRoles.ElementAt(m).Value;
                if (roleBehaviour.Role != RoleTypes.ImpostorGhost && roleBehaviour.Role != RoleTypes.CrewmateGhost && !roleBehaviour.IsDead)
                {
                    var taskAddButton2 = Object.Instantiate(__instance.RoleButton);
                    taskAddButton2.SafePositionWorld = __instance.SafePositionWorld;
                    taskAddButton2.Text.text = "Be_" + roleBehaviour.NiceName + ".exe";
                    __instance.AddFileAsChildCustom(_rolesFolder, taskAddButton2, ref num, ref num2, ref num3);
                    taskAddButton2.Role = roleBehaviour;
                    if (taskAddButton2 != null && taskAddButton2.Button != null)
                    {
                        ControllerManager.Instance.AddSelectableUiElement(taskAddButton2.Button);
                        if (m == 0 && __instance.restorePreviousSelectionFound != null)
                        {
                            ControllerManager.Instance.SetDefaultSelection(__instance.restorePreviousSelectionFound);
                            __instance.restorePreviousSelectionByFolderName = string.Empty;
                            __instance.restorePreviousSelectionFound = null;
                        }
                        else if (m == 0)
                        {
                            ControllerManager.Instance.SetDefaultSelection(taskAddButton2.Button);
                        }
                    }
                }
            }
        }

        foreach (var chip in __instance.ActiveItems)
        {
            chip.GetComponentInChildren<TextMeshPro>().EnableMasking();
            chip.GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        }

        if (_scroller)
        {
            _scroller.CalculateAndSetYBounds(__instance.ActiveItems.Count, 6, 3, 1f);
            _scroller.SetYBoundsMin(0.0f);
            _scroller.ScrollToTop();
        }
        if (__instance.Hierarchy.Count == 1)
        {
            ControllerManager.Instance.SetBackButton(__instance.BackButton);
            return false;
        }
        ControllerManager.Instance.SetBackButton(__instance.FolderBackButton);
        return false;
    }
}
