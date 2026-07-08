using System;
using UnityEngine;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// Reads input only and raises events - zero UI logic, zero knowledge of
    /// menu state, per the README. Translates the project's existing
    /// BrailleMapping D-pad events into the GameMenu's own vocabulary
    /// (Up/Down/Left/Right/Enter/Escape) so GameMenuManager never depends on
    /// BrailleMapping directly.
    ///
    /// ASSUMPTION: this assumes BrailleMapping exposes OnLeft/OnRight events
    /// in addition to the OnUp/OnDown/OnSubmit/OnBack it already exposes
    /// (used today by AccessibleFormNavigator). If BrailleMapping doesn't
    /// have Left/Right yet, add them there for consistency with the rest of
    /// the accessible-input pipeline - don't poll Input directly here, or
    /// every other braille-driven screen will interpret the D-pad differently
    /// from this one.
    ///
    /// InputLocked is set by GameMenuManager while an animation plays (see
    /// README "Input Lock" requirement) - every handler below bails out
    /// early while it's true.
    /// </summary>
    public class GameMenuNavigator : MonoBehaviour
    {
        public event Action OnUp;
        public event Action OnDown;
        public event Action OnLeft;
        public event Action OnRight;
        public event Action OnEnter;
        public event Action OnEscape;

        /// <summary>While true, all input is ignored. Set by GameMenuManager during animations.</summary>
        public bool InputLocked { get; set; }

        private void OnEnable()
        {
            BrailleMapping.OnUp += HandleUp;
            BrailleMapping.OnDown += HandleDown;
            BrailleMapping.OnLeft += HandleLeft;
            BrailleMapping.OnRight += HandleRight;
            BrailleMapping.OnSubmit += HandleEnter;
            BrailleMapping.OnBack += HandleEscape;
        }

        private void OnDisable()
        {
            BrailleMapping.OnUp -= HandleUp;
            BrailleMapping.OnDown -= HandleDown;
            BrailleMapping.OnLeft -= HandleLeft;
            BrailleMapping.OnRight -= HandleRight;
            BrailleMapping.OnSubmit -= HandleEnter;
            BrailleMapping.OnBack -= HandleEscape;
        }

        private void HandleUp() { if (!InputLocked) OnUp?.Invoke(); }
        private void HandleDown() { if (!InputLocked) OnDown?.Invoke(); }
        private void HandleLeft() { if (!InputLocked) OnLeft?.Invoke(); }
        private void HandleRight() { if (!InputLocked) OnRight?.Invoke(); }
        private void HandleEnter() { if (!InputLocked) OnEnter?.Invoke(); }
        private void HandleEscape() { if (!InputLocked) OnEscape?.Invoke(); }
    }
}