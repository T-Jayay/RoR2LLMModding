# Testing

Builds on the R2Wiki's [Debugging Your Mods](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Debugging-Your-Mods/) and [Testing Multiplayer Alone](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/Testing/Testing-Multiplayer-Alone/) pages.

Launching the game isn't proof that a mod works, and neither is seeing your code path run. Players use dozens to hundreds of mods, several of which hook the same methods you do, and play in multiplayer, where code runs on machines you didn't test. An agent can check that the mod builds and loads; the human has to play it.

## What the agent checks

1. `dotnet build -c Release` with 0 warnings (`BepInEx.Analyzers` is on).
2. Deploy to the test profile ([setup.md](setup.md)).
3. After the human has started the game with it, read `<profile>/BepInEx/LogOutput.log`:
   - `Loading [<PluginName> <version>]` for your plugin, and nothing after it that names your plugin, namespace or hooks at `[Error` or `[Warning` level (including your own "IL hook ... failed" messages);
   - some errors aren't from your mod (seen on 1.4.1 and 1.5.0: `Could not register 'PrinterItemIcon' since it contains no NetworkIdentity component`, an Addressables `InvalidKeyException`). Compare with a log from the same profile without your mod before blaming or excusing anything.
4. Find installed mods that touch the same methods. A quick first pass is a text search of the plugin DLLs for the method name (`grep -rl --include=*.dll PollButtonInput <profile>/BepInEx/plugins`): it finds MMHOOK-based hooks but misses targets named by string, so decompile the candidates to be sure. Tell the human which mods to include in the test.

Don't claim a feature works because the log is clean.

## Test steps for the human

Write numbered steps the human can follow without reading the code. For each: the setup, what to do, what should happen, and what a failure looks like (on this screen, on the other player's screen, in the log). Cover what applies:

1. **Solo.** Solo play is a host with no clients: it tests the server and owner paths on one machine, and no networking at all.
2. **Host with a client.** The client with and without the mod, according to who needs it ([networking.md](networking.md)). Check the effect on both screens: a desync shows as rubber-banding, values that differ between screens, or effects only one player sees.
3. **Client in someone else's lobby.** As host, the owner and the server are the same machine; only here do owner-decided things (movement, sprint, skills) and the server's copy run apart.
4. **Game situations** that change the objects your code holds: death and respawn, stage transitions, a new run, survivors other than Commando (including modded survivors), and anything the feature interacts with (items, artifacts, drones, Eclipse).
5. **With popular mods**, especially ones that hook the same methods (step 4 above). Popular non-library mods include Starstorm 2, LookingGlass, ProperSave, ShareSuite and DropinMultiplayer (check Thunderstore's current list), plus MiscFixes, which every BepInExPack install includes.

[DebugToolkit](https://thunderstore.io/c/riskofrain2/p/IHarbHD/DebugToolkit/) console commands make steps short: `give_item`, `give_money`, `spawn_ai`, `spawn_as`, `next_stage`, `noclip`, `god`, `buddha`, `time_scale`. Open the console with Ctrl+Alt+` (US layout).

Example, for the template (host-only fall damage):

1. In a profile with ExampleMod and DebugToolkit, start a singleplayer run on Rainstorm.
2. `noclip 1`, fly up high enough that landing will hurt, `noclip 0`, and land on flat ground. Note the health lost.
3. Quit, set `Fall damage multiplier = 1` in `BepInEx/config/AuthorName.ExampleMod.cfg`, and repeat the same fall. You should lose about twice as much as in step 2 (fall damage is non-lethal on Rainstorm, so pick a height that doesn't take you to 1 HP). Failure: the same damage both times, or an error mentioning ExampleMod in the log.
4. Host a lobby with the multiplier at 0.5 and have a player without the mod join. They should connect normally (no "mod mismatch" kick) and take the reduced fall damage too, because the host applies all damage.

## Multiplayer on one PC

The Testing Multiplayer Alone page describes running two game instances (via the profile's doorstop arguments) and connecting with `connect localhost:7777`, using either a one-line hook or the [MultiplayerModTesting](https://thunderstore.io/c/riskofrain2/p/Nuxlar/MultiplayerModTesting/) mod. Not verified here. It still isn't the same as two machines over Steam, so test networking features in a real lobby before release.

For features that only happen with other players (e.g. a teammate dying), a small test-only plugin that is never packaged can simulate them, e.g. spawning an invincible AI survivor on the player team as a stand-in. Keep it out of the release zip.

## Debugger

The Debugging Your Mods page covers attaching a debugger through doorstop's `debug_enabled` setting. Useful when a hook misbehaves; not a substitute for the steps above.
