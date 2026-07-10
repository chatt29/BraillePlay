using System;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// One entry in <see cref="GuideDatabase"/>: everything the menu needs to
    /// display and open a single Guide. Plain serializable data only - no
    /// behaviour, per the project's "strongly typed data" requirement.
    /// </summary>
    [Serializable]
    public class GuideData
    {
        [UnityEngine.SerializeField] private string guideTitle;
        [UnityEngine.SerializeField] private string sceneName;
        [UnityEngine.SerializeField, UnityEngine.TextArea] private string description;

        /// <summary>Spoken/displayed title, e.g. "Braille Alphabet".</summary>
        public string GuideTitle => guideTitle;

        /// <summary>Scene to load (via SceneLoader.LoadGuide) when this guide is opened.</summary>
        public string SceneName => sceneName;

        /// <summary>Optional longer description, not currently spoken but available for UI/tooltips.</summary>
        public string Description => description;
    }
}