# Setup and project template

Builds on the R2Wiki's [First Mod](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Getting-Started/First-Mod/) and [Assembly References](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Assembly-References/) pages and the [ExamplePlugin](https://github.com/risk-of-thunder/ExamplePlugin). Versions are for game 1.5.0 (Unity 2021.3.45).

## What runs in the game

- **BepInExPack** (`bbepis-BepInExPack`) loads plugins. For Risk of Rain 2 it also pulls in RoR2BepInExPack (community fixes to the game and loader), BepInEx_GUI, FixPluginTypesSerialization, SeekersPatcher and MiscFixes. It ships HarmonyX and MonoMod (`BepInEx/core`).
- **HookGenPatcher** (`RiskofThunder-HookGenPatcher`) generates `BepInEx/plugins/MMHOOK/MMHOOK_RoR2.dll` at startup, which provides the `On.` and `IL.` hook namespaces. A mod that uses them lists HookGenPatcher as a dependency in its manifest.
- Your plugin DLL goes in `BepInEx/plugins/<ModName>/`.

Develop in a separate mod manager profile (r2modman, Thunderstore Mod Manager or Gale), not the one you play with. Profile folders:

| Mod manager | Profile folder |
|---|---|
| r2modman | `%APPDATA%\r2modmanPlus-local\RiskOfRain2\profiles\<profile>` |
| Thunderstore Mod Manager | `%APPDATA%\Thunderstore Mod Manager\DataFolder\RiskOfRain2\profiles\<profile>` |
| Gale | `%APPDATA%\com.kesomannen.gale\riskofrain2\profiles\<profile>` (unverified) |

## Build environment

- A .NET SDK (8 or later) for `dotnet build`. Visual Studio or Rider work too.
- Internet access on the first build: packages come from nuget.org and the [BepInEx feed](https://nuget.bepinex.dev/).
- `ilspycmd` for reading game code (see [decompiling.md](decompiling.md)).

## The template

`template/` is a minimal mod that builds with 0 warnings: one plugin class, one config entry and one On hook, plus `thunderstore/` with the package files.

| Package | Version | Feed | Why |
|---|---|---|---|
| `BepInEx.Core` | 5.4.21 | BepInEx | `BaseUnityPlugin`, config, logging |
| `RiskOfRain2.GameLibs` | 1.5.0-r.1 | BepInEx | Game assemblies: stripped (no method bodies) and publicized. nuget.org only has an old version. |
| `UnityEngine.Modules` | 2021.3.45 | BepInEx | Unity API, compile-only. nuget.org only has 2021.3.33. |
| `MMHOOK.RoR2` | 2026.10.8 | nuget.org | `On.`/`IL.` hooks, compile-only; published with GameLibs 1.5.0-r.1. `NoWarn="NU1701"` because the package has no netstandard folder. |
| `BepInEx.Analyzers` | 1.0.8 | BepInEx | Checks plugin and Harmony code |

- Target `netstandard2.1`. A class library copies none of its package references to the output, so only your DLL is shipped.
- **Publicized GameLibs**: private game members compile as public, so use them directly (`self.somePrivateField`, `nameof(Type.PrivateMethod)`). The R2Wiki's Assembly References page requires this line for that (the template has it):
  ```csharp
  [assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
  ```
- **After a game update**, bump `RiskOfRain2.GameLibs` and `MMHOOK.RoR2` together (the MMHOOK must be generated from the same game build), and `UnityEngine.Modules` if the game's Unity version changed. Rebuild and fix what no longer compiles: 1.5.0, for one, removed `PlayerCharacterMasterController.sprintInputPressReceived` (the sprint key now goes through `ButtonBuffer.Poll()`, read as `PollResult.justPressed`) and renamed `SkillIcon.Update` to `LateUpdate`. Not every change breaks the build: a hook on a method the game made `[Obsolete]` compiles and never runs, so check every hook target for new overloads ([hooking.md](hooking.md)). The R2Wiki keeps a changelog of game API changes per update (e.g. [1.4.1](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Updating-Your-Mods/Updating-Core-API-Changelog-1.4.1/)).
- Add R2API modules, `RoR2BepInExPack` or `Rune580.Mods.RiskOfRain2.RiskOfOptions` from nuget.org only when you use them (see [content.md](content.md), [config.md](config.md)).

### Starting a new mod from it

1. Copy `template/` to a new folder (its own git repo).
2. Rename `ExampleMod.csproj`, the namespace, `ExampleModPlugin`, and set `PluginAuthor` and `PluginName`. The GUID (`Author.ModName`) must be unique and must not change after release: it names the config file and is how other mods find yours.
3. Set `name`, `description` and `website_url` in `thunderstore/manifest.json` and write `thunderstore/README.md` (the store page). Keep `version_number` equal to `PluginVersion`.
4. Replace `thunderstore/icon.png` (256x256 PNG; the template's is a placeholder).
5. Delete the example hook and config entry.

## Build and deploy

```
dotnet build -c Release
dotnet build -c Release -p:ProfileDir="%APPDATA%\r2modmanPlus-local\RiskOfRain2\profiles\Dev"
```

The second form also copies the DLL and PDB to `<ProfileDir>\BepInEx\plugins\<AssemblyName>\` (the template's `CopyToProfile` target). Close the game first: a running game keeps loaded plugin DLLs locked.

To start a profile without the mod manager (e.g. two instances for multiplayer tests), run the game with the profile's doorstop arguments (your mod manager can show them; where exactly isn't verified here):

```
"Risk of Rain 2.exe" --doorstop-enabled true --doorstop-target-assembly "<profile>\BepInEx\core\BepInEx.Preloader.dll"
```

The log is `<profile>\BepInEx\LogOutput.log`. BepInEx writes `Loading [<PluginName> <version>]` for each plugin; see [testing.md](testing.md) for what else to check.
