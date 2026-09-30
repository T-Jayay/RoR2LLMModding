# Config and Risk of Options

Builds on the R2Wiki's [Configuration](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Configuration/) and [Mod Compatibility: Soft Dependency](https://risk-of-thunder.github.io/R2Wiki/Mod-Creation/C%23-Programming/Mod-Compatibility:-Soft-Dependency/) pages. API details checked against BepInEx 5.4 (as shipped in BepInExPack 5.4.2122) and Risk of Options 2.8.6.

## BepInEx config

Bind entries in `Awake` with `Config.Bind(section, key, defaultValue, description)`; the file is `BepInEx/config/<PluginGUID>.cfg`. Only add settings someone will actually want to change: each one is another combination to test.

- Give numbers a range: `new ConfigDescription("...", new AcceptableValueRange<float>(0f, 10f))`. The `Value` setter clamps to it (`ConfigEntryBase.ClampValue`), so an out-of-range value can't reach your code. `AcceptableValueList<T>` does the same for a fixed set.
- React to changes with `entry.SettingChanged` (or `Config.SettingChanged` for all entries), or just read `entry.Value` where it's used.
- Say in the description whose value counts. Settings read by server-side code only matter on the host; the template's description says so.

## Risk of Options

[Risk of Options](https://thunderstore.io/c/riskofrain2/p/Rune580/Risk_Of_Options/) shows config entries in the game's settings menu. Mods usually treat it as a **soft dependency**: the mod works without it, and it's not in the manifest.

```csharp
[BepInDependency(RiskOfOptionsCompat.Guid, BepInDependency.DependencyFlags.SoftDependency)]
public sealed class ExampleModPlugin : BaseUnityPlugin
{
    private void Awake()
    {
        var multiplier = Config.Bind(/* ... */);
        if (RiskOfOptionsCompat.Enabled)
        {
            RiskOfOptionsCompat.Init(multiplier);
        }
    }
}

internal static class RiskOfOptionsCompat
{
    internal const string Guid = "com.rune580.riskofoptions";

    internal static bool Enabled => Chainloader.PluginInfos.ContainsKey(Guid);

    // Risk of Options types only inside NoInlining methods, so they load only when it is installed.
    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    internal static void Init(ConfigEntry<float> multiplier)
    {
        ModSettingsManager.AddOption(new StepSliderOption(multiplier, new StepSliderConfig
        {
            min = 0f,
            max = 10f,
            increment = 0.1f,
            FormatString = "{0:0.0}x",
            checkIfDisabled = () => NetworkClient.active && !NetworkServer.active, // greyed out on clients
        }));
    }
}
```

- Keep every Risk of Options type inside the bodies of the `NoInlining | NoOptimization` methods: not in fields, signatures or other classes. This is the soft dependency pattern from the R2Wiki, and the only place `NoInlining` belongs.
- Options: `CheckBoxOption` (bool), `SliderOption`/`StepSliderOption`/`FloatFieldOption` (float), `IntSliderOption`/`IntFieldOption` (int), `ChoiceOption` (enum), `KeyBindOption` (`KeyboardShortcut`), `StringInputFieldOption`, `ColorOption`, and `GenericButtonOption` for a button.
- Sliders take their range from the option's config (`SliderConfig.min`/`max`, default 0 to 100), not from the entry's `AcceptableValueRange`: set both.
- `ModSettingsManager` finds your mod with `Assembly.GetCallingAssembly()`, so call it from your own assembly. Without `ModSettingsManager.SetModIcon(sprite)` the mod list shows a placeholder icon; `SetModDescription` sets the text.
- A mod that *is* a Risk of Options feature (e.g. only adds a button) uses a hard dependency instead: `[BepInDependency(guid)]` plus `Rune580-Risk_Of_Options-2.8.6` in the manifest.
