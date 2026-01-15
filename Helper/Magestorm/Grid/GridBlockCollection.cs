using System;
using SharpDX;
using OrientedBoundingBox = Helper.Math.OrientedBoundingBox;

namespace Helper
{
    public class GridBlockCollection : ListCollection<GridBlock>
    {
        public GridBlockCollection(Boolean isBase)
        {
            
        }

        public GridBlock GetBlockByLocation(Single x, Single y)
        {
            int gX = (int)x >> 6;
            int gY = (int)y >> 6;

            // Clamp to prevent out-of-bounds on map edges
            if (gX < 0 || gX >= 128 || gY < 0 || gY >= 128) return null;

            return this[gY + (gX << 7)];
        }

        public GridBlockCollection GetBlocksNearBoundingBox(OrientedBoundingBox boundingBox)
        {
            return new GridBlockCollection(false)
                       {
                           GetBlockByLocation(boundingBox.Origin.X,boundingBox.Origin.Y),
                           GetBlockByLocation(boundingBox.Corners[0].X, boundingBox.Corners[0].Y),
                           GetBlockByLocation(boundingBox.Corners[1].X, boundingBox.Corners[1].Y),
                           GetBlockByLocation(boundingBox.Corners[2].X, boundingBox.Corners[2].Y),
                           GetBlockByLocation(boundingBox.Corners[3].X, boundingBox.Corners[3].Y)
                       };
        }

        public GridBlockCollection GetBlocksInLine(Vector3 startPoint, Vector3 endPoint)
        {
            GridBlockCollection gridBlockCollection = new GridBlockCollection(false);

            Vector3 currentPoint = startPoint;
            Vector3 direction = Vector3.Normalize(endPoint - startPoint);
            Single originalDistance = Vector3.Distance(currentPoint, endPoint);

            while (Vector3.Distance(startPoint, currentPoint) < originalDistance)
            {
                GridBlock block = GetBlockByLocation(currentPoint.X, currentPoint.Y);

                if (block != null && !gridBlockCollection.Contains(block)) gridBlockCollection.Add(block);

                currentPoint += direction;
            }

            for (Int32 i = gridBlockCollection.Count - 1; i >= 0; i--)
            {
                GridBlock block = gridBlockCollection[i];
                if (block == null) continue;

                if (block.LowBox == null || block.MidBox == null || block.HighBox == null) continue;
                if (block.LowBox.LineInBox(startPoint, endPoint) || block.MidBox.LineInBox(startPoint, endPoint) || block.HighBox.LineInBox(startPoint, endPoint)) continue;

                gridBlockCollection.RemoveAt(i);
             }

            return gridBlockCollection;
        }

        public GridBlockCollection GetBlocksAroundLine(Vector3 startPoint, Vector3 endPoint)
        {
            GridBlockCollection gridBlockCollection = new GridBlockCollection(false);

            Vector3 currentPoint = startPoint;
            Vector3 direction = Vector3.Normalize(endPoint - startPoint);
            Single originalDistance = Vector3.Distance(currentPoint, endPoint);

            while (Vector3.Distance(startPoint, currentPoint) < originalDistance)
            {
                GridBlock block = GetBlockByLocation(currentPoint.X, currentPoint.Y);

                if (block != null && !gridBlockCollection.Contains(block)) gridBlockCollection.Add(block);

                currentPoint += direction;
            }

            for (Int32 i = gridBlockCollection.Count - 1; i >= 0; i--)
            {
                GridBlock block = gridBlockCollection[i];
                if (block == null) continue;

                if (block.ContainerBox == null) continue;
                if (block.ContainerBox.LineInBox(startPoint, endPoint)) continue;

                gridBlockCollection.RemoveAt(i);
            }

            return gridBlockCollection;
        }

        public GridBlock GetHighestGravityBlock(OrientedBoundingBox boundingBox)
        {
            GridBlockCollection gridBlockCollection = GetBlocksNearBoundingBox(boundingBox);

            for (Int32 i = gridBlockCollection.Count - 1; i > 0; i--)
            {
                if (gridBlockCollection[i].LowBoxTopZ < gridBlockCollection[i-1].LowBoxTopZ)
                {
                    gridBlockCollection.RemoveAt(i);
                }
                else
                {
                    gridBlockCollection.RemoveAt(i-1);
                }
            }

            return gridBlockCollection[0];
        }
    }
}
