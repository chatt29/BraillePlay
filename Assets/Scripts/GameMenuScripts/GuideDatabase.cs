using System.Collections.Generic;
using UnityEngine;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// Ordered list of every Guide in the game. A ScriptableObject asset so
    /// designers can add guides forever without touching a script or scene -
    /// this is the "dozens of lessons, hundreds of quizzes" scalability
    /// requirement from the README.
    /// </summary>
    [CreateAssetMenu(fileName = "GuideDatabase", menuName = "BraillePlay/GameMenu/Guide Database")]
    public class GuideDatabase : ScriptableObject
    {
        [SerializeField] private List<GuideData> guides = new List<GuideData>();

        public int Count => guides.Count;

        public GuideData Get(int index)
        {
            return (index >= 0 && index < guides.Count) ? guides[index] : null;
        }
    }
}