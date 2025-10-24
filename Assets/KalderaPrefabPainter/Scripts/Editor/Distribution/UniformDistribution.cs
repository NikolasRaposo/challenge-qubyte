using CollisionBear.WorldEditor.Generation;
using System.Collections.Generic;
using UnityEngine;

namespace CollisionBear.WorldEditor.Distribution
{
    public class UniformDistribution : DistributionBase
    {
        public override string Name => "Grid";

        protected override string ToolTip => "Distributes item exactly evenly, filling the area in a grid pattern";

        protected override string ButtonImagePath => "Icons/IconDistributionUniform.png";

        public UniformDistribution(int index) : base(index) { }

        public override List<Vector3> GetPoints(float size, float spacing, IGenerationBounds boundsProvider)
        {
            var itemCount = Mathf.FloorToInt(size / spacing);

            var result = new List<Vector3>();
            var halfSpacing = spacing / 2;

            for (var y = -itemCount; y < itemCount; y++) {
                for (var x = -itemCount; x < itemCount; x++) {
                    var position = new Vector3((x * spacing) + halfSpacing, 0, (y * spacing) + halfSpacing);
                    if (boundsProvider.IsWithinBounds(size, new BoxRect(new Vector2(position.x, position.z), Vector2.zero))) {
                        result.Add(position);
                    }
                }
            }

            return result;
        }
    }
}