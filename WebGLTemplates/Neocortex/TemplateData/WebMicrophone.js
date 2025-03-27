let audioContext = null;
let microphoneStream = null;
let recorder = null;

let isUserSpeaking = false;
let usePushToTalk = false;

let amplitude = 0;
let amplitudeMultiplier = 10;
let amplitudeThreshold = 0.1;

let maxWaitTime = 1;
let elapsedWaitTime = 0;

let bufferSize = 2048;
let floatPCMPointer = -1;

const MicrophoneState = {
    NotActive: 0,
    Booting: 1,
    Recording: 2,
};

async function startRecording() {
    unityGame.SendMessage(objectName, "NotifyRecordingChange", MicrophoneState.Booting);

    try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        initializeMicrophone(stream);
    } catch (error) {
        console.error("Error capturing audio:", error);
        alert("Error capturing audio.");
        unityGame.SendMessage(objectName, "NotifyRecordingChange", MicrophoneState.NotActive);
    }
}

function stopRecording() {
    if (!audioContext) return;

    try {
        isUserSpeaking = false;
        if (recorder) {
            recorder.disconnect();
            recorder.onaudioprocess = null;
            recorder = null;
        }
        if (microphoneStream) {
            microphoneStream.disconnect();
            microphoneStream = null;
        }
        audioContext.close().catch(err => console.error("Error closing audio context:", err));
        audioContext = null;
    } catch (error) {
        console.error("Error stopping recording:", error);
    }

    unityGame.SendMessage(objectName, "NotifyRecordingChange", MicrophoneState.NotActive);
}

function initializeMicrophone(stream) {
    try {
        const audioTracks = stream.getAudioTracks();
        const sampleRate = audioTracks[0]?.getSettings().sampleRate || 48000;
        
        audioContext = new AudioContext({ sampleRate });
        microphoneStream = audioContext.createMediaStreamSource(stream);

        recorder = audioContext.createScriptProcessor(bufferSize, 1, 1);
        recorder.onaudioprocess = processAudio;

        microphoneStream.connect(recorder);
        recorder.connect(audioContext.destination);
        
        unityGame.SendMessage(objectName, "NotifyRecordingChange", MicrophoneState.Recording);
    } catch (error) {
        console.error("Error initializing microphone:", error);
        stopRecording();
    }
}

function processAudio(event) {
    try {
        const floatPCM = event.inputBuffer.getChannelData(0);
        unityGame.SendMessage(objectName, "LogWrittenBuffer", floatPCM.length);

        if (floatPCMPointer !== -1) {
            const writeTarg = new Float32Array(unityGame.Module.HEAP8.buffer, floatPCMPointer, bufferSize);
            writeTarg.set(floatPCM);
        }

        updateAmplitude(floatPCM);
        handleSpeechDetection(floatPCM);
    } catch (error) {
        console.error("Error processing audio:", error);
    }
}

function updateAmplitude(floatPCM) {
    let sum = floatPCM.reduce((acc, val) => acc + val * val, 0);
    amplitude = Math.sqrt(sum / floatPCM.length) * amplitudeMultiplier;
    unityGame.SendMessage(objectName, "UpdateAmplitude", amplitude);
}

function handleSpeechDetection(floatPCM) {
    if (usePushToTalk) return;

    const deltaTime = bufferSize / (audioContext?.sampleRate || 48000);

    if (!isUserSpeaking && amplitude > amplitudeThreshold) {
        isUserSpeaking = true;
    }

    if (isUserSpeaking) {
        if (amplitude < amplitudeThreshold) {
            elapsedWaitTime += deltaTime;
            if (elapsedWaitTime >= maxWaitTime) {
                elapsedWaitTime = 0;
                stopRecording();
            }
        } else {
            elapsedWaitTime = 0;
        }

        unityGame.SendMessage(objectName, "UpdateElapsedWaitTime", elapsedWaitTime);
    }
}
