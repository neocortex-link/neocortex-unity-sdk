## Neocortex Unity SDK
The Neocortex Unity SDK is a Unity package that allows you to easily integrate Neocortex into your Unity project.
The SDK provides a set of APIs that allow you to interact with the projects created on the Neocortex web platform.

You can find more about the Unity SDK integration in our documentation: https://docs.neocortex.link

## Requirements
- Neocortex account [Sign Up Here](https://neocortex.link/register)
- Unity 6 (6000.0) or above [Download Here](https://unity3d.com/get-unity/download)
- Git version control system [Download Here](https://git-scm.com/download)

## Installation
- Open your Unity project
- Go to `Window` > `Package Manager`
- Click on the `+` button and select `Add package from git URL`
- Paste the following URL: `https://github.com/neocortex-link/neocortex-unity-sdk.git`
- Click on the `Add` button

## Quick Start (two clicks to a talking character)
1. **Paste your API key.** The Neocortex Settings window opens by itself on first install (or via `Tools` > `Neocortex` > `Settings`). Create a key on the [API Keys](https://neocortex.link/dashboard/api-keys) page, paste it, hit `Save` — the window shows your account status and lists your characters.
2. **Scaffold the scene.** Right-click in the Hierarchy → `Neocortex` > `Complete Voice Chat` (or `Complete Text Chat`). This places the chat UI, creates a fully wired `Neocortex Character` object (Smart Agent + audio + microphone + UI glue) — nothing to drag.
3. **Pick your character.** On the `Neocortex Character` object, choose your character from the dropdown (fetched from your account — no id copying needed).
4. **Press Play** and talk.

Characters are built in the [Neocortex web platform](https://neocortex.link/dashboard/characters) with the node editor.

## The high-level components
| Component | What it does |
|---|---|
| `NeocortexSmartAgent` | The character: send text/audio, receive chat lines, emotions, actions. One per character, lives on its GameObject. |
| `NeocortexAudioReceiver` | THE microphone: works on desktop, mobile and WebGL, handles permission and mic selection internally. |
| `NeocortexChatUI` | The glue for ONE character: binds an agent to the chat panel, inputs and thinking indicator — the standard conversation loop with zero code. |
| `NeocortexGroupDirector` | Multi-character scenes: assign several Smart Agents, the director orchestrates who speaks; each character speaks through its own agent. |
| `NeocortexGroupChatUI` | The same glue for a whole cast: one UI bound to the director instead of one per character. |
| `NeocortexChatPanel` | The transcript: bubbles with per-sender avatars and colors. |
| `NeocortexInteractable` | Makes a GameObject part of what characters perceive — name, properties and position go out with every message. |
| `NeocortexActionQueue` | Runs the actions a reply triggers, in order, through coroutine handlers you register per keyword. |

> In a group scene use **one** `NeocortexGroupChatUI` on the director — not a `NeocortexChatUI` per character, or every reply is printed once per UI.

## API Reference
After setting up the Neocortex SDK in your Unity project, you can start using the APIs to interact with the Neocortex project.

### Neocortex Smart Agent component
The `Neocortex Smart Agent` component is the main component that allows you to interact with the Neocortex project. 

<!-- REPLACE: screenshot of the Smart Agent inspector (character dropdown, Chat Lines Mode, Audio Source, Load History On Start) -->
<p align="center">
  <img width="393" alt="Neocortex Smart Agent component" src="https://placehold.co/393x420/1f1f1f/f59e0b/png?text=REPLACE%0ASmart+Agent+inspector">
</p>

**public async void TextToText(string message)**
  - Send a text message to the Neocortex project, and expect a text response.
  - Parameters:
    - `message`: The text message to send.
  - Example:
    ```csharp
    var smartAgent = GetComponent<NeocortexSmartAgent>();
    smartAgent.OnChatResponseReceived.AddListener((response) =>
    {
        Debug.Log($"Message: {response.message}");
        Debug.Log($"Action: {response.action}");
    });
    smartAgent.TextToText("Hello, Neocortex!");
    ```

**public async void TextToAudio(string message)**
  - Send a text message to the Neocortex project, and expect a audio response.
  - Parameters:
    - `message`: The text message to send.
  - Example:
    ```csharp
    var audioSource = GetComponent<AudioSource>();
    var smartAgent = GetComponent<NeocortexSmartAgent>();
    smartAgent.OnChatResponseReceived.AddListener((response) =>
    {
        Debug.Log($"Message: {response.message}");
        Debug.Log($"Action: {response.action}");
    });
    smartAgent.OnAudioResponseReceived.AddListener((audioClip) =>
    {
        audioSource.clip = audioClip;
        audioSource.Play();
    });
    
    smartAgent.TextToAudio("Hello, Neocortex!");
    ```

**public async void AudioToText(AudioClip audio)**
  - Sends an audio clip to the Neocortex project. This method is used with `NeocortexAudioReceiver` component to send audio data.
  - Parameters:
    - `audioClip`: The audio clip to send.
  - Example:
    ```csharp
    var smartAgent = GetComponent<NeocortexSmartAgent>();
    smartAgent.OnTranscriptionReceived.AddListener((message) =>
    {
        Debug.Log($"You: {message}");
    });

    var audioReceiver = GetComponent<NeocortexAudioReceiver>();
    audioReceiver.OnAudioRecorded.AddListener((audioClip) =>
    {
        Debug.Log($"Audio Data Length: {audioClip.samples}");
        smartAgent.AudioToText(audioClip);
    });

    // Start recording audio for 3 seconds
    audioReceiver.StartMicrophone();
    await Task.Delay(3000);
    audioReceiver.StopMicrophone();
    ```

**public async void AudioToAudio(AudioClip audio)**
  - Sends an audio clip to the Neocortex project and expects an audio response. This method is used with `NeocortexAudioReceiver` component to send audio data.
  - Parameters:
    - `audioClip`: The audio clip to send.
  - Example:
    ```csharp
    var audioSource = GetComponent<AudioSource>();
    var smartAgent = GetComponent<NeocortexSmartAgent>();
    smartAgent.OnAudioResponseReceived.AddListener((audioClip) =>
    {
        audioSource.clip = audioClip;
        audioSource.Play();
    });
    smartAgent.OnTranscriptionReceived.AddListener((message) =>
    {
        Debug.Log($"You: {message}");
    });
    smartAgent.OnChatResponseReceived.AddListener((response) =>
    {
        Debug.Log($"Message: {response.message}");
        Debug.Log($"Action: {response.action}");
    });

    var audioReceiver = GetComponent<NeocortexAudioReceiver>();
    audioReceiver.OnAudioRecorded.AddListener((audioClip) =>
    {
        Debug.Log($"Audio Data Length: {audioClip.samples}");
        smartAgent.AudioToAudio(audioClip);
    });

    // Start recording audio for 3 seconds
    audioReceiver.StartMicrophone();
    await Task.Delay(3000);
    audioReceiver.StopMicrophone();
    ```

**public UnityEvent<ChatResponse> OnChatResponseReceived**
  - Event that is triggered when the Neocortex project responds to a text message.
  - Parameters:
    - `response`: The response from the Neocortex project.
  - Example:
    ```csharp
    var smartAgent = GetComponent<NeocortexSmartAgent>();
    smartAgent.OnChatResponseReceived += (response) =>
    {
        Debug.Log($"Message: {response.message}");
        Debug.Log($"Action: {response.action}");
    };
    ```

**public UnityEvent<string> OnTranscriptionReceived**
  - Event that is triggered when the Neocortex project transcribes an audio message to text.
  - Parameters:
    - `message`: The transcribed audio message.
  - Example:
    ```csharp
    var smartAgent = GetComponent<NeocortexSmartAgent>();
    smartAgent.OnTranscriptionReceived += (message) =>
    {
        Debug.Log($"You: {message}");
    };
    ```

**public UnityEvent<AudioClip> OnAudioResponseReceived**
  - Event that is triggered when the Neocortex project responds with an audio message.
  - Parameters:
    - `audioClip`: The audio clip received from the Neocortex project.
  - Example:
    ```csharp
    var audioSource = GetComponent<AudioSource>();
    var smartAgent = GetComponent<NeocortexSmartAgent>();
    smartAgent.OnAudioResponseReceived += (audioClip) =>
    {
        audioSource.clip = audioClip;
        audioSource.Play();
    };
    ```

**public UnityEvent<string> OnRequestFailed**
  - Event that is triggered when a request to the Neocortex project fails.
  - Parameters:
    - `error`: The error message.
  - Example:
    ```csharp
    var smartAgent = GetComponent<NeocortexSmartAgent>();
    smartAgent.OnRequestFailed += (error) =>
    {
        Debug.LogError(error);
    };
    ```

### NeocortexAudioReceiver component
The `NeocortexAudioReceiver` component is used to record audio data from the microphone via loudness of the souned, so you can have a hands free chat with the smart agent. On this component you can:
- pick the microphone device to use
- set the amplitude threshold for when to start and stop recording
- set the max wait time for the recording to automatically stop if no sound is detected

<!-- REPLACE: screenshot of the Audio Receiver inspector (microphone picker, push-to-talk, amplitude threshold, max wait time) -->
<p align="center">
  <img width="394" alt="Neocortex Audio Receiver component" src="https://placehold.co/394x360/1f1f1f/f59e0b/png?text=REPLACE%0AAudio+Receiver+inspector">
</p>

**public void StartMicrophone()**
  - Starts recording audio from the microphone
  - Example:
  ```csharp
  var audioReceiver = GetComponent<NeocortexAudioReceiver>();
  audioReceiver.StartMicrophone();
  ```

**public void StopMicrophone()**
  - Stops recording audio from the microphone
  - Example:
  ```csharp
  var audioReceiver = GetComponent<NeocortexAudioReceiver>();
  audioReceiver.StopMicrophone();
  ```

**public UnityEvent<AudioClip> OnAudioRecorded OnAudioRecorded**
  - Event that is triggered when audio data is recorded from the microphone.
  - Returns:
    - `audioClip`: The recorded audio clip.
  - Example:
  ```csharp
  var audioReceiver = GetComponent<NeocortexAudioReceiver>();
  audioReceiver.OnAudioRecorded.AddListener((audioClip) =>
  {
      Debug.Log($"Audio Data Length: {audioClip.samples}");
  });
  ```

### Account & Usage API
The SDK exposes two read-only endpoints for gating smart NPC features. Calling them is free — they never cost a credit.

**GET /account — `ApiRequest.GetAccount()`**
  - Returns the developer account info: `tier` (`FREE` / `PRO` / `TEAM`), owner `email`, `creditsRemaining`, and `nextRefresh` (nullable).
  - You can also view this in the editor under `Tools` > `Neocortex` > `Account Status`.
  - Example:
    ```csharp
    var apiRequest = new ApiRequest();
    ApiAccountResponse account = await apiRequest.GetAccount();
    Debug.Log($"{account.tier}: {account.creditsRemaining} credits left");
    ```

**GET /usage — `ApiRequest.GetUsage(playerId, characterId)`**
  - Returns the team credit `status` (`ok` / `low` / `empty`) and `creditsRemaining`, plus per-player usage when `playerId` is passed and per-character usage when `characterId` is passed. `overLimit` reflects caps configured in the dashboard. An unknown player returns zero usage, not an error.
  - `playerId` is the external player id the game already uses for chat — by default the SDK sends `SystemInfo.deviceUniqueIdentifier`.

**NeocortexUsageGate**
  - A small helper that caches usage results and turns them into events, so you can gate features without polling. On request failure it raises `OnRequestFailed` and fails open instead of blocking the game.
  - Example:
    ```csharp
    var usageGate = new NeocortexUsageGate();
    usageGate.OnLowCredits += usage => Debug.LogWarning($"Low credits: {usage.creditsRemaining} left");
    usageGate.OnCreditsEmpty += _ => DisableSmartNpcUi();
    usageGate.OnPlayerOverLimit += _ => ShowDailyLimitMessage();
    usageGate.OnCharacterOverLimit += _ => DisableThisNpc();

    // Cheap to call before every message; cached within MinRefreshInterval (default 30s)
    bool canChat = await usageGate.CanUseSmartNPC(characterId: smartAgent.characterID);
    if (canChat)
    {
        smartAgent.TextToText(message);
    }

    // Optional: keep the flags warm in the background (low frequency)
    usageGate.StartAutoRefresh(intervalSeconds: 300, characterId: smartAgent.characterID);
    ```
  - See `UsageGatingSample` in the samples for a full chat example.

### Chat Lines
A reply can arrive as ordered **chat lines** — short chunks that drop in one after another as separate messages, each with its own emotion (their text concatenated equals the full reply). It's all on the `NeocortexSmartAgent` you already use: set one **Chat Lines Mode** dropdown. The message drop is the same in every mode; the mode only decides the audio.

| Chat Lines Mode | What the player gets | Cost |
|---|---|---|
| `Off` *(default)* | One normal reply, unchanged | — |
| `Text` | Chat lines drop in as messages, emotion per line | No extra cost |
| `SingleAudio` | Same, plus one voice clip for the whole reply | 1 audio credit |
| `PerLineAudio` | Same, but each line is voiced separately, in order | ⚠️ ~1 audio credit **per line** |

```csharp
agent.ChatLinesMode = ChatLinesMode.Text; // or SingleAudio / PerLineAudio

agent.OnChatLineStarted.AddListener(line => chatPanel.AddMessage(line.text, false));
agent.OnEmotionChanged.AddListener(emotion => animator.SetTrigger(emotion.ToString()));
agent.OnReplyFinished.AddListener(() => Debug.Log("Character finished speaking"));

// Send as usual — nothing else changes. Input sent while the character is still speaking is
// queued and submitted once the reply finishes (no barge-in).
agent.TextToText("Hello!");
```

The audio modes need an `AudioSource` assigned on the agent. `PerLineAudio` plays line 1 as soon as its clip is ready while later lines keep synthesizing, and it's credit-aware: when the balance is low it quietly falls back to a single clip, and when empty to text only — so it degrades instead of failing. A reply with no chat lines (older server) plays as one line, exactly like a normal reply.

In `Text` mode the gap before each line is estimated from its length, so a reply arrives at a natural pace instead of all at once. `OnComposingNextLine` fires during each gap — `NeocortexChatUI` uses it to show the thinking indicator, so the pause reads as "typing…".

### Perception: what the character can sense
Add a `NeocortexInteractable` to any GameObject and it becomes part of what nearby characters perceive — its properties and position go out with every message automatically, no code.

<!-- REPLACE: screenshot of the Neocortex Interactable inspector (properties list, Id, Resolved Id) -->
<p align="center">
  <img width="394" alt="Neocortex Interactable component" src="https://placehold.co/394x300/1f1f1f/f59e0b/png?text=REPLACE%0AInteractable+inspector">
</p>

- **Properties** are free-form name/value pairs describing what the thing is and its current state: `name = Blue Cube`, `type = cube`, `color = blue`, `locked = true`. A `name` property is seeded from the GameObject when you add the component; edit it, add more, or remove any.
- **Id** is how characters reference the thing. Leave it empty and a short stable id is derived from the scene path (shown as `Resolved Id`), so two objects called "Red Cube" stay distinguishable.
- Put one on a character's own GameObject and it links to that character automatically, so characters perceive each other.
- The character does the interpreting: it receives raw positions and works out what is near, far or worth acting on. The SDK never pre-computes distances or directions.
- The agent contributes its own position too, and perception is bounded (nearest first) so a busy scene stays cheap.

### Actions
Actions are the keywords you author on the character in the web platform. Each triggered action carries the **id of the entity it applies to**, so a reply can stack several actions each pointing at a different thing — "go to the blue cube, then the red one" arrives as two `GO_TO` actions with two different targets.

```csharp
public class ChatAction { public string name; public string targetId; }
```

`NeocortexActionQueue` runs them one at a time, in order, through a coroutine handler you register per keyword:

```csharp
var queue = GetComponent<NeocortexActionQueue>();
queue.RegisterAction("GO_TO_CUBE", GoToCube);
queue.OnUnhandledAction += keyword => Debug.Log($"No handler for {keyword}");
queue.OnQueueCompleted += () => Debug.Log("All actions done");

private IEnumerator GoToCube(ChatAction action)
{
    // Resolve the LIVE object by id, so it works even if the thing has moved.
    NeocortexInteractable target = Find(action.targetId);
    ...
}
```

**Trigger** decides *when* a reply's actions run:

| Trigger | Fires on | Feel |
|---|---|---|
| `WhenResponseReceived` *(default)* | the reply arriving | acts immediately, possibly before speaking |
| `WhenSpeechStarts` | the first spoken line | movement and voice begin together |
| `AfterReplySpoken` | the reply finishing | speaks the line, then acts |

The last two need an audio Chat Lines Mode; in `Off` mode there is no speech to wait for, so the queue fires on arrival and warns once.

### Group chat
Several characters in one shared conversation. Assign the cast to a `NeocortexGroupDirector`; it sends one group turn and routes each reply back to the matching agent, so every character speaks with its own voice, animation and events.

<!-- REPLACE: screenshot of the Group Director + Group Chat UI inspectors side by side -->
<p align="center">
  <img width="700" alt="Neocortex Group Director and Group Chat UI" src="https://placehold.co/700x360/1f1f1f/f59e0b/png?text=REPLACE%0AGroup+Director+%2B+Group+Chat+UI">
</p>

```csharp
director.Send("Hi everyone, introduce yourselves");  // the AI director picks who answers
director.SendTo(alice, "Alice, what do you think?"); // one character answers this turn
director.Continue();                                 // no player input: the cast talks among themselves
director.SendAudio(clip);                            // voice in: transcribed, then sent to the group
```

| Member | What it does |
|---|---|
| `AddAgent` / `RemoveAgent` | Change the cast mid-scene. The server notices who arrived or left and the cast reacts. |
| `Agents`, `IsBusy`, `SessionId`, `ClearSession()` | Current cast, whether a turn is running, and the shared scene session. |
| `GetHistory(limit, before)` | The shared transcript, name-labeled per speaker. |
| `OnSpeaker` | Raised per speaker with its `GroupMessage` (`name`, `lines`, `actions`). |
| `OnPlayerSpeech` | The player's spoken line, once transcribed. |
| `OnTurnStarted` / `OnTurnFinished` | Lock and release input for the duration of a turn. |
| `OnGroupResponseReceived`, `OnHistoryReceived`, `OnRequestFailed` | Whole-turn payload, history, and failures. |

A character that joins later is **new to the conversation** — it sees the transcript only from the moment it joined, so secrets shared before it arrived stay secret. A multi-character cast needs a Pro/Team API key; a cast of one behaves like normal single-character chat on any tier.

Add a `NeocortexGroupChatUI` on the director's GameObject for the whole UI loop (input, transcript, thinking indicator, mic) — the group counterpart of `NeocortexChatUI`.

### Chat panel appearance
`NeocortexChatPanel` draws the transcript. Messages carry a sender name, which becomes the avatar's initial and picks the bubble color — so in a group scene it's clear who said what.

<!-- REPLACE: screenshot of a group conversation showing avatars and per-sender bubble colors -->
<p align="center">
  <img width="700" alt="Chat panel with avatars" src="https://placehold.co/700x420/1f1f1f/f59e0b/png?text=REPLACE%0AChat+panel+with+avatars">
</p>

```csharp
chatPanel.AddMessage("Alice", "Well met, traveller.", false); // sender, text, isUser
chatPanel.AddMessage("Something happened.", false);           // no sender: no avatar
chatPanel.DisplayAvatars = false;                             // turn avatars off
```

Player and character bubble/text colors are set on the component. Assign your own `NeocortexMessage` prefab to restyle every bubble; a prefab without an Avatar object simply shows no avatar.

### Chat history
Conversations persist server-side per session. Toggle **Load History On Start** on the agent to replay it via `OnChatHistoryReceived`, or page back manually:

```csharp
ApiChatHistory page = await agent.RequestChatHistory(limit: 20);
// page.messages: content, sender, name, addressedTo, emotion, actions, createdAt
// Pass page.nextCursor as `before` to load older messages; null once you reach the start.
```

## Sample Projects
Import them from the Package Manager window under the `Samples` section of the Neocortex package.

| Scene | Shows |
|---|---|
| `1 - Text Chat Demo` | Typed conversation with one character, chat lines, idle/thinking/talking animation. |
| `2 - Audio Chat Demo` | The same with voice in and out, plus blendshape face animation. |
| `3 - Actions Demo` | Stacked actions (`DANCE`, `JUMP`) played in order through the action queue. |
| `4 - Interactables Demo` | Perception and targeting: the character walks to the cube it means, by `targetId`. |
| `5 - Group Chat Demo` | Voice group chat with a living roster — characters walk in to join and walk off to leave. |

`UsageGatingSample.cs` shows credit-aware gating with `NeocortexUsageGate`.
