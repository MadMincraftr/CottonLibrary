using System.Reflection;
using HarmonyLib;
using Il2Cpp;
using Il2CppMonomiPark.SlimeRancher;
using Il2CppMonomiPark.SlimeRancher.DataModel;
using Il2CppMonomiPark.SlimeRancher.Persist;
using Il2CppMonomiPark.SlimeRancher.SceneManagement;
using Il2CppMonomiPark.SlimeRancher.Slime;
using Il2CppMonomiPark.UnitPropertySystem;
using Il2CppSystem.Text;
using MelonLoader;
using static CottonLibrary.Library;
using Selections = Il2CppSystem.Collections.Generic.Dictionary<int, Il2Cpp.SlimeAppearance.AppearanceSaveSet>;
using Unlocks = Il2CppSystem.Collections.Generic.Dictionary<int, Il2CppSystem.Collections.Generic.List<Il2Cpp.SlimeAppearance.AppearanceSaveSet>>;

namespace CottonLibrary.Patches;

[HarmonyPatch(typeof(AppearancesModel),nameof(AppearancesModel.Pull))]
public class AppearancesSaveFix
{
    public static bool Prefix(AppearancesModel __instance, ref AppearancesV01 __result, IdentifiableTypePersistenceIdLookupTable identifiableTypeToPersistenceId)
    {
        
        __result = new AppearancesV01();

        Selections selections = new Selections();
        Unlocks unlocks = new Unlocks();

        foreach (var selection in __instance.AppearanceSelections._selections)
        {
            gameContext.AutoSaveDirector.SavedGame.identifiableTypeToPersistenceId.RefreshIfNotFound(selection.Key);
            
            selections.Add(gameContext.AutoSaveDirector.SavedGame.identifiableTypeToPersistenceId._reverseIndex[selection.key.ReferenceId], selection.value.SaveSet);
        }

        foreach (var unlock in __instance.AppearanceSelections._unlocks)
        {
            var list = new Il2CppSystem.Collections.Generic.List<SlimeAppearance.AppearanceSaveSet>();

            foreach (var app in unlock.value)
            {
                list.Add(app.SaveSet);
            }
            
            unlocks.Add(gameContext.AutoSaveDirector.SavedGame.identifiableTypeToPersistenceId._reverseIndex[unlock.key.ReferenceId], list);
        }

        __result.Unlocks = unlocks;
        __result.Selections = selections;
        
        return false;
    }
}

[HarmonyPatch(typeof(SavedGame), nameof(SavedGame.BuildActorData))]
public class CustomActorSaveFix
{
    public static bool Prefix(
        SavedGame __instance, 
        ref ActorDataV02 __result,
        ActorModel actorModel, 
        IdentifiableTypePersistenceIdLookupTable identToPersistenceId,
        PersistenceIdLookupTable<SceneGroup> sceneToPersistenceId,
        PersistenceIdLookupTable<StatusEffectDefinition> statusEffectToPersistenceId)
    {
        var identTable = gameContext._AutoSaveDirector_k__BackingField._savedGame
            .IdentifiableTypePersistenceIdLookupTable;
        
        identTable.RefreshIfNotFound(actorModel.ident);
        
        ActorDataV02 actorDataV = new ActorDataV02();

        IdentifiableType ident = actorModel.ident;
        actorDataV.TypeId = identTable.GetPersistenceId(ident);

        actorDataV.ActorId = actorModel.actorId.Value;

        Vector3V01 position = new Vector3V01();
        position.Value = actorModel.lastPosition;
        actorDataV.Pos = position;

        Vector3V01 rotation = new Vector3V01();
        rotation.Value = actorModel.lastRotation.eulerAngles;
        actorDataV.Rot = rotation;

        SceneGroup sceneGroup = actorModel.SceneGroup;
        actorDataV.SceneGroup = sceneToPersistenceId.GetPersistenceId(sceneGroup);

        var emotions = new SlimeEmotionDataV01();
        emotions.EmotionData = new Il2CppSystem.Collections.Generic.Dictionary<SlimeEmotions.Emotion, float>();
        emotions.EmotionData.Add(SlimeEmotions.Emotion.FEAR, actorModel.TryCast<SlimeModel>() != null ? actorModel.Cast<SlimeModel>().Emotions.x : 0f);
        emotions.EmotionData.Add(SlimeEmotions.Emotion.HUNGER, actorModel.TryCast<SlimeModel>() != null ? actorModel.Cast<SlimeModel>().Emotions.y : 0f);
        emotions.EmotionData.Add(SlimeEmotions.Emotion.AGITATION, actorModel.TryCast<SlimeModel>() != null ? actorModel.Cast<SlimeModel>().Emotions.z : 0f);
        emotions.EmotionData.Add(SlimeEmotions.Emotion.SLEEPINESS, actorModel.TryCast<SlimeModel>() != null ? actorModel.Cast<SlimeModel>().Emotions.w : 0f);
        actorDataV.Emotions = emotions;
       
        
        Il2CppSystem.Collections.Generic.List<StatusEffectV01> statusEffects = new Il2CppSystem.Collections.Generic.List<StatusEffectV01>();
        foreach (var effect in actorModel.statusEffects)
        {
            var effectV01 = new StatusEffectV01()
            {
                ExpirationTime = effect.value.ExpirationTime,
                ID = statusEffectToPersistenceId.GetPersistenceId(effect.key)
            };
            statusEffects.Add(effectV01);
        }
        actorDataV.StatusEffects = statusEffects;
        
        actorDataV.CycleData = new ResourceCycleDataV01();

        if (actorModel is SlimeModel slimeModel)
        {
            slimeModel.Pull(ref actorDataV, identTable);
        }
        else if (actorModel is AnimalModel animalModel)
        {
            animalModel.Pull(ref actorDataV, identTable);
        }
        else if (actorModel is ProduceModel produceModel)
        {
            produceModel.Pull(out var state, out var time);
            actorDataV.CycleData.State = state;
            actorDataV.CycleData.ProgressTime = time;
        }
        else if (actorModel is StatueFormModel statueFormModel)
        {
            actorDataV.IsStatue = true;
        }
        
        __result = actorDataV;
        
        return false;
    }
}

[HarmonyPatch(typeof(GameV06), nameof(GameV06.LoadSummaryData))]
public static class EnsureSlimesSavableFix
{
    static void Prefix()
    {
        foreach (var actor in savedIdents)
        {
            gameContext.AutoSaveDirector.SavedGame.identifiableTypeToPersistenceId.RefreshIfNotFound(actor.Value);
        }
    }
}