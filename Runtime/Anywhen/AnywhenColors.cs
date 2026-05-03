using UnityEngine;

namespace Anywhen
{
    public static class AnywhenColors
    {
        // Core Palette
        public static readonly Color NoteChance = new Color(93f / 255f, 156f / 255f, 168f / 255f); // rgb(93, 156, 168)
        public static readonly Color NoteVelocity = new Color(195f / 255f, 146f / 255f, 78f / 255f); // rgb(195, 146, 78)

        public static readonly Color NoteLength = new Color(195f / 255f, 110f / 255f, 130f / 255f); // rgb(195, 146, 78)
        public static readonly Color NoteWeight = new Color(220f / 255f, 220f / 255f, 220f / 255f); // rgb(195, 146, 78)

        public static readonly Color Accent = new Color(102f / 255f, 177f / 255f, 229f / 255f); // rgb(102, 177, 229)
        public static readonly Color DarkBg = new Color(31f / 255f, 31f / 255f, 31f / 255f); // rgb(31, 31, 31)
        public static readonly Color Border = new Color(149f / 255f, 149f / 255f, 149f / 255f); // rgb(149, 149, 149)

        /// <summary>
        /// Returns a color based on the note type and velocity.
        /// </summary>
        public static Color GetNoteColor(Color noteInitialColor, float velocity01)
        {
            return Color.Lerp(noteInitialColor * 0.5f, noteInitialColor, velocity01);
        }
    }
}