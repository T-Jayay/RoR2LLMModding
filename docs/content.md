# Content, catalogs and startup

Builds on the R2Wiki's [IContentPackProvider](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/IContentPackProvider/), [Items](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Items/), [Skills](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Skills/) and [Localization](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Assets/Localization/) pages, and on [R2API](https://github.com/risk-of-thunder/R2API).

## Adding content

Use R2API, split into modules; depend only on the ones you use. Each is a NuGet package (`R2API.Items`) and a Thunderstore package (`RiskofThunder-R2API_Items`), and each plugin class has a `PluginGUID` for `[BepInDependency]`.

| Module | For |
|---|---|
| `R2API.Items` (`ItemAPI`), `R2API.Language` (`LanguageAPI`) | Items and equipment, text tokens (what the ExamplePlugin uses) |
| `R2API.ContentManagement` (`ContentAddition`) | Bodies, masters, projectiles, skill defs and families, entity states (`AddEntityState<T>`), buffs, effects, ... |
| `R2API.Prefab` (`PrefabAPI`) | Cloning prefabs, including networked ones |
| `R2API.RecalculateStats` (`RecalculateStatsAPI.GetStatCoefficients`) | Stat changes without hooking `RecalculateStats` |
| `R2API.Networking` (`NetworkingAPI`) | Network messages ([networking.md](networking.md)) |

Without R2API, the game's own route is an `IContentPackProvider` added through `ContentManager.collectContentPackProviders` (see the wiki page). Every asset in a content pack needs a unique name.

A mod that adds content must be on every player's machine in the same version (see Network mod list below).

## Catalogs and indices

The game builds its catalogs (`BodyCatalog`, `ItemCatalog`, `SkillCatalog`, `EntityStateCatalog`, ...) during loading, after every plugin's `Awake`. Indices (`BodyIndex`, `ItemIndex`, `EntityStateIndex`) depend on which mods are installed, so never hardcode them: look them up by name (`BodyCatalog.FindBodyIndex("CommandoBody")`) or through the def.

Work out lookups once, when the catalog is ready, instead of every frame:

```csharp
[assembly: HG.Reflection.SearchableAttribute.OptIn]

internal static class SprintStates
{
    // Entity states that end when the body starts sprinting, decided once from the skill catalog.
    internal static readonly HashSet<Type> canceledBySprint = new();

    [SystemInitializer(typeof(SkillCatalog))]
    private static void Init()
    {
        foreach (SkillDef skillDef in SkillCatalog.allSkillDefs)
        {
            if (skillDef && skillDef.canceledFromSprinting && skillDef.activationState.stateType != null)
            {
                canceledBySprint.Add(skillDef.activationState.stateType);
            }
        }
    }
}
```

- `[SystemInitializer(typeof(X))]` runs a static method after the listed systems have initialized. Vanilla 1.4.1 only runs initializers found in `RoR2.dll` (`SystemInitializerAttribute.ExecuteStatic` reads `RoR2Application.AssemblyTypes`). For mod assemblies it works because RoR2BepInExPack (1.18.0 and later) patches `SystemInitializerAttribute` to find them through `SearchableAttribute`, which only scans assemblies with the `OptIn` attribute, so the assembly needs it.
- The entity state catalog is not ready at that point: `RoR2Application.InitializeGameRoutine` runs the `[SystemInitializer]`s, then `EntityStateCatalog.Init`, then `RoR2Application.onLoad`. That is why the example keeps `Type`s (`SerializableEntityStateType.stateType` resolves by name) rather than calling `EntityStateCatalog.GetStateIndex`, which would log an error and return `Invalid`. For anything that needs entity state indices, use `RoR2Application.onLoad` (subscribe in `Awake`; it is cleared after firing) or an On hook on `EntityStateCatalog.Init` (AutoSprint does this).
- Skill and body data (`forceSprintDuringState`, `cancelSprintingOnActivation`, body flags) are asset values: read them from the catalogs like this rather than hardcoding what you saw for one survivor, so modded survivors work too.

## Loading game assets

`Addressables.LoadAssetAsync<T>(key).WaitForCompletion()`, as in the ExamplePlugin. Paths change between updates; RoR2BepInExPack generates GUID keys, which don't ([1.3.9 updating guide](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Updating-Your-Mods/1.3.9-Memory-Optimization/Updating-Guide/)):

```csharp
ItemTierDef tier2 = Addressables.LoadAssetAsync<ItemTierDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common.Tier2Def_asset).WaitForCompletion();
```

Since RoR2BepInExPack 1.33.0 the older `RoR2BepInExPack.GameAssetPaths` classes are marked obsolete in favour of `GameAssetPathsBetter`. Reference the `RoR2BepInExPack` NuGet package to use them. The keys are `static readonly` fields, read from RoR2BepInExPack at runtime, and BepInExPack 5.4.2122 only requires RoR2BepInExPack 1.9.0, which predates them; listing `RiskofThunder-RoR2BepInExPack-<version you built against>` in the manifest makes the requirement explicit (a judgement call, not a documented convention).

## Network mod list

The server compares an MD5 of `NetworkModCompatibilityHelper.networkModList` with each joining client's and rejects a mismatch ("Mod mismatch", `ServerAuthManager`). A mod that adds catalog content or its own network messages must be on it; a host-only or client-only mod must not, or players without it can't join.

- R2API adds every plugin with a hard dependency on an R2API module as `GUID;version`. A host-only or client-only mod that uses R2API opts out with `[assembly: NetworkCompatibility(CompatibilityLevel.NoNeedForSync, VersionStrictness.DifferentModVersionsAreOk)]` (`R2API.Utils`).
- Without R2API, add yourself in `Awake` (with `using System.Linq;`):
  ```csharp
  NetworkModCompatibilityHelper.networkModList = NetworkModCompatibilityHelper.networkModList.Append(PluginGUID + ";" + PluginVersion);
  ```

## Console commands

A static `void (ConCommandArgs)` method with `[ConCommand(commandName = ..., flags = ..., helpText = ...)]` becomes a console command; `ConCommandAttribute` is also a `SearchableAttribute`, so the assembly needs `OptIn`. See the R2Wiki's [Console Commands](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Console-Commands/) page.
