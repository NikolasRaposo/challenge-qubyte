using CollisionBear.WorldEditor.Utils;
using System.Collections.Generic;
using UnityEngine;

namespace CollisionBear.WorldEditor
{
    [CreateAssetMenu(menuName = KalderaEditorUtils.AssetBasePath + "/Prefab Palette Collection", fileName = "New Prefab Palette Collection")]
    public class PaletteSet : SelectableAsset
    {
        public List<Palette> Categories = new List<Palette>();
    }
}