# Reading the game code

Builds on the R2Wiki's [Code Analysis](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Code-Analysis/) page, which recommends ILSpy.

Read the real code before writing a hook: the method, its callers, the fields it touches, and which machine runs it (server, the owning client, or every copy). The GameLibs package has no method bodies, so decompile the game's own DLLs.

## Decompile

```
dotnet tool install -g ilspycmd
ilspycmd -p -o <scratch>/ror2src "<game>/Risk of Rain 2_Data/Managed/RoR2.dll"
```

`<game>` is usually `C:\Program Files (x86)\Steam\steamapps\common\Risk of Rain 2`. Then grep the output. Other assemblies in `Managed/` worth decompiling the same way:

| Assembly | Contains |
|---|---|
| `RoR2.dll` | Almost all game code, including `EntityStates.*` |
| `HGCSharpUtils.dll` | `HG.Reflection.SearchableAttribute` (how the game finds `[SystemInitializer]`, `[NetworkMessageHandler]`, `[ConCommand]`) |
| `com.unity.multiplayer-hlapi.Runtime.dll` | UNet (`NetworkServer`, `NetworkBehaviour`, `NetworkMessageHandlers`) as the game ships it |
| `Rewired_Core.dll` | Input |

Mods are assemblies too: decompile a mod from `<profile>/BepInEx/plugins/` to see how it hooks the method you are about to hook (RoR2BepInExPack and R2API are also open source).

Keep decompiled code in a scratch folder outside the repo. Never commit it or paste it into a mod; describe what the game does and name the member instead.

## IL

IL hooks match instructions, so read the IL, not just the C#:

```
ilspycmd -il "<game>/Risk of Rain 2_Data/Managed/RoR2.dll" > <scratch>/ror2.il
```

and find the method (`.method ... TakeDamageProcess`). The decompiled C# hides compiler details (display classes for lambdas, state machines for coroutines, `ldloc.s 4` for a named local). At runtime the IL may already be changed by other mods' IL hooks, so match loosely (see [hooking.md](hooking.md)).

## What decompiling doesn't show

- **Asset data.** Values set in the Unity editor live in assets, not code: a `SkillDef`'s `forceSprintDuringState`, `cancelSprintingOnActivation` or `activationState`, an entity state's serialized fields, prefab components. Read them at runtime from the catalogs after they load (see [content.md](content.md)), not by hardcoding what you saw in one survivor.
- **Addressable keys.** Load game assets by GUID through `RoR2BepInExPack.GameAssetPathsBetter` (see [content.md](content.md)); the R2Wiki's [Addressables Assets Keys](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Developer-Reference/Addressables-Assets-Keys/) page lists paths.

## After a game update

The R2Wiki's [Source Code Diff](https://risk-of-thunder.github.io/R2Wiki/Tools/Source-Code-Diff/) page shows how to diff two decompiled versions with git, and the Updating Your Mods pages summarise API changes per patch.
