let recorder = null;
let audioInput = null;
let audioContext = null;
let microphoneStream = null;

let floatPCMPointer = -1;

let objectName = "";
let bufferSize = 2048;
let isUserSpeaking = false;
let usePushToTalk = false;

let amplitude = 0;
let amplitudeMultiplier = 10;
let amplitudeThreshold = 0.1;

let maxWaitTime = 1;
let elapsedWaitTime = 0;

const MicrophoneState =
{
	NotActive: 	0,
	Booting: 	1,
	Recording: 	2
}

async function startRecording() {
	unityGame.SendMessage(objectName, "NotifyRecordingChange", MicrophoneState.Booting);
	
	try {
		const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
		startMicrophone(stream);
	} catch (error) {
		console.error("Error capturing audio:", error);
		alert("Error capturing audio.");
		unityGame.SendMessage(objectName, "NotifyRecordingChange", MicrophoneState.NotActive);
	}
}

function stopRecording()
{
	if(audioContext == null)
		return;
	
	isUserSpeaking = false;

	recorder.disconnect(audioContext.destination);
	recorder = null;
	
	microphoneStream.disconnect(recorder);
	microphoneStream = null;
	
	audioContext.close().catch((err) => console.error("Error closing audio context:", err));
	audioContext = null;

	unityGame.SendMessage(objectName, "NotifyRecordingChange", MicrophoneState.NotActive);
}

function startMicrophone(stream)
{	
	const audioTracks = stream.getAudioTracks();
	const sampleRate = audioTracks[0].getSettings().sampleRate || 48000;
	
	audioContext = new AudioContext({ sampleRate });
	microphoneStream = audioContext.createMediaStreamSource(stream);

	const numberOfInputChannels = 1;
	const numberOfOutputChannels = 1;
	
	recorder = audioContext.createScriptProcessor 
		? audioContext.createScriptProcessor(bufferSize, numberOfInputChannels, numberOfOutputChannels)
		: audioContext.createJavaScriptNode(bufferSize, numberOfInputChannels, numberOfOutputChannels);
	
	recorder.onaudioprocess = processAudio;

	microphoneStream.connect(recorder);
	recorder.connect(audioContext.destination)

    unityGame.SendMessage(objectName, "NotifyRecordingChange", MicrophoneState.Recording);
}

function processAudio(e)
{
	dstPtr = floatPCMPointer;
	floatPCM = e.inputBuffer.getChannelData(0);
	unityGame.SendMessage(objectName, "LogWrittenBuffer", floatPCM.length);

	writeTarg = new Float32Array(unityGame.Module.HEAP8.buffer, dstPtr, bufferSize);
	writeTarg.set(floatPCM);

	updateAmplitude(floatPCM);
	
	if(usePushToTalk) return;

	const deltaTime = bufferSize / audioContext.sampleRate;
	
	if(!isUserSpeaking && amplitude > amplitudeThreshold)
	{
		isUserSpeaking = true;
	}
	
	if(isUserSpeaking)
	{
		if(amplitude < amplitudeThreshold)
		{
			elapsedWaitTime += deltaTime;
			
			if(elapsedWaitTime >= maxWaitTime)
			{
				elapsedWaitTime = 0;
				stopRecording();
			}
		}
		else
		{
			elapsedWaitTime = 0;
		}

		unityGame.SendMessage(objectName, "UpdateElapsedWaitTime", elapsedWaitTime);
	}
}

function updateAmplitude(floatPCM)
{
	// Calculate amplitude (RMS)
	let sum = 0;
	for (let i = 0; i < floatPCM.length; i++) {
		sum += floatPCM[i] * floatPCM[i];
	}
	amplitude = Math.sqrt(sum / floatPCM.length) * amplitudeMultiplier;

	// Send amplitude to Unity
	unityGame.SendMessage(objectName, "UpdateAmplitude", amplitude);
}