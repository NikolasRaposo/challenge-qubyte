using CollisionBear.WorldEditor.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace CollisionBear.WorldEditor
{
    [CreateAssetMenu(fileName = "New Prefab Palette", menuName = KalderaEditorUtils.AssetBasePath + "/Prefab Palette")]
    public class Palette : SelectableAsset
    {
        public KeyCode ShortKey = KeyCode.None;

        public List<PaletteGroup> Groups = new List<PaletteGroup>();

        [System.NonSerialized]
        public Vector2 CurrentScroll;

        public bool HasGroups() => Groups.Count > 0;

        public bool HasAnyGroupWithItems()
        {
            foreach (var group in Groups) {
                foreach (var item in group.Items) {
                    foreach (var variant in item.GameObjectVariants) {
                        if (variant != null) {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void MoveGroupUp(PaletteGroup group)
        {
            var currentIndex = Groups.IndexOf(group);
            Groups.Remove(group);
            Groups.Insert(Mathf.Max(0, currentIndex - 1), group);
        }

        public void MoveGroupDown(PaletteGroup group)
        {
            var currentIndex = Groups.IndexOf(group);
            Groups.Remove(group);
            Groups.Insert(Mathf.Min(Groups.Count, currentIndex + 1), group);

        }

        public string GetCategoryName()
        {
            if (ShortKey != KeyCode.None) {
                return string.Format("{0} {1} {2}", GetCategoryBaseName(name), "\t Shft", ShortKey.ToString());
            } else {
                return GetCategoryBaseName(name);
            }
        }

        private string GetCategoryBaseName(string categoryName)
        {
            if (categoryName == string.Empty) {
                return "[Nameless palatte]";
            } else {
                return categoryName;
            }
        }
    }
}