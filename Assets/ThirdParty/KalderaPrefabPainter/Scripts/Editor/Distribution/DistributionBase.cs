using CollisionBear.WorldEditor.Generation;
using CollisionBear.WorldEditor.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace CollisionBear.WorldEditor.Distribution
{
    public abstract class DistributionBase
    {
        public readonly int Index;

        public abstract string Name { get; }
        protected abstract string ToolTip { get; }

        protected abstract string ButtonImagePath { get; }

        private GUIContent ButtonContent;

        public DistributionBase(int index)
        {
            Index = index;
        }

        public GUIContent GetGUIContent()
        {
            if (ButtonContent == null) {
                ButtonContent = LoadGUIContent();
            }

            return ButtonContent;
        }

        protected virtual GUIContent LoadGUIContent()
        {
            var image = KalderaEditorUtils.LoadAssetPath(ButtonImagePath);
            return new GUIContent(image, Name + "\n" + ToolTip);
        }

        public abstract List<Vector3> GetPoints(float size, float spacing, IGenerationBounds boundsProvider);
    }
}