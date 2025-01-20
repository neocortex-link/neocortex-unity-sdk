## Neocortex Unity SDK
The Neocortex Unity SDK is a Unity package that allows you to easily integrate Neocortex into your Unity project.
The SDK provides a set of APIs that allow you to interact with the projects created on the Neocortex web platform.

You can find more about the Unity SDK integration here in our documentatons: https://neocortex.link/docs/integrations/unity/quick-start

## Requirements
- Neocortex account [Sign Up Here](https://neocortex.link/register)
- Unity 2021.3 or above [Download Here](https://unity3d.com/get-unity/download)
- Git version control system [Download Here](https://git-scm.com/download)

## Installation
- Open your Unity project
- Go to `Window` > `Package Manager`
- Click on the `+` button and select `Add package from git URL`
- Paste the following URL: `https://github.com/neocortex-link/neocortex-unity-sdk.git`
- Click on the `Add` button

## Setup
### Save API Key
To start using the Neocortex SDK, you need to initialize it with your Neocortex API key. You can create a new API key from the Neocortex web platform by going to the [API Keys](https://neocortex.link/dashboard/api-keys) page.
- Create a new API key and copy it
- Open your Unity project
- Go to `Tools` > `Neocortex Settings`
- Paste the API key in the `API Key` field and click on the `Save` button

<p align="center">
  <img width="382" alt="neocortex_unity_settings" src="https://github.com/user-attachments/assets/517f906a-889a-48ee-a39b-a19daaff5648">
</p>


### Create a new Neocortex project
- Go to the [Neocortex web platform](https://neocortex.link/dashboard/projects) and create a new project
- Copy the project ID from the project details page
- Open your Unity project and go to your scene
- Create an empty GameObject and add the `Neocortex Smart Agent` component to it
- Paste the project ID in the `Project ID` field

## API Reference
After setting up the Neocortex SDK in your Unity project, you can start using the APIs to interact with the Neocortex project.

### Neocortex Smart Agent component
The `Neocortex Smart Agent` component is the main component that allows you to interact with the Neocortex project. 

<p align="center">
  <img width="393" alt="neocortex_unity_smart_agent_component" src="https://github.com/user-attachments/assets/9613bb88-87a9-4ba5-b412-d404c0bf63e3">
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

<p align="center">
  <img width="394" alt="neocortex_unity_audio_receiver_component" src="https://github.com/user-attachments/assets/58b17620-fec7-4c85-af38-699f292ce08e">
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

## Sample Projects
You can find sample projects that demonstrate how to use the Neocortex Unity SDK in the Package Manager window under the `Samples` section of the Neocortex package.
