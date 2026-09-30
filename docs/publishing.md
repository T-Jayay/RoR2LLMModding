# Publishing to Thunderstore

Builds on the Thunderstore wiki's [Creating a Package](https://wiki.thunderstore.io/mods/creating-a-package), [Updating a Package](https://wiki.thunderstore.io/mods/updating-a-package) and [Packaging Your Mods](https://wiki.thunderstore.io/mods/packaging-your-mods) pages. The human uploads; the agent prepares the zip and says what changed.

## The package

A zip with these at its root (names are case-sensitive):

| File | Rules |
|---|---|
| `manifest.json` | See below |
| `README.md` | The store page. UTF-8 Markdown. |
| `icon.png` | Exactly 256x256 PNG |
| `CHANGELOG.md` | Optional; shown on the store page. One `## <version>` section per release. |
| `<Mod>.dll` | Your plugin only: never game, BepInEx or other mods' DLLs. |

Mod managers install a DLL at the zip root into `BepInEx/plugins/<Team>-<Package>/`. From `template/` after a Release build (PowerShell):

```
Compress-Archive -Path thunderstore\*, bin\Release\netstandard2.1\ExampleMod.dll -DestinationPath ExampleMod-1.0.0.zip
```

Before uploading, install the zip into a clean profile with the mod manager's "Import local mod" and check it loads.

## manifest.json

```json
{
  "name": "ExampleMod",
  "version_number": "1.0.0",
  "website_url": "https://github.com/AuthorName/ExampleMod",
  "description": "Halves fall damage (configurable). Only the host needs it.",
  "dependencies": [
    "bbepis-BepInExPack-5.4.2122",
    "RiskofThunder-HookGenPatcher-1.2.9"
  ]
}
```

- `name`: letters, digits and `_` only (underscores display as spaces), at most 128 characters. Changing it later creates a different package.
- `version_number`: `Major.Minor.Patch`; each part is a whole number (1.0.10 is newer than 1.0.9).
- `description`: at most 250 characters; shown in mod lists.
- `website_url`: required key (may be `""`); set it to the source repo and make sure the repo is public before uploading.
- `dependencies`: `Team-Package-Version` strings (the "Dependency string" on each package page). Hard dependencies only: BepInExPack; HookGenPatcher if you use `On.`/`IL.` hooks; each R2API module you depend on (`RiskofThunder-R2API_Items-1.0.8`, ...). Soft dependencies such as Risk of Options stay out.

## Versions are immutable

An uploaded version can't be changed or re-uploaded. Any change, even to the README or manifest only, is a new version:

1. Bump `version_number` in `manifest.json` **and** `PluginVersion` in the plugin (they must match).
2. Add a `## <version>` entry to `CHANGELOG.md`.
3. Build, zip, test the zip, then hand it over.

## Upload

At [thunderstore.io/package/create](https://thunderstore.io/package/create/): the team, the Risk of Rain 2 community, and categories. Categories include `Mods`, `Tweaks`, `Client-side`, `Server-side`, `Items`, `Player Characters`, ... and **`AI Generated`**. Thunderstore's [Global Rules](https://wiki.thunderstore.io/moderation/global-rules) ask for that category on any package with AI-generated code or assets, and list untested or non-functional AI-generated mods among packages that may be removed.

## Store README

Short and factual:

- what the mod does, in a sentence or two;
- who needs it: host only, client only, or everyone (see [networking.md](networking.md));
- settings and their defaults;
- known incompatibilities, and where to report bugs (the repo's issues).

No marketing tone, no feature lists padded with things the game already does.
