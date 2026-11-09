using System;
using System.Text;
using UnityEngine;

/// <summary>
/// Turns the bytes of a .wav file into an AudioClip at runtime.
/// Unity cannot decode mp3/m4a/aac/flac from raw bytes at runtime, so this
/// only supports uncompressed 16-bit PCM WAV (what most recorders produce).
/// Returns null (and logs why) for anything else.
/// </summary>
public static class WavDecoder
{
    public static AudioClip ToClip(string clipName, byte[] wav)
    {
        if (wav == null || wav.Length < 44 ||
            Encoding.ASCII.GetString(wav, 0, 4) != "RIFF" ||
            Encoding.ASCII.GetString(wav, 8, 4) != "WAVE")
        {
            Debug.LogWarning("[WavDecoder] '" + clipName + "' is not a WAV file (mp3/m4a can't be decoded at runtime).");
            return null;
        }

        int format = 0, channels = 0, sampleRate = 0, bits = 0;
        int dataStart = -1, dataLength = 0;
        int pos = 12;

        while (pos + 8 <= wav.Length)
        {
            string id = Encoding.ASCII.GetString(wav, pos, 4);
            int size = BitConverter.ToInt32(wav, pos + 4);
            int body = pos + 8;

            if (id == "fmt ")
            {
                format = (ushort)BitConverter.ToInt16(wav, body);
                channels = BitConverter.ToInt16(wav, body + 2);
                sampleRate = BitConverter.ToInt32(wav, body + 4);
                bits = BitConverter.ToInt16(wav, body + 14);
            }
            else if (id == "data")
            {
                dataStart = body;
                dataLength = Mathf.Min(size, wav.Length - body);
                break;
            }

            pos = body + size + (size & 1); // chunks are padded to even sizes
        }

        bool isPcm = format == 1 || format == 0xFFFE; // 0xFFFE = WAVE_FORMAT_EXTENSIBLE
        if (dataStart < 0 || !isPcm || bits != 16 || channels < 1 || sampleRate < 1)
        {
            Debug.LogWarning("[WavDecoder] '" + clipName + "' unsupported (format=" + format + ", bits=" + bits +
                             ", channels=" + channels + "). Need 16-bit PCM WAV.");
            return null;
        }

        int frames = dataLength / 2 / channels;
        float[] samples = new float[frames * channels];
        for (int i = 0; i < samples.Length; i++)
            samples[i] = BitConverter.ToInt16(wav, dataStart + i * 2) / 32768f;

        AudioClip clip = AudioClip.Create(clipName, frames, channels, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}