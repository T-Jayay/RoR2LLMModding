# Writing Risk of Rain 2 mods

Rules for a coding agent writing BepInEx mods for Risk of Rain 2 (game 1.4.1). Mods are public and read by experienced modders, and the community has well-tested conventions for almost everything: follow them. The details are in `docs/`; `template/` is a project that builds.

When unsure which way the community would do something, stop and ask the human. They can ask in the [RoR2 modding Discord](https://discord.gg/5MbXZvd), which is often the fastest route to the right answer.

## Process

1. **Look before designing.** Read the R2Wiki page for the topic and the source of a mod that does something similar ([docs/reading-list.md](docs/reading-list.md)). If there is an established way (a game event, an R2API module, a RoR2BepInExPack feature, a well-known hook), use it.
2. **Read the real game code.** Decompile `RoR2.dll` ([docs/decompiling.md](docs/decompiling.md)) and read the method you'll change, its callers, and on which machines it runs: the server, the owning client, or every copy. Never hook a method based on a guess about what it does.
3. **Use the lightest tool**: game event, then R2API, then an On hook that calls `orig`, then an IL hook ([docs/hooking.md](docs/hooking.md)).
4. **Work with the game's authority.** Find the machine that decides the value and change it there, through the path the game already networks (the player's input, a command the game sends, an entity state). Don't overwrite what another machine owns and then patch the resulting desync ([docs/networking.md](docs/networking.md)).
5. **Precompute at startup.** Work out lookups (which skills, entity states, bodies) once, when the catalog is ready, from catalog data rather than hardcoded indices or lists copied from one survivor ([docs/content.md](docs/content.md)).
6. **Keep it small.** Only what was asked, with as few hooks as possible, in code a reviewer can check at a glance.
7. **Ask when choosing.** If there are two reasonable approaches (On vs IL hook, R2API vs the game's own mechanism, who needs the mod installed), describe both briefly with their trade-offs and let the human decide.
8. **Build with zero warnings and hand over test steps.** You can't prove a mod works ([Testing](#testing)).

## Hard rules

- Reference the game through `RiskOfRain2.GameLibs` (publicized) and, for `On.`/`IL.` hooks, `MMHOOK.RoR2`, both from NuGet, targeting `netstandard2.1` ([docs/setup.md](docs/setup.md)). Never reference or ship game DLLs.
- Never commit decompiled game code, and never copy other people's code: link to it and describe the pattern.
- Use the publicized game members directly (`self.privateField`, `nameof(Type.PrivateMethod)`). Reflection and member-name strings are only for other mods' private members.
- On hooks call `orig` exactly once. Skipping it breaks the game and every mod hooked before yours; if a feature really needs it, make the condition narrow and explain it in a comment.
- IL hooks: `TryGotoNext` with the smallest pattern that is unique; on failure, log an error naming the hook and leave the method unchanged. No cursor index arithmetic outside a matched block.
- Know where each piece of code runs, and check it: `NetworkServer.active` for server state, authority (`hasEffectiveAuthority`, `isAuthority` in entity states) for what the owner decides.
- A mod that adds catalog content or its own network messages must be on every player's machine in the same version (the network mod list). A mod that doesn't must stay off it.
- Soft dependencies: `[BepInDependency(guid, SoftDependency)]`, `Chainloader.PluginInfos.ContainsKey(guid)`, and the other mod's types only inside `[MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]` methods ([docs/config.md](docs/config.md)).
- Uploaded Thunderstore versions are immutable: any change needs a new version in both `manifest.json` and `PluginVersion`. AI-written mods get the AI Generated category ([docs/publishing.md](docs/publishing.md)).
- Don't push, publish or upload anything unless the human asks.

## Anti-patterns

From reviews of AI-written mods by experienced RoR2 modders. Each makes code look careful while making bugs harder to find.

- **Defensive noise.** Comment banners like `// NUCLEAR-SAFE EXIT: Only process tokens we absolutely know about`, guards for states that can't happen, "just in case" checks. Unreadable code is the root of all evil. Null-check what really is null in normal play (`damageReport.attackerBody` when there was no attacker, e.g. fall damage; `body.master` for bodies without one) and nothing else.
- **Hiding errors**: catch-all `try/catch` in hooks, "report once" logging, silent fallback to vanilla. The mod then looks like it works while part of it doesn't, and bug reports lose the stack trace. Let exceptions reach the BepInEx log.
- **Scaffolding against game updates**: `Prepare()` member checks, `NoInlining` helper layers (outside soft dependencies), per-feature patch groups each wrapped in a try/catch, `AccessTools` string lookups for members GameLibs already exposes. A game update that breaks a mod is fixed by a mod update, and with GameLibs it shows up as a compile error. (A collection of independent game fixes such as MiscFixes applies each fix class separately and logs failures; an ordinary mod registers one set of hooks in `Awake`.)
- **Reinventing what exists**: custom network message registration instead of the game's `[NetworkMessageHandler]` or R2API.Networking; hand-written state sync for something the game already replicates.
- **Fighting the game's writes, then patching the desync with timers.** Sprint is decided by the owning client, and the server relays it. Prefer acting through the input path: if some conditions are met, press the button, with the relevant entity states worked out once at startup from the `SkillCatalog` (AutoSprint). If a feature really must override a game write, first establish which copies run that write (owner only, or every copy including the server's), and correct the server once through the game's own command at a point you can justify from the message order. Never with timers or repeated resends. New ground in networking is where the bugs are that no solo test finds.
- **Over-engineering**: settings for everything, abstraction layers, managers wrapping one hook. Each extra branch is a niche bug for some subset of users.
- **Replacing a method instead of calling `orig`**, or rewriting a whole method in an On hook to change one line (use an IL hook).

## Testing

Launching the game isn't proof of functionality, and neither is seeing your code path run. Other mods change the same methods in ways you don't expect: the real test is multiplayer with a big mod list.

What you can do: build with 0 warnings, deploy to the test profile, and after the human has launched the game, read `BepInEx/LogOutput.log` for your plugin loading and for errors from your hooks.

What you hand over: numbered in-game test steps ([docs/testing.md](docs/testing.md)) for solo, host with a client, client in another host's lobby when the owner's side matters, and a profile with popular mods that touch the same code. Say what should happen and what a failure looks like. Don't try to play-test through screen control or input automation.
