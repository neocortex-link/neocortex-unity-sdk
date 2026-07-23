# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.5.0] 23 July 2026
### Added
- **API v3**: the SDK now targets the unified v3 base URL. Chat requests send `characterIds`; responses carry per-speaker `lines` + stacked `actions` (`ChatResponse` gains `actions`, `characterId`, `name`).
- **`NeocortexGroupDirector`**: multi-character scenes. Assign several `NeocortexSmartAgent`s (each on its own character's GameObject); the director sends one group turn (`Send` / `SendTo` / `Continue`), routes each reply to the matching agent, and plays speakers in order. Events: `OnSpeaker`, `OnGroupResponseReceived`, `OnTurnStarted/Finished`, `OnHistoryReceived`.
- **`NeocortexAudioReceiver`**: one microphone component (the facade) for every platform. Spawns the right capture backend internally (standalone/mobile vs WebGL), handles Android/iOS permission (`OnPermissionGranted/Denied`), and owns mic selection (`Microphones`, `SelectMicrophone`). Anything that accepts an `AudioReceiver` accepts it.
- **`NeocortexChatUI`**: the standard input→agent→panel conversation loop as one component — transcriptions, chat-line bubbles, thinking indicator, mic handoff, history painting and error surfacing, with auto-resolved optional references. Zero glue code for the common case.
- **One-click scaffolding**: Hierarchy → `Neocortex` > `Complete Text Chat` / `Complete Voice Chat` places AND wires the whole rig.
- **Character picker**: the Smart Agent inspector lists your characters in a dropdown (fetched via the new `/characters` endpoint); the settings window lists them with copy-id buttons. The plain string field remains as fallback.
- **Settings window**: opens automatically on first install, validates the API key, and (on WebGL) checks + one-click-fixes the template selection. Settings asset now exists independently of the window (`NeocortexSettingsProvider`).
- **Chat history enriched**: `ChatHistoryEntry` (renamed from `Message`) carries `speakerCharacterId`, server-resolved `name`, `addressedTo`, `emotion`, `actions`; `RequestChatHistory(limit, before)` pages backward via `nextCursor`; `NeocortexSmartAgent.loadHistoryOnStart` toggle; `NeocortexGroupDirector.GetHistory()` loads the shared group transcript, name-labeled.
- Audio chat-lines modes now raise `OnAudioResponseReceived` with each generated clip (notification — the agent still owns playback).

### Changed / Removed (breaking, pre-1.0)
- `Message` → `ChatHistoryEntry` (chat history model).
- `NeocortexSmartAgent.GetSessionID()` / `CleanSessionID()` obsolete shims removed — use `NeocortexSessionManager`.
- The platform capture backends (`NeocortexNativeAudioReceiver` / `NeocortexWebAudioReceiver`) are internal now (hidden from Add Component) — `NeocortexAudioReceiver` is the one public microphone component. Their shared config (`usePushToTalk`, `amplitudeThreshold`, `maxWaitTime`) moved to the `AudioReceiver` base (scene values migrate automatically).
- `MicrophonePermission` is superseded by `NeocortexAudioReceiver`'s built-in permission handling (component still ships for existing scenes).
- `NeocortexInteractable.IsSubject` removed (was never settable and always false).
- v3 group message shape: no more joint `message` / single `action` fields — use `lines` + `actions`.

### Earlier unreleased work, now shipping in 0.5.0
- Chat lines: `NeocortexSmartAgent.ChatLinesMode` (Off / Text / SingleAudio / PerLineAudio) delivers replies as ordered per-emotion messages that drop in one after another, with `OnChatLineStarted` / `OnEmotionChanged` / `OnReplyFinished` events. Audio modes are credit-aware and queue input during playback.
- Account & Usage endpoint implementation
- NeocortexUsageGate helper with credit/limit events and `CanUseSmartNPC` for gating smart NPC features
- Account Status editor window under Tools > Neocortex
- Samples reorganized into one "Neocortex Samples" set with all scenes at the top level, sample/helper scripts split, and shared visuals, plus new Chat Lines and Usage Gating sample scenes
- Samples now deliver replies as chat lines instead of one joint message (text samples use Text mode; audio samples use Single Audio at the same 1-credit cost)
- `NeocortexSmartAgent.AudioSource` property so a script can hand the agent its playback source at runtime
- `NeocortexChatPanel.messageItemPrefab` is now inspector-assignable (falls back to the built-in bubble), and `NeocortexMessage` exposes user/agent bubble and text colors

## [0.4.9] 29 March 2026
- Json library reference bug fix

## [0.4.8] 2 March 2026
- Session Manager to cache multiple sessions IDs 

## [0.4.7] 2 March 2026
- Conversation flow state in API response
- Player event logging 

## [0.4.6] 28 January 2026
- Emotion in generated audio responses

## [0.4.5] - 7 January 2026
- Font overwrite support in Chat Panel and Chat Input
- Unity 6 support

## [0.4.4] - 25 October 2025
- Chat Panel CleanMessages method fix

## [0.4.3] - 28 September 2025
- Neocortex Interactable component for Spatial Awareness
- Metadata handling in ApiRequest

## [0.4.2] - 19 September 2025
- Push-to-Talk UI button fix
- Handle string errors coming from audio gen endpoint

## [0.4.1] - 16 August 2025
- Emotion Node support, ApiResponse and ChatResponse has emotion field for enum values
- Audio and Chat Samples are updated with emotion value debugging

## [0.4.0] - 30 July 2025
- Neocortex API V2 implementation in API Request class
- Neocortex Smart Agent, GetChatHistory, Clean Session ID and Get Session ID methods
- Text Chat History sample project
- Chat Panel, Clean Messages method

## [0.3.7] - 2 April 2025
- Audio trimming improved

## [0.3.6] - 28 March 2025
- Microphone Permission utility for mobile builds
- Mobile Audio Chat Test sample

## [0.3.5] - 26 March 2025
- Neocortex Audio Receiver microphone picker improvements

## [0.3.4] - 21 February 2025
- Webrequest progress property and cancellation support

## [0.3.3] - 18 February 2025
- Various UI Elements fixes
- Web Request class decoupling
- Writing Indicator for Chat Panel

## [0.3.2] - 12 February 2025
- Right-to-Left language support in Chat Panel

## [0.3.1] - 3 February 2025
- Microphone dropdown component
- Component and sample updates

## [0.3.0] - 21 January 2025
- WebGL audio support

## [0.2.0] - 6 December 2024
- API updates and request unification
- Audio receiver fixes
- Sample project updates

## [0.1.0] - 21 October 2024
- Initial Release
