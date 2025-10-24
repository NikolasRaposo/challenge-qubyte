using System.Collections.Generic;
using System.Linq;

namespace CollisionBear.WorldEditor
{
    [System.Serializable]
    public class SelectionSettings
    {
        public List<PaletteItem> SelectedItems = new List<PaletteItem>();
        public int PlacementModeIndex = 0;
        public int RaycastModeIndex = 0;
        public int SelectedBrushIndex = 0;
        public int SelectedDistributionIndex = 0;

        public bool OptionsExtended = false;

        public bool ParentObjectsToBaseObject = true;
        public bool AvoidCollisions = false;
        public float CollisionSizeFactor = 1.0f;
        public bool OrientToBrushNormal = false;
        public bool OrientToGroundNormal = false;
        public bool ChildObjectsToStroke = false;
        public int ObjectLimit = 100;

        public bool HasItems() => SelectedItems.Count > 0;

        public List<PaletteItem> GetItemsWithVariants() => SelectedItems
                .Where(i => i.HasVariants())
                .ToList();

        public void ToggleSelectedItem(PaletteItem item)
        {
            if (SelectedItems.Contains(item)) {
                SelectedItems.Remove(item);
            } else {
                SelectedItems.Add(item);
            }

            if (SelectedItems.Count > 0) {
                ClearSceneSelection();
            }
        }

        public void SetSelectedItem(PaletteItem item)
        {
            SelectedItems.Clear();
            SelectedItems.Add(item);

            ClearSceneSelection();
        }

        private void ClearSceneSelection()
        {
            if (UnityEditor.Selection.gameObjects.Length == 0) {
                return;
            }

            UnityEditor.Selection.activeGameObject = null;
        }

        public void ClearSelection()
        {
            SelectedItems.Clear();
        }

        public float GetSelectedItemSize() => GetItemsSortedBySize(GetItemsWithVariants()).First();

        private List<float> GetItemsSortedBySize(List<PaletteItem> items) => items
            .Select(i => i.GetItemSize())
            .OrderByDescending(i => i)
            .ToList();
    }
}
