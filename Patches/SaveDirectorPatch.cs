﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CottonLibrary;
using static CottonLibrary.Library;

using HarmonyLib;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher.Pedia;
using Il2CppMonomiPark.SlimeRancher.UI.Localization;
using Il2CppTMPro;
using UnityEngine;

namespace CottonLibrary.Patches;


[HarmonyPatch(typeof(AutoSaveDirector), "Awake")]
public static class SaveDirectorPatch
{
    public static void Prefix(AutoSaveDirector __instance)
    {

        slimes = Get<IdentifiableTypeGroup>("SlimesGroup");
        baseSlimes = Get<IdentifiableTypeGroup>("BaseSlimeGroup");
        largos = Get<IdentifiableTypeGroup>("LargoGroup");
        meat = Get<IdentifiableTypeGroup>("MeatGroup");
        food = Get<IdentifiableTypeGroup>("FoodGroup");
        veggies = Get<IdentifiableTypeGroup>("VeggieGroup");
        fruits = Get<IdentifiableTypeGroup>("FruitGroup");
        plorts = Get<IdentifiableTypeGroup>("PlortGroup");
        crafts = Get<IdentifiableTypeGroup>("CraftGroup");

        foreach (CottonMod lib in mods)
        {
            lib.SaveDirectorLoading(__instance);
        }
    }
    public static void Postfix()
    {
        PediaDetailInitialize();
        
        foreach (CottonMod lib in mods)
        {
            lib.SaveDirectorLoaded();
        }
        
        foreach (CottonMod lib in mods)
        {
            lib.LateSaveDirectorLoaded();
        }
        
        // 0.6: ffs why
        var steamToy = Get<ToyDefinition>("SteamFox");
        if (steamToy)
            INTERNAL_SetupLoadForIdent(steamToy.ReferenceId, steamToy);
        // add more platforms please

        // Doing this so it executes after all mods have made their slimes.
        foreach (var largoAction in createLargoActions)
        {
            largoAction();
        }

        foreach (var category in Resources.FindObjectsOfTypeAll<PediaCategory>())
        {
            category.GetRuntimeCategory();
        }
    }
}
