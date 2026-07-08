using System.Collections.Generic;
using UnityEngine;

namespace BraillePlay.GameMenu
{
    /// <summary>
    /// Ordered list of every Lesson in the game. A ScriptableObject asset for
    /// the same scalability reason as <see cref="GuideDatabase"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "LessonDatabase", menuName = "BraillePlay/GameMenu/Lesson Database")]
    public class LessonDatabase : ScriptableObject
    {
        [SerializeField] private List<LessonData> lessons = new List<LessonData>();

        public int Count => lessons.Count;

        public LessonData Get(int index)
        {
            return (index >= 0 && index < lessons.Count) ? lessons[index] : null;
        }
    }
}