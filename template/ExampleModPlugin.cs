using System.Security;
using System.Security.Permissions;
using BepInEx;
using BepInEx.Configuration;
using RoR2;
using UnityEngine.Networking;

// RiskOfRain2.GameLibs is publicized (private members compile as public); the R2Wiki's "Assembly References"
// page requires this line to use them.
#pragma warning disable CS0618 // SecurityAction.RequestMinimum is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

namespace ExampleMod;

[BepInPlugin(PluginGUID, PluginName, PluginVersion)]
public sealed class ExampleModPlugin : BaseUnityPlugin
{
    public const string PluginAuthor = "AuthorName";
    public const string PluginName = "ExampleMod";
    public const string PluginGUID = PluginAuthor + "." + PluginName;
    public const string PluginVersion = "1.0.0"; // same as version_number in thunderstore/manifest.json

    private static ConfigEntry<float> fallDamageMultiplier;

    private void Awake()
    {
        fallDamageMultiplier = Config.Bind("General", "Fall damage multiplier", 0.5f,
            new ConfigDescription("Multiplies fall damage; 1 is vanilla. Only the host's value matters.",
                new AcceptableValueRange<float>(0f, 10f)));

        On.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
    }

    // Damage is applied on the server only, so the host's copy of this mod decides.
    private static void HealthComponent_TakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
    {
        if (NetworkServer.active && (damageInfo.damageType.damageType & DamageType.FallDamage) != 0)
        {
            damageInfo.damage *= fallDamageMultiplier.Value;
        }
        orig(self, damageInfo);
    }
}
