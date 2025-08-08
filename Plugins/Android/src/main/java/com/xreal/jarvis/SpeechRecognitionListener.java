package com.xreal.jarvis;

import android.os.Bundle;
import android.speech.RecognitionListener;
import android.speech.SpeechRecognizer;
import android.util.Log;
import com.unity3d.player.UnityPlayer;
import java.util.ArrayList;

/**
 * Android Speech Recognition Listener for Unity integration
 * Handles speech recognition callbacks and forwards them to Unity
 */
public class SpeechRecognitionListener implements RecognitionListener {
    private static final String TAG = "JarvisSpeechListener";
    private static final String UNITY_OBJECT = "VoiceInputManager";
    
    @Override
    public void onReadyForSpeech(Bundle params) {
        Log.d(TAG, "Ready for speech");
    }

    @Override
    public void onBeginningOfSpeech() {
        Log.d(TAG, "Beginning of speech");
    }

    @Override
    public void onRmsChanged(float rmsdB) {
        // RMS value changed - can be used for volume visualization
    }

    @Override
    public void onBufferReceived(byte[] buffer) {
        // Audio buffer received
    }

    @Override
    public void onEndOfSpeech() {
        Log.d(TAG, "End of speech");
    }

    @Override
    public void onError(int error) {
        String errorMessage = getErrorMessage(error);
        Log.e(TAG, "Speech recognition error: " + errorMessage);
        
        // Send error to Unity
        UnityPlayer.UnitySendMessage(UNITY_OBJECT, "OnSpeechRecognitionError", errorMessage);
    }

    @Override
    public void onResults(Bundle results) {
        ArrayList<String> matches = results.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION);
        if (matches != null && !matches.isEmpty()) {
            String recognizedText = matches.get(0);
            Log.d(TAG, "Speech recognition result: " + recognizedText);
            
            // Send result to Unity
            UnityPlayer.UnitySendMessage(UNITY_OBJECT, "OnSpeechRecognitionResult", recognizedText);
        }
    }

    @Override
    public void onPartialResults(Bundle partialResults) {
        ArrayList<String> matches = partialResults.getStringArrayList(SpeechRecognizer.RESULTS_RECOGNITION);
        if (matches != null && !matches.isEmpty()) {
            String partialText = matches.get(0);
            Log.d(TAG, "Partial speech result: " + partialText);
            
            // Send partial result to Unity (optional)
            UnityPlayer.UnitySendMessage(UNITY_OBJECT, "OnPartialSpeechResult", partialText);
        }
    }

    @Override
    public void onEvent(int eventType, Bundle params) {
        Log.d(TAG, "Speech recognition event: " + eventType);
    }

    private String getErrorMessage(int error) {
        switch (error) {
            case SpeechRecognizer.ERROR_AUDIO:
                return "Audio recording error";
            case SpeechRecognizer.ERROR_CLIENT:
                return "Client side error";
            case SpeechRecognizer.ERROR_INSUFFICIENT_PERMISSIONS:
                return "Insufficient permissions";
            case SpeechRecognizer.ERROR_NETWORK:
                return "Network error";
            case SpeechRecognizer.ERROR_NETWORK_TIMEOUT:
                return "Network timeout";
            case SpeechRecognizer.ERROR_NO_MATCH:
                return "No speech match found";
            case SpeechRecognizer.ERROR_RECOGNIZER_BUSY:
                return "Recognition service busy";
            case SpeechRecognizer.ERROR_SERVER:
                return "Server error";
            case SpeechRecognizer.ERROR_SPEECH_TIMEOUT:
                return "No speech input";
            default:
                return "Unknown error: " + error;
        }
    }
}