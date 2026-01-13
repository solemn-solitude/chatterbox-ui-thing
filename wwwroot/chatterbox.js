// WAV Audio Playback for Chatterbox TTS
window.playWAVAudio = function (base64Audio) {
    console.log('playWAVAudio called, data length:', base64Audio.length);

    const audioBytes = Uint8Array.from(atob(base64Audio), c => c.charCodeAt(0));
    console.log('Decoded audio bytes:', audioBytes.length);

    const blob = new Blob([audioBytes], { type: 'audio/wav' });
    console.log('Created blob, size:', blob.size, 'type:', blob.type);

    const audioUrl = URL.createObjectURL(blob);
    console.log('Created object URL:', audioUrl);

    // Update audio element for download/replay
    const audioElement = document.getElementById('audioPlayer');
    if (audioElement) {
        console.log('Found audio element');

        // Set up event listeners for debugging
        audioElement.onloadedmetadata = () => console.log('Audio metadata loaded, duration:', audioElement.duration);
        audioElement.oncanplay = () => console.log('Audio can play');
        audioElement.onerror = (e) => console.error('Audio error:', e, audioElement.error);
        audioElement.onloadstart = () => console.log('Audio load started');
        audioElement.onloadeddata = () => console.log('Audio data loaded');

        audioElement.src = audioUrl;
        audioElement.load(); // Explicitly load the audio

        // Try to play after a brief moment
        setTimeout(() => {
            audioElement.play()
                .then(() => console.log('Audio playback started'))
                .catch(e => console.error('Error playing audio:', e));
        }, 100);
    } else {
        console.error('Audio element not found!');
    }
};

// Download Audio File
window.downloadAudio = function (base64Audio, filename) {
    const audioBytes = Uint8Array.from(atob(base64Audio), c => c.charCodeAt(0));
    const blob = new Blob([audioBytes], { type: 'audio/wav' });
    const url = URL.createObjectURL(blob);

    const link = document.createElement('a');
    link.href = url;
    link.download = filename;
    document.body.appendChild(link);
    link.click();
    document.body.removeChild(link);

    // Clean up the URL
    setTimeout(() => URL.revokeObjectURL(url), 100);
};

// PCM Audio Playback for Chatterbox TTS (legacy)
window.playPCMAudio = function (base64Audio, sampleRate) {
    const audioContext = new (window.AudioContext || window.webkitAudioContext)();
    const audioBytes = Uint8Array.from(atob(base64Audio), c => c.charCodeAt(0));

    // Convert PCM bytes to float samples
    const samples = new Float32Array(audioBytes.length / 2);
    for (let i = 0; i < samples.length; i++) {
        // Convert 16-bit PCM to float (-1.0 to 1.0)
        const sample = (audioBytes[i * 2] | (audioBytes[i * 2 + 1] << 8));
        samples[i] = sample < 32768 ? sample / 32768.0 : (sample - 65536) / 32768.0;
    }

    // Create audio buffer
    const audioBuffer = audioContext.createBuffer(1, samples.length, sampleRate);
    audioBuffer.getChannelData(0).set(samples);

    // Play audio
    const source = audioContext.createBufferSource();
    source.buffer = audioBuffer;
    source.connect(audioContext.destination);
    source.start(0);

    // Update audio element for download/replay
    const audioElement = document.getElementById('audioPlayer');
    if (audioElement) {
        const blob = new Blob([audioBytes], { type: 'audio/wav' });
        audioElement.src = URL.createObjectURL(blob);
    }
};

// Audio Recording for Voice Upload
let mediaRecorder = null;
let recordedChunks = [];

window.startRecording = async function () {
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ audio: true });
        mediaRecorder = new MediaRecorder(stream);
        recordedChunks = [];

        mediaRecorder.ondataavailable = (event) => {
            if (event.data.size > 0) {
                recordedChunks.push(event.data);
            }
        };

        mediaRecorder.start();
        return true;
    } catch (error) {
        console.error('Error starting recording:', error);
        return false;
    }
};

window.stopRecording = function () {
    return new Promise((resolve) => {
        if (!mediaRecorder || mediaRecorder.state === 'inactive') {
            resolve(null);
            return;
        }

        mediaRecorder.onstop = async () => {
            const blob = new Blob(recordedChunks, { type: 'audio/webm' });

            // Convert to WAV format
            const arrayBuffer = await blob.arrayBuffer();
            const audioContext = new (window.AudioContext || window.webkitAudioContext)();
            const audioBuffer = await audioContext.decodeAudioData(arrayBuffer);

            // Create WAV file
            const wavBlob = audioBufferToWav(audioBuffer);
            const base64 = await blobToBase64(wavBlob);

            // Stop all tracks
            mediaRecorder.stream.getTracks().forEach(track => track.stop());

            resolve(base64);
        };

        mediaRecorder.stop();
    });
};

function audioBufferToWav(audioBuffer) {
    const numChannels = audioBuffer.numberOfChannels;
    const sampleRate = audioBuffer.sampleRate;
    const format = 1; // PCM
    const bitDepth = 16;

    const bytesPerSample = bitDepth / 8;
    const blockAlign = numChannels * bytesPerSample;

    const data = audioBuffer.getChannelData(0);
    const dataLength = data.length * bytesPerSample;
    const buffer = new ArrayBuffer(44 + dataLength);
    const view = new DataView(buffer);

    // Write WAV header
    writeString(view, 0, 'RIFF');
    view.setUint32(4, 36 + dataLength, true);
    writeString(view, 8, 'WAVE');
    writeString(view, 12, 'fmt ');
    view.setUint32(16, 16, true); // fmt chunk size
    view.setUint16(20, format, true);
    view.setUint16(22, numChannels, true);
    view.setUint32(24, sampleRate, true);
    view.setUint32(28, sampleRate * blockAlign, true); // byte rate
    view.setUint16(32, blockAlign, true);
    view.setUint16(34, bitDepth, true);
    writeString(view, 36, 'data');
    view.setUint32(40, dataLength, true);

    // Write audio data
    let offset = 44;
    for (let i = 0; i < data.length; i++) {
        const sample = Math.max(-1, Math.min(1, data[i]));
        view.setInt16(offset, sample < 0 ? sample * 0x8000 : sample * 0x7FFF, true);
        offset += 2;
    }

    return new Blob([buffer], { type: 'audio/wav' });
}

function writeString(view, offset, string) {
    for (let i = 0; i < string.length; i++) {
        view.setUint8(offset + i, string.charCodeAt(i));
    }
}

function blobToBase64(blob) {
    return new Promise((resolve) => {
        const reader = new FileReader();
        reader.onloadend = () => resolve(reader.result.split(',')[1]);
        reader.readAsDataURL(blob);
    });
}
