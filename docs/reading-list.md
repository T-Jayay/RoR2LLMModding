# Reading list

Read these before designing something; most problems already have a community answer. Link to them, describe the pattern, and don't copy code.

## Guides

- [R2Wiki](https://risk-of-thunder.github.io/R2Wiki/) ([GitHub wiki source](https://github.com/risk-of-thunder/R2Wiki/wiki)): start with First Mod, Assembly References, Hooking (and its On Hook / IL Hook pages), Networking, and the Updating Your Mods changelogs. Some pages are older than others (e.g. Code Analysis marks its dnSpy section as out of date); check against the game code.
- [ExamplePlugin](https://github.com/risk-of-thunder/ExamplePlugin): the wiki's reference project (an item added with R2API, a game event, a Thunderstore folder).
- Thunderstore wiki: [Creating a Package](https://wiki.thunderstore.io/mods/creating-a-package), [Global Rules](https://wiki.thunderstore.io/moderation/global-rules).
- [Risk of Rain 2 modding Discord](https://discord.gg/5MbXZvd): where experienced modders answer questions. When the agent is unsure which approach the community would take, the human asks there.

## Mods to learn from

| Source | Look at |
|---|---|
| [AutoSprint](https://github.com/goldenguy00/AutoSprint) | Sprinting by acting through the player's input: an IL hook in `PlayerCharacterMasterController.PollButtonInput` that sets `sprintInputPressReceived`. The entity states that block sprint are decided once at startup from the `SkillCatalog` (`[SystemInitializer(typeof(SkillCatalog))]`) and converted to indices after an On hook on the `EntityStateCatalog.Init` coroutine. Failed IL matches log an error and leave the method alone. |
| [MiscFixes](https://github.com/goldenguy00/MiscFixes) | Small, targeted fixes for game bugs with HarmonyX: `[HarmonyPatch]` classes using prefixes and `[HarmonyILManipulator]` IL edits, each match checked with `TryGotoNext` and a logged failure. Every BepInExPack install includes it. |
| [RoR2BepInExPack](https://github.com/risk-of-thunder/RoR2BepInExPack) | How the community patches the game and loader for everyone (e.g. `FixSystemInitializer` makes `[SystemInitializer]` work in mod assemblies), and the `GameAssetPathsBetter` asset keys. |
| [R2API](https://github.com/risk-of-thunder/R2API) | Content, stats, networking and language APIs, and how it builds the network mod list (`NetworkCompatibility`). |
| [RiskOfOptions](https://github.com/Rune580/RiskOfOptions) | The settings UI most mods integrate with. |
| [DebugToolkit](https://github.com/harbingerofme/DebugToolkit) | The console commands used in test steps ([testing.md](testing.md)), and how a mod built on `[ConCommand]` is organised. |

The decompiled game is the reference for how the game itself does things (e.g. `NetworkStateMachine.HandleSetEntityState` for validating a client's message); see [decompiling.md](decompiling.md).
