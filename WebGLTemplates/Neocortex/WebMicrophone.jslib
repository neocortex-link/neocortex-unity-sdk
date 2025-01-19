mergeInto(LibraryManager.library, 
{
    WebGL_Initialize: function(_objectName, _amplitudeThreshold, _maxWaitTime, _usePushToTalk) {
        objectName = UTF8ToString(_objectName);
        amplitudeThreshold = _amplitudeThreshold;
        maxWaitTime = _maxWaitTime;
        usePushToTalk = _usePushToTalk;
    },

	WebGL_StartRecording: function () {
	    startRecording();
	},
	
	WebGL_StopRecording: function() {
	    stopRecording();
	},

	WebGL_RecordingUpdatePointer: function(index) {
		floatPCMPointer = index;
	}
});
