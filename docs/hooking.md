# Hooking

Builds on the R2Wiki's [Hooking](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Hooking/), [What is Hooking?](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Hooking/Hooking/), [On Hook](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Hooking/On-Hook/) and [IL Hook](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Hooking/IL-Hook/) pages. Read those first; this page adds what is easy to get wrong.

## Pick the lightest tool

1. **A game event or callback**, if one fires where you need it. Subscribing costs nothing and can't break other mods.
2. **An R2API module** that already does it, e.g. `RecalculateStatsAPI.GetStatCoefficients` for stat changes instead of hooking `CharacterBody.RecalculateStats` (see [content.md](content.md)).
3. **An On hook**: code before and/or after the original, calling `orig`.
4. **An IL hook**: when you must change something in the middle of a method.

Some events and callbacks in 1.4.1 (all static members; check the decompiled invocation site before relying on where one runs):

| Event | Where it runs |
|---|---|
| `RoR2Application.onLoad` | Once, at the end of game loading. It is cleared after it fires, so subscribe in `Awake`. |
| `Run.onRunStartGlobal`, `Run.onRunDestroyGlobal` | Every machine |
| `Stage.onStageStartGlobal` / `Stage.onServerStageBegin` | Every machine / server |
| `CharacterBody.onBodyStartGlobal`, `CharacterBody.onBodyInventoryChangedGlobal`, `CharacterMaster.onStartGlobal` | Every copy of the object, on every machine |
| `GlobalEventManager.onCharacterDeathGlobal`, `onServerDamageDealt`, `OnInteractionsGlobal` | Server |
| `TeleporterInteraction.onTeleporterChargedGlobal` | Every machine (the charged state runs everywhere) |
| `ContentManager.collectContentPackProviders` | Once, when content loads |

```csharp
GlobalEventManager.onCharacterDeathGlobal += OnCharacterDeath; // in Awake

// Server only: GlobalEventManager.OnCharacterDeath returns early on clients.
private static void OnCharacterDeath(DamageReport report)
{
    if (report.attackerBody && report.victimIsBoss) { /* ... */ }
}
```

`report.attackerBody` is null when the damage had no attacker (fall damage, for one: `GlobalEventManager.OnCharacterHitGroundServer` sets `attacker = null`), so checking it isn't defensive noise.

## On hooks

`template/ExampleModPlugin.cs` shows the shape. Type `On.RoR2.HealthComponent.TakeDamage +=` and let the IDE generate the handler: `orig`, then `self` for instance methods, then the method's parameters.

- **Call `orig` exactly once.** Hooks chain: the last one registered runs first and calls the next through `orig`, down to the game's method. Not calling it skips the game's code *and every mod hooked before you*. If you really must skip the method, say why in a comment and make the condition as narrow as possible.
- **Overloads** get their parameter types appended to the name. In MMHOOK.RoR2 2025.12.9, `HealthComponent.TakeDamageForce` has three: `On.RoR2.HealthComponent.TakeDamageForce_DamageInfo_bool_bool`, `..._Vector3_bool_bool` and `..._PhysForceInfo`. Check the decompiled class for overloads before hooking; the IDE's autocomplete lists them.
- **Return values**: `var result = orig(self, ...);`, adjust, return it.
- **Coroutines**: `yield return orig(self);` runs your code after the whole coroutine. AutoSprint hooks `EntityStateCatalog.Init` this way, because the entity state catalog is filled after `[SystemInitializer]` methods run (`RoR2Application.InitializeGameRoutine`).
- **Properties have no MMHOOK hooks.** Hook the accessor manually with MonoMod's `Hook`:
  ```csharp
  // using MonoMod.RuntimeDetour; using HarmonyLib;
  new Hook(AccessTools.PropertyGetter(typeof(DeathRewards), nameof(DeathRewards.goldReward)), DeathRewards_get_goldReward);

  private static uint DeathRewards_get_goldReward(Func<DeathRewards, uint> orig, DeathRewards self) => orig(self) * 2;
  ```
  Before hooking a setter, see who writes the property and on which machines ([networking.md](networking.md)).
- **Tiny methods may be inlined** by the JIT into their callers; a hook on them then doesn't run from those call sites. Hook a caller instead. (Harmony's [Edge cases](https://harmony.pardeike.net/articles/patching-edgecases.html) page, "Inlining"; MonoMod detours have the same limit.)
- **Register once, in `Awake`.** To switch a feature off at runtime, check a config value inside the hook; unhooking is expensive and rarely needed (On Hook page, General advice).

## IL hooks

```csharp
// using Mono.Cecil.Cil; using MonoMod.Cil; using HarmonyLib (for AccessTools); Log is the plugin's ManualLogSource
IL.RoR2.HealthComponent.TakeDamageProcess += HealthComponent_TakeDamageProcess; // in Awake

// Crowbar: `if (num >= fullCombinedHealth * 0.9f)` -> 0.8f
private static void HealthComponent_TakeDamageProcess(ILContext il)
{
    var c = new ILCursor(il);
    if (!c.TryGotoNext(MoveType.After,
            x => x.MatchCallOrCallvirt(AccessTools.PropertyGetter(typeof(HealthComponent), nameof(HealthComponent.fullCombinedHealth))),
            x => x.MatchLdcR4(0.9f)))
    {
        Log.LogError($"IL hook {il.Method.Name} failed: Crowbar threshold not found");
        return;
    }
    c.EmitDelegate<Func<float, float>>(threshold => 0.8f);
}
```

(The pattern is unique in `TakeDamageProcess` in 1.4.1.) The rules, from the IL Hook page and the community's mods:

- **`TryGotoNext`, not `GotoNext`.** On a failed match, log an error that names the hook and return, leaving the method unchanged. This is what AutoSprint and MiscFixes do. Don't throw from the manipulator and don't wrap it in a try/catch.
- **Match as little as uniquely identifies the spot.** Other mods insert instructions into popular methods (`CharacterBody.RecalculateStats`, `HealthComponent.TakeDamageProcess`, `GlobalEventManager.ProcessHitEnemy`), so long patterns break. Use a landmark first (`TryGotoNext(x => x.MatchLdsfld(typeof(RoR2Content.Items), nameof(RoR2Content.Items.StickyBomb)))`) and then the short pattern.
- **Don't hardcode locals**: capture them (`x.MatchStloc(out int index)`).
- **`c.Index++` only inside a block you matched**; you can't know what another mod inserted outside it.
- **Prefer `EmitDelegate`** for logic (plain C#, takes the values on the stack in order, returns what goes back). Avoid `Remove`/`RemoveRange`: other instructions or branches may point at what you remove; redirect with a branch instead.
- Use `MatchCallOrCallvirt` for calls, and `nameof` with the publicized members for every name.
- Coroutines: the code is in the state machine's `MoveNext`: `new ILHook(AccessTools.Method(typeof(T), nameof(T.Coroutine)).GetStateMachineTarget(), Manipulator)` (`GetStateMachineTarget` is in `MonoMod.Utils`).
- IL hooks on one method are re-applied from the original each time a mod adds one, so a manipulator can run several times, and an error logged many times started at its first occurrence.
- Read the IL you're matching (see [decompiling.md](decompiling.md)); `Log.LogInfo(il)` prints what your manipulator actually sees.

## HarmonyX

BepInExPack ships HarmonyX, and experienced modders use it: MiscFixes applies `[HarmonyPatch]` classes with `[HarmonyPrefix]`, `[HarmonyPostfix]` and `[HarmonyILManipulator]` (an IL manipulator with the same `ILContext`/`ILCursor` API as above). It's a fine choice when:

- you don't want a HookGenPatcher dependency;
- you patch things MMHOOK doesn't cover (property accessors, other mods' methods);
- you prefer attribute declarations.

The same rules apply: a prefix returning `false` is skipping `orig`; name targets with `nameof` and pass argument types for overloads (`[HarmonyPatch(typeof(T), nameof(T.M), typeof(A), typeof(B))]`), otherwise Harmony can't pick one; apply everything with `new Harmony(PluginGUID).PatchAll()` in `Awake`. Pick one hooking style per mod unless there's a reason for both. Use `__instance` and publicized fields rather than `___field` injection or `AccessTools` strings.
