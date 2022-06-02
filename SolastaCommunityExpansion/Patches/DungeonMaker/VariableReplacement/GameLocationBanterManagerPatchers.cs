﻿using System.Diagnostics.CodeAnalysis;
using HarmonyLib;
using SolastaCommunityExpansion.Models;

namespace SolastaCommunityExpansion.Patches.DungeonMaker.VariableReplacement;

[HarmonyPatch(typeof(GameLocationBanterManager), "PlayLine")]
[SuppressMessage("Minor Code Smell", "S101:Types should be named in PascalCase", Justification = "Patch")]
internal static class Gui_Format
{
    internal static void Prefix(ref string line)
    {
        line = DungeonMakerContext.ReplaceVariable(line);
    }
}
