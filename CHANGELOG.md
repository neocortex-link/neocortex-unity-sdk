# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]
- Quiz support. `NeocortexQuizAgent` runs a whole quiz, game show or spoken test from a character id and a question set id. Your scene only has to say what the player did, with `Say` or `Choose`; question order, retries, hints, scoring and when the run ends are decided on the server from the question set.
- Each question carries the author's scene `keyword` (SHOW_RED_APPLE), on the current question and on the one after it, so a game can put the right thing on screen and preload the next while the host is still talking. Generated question ids stay out of your scene code.
- `OnExpectingChanged` says whether to collect an answer, a yes or no, or nothing at all, which is what a voice game needs to know when to open the microphone.
- `ApiRequest.BeginQuiz` and `ApiRequest.ContinueQuiz` for driving it yourself, `NeocortexQuizUI` to bind the standard chat widgets, and `GameObject > Neocortex > Complete Quiz` to build the whole rig in one click.
- `OnBusyChanged` says when a turn is in flight, so a thinking indicator no longer has to be inferred from `OnExpectingChanged`, which left it spinning forever once a run had finished.
- The Quiz Agent inspector picks the character AND the question set from dropdowns of your own team's, fetched with your API key, instead of two pasted ids. Question sets show how many questions they hold. Both raw id fields stay visible and editable, and a missing key or failed fetch leaves plain text boxes.
- Added `ApiRequest.GetQuestionSets()`, the question set half of the existing `GetCharacters()`.
- Host lines carry a `role`: `Greeting`, `Reaction`, `Question` or `Closing`. A turn often reacts to the last answer AND asks the next question, so `NeocortexQuizAgent` now raises `OnQuestionChanged` as the `Question` line begins rather than when the turn arrives. The scene changes on the right beat instead of while the host is still talking about the previous answer, which is what a voice game needs.
- The host now greets the player as its own turn and asks the first question on the next one, so a game can put the question's scene on screen when the question arrives rather than while the host is still saying hello. `NeocortexQuizAgent` fetches that second turn itself.
- Added a Quiz sample: press Start, type an answer, read what the host decides, and watch the scene follow the question's keyword. Three shapes are wired to SHAPE_TRIANGLE, SHAPE_SQUARE and SHAPE_CIRCLE; the sample never decides which one to show.
- Every request now has a deadline (30s for a turn, 20s for speech, 15s for a lookup) and every `UnityWebRequest` is disposed. Without one, a request that never answered left an agent busy, and the microphone shut, for the rest of the session.
- `WebRequest.Abort()` no longer poisons the requests that follow it. Cancellation is per call and linked to a source that is replaced on abort, rather than one source shared for the life of the object.
- `NeocortexQuizAgent` survives a scene change, a restart and a listener that throws: the turn in flight is identified by a token that every resume point checks, `OnDestroy` cancels and silences it, and the busy flag clears in a `finally`. A game bug in an `OnHostLine` handler no longer ends the run.
- Added `NeocortexQuizAgent.Abort()`, and `Begin()` on a live run now calls it rather than being ignored, so starting a second run cannot leave two answering into the same scene.
- The first question is fetched while the welcome is still being spoken, saving a second or two of silence at the start of every run.
- Each line's speech is requested while the line before it is still playing, so the gap between two spoken lines is a frame rather than a whole round trip. `OnHostLine` is now raised as its line starts rather than when the turn arrived, so a caption and the voice saying it are one event.
- Spoken clips are destroyed after they play. A run used to leak twenty to forty of them.
- Playback no longer waits on `AudioSource.isPlaying` alone: a paused `AudioListener`, a disabled source or a zeroed `timeScale` each left that wait either cutting the host off or hanging the turn for good.
- `OnEmotionChanged` is raised when the emotion changes, not once per line.
- What the player says while the host is talking is held rather than silently dropped: the newest one is sent when the turn lands, anything older than `maxPendingInputAge` (4s) is let go, and every drop is reported through the new `OnInputDropped(string, QuizInputDropReason)` with a reason. Turn it off with `queueInputWhileBusy`.
- Sending to a run that has already finished returns that run's closing turn instead of an error, so a client whose response was lost can still speak the sign off. A turn refused because the session is busy is retried twice before it fails.
- Added `ApiRequest.GetQuizSession(sessionId)` and `NeocortexQuizAgent.Resync()`. After a turn whose response never arrived, the run is read back from the server (the question on the table, the standings, what the host is waiting for) rather than guessed at locally. The lost utterance is never re-sent. Called automatically when a turn fails.
- `WebRequest.LastErrorCode` carries the API's own code for a failure, so a caller can branch on what happened rather than on the wording of a sentence.
- The Quiz UI answers a dropped utterance instead of ignoring it, and shows failures in the panel. The Quiz sample re-enables Start after a failure (one bad turn used to brick the demo) and sends an empty submit as the documented pass.

## [0.6.0] - 16 September 2026
- Characters start speaking as the voice arrives instead of waiting for the whole clip. First sound in well under a second, rather than two to four. Nothing to turn on; `Off` mode still returns a finished `AudioClip`, and an older server falls back automatically.
- Added `com.unity.cloud.gltfast` and `com.unity.cloud.draco` as dependencies. The sample scenes load Cora as a Draco-compressed glb, so without them the character is missing on import.
- Added `ApiRequest.GenerateAudioStream`, `ApiResponseType.Stream`, and a cancellation token on `WebRequest.Send`.

## [0.5.2] - 09 September 2026
- SpokenText field for STT backend.

## [0.5.1] - 20 August 2026
- Structured error handling with error codes and suppressed in-game chat error bubbles
- Unified `NeocortexAudioReceiver` into a single multiplatform component without duplicate backends
- Robust JSON enum deserialization with safe fallbacks for empty or unknown values

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
