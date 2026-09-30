# RoR2LLMModding

What a coding agent (Claude Code, Codex, Cursor, ...) needs to know to write Risk of Rain 2 mods the way the RoR2 modding community expects: its conventions, the game's networking model, how to test, and how to publish. It targets game version 1.4.1 and builds on the community's [R2Wiki](https://risk-of-thunder.github.io/R2Wiki/), which it links to throughout.

It exists because AI-written mods tend to share the same problems: defensive code nobody can read, errors swallowed by try/catch, networking that fights the game, and "it launched, so it works". Facts were checked against the decompiled game, the community's tools and mods, or their documentation; what couldn't be checked is marked as unverified.

## Contents

| Path | What |
|---|---|
| [AGENTS.md](AGENTS.md) | Rules for the agent: process, hard rules, anti-patterns, testing. [CLAUDE.md](CLAUDE.md) imports it for Claude Code. |
| [docs/setup.md](docs/setup.md) | Environment, NuGet references, the project template, build and deploy |
| [docs/decompiling.md](docs/decompiling.md) | Reading the game's code and IL |
| [docs/hooking.md](docs/hooking.md) | Game events, On and IL hooks, HarmonyX |
| [docs/networking.md](docs/networking.md) | Authority, who needs the mod, a worked sprint example, custom messages |
| [docs/content.md](docs/content.md) | R2API, catalogs, startup timing, asset loading, the network mod list |
| [docs/config.md](docs/config.md) | BepInEx config and Risk of Options |
| [docs/testing.md](docs/testing.md) | What an agent can check, and writing test steps for a human |
| [docs/publishing.md](docs/publishing.md) | Thunderstore package rules and versioning |
| [docs/reading-list.md](docs/reading-list.md) | Guides and mods worth reading |
| [template/](template/) | A minimal mod that builds with `dotnet build` and 0 warnings |

## Using it

- **Claude Code**: open the mod's folder and add a line to its `CLAUDE.md` that imports this repo's rules, e.g. `@../RoR2LLMModding/AGENTS.md` (adjust the path), or start Claude Code in this folder and create the mod next to it.
- **Other agents**: point them at `AGENTS.md` (many read an `AGENTS.md` in the working directory automatically) and let them follow its links into `docs/`.
- **A new mod**: copy `template/` into its own repo and follow [docs/setup.md](docs/setup.md#starting-a-new-mod-from-it).

The agent writes and builds the code; a human plays it, asks the [modding Discord](https://discord.gg/5MbXZvd) when a design question comes up, and uploads it.

## License

The Unlicense (public domain). See [LICENSE](LICENSE).
