﻿using MiraAPI.Utilities.Assets;

namespace MiraAPI.Example;

public static class ExampleAssets
{
    public static LoadableResourceAsset ExampleButton { get; } = new("MiraAPI.Example.Resources.ExampleButton.png");

    // Credit to EpicHorrors for the teleport button asset. 
    public static LoadableResourceAsset TeleportButton { get; } = new("MiraAPI.Example.Resources.TeleportButton.png");
    public static LoadableResourceAsset Banner { get; } = new("MiraAPI.Example.Resources.FortniteBanner.jpeg");
}