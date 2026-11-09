using System;
using System.Collections;
using UnityEngine;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// Plays recorded voice clips (our own voices - no TTS / AI voices) and
    /// raises a caption event with the matching text, so the on-screen
    /// speech cloud teachers use to monitor the app keeps working.
    /// Play() interrupts whatever is still speaking.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class VoicePlayer : MonoBehaviour
    {
        [SerializeField] private AudioSource source;
        [SerializeField] private float gapBetweenClips = 0.05f;

        [Tooltip("TEMPORARY bridge: also triggers TTSManager.OnSpeak (text only, never audio) so an existing CloudManager that listens to it keeps showing text. Turn off once CloudManager listens to OnCaption instead.")]
        [SerializeField] private bool forwardCaptionToTTSManagerEvent = true;

        /// <summary>Raised with the text of every announcement, for speech-bubble / teacher-monitor UI.</summary>
        public event Action<string> OnCaption;

        public bool IsPlaying { get; private set; }

        private Coroutine routine;

        private void Reset() { source = GetComponent<AudioSource>(); }

        private void Awake()
        {
            if (source == null) source = GetComponent<AudioSource>();
            source.playOnAwake = false;
        }

        public void Play(string caption, params AudioClip[] clips)
        {
            Stop();

            if (!string.IsNullOrWhiteSpace(caption))
            {
                OnCaption?.Invoke(caption);
                if (forwardCaptionToTTSManagerEvent && TTSManager.Instance != null)
                    TTSManager.Instance.ShowCaption(caption);
            }

            routine = StartCoroutine(PlaySequence(clips));
        }

        public void Stop()
        {
            if (routine != null) StopCoroutine(routine);
            routine = null;
            IsPlaying = false;
            if (source != null) source.Stop();
        }

        /// <summary>Use inside a coroutine to wait for the announcement to finish.</summary>
        public IEnumerator WaitUntilFinished()
        {
            while (IsPlaying) yield return null;
        }

        private IEnumerator PlaySequence(AudioClip[] clips)
        {
            IsPlaying = true;

            foreach (AudioClip clip in clips)
            {
                if (clip == null)
                {
                    Debug.LogWarning("[VoicePlayer] A clip in the sequence is missing (not assigned or not recorded yet) - skipping it.");
                    continue;
                }

                source.clip = clip;
                source.Play();
                yield return new WaitForSecondsRealtime(clip.length + gapBetweenClips);
            }

            IsPlaying = false;
            routine = null;
        }
    }
}