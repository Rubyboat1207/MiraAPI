﻿using MiraAPI.GameOptions.OptionTypes;
using MiraAPI.Utilities;
using System;
using System.Reflection;

namespace MiraAPI.GameOptions.Attributes;

public class ModdedNumberOptionAttribute(
    string title,
    float min,
    float max,
    float increment = 1,
    MiraNumberSuffixes suffixType = MiraNumberSuffixes.None,
    bool zeroInfinity = false,
    Type roleType = null)
    : ModdedOptionAttribute(title, roleType)
{
    internal override IModdedOption CreateOption(object value, PropertyInfo property)
    {
        var toggleOpt = new ModdedNumberOption(Title, (float)value, min, max, increment, suffixType, zeroInfinity, RoleType);
        return toggleOpt;
    }

    public override void SetValue(object value)
    {
        var toggleOpt = HolderOption as ModdedNumberOption;
        toggleOpt.SetValue((float)value);
    }

    public override object GetValue()
    {
        var toggleOpt = HolderOption as ModdedNumberOption;
        return toggleOpt.Value;
    }
}