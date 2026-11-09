using System;
using UnityEngine;

namespace BraillePlay.GameMenu
{
    /// <summary>One entry in <see cref="GuideDatabase"/>. Plain serializable data only.</summary>
    [Serializable]
    public class GuideData
    {
        [SerializeField] private string guideTitle;
        [Tooltip("Recording of the guide title (our own voice).")]
        [SerializeField] private AudioClip titleClip;
        [SerializeField] private string sceneName;
        [SerializeField, TextArea] private string description;

        /// <summary>Displayed title, e.g. "Braille Alphabet".</summary>
        public string GuideTitle => guideTitle;

        /// <summary>Recorded voice for the title.</summary>
        public AudioClip TitleClip => titleClip;

        public string SceneName => sceneName;
        public string Description => description;
    }
}