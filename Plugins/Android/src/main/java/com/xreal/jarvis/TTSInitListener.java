package com.xreal.jarvis;

import android.speech.tts.TextToSpeech;
import android.util.Log;
import com.unity3d.player.UnityPlayer;

/**
 * Text-to-Speech Initialization Listener for Unity integration
 * Handles TTS initialization and forwards status to Unity
 */
public class TTSInitListener implements TextToSpeech.OnInitListener {
    private static final String TAG = "JarvisTTSListener";
    private static final String UNITY_OBJECT = "VoiceOutputManager";

    @Override
    public void onInit(int status) {
        Log.d(TAG, "TTS initialization status: " + status);
        
        // Send initialization status to Unity
        UnityPlayer.UnitySendMessage(UNITY_OBJECT, "OnTTSInitialized", String.valueOf(status));
        
        if (status == TextToSpeech.SUCCESS) {
            Log.d(TAG, "TTS initialized successfully");
        } else {
            Log.e(TAG, "TTS initialization failed with status: " + status);
        }
    }
}