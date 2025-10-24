using CollisionBear.WorldEditor.Utils;
using UnityEngine;

namespace CollisionBear.WorldEditor.Brushes
{
    public interface IBrushButton
    {
        KeyBinding KeyBinding { get; set; }
        bool Disabled { get; }

        GUIContent GetButtonContent();
        Texture2D GetButtonTexture();
        void OnButtonPress(PaletteWindow paletteWindow);
        void OnSelected(ScenePlacer placer);
    }
}