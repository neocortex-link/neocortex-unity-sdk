# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [0.5.0] 29 July 2026
- API v3: unified base URL, per-speaker `lines` and stacked `actions` in every response
- Perception: each message carries the `NeocortexInteractable`s the character can sense, with stable ids and free-form name/value properties
- Targeted actions: every action is `{ name, targetId }`; register coroutine handlers with `agent.RegisterAction` and fire them on response, on speech start, or after the reply
- Group chat: `NeocortexGroupDirector` runs a cast of agents (`Send`, `SendTo`, `Continue`, `SendAudio`) and routes each reply to its own character
- `NeocortexChatUI` and `NeocortexGroupChatUI`: the whole conversation loop (input, bubbles, thinking indicator, mic handoff, history, errors) in one component
- `NeocortexAudioReceiver`: one microphone component for every platform, with permission handling and mic selection
- Auto voice input: an agent with a microphone listens, answers and re-arms by itself, so a lone Smart Agent is already a working conversation
- Chat lines: `chatLinesMode` (Off, Text, Single Audio, Per-Line Audio) delivers replies as ordered per-emotion messages paced by reading time, with `OnChatLineStarted`, `OnEmotionChanged`, `OnComposingNextLine` and `OnReplyFinished`
- Audio modes bring their own `AudioSource` and microphone, and degrade on their own when credits run low
- Account and usage endpoints, plus `NeocortexUsageGate` with credit/limit events and `CanUseSmartNPC`
- Chat history: `ChatHistoryEntry` carries speaker name, emotion and actions; `RequestChatHistory(limit, before)` pages backward; `loadHistoryOnStart` toggle
- Message avatars and per-sender colors in `NeocortexChatPanel`, and a custom message prefab can replace the built-in bubble
- Live microphone readout in the inspector: state, input level against the speech threshold, and the silence timeout draining to zero
- Character dropdown in the Smart Agent inspector, and a settings window that validates the API key
- One-click scaffolding: Hierarchy > Neocortex > Complete Text Chat / Complete Voice Chat
- Samples reorganized into one "Neocortex Samples" set: text, voice, actions, interactables, group chat and usage gating

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
