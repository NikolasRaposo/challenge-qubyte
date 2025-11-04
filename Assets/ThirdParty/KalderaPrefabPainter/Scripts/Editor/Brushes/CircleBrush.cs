using CollisionBear.WorldEditor.Generation;
using UnityEditor;
using UnityEngine;

namespace CollisionBear.WorldEditor.Brushes
{
    [System.Serializable]
    public class CircleBrush : AreaBrushBase
    {
        public class CircleGenerationBounds : IGenerationBounds
        {
            public bool IsWithinBounds(float circleSize, BoxRect box)
            {
                var circleSizeSquare = Mathf.Pow(circleSize, 2);
                return (
                    box.TopLeft.sqrMagnitude < circleSizeSquare &&
                    box.BottomLeft.sqrMagnitude < circleSizeSquare &&
                    box.TopRight.sqrMagnitude < circleSizeSquare &&
                    box.BottomRight.sqrMagnitude < circleSizeSquare
                );
            }
        }

        protected override string ButtonImagePath => "Icons/IconGridCircle.png";

        public CircleBrush()
        {
            GenerationBounds = new CircleGenerationBounds();
        }

        public override void DrawBrushHandle(Vector3 placementPosition, Vector3 mousePosition)
        {
            Handles.color = HandleBrushColor;
            Handles.DrawSolidDisc(placementPosition, Normal, Settings.BrushSize);
            Handles.color = HandleOutlineColor;
            Handles.DrawWireDisc(placementPosition, Normal, Settings.BrushSize);

            if (HasDrag(StartDragPosition, EndDragPosition)) {
                DrawRotationCompass(StartDragPosition.Value, mousePosition, Rotation, Normal);
                DrawRotationArrow(StartDragPosition.Value, mousePosition, Rotation, Normal);
            }
        }
    }
}
