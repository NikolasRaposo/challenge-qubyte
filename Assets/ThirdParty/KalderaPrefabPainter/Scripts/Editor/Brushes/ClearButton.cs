using CollisionBear.WorldEditor.Utils;
using UnityEngine;

namespace CollisionBear.WorldEditor.Brushes
{
    public class ClearButton : IBrushButton
    {
        private const string ButtonImagePath = "Icons/TrashIcon.png";

        public KeyBinding KeyBinding { get; set; }

        public bool Disabled => false;

        private Texture2D ButtonTexture;

        public Texture2D GetButtonTexture()
        {
            if (ButtonTexture == null) {
                ButtonTexture = KalderaEditorUtils.LoadAssetPath(ButtonImagePath); ;
            }

            return ButtonTexture;
        }

        public GUIContent GetButtonContent() => new GUIContent(GetButtonTexture(), KeyBinding.GetToolTip());

        public void OnButtonPress(PaletteWindow paletteWindow)
        {
            paletteWindow.ClearSelection();
        }

        public void OnSelected(ScenePlacer placer) { }
    }
}