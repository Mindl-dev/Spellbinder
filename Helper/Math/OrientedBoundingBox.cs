using System;
using System.Linq;
using SharpDX;

namespace Helper.Math
{
    public class OrientedBoundingBox
    {
        public BoundingBox AxisBoundingBox;
        public Vector3 Extents;
        public Matrix InvertedRotationMatrix;

        public Vector3 Location;
        public Vector3 MaxLocation;
        public Vector3 Origin;

        public Single Rotation;
        public Matrix RotationMatrix;
        public Vector3 Size;

        public Boolean IsPivotRotation;

        public Vector3[] Axes => new Vector3[] {
            new Vector3(RotationMatrix.M11, RotationMatrix.M12, RotationMatrix.M13), // Right
            new Vector3(RotationMatrix.M21, RotationMatrix.M22, RotationMatrix.M23), // Up
            new Vector3(RotationMatrix.M31, RotationMatrix.M32, RotationMatrix.M33)  // Forward
        };

        // The geometric center of the box
        public Vector3 Center => Origin;

        public Vector3[] ObjectSpaceCorners
        {
            get; private set;
        }

        public Vector3[] Corners
        {
            get; private set;
        }

        public BoundingSphere ExtentSphere
        {
            get; private set;
        }

        public Boolean IsBelowDeathZ
        {
            get
            {
                return Location.Z <= -450f;
            }
        }

        /* A Line based Oriented Bounding Box with pivot rotation. */
        public OrientedBoundingBox(Vector3 point1, Vector3 point2, Vector3 size)
        {
            IsPivotRotation = true;
            Location = point1;
			MaxLocation = new Vector3(point1.X, point1.Y + (Single)System.Math.Round(Vector3.Distance(point1, point2), MidpointRounding.AwayFromZero), point1.Z + size.Z);
            Size = new Vector3((MaxLocation.X - Location.X), (MaxLocation.Y - Location.Y), MaxLocation.Z - Location.Z);

            Extents = Size * 0.5f;
            Origin = Location + Extents;

			Rotation = (Single)System.Math.Atan2(point2.Y - Location.Y, point2.X - Location.X) - (Single)System.Math.Atan2(MaxLocation.Y - Location.Y, MaxLocation.X - Location.X);
            RotationMatrix = MathHelper.CreateMatrixFromAxisAngle(new Vector3(0f, 0f, 1f), Rotation);

            AxisBoundingBox = new BoundingBox(Location, MaxLocation);
            ObjectSpaceCorners = AxisBoundingBox.GetCorners();
            Corners = AxisBoundingBox.GetCorners();

            Single radius = Extents.Length();

            for (Byte i = 0; i < 8; i++)
            {
                Single fDist = Vector3.Distance(Origin, Corners[i]);
                if (fDist > radius) radius = fDist;
            }

            ExtentSphere = new BoundingSphere(Origin, radius);

            Rotate();
        }

        public OrientedBoundingBox(Vector3 location, Vector3 size, Single rotation)
        {
            IsPivotRotation = false;

            Location = location;
            Size = new Vector3(System.Math.Abs(size.X), System.Math.Abs(size.Y), System.Math.Abs(size.Z)); // Force positive
            Rotation = rotation;
            RotationMatrix = MathHelper.CreateMatrixFromAxisAngle(new Vector3(0f, 0f, 1f), rotation);

            Extents = Size * 0.5f;
            Origin = Location + Extents;

            MaxLocation = Location + Size;

            AxisBoundingBox = new BoundingBox(Location, MaxLocation);
            ObjectSpaceCorners = AxisBoundingBox.GetCorners();
            Corners = AxisBoundingBox.GetCorners();

            Single radius = 0;

            for (Byte i = 0; i < 8; i++)
            {
                Single fDist = Vector3.Distance(Origin, Corners[i]);
                if (fDist > radius) radius = fDist;
            }

            ExtentSphere = new BoundingSphere(Origin, radius);

            Rotate();
        }

        public void Move(Vector3 location)
        {
            Location = location;
            MaxLocation = new Vector3(Size.X + Location.X, Size.Y + Location.Y, Size.Z + Location.Z);
            Origin = Location + Extents;

            AxisBoundingBox = new BoundingBox(Location, MaxLocation);
            ObjectSpaceCorners = AxisBoundingBox.GetCorners();
            Corners = AxisBoundingBox.GetCorners();

            if (Rotation > 0.0f) Rotate();

            ExtentSphere = new BoundingSphere(Origin, ExtentSphere.Radius);
        }

        public void MoveAndResize(Vector3 location, Vector3 size)
        {
            Location = location;
            MaxLocation = new Vector3(Size.X + Location.X, Size.Y + Location.Y, Size.Z + Location.Z);
            Size = size;
            Extents = (MaxLocation - Location) * 0.5f;
            Origin = Location + Extents;

            AxisBoundingBox = new BoundingBox(Location, MaxLocation);
            ObjectSpaceCorners = AxisBoundingBox.GetCorners();
            Corners = AxisBoundingBox.GetCorners();

            if (Rotation > 0.0f) Rotate();

            ExtentSphere = new BoundingSphere(Origin, ExtentSphere.Radius);
        }

        public void Rotate()
        {
            InvertedRotationMatrix = Matrix.Invert(RotationMatrix);

            for (Byte i = 0; i < 8; i++)
            {
                Vector3 diffVect = ObjectSpaceCorners[i] - (IsPivotRotation ? Location : Origin);
                Vector3 rotatedVect = (Vector3)Vector3.Transform(diffVect, RotationMatrix) + (IsPivotRotation ? Location : Origin);
                Corners[i] = rotatedVect;
            }

            if (IsPivotRotation)
            {
                Origin = Corners[6] - ((Corners[6] - Corners[0]) * 0.5f);
                MaxLocation = Corners[0];

                ExtentSphere = new BoundingSphere(Origin, ExtentSphere.Radius);
            }
        }
        public float GetProjectionRadius(Vector3 axis)
        {
            // The projected radius is the sum of the absolute dot products 
            // of the axis with the box's local axes scaled by extents.
            Vector3[] localAxes = this.Axes;

            return Extents.X * System.Math.Abs(Vector3.Dot(axis, localAxes[0])) +
                   Extents.Y * System.Math.Abs(Vector3.Dot(axis, localAxes[1])) +
                   Extents.Z * System.Math.Abs(Vector3.Dot(axis, localAxes[2]));
        }
        public Vector3 GetNormal(Vector3 impactPoint)
        {
            Vector3 localPoint = (Rotation == 0.0f)
                ? (impactPoint - Origin)
                : Vector3.TransformCoordinate(impactPoint - Origin, InvertedRotationMatrix);

            // Calculate how far we are from the center relative to the edges
            float xDist = Extents.X - System.Math.Abs(localPoint.X);
            float yDist = Extents.Y - System.Math.Abs(localPoint.Y);
            float zDist = Extents.Z - System.Math.Abs(localPoint.Z);

            // The SMALLEST distance to an edge is the face we hit
            float min = System.Math.Min(xDist, System.Math.Min(yDist, zDist));

            if (min == xDist) return new Vector3(System.Math.Sign(localPoint.X), 0, 0);
            if (min == yDist) return new Vector3(0, System.Math.Sign(localPoint.Y), 0);
            return new Vector3(0, 0, System.Math.Sign(localPoint.Z));
        }
        public Boolean PointInBox(Vector3 point)
        {
            Vector3 boxSpacePoint = (Vector3)Vector3.Transform(point - Origin, InvertedRotationMatrix);
			return System.Math.Abs(boxSpacePoint.X) <= Extents.X && System.Math.Abs(boxSpacePoint.Y) <= Extents.Y && System.Math.Abs(boxSpacePoint.Z) <= Extents.Z;
        }

        public Boolean LineInBox(Vector3 startPoint, Vector3 endPoint)
        {
            Vector3 boxSpaceStartPoint = (Vector3)Vector3.Transform(startPoint - Origin, InvertedRotationMatrix);
            Vector3 boxSpaceEndPoint = (Vector3)Vector3.Transform(endPoint - Origin, InvertedRotationMatrix);

            Vector3 lMid = (boxSpaceStartPoint + boxSpaceEndPoint) * 0.5f;
            Vector3 l = (boxSpaceStartPoint - lMid);
			Vector3 lineExtent = new Vector3(System.Math.Abs(l.X), System.Math.Abs(l.Y), System.Math.Abs(l.Z));

            if (System.Math.Abs(lMid.X) > Extents.X + lineExtent.X) return false;
			if (System.Math.Abs(lMid.Y) > Extents.Y + lineExtent.Y) return false;
			if (System.Math.Abs(lMid.Z) > Extents.Z + lineExtent.Z) return false;

			if (System.Math.Abs(lMid.Y * l.Z - lMid.Z * l.Y) > (Extents.Y * lineExtent.Z + Extents.Z * lineExtent.Y)) return false;
			if (System.Math.Abs(lMid.X * l.Z - lMid.Z * l.X) > (Extents.X * lineExtent.Z + Extents.Z * lineExtent.X)) return false;
			if (System.Math.Abs(lMid.X * l.Y - lMid.Y * l.X) > (Extents.X * lineExtent.Y + Extents.Y * lineExtent.X)) return false;

            return true;
        }

        public Boolean IsBoxVisibleToPoint(Vector3 startPoint, OrientedBoundingBox blockingBox)
        {
            Boolean isBlocked = Corners.All(t => blockingBox.LineInBox(startPoint, t));

            if (!blockingBox.LineInBox(startPoint, blockingBox.Origin)) isBlocked = false;

            return isBlocked;
        }

        public Vector3 LineImpactVector(Vector3 startPoint, Vector3 endPoint)
        {
            Vector3 impactPoint = endPoint;
            Vector3 direction = Vector3.Normalize(endPoint - startPoint);

            while (PointInBox(impactPoint))
            {
                impactPoint -= direction;
            }

            return impactPoint;
        }

        public Single DistanceFromPointToClosestCorner(Vector3 point)
        {
            Single distance = 2147483647;
            Single pointDistance;

            for (Byte i = 0; i < 8; i++)
            {
                pointDistance = Vector3.Distance(point, Corners[i]);
                if (pointDistance < distance) distance = pointDistance;
            }

            pointDistance = Vector3.Distance(point, Origin);
            if (pointDistance < distance) distance = pointDistance;

            return distance;
        }

        public bool Intersects(OrientedBoundingBox other)
        {
            // 1. Pre-test with Bounding Sphere (Your current logic is good for early exit)
            BoundingSphere otherSphere = other.ExtentSphere;
            if (ExtentSphere.Contains(ref otherSphere) == ContainmentType.Disjoint)
                return false;

            // 2. SAT Test
            // We need the axes of both boxes. Assuming your OBB stores its 
            // Right (X), Up (Y), and Forward (Z) vectors based on Rotation.
            Vector3[] axesA = this.Axes; // The 3 local normalized vectors
            Vector3[] axesB = other.Axes;

            Vector3 distanceVector = other.Center - this.Center;

            for (int i = 0; i < 3; i++)
            {
                // Test axes of Box A
                if (IsSeparatedOnAxis(axesA[i], this, other, distanceVector)) return false;
                // Test axes of Box B
                if (IsSeparatedOnAxis(axesB[i], this, other, distanceVector)) return false;
            }

            for (int i = 0; i < 3; i++)
            {
                for (int j = 0; j < 3; j++)
                {
                    // Test 9 cross-product axes
                    Vector3 axis = Vector3.Cross(axesA[i], axesB[j]);
                    if (axis.LengthSquared() < 1.0e-6f) continue; // Skip parallel axes
                    if (IsSeparatedOnAxis(axis, this, other, distanceVector)) return false;
                }
            }

            return true; // No separating axis found
        }

        private bool IsSeparatedOnAxis(Vector3 axis, OrientedBoundingBox a, OrientedBoundingBox b, Vector3 distanceVector)
        {
            // Project the extents of both boxes onto the axis
            float projectionA = a.GetProjectionRadius(axis);
            float projectionB = b.GetProjectionRadius(axis);
            float distance = System.Math.Abs(Vector3.Dot(distanceVector, axis));

            // If the distance between centers is greater than the sum of projected radii, 
            // there is a gap (a separating axis exists).
            return distance > (projectionA + projectionB);
        }

        public Boolean Collides(OrientedBoundingBox box)
        {
            BoundingSphere boxSphere = box.ExtentSphere;

            switch (ExtentSphere.Contains(ref boxSphere))
            {
                case ContainmentType.Disjoint:
                {
                    return false;
                }
            }

            for (Int32 i = 0; i < 4; i++)
            {
                if (LineInBox(box.Corners[i], box.Corners[i + 4]) || box.LineInBox(Corners[i], Corners[i + 4]))
                {
                    return true;
                }
            }

            for (Int32 i = 0; i < 8; i++)
            {
                Int32 pIndex;

                switch (i)
                {
                    case 3:
                        {
                            pIndex = 0;
                            break;
                        }
                    case 7:
                        {
                            pIndex = 4;
                            break;
                        }
                    default:
                        {
                            pIndex = i + 1;
                            break;
                        }
                }

                if (LineInBox(box.Corners[i], box.Corners[pIndex]) || box.LineInBox(Corners[i], Corners[pIndex]))
                {
                    return true;
                }
            }

            return false;
        }

        public Vector3 GetOBBHitNormal(OrientedBoundingBox obb, Vector3 projectilePos)
        {
            // 1. Get the relative vector from the center (Origin) to the projectile
            Vector3 relativePos = projectilePos - obb.Origin;

            // 2. Extract Basis Vectors from the RotationMatrix
            Vector3 bX = new Vector3(obb.RotationMatrix.M11, obb.RotationMatrix.M12, obb.RotationMatrix.M13);
            Vector3 bY = new Vector3(obb.RotationMatrix.M21, obb.RotationMatrix.M22, obb.RotationMatrix.M23);
            Vector3 bZ = new Vector3(obb.RotationMatrix.M31, obb.RotationMatrix.M32, obb.RotationMatrix.M33);

            // 3. Project relativePos onto axes to get local coordinates
            float localX = Vector3.Dot(relativePos, bX);
            float localY = Vector3.Dot(relativePos, bY);
            float localZ = Vector3.Dot(relativePos, bZ);

            // 4. Determine which face was hit by checking the "pushout" distance
            // We compare how close the projectile is to the edge of each extent
            float minDist = float.MaxValue;
            Vector3 worldNormal = bY; // Default fallback

            // Check X faces
            float distX = obb.Extents.X - System.Math.Abs(localX);
            if (distX < minDist)
            {
                minDist = distX;
                worldNormal = bX * System.Math.Sign(localX);
            }

            // Check Y faces
            float distY = obb.Extents.Y - System.Math.Abs(localY);
            if (distY < minDist)
            {
                minDist = distY;
                worldNormal = bY * System.Math.Sign(localY);
            }

            // Check Z faces
            float distZ = obb.Extents.Z - System.Math.Abs(localZ);
            if (distZ < minDist)
            {
                worldNormal = bZ * System.Math.Sign(localZ);
            }

            return worldNormal;
        }
    }
}
