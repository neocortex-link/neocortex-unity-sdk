# Neocortex Unity SDK

Bring the characters you build on the [Neocortex web platform](https://neocortex.link) into your Unity game: characters that talk, listen, act on the world around them, and hold a conversation with each other.

**Full documentation: [docs.neocortex.link](https://docs.neocortex.link/integrations/unity/quick-start)**

## What it does

- **Talk and listen.** Text or voice in, text or voice out. One microphone component that works on desktop, mobile and WebGL, permissions and device selection included.
- **Replies with delivery.** Every reply arrives as ordered chat lines, each with its own emotion, so you can pace them, animate per line and voice them separately.
- **Actions.** Characters trigger the action keywords you author, each carrying the entity it applies to, so one reply can act on several things in order.
- **Perception.** Tag objects in your scene and characters sense them, reason about where things are, and act on the one you meant.
- **Group scenes.** Several characters in one shared conversation with an AI director deciding who speaks, plus a roster that can change while the game runs.
- **A complete chat UI.** Panel, avatars, inputs and indicators, scaffolded into your scene in one click.
- **Usage aware.** Read your plan, credits and per player limits, and gate features before spending anything.

## Requirements

- A Neocortex account, [sign up here](https://neocortex.link/register)
- Unity 6 (6000.0) or above, [download here](https://unity.com/download)
- Git, [download here](https://git-scm.com/downloads)

## Installation

- Open your Unity project
- Go to `Window` > `Package Manager`
- Click `+` and choose `Add package from git URL`
- Paste `https://github.com/neocortex-link/neocortex-unity-sdk.git`
- Click `Add`

Then paste your API key into the settings window that opens, right click the Hierarchy and choose `Neocortex` > `Complete Voice Chat`, pick your character from the dropdown, and press Play.

The [Quick Start](https://docs.neocortex.link/integrations/unity/quick-start) walks through it with screenshots.

## Samples

Six scenes ship with the package, importable from the `Samples` tab of the Package Manager: text chat, audio chat, actions, interactables, group chat and usage gating. See [Sample Projects](https://docs.neocortex.link/integrations/unity/sample-projects).

## Documentation

| | |
| --- | --- |
| [Quick Start](https://docs.neocortex.link/integrations/unity/quick-start) | From install to a talking character. |
| [API Reference](https://docs.neocortex.link/integrations/unity/api-reference) | Every component, method and event. |
| [UI Elements](https://docs.neocortex.link/integrations/unity/ui-elements) | The chat panel, inputs and indicators. |
| [Sample Projects](https://docs.neocortex.link/integrations/unity/sample-projects) | What each sample scene demonstrates. |

Changes are listed in [CHANGELOG.md](CHANGELOG.md).

## Support

Questions and bug reports: [docs.neocortex.link/support](https://docs.neocortex.link/support).
