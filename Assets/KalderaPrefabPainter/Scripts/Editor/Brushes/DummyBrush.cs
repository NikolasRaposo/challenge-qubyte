using System.Collections.Generic;
using UnityEngine;

namespace CollisionBear.WorldEditor.Brushes
{
    [System.Serializable]
    public class DummyBrush : BrushBase
    {
        private string ImagePath;

        protected override string ButtonImagePath => ImagePath;

        public override bool Disabled => true;

        public DummyBrush(string brushName, KeyCode hotkey, string toolTip, string imagePath)
        {
            //BrushToolTip = toolTip;
            ImagePath = imagePath;
        }

        public override void DrawBrushEditor(ScenePlacer placer) { }

        protected override List<Vector3> GetPlacementOffsetValues(Vector3 position, SelectionSettings selectionSettings, ScenePlacer placer) => EmptyPointList;
    }
}
