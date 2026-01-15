using System;
using SharpDX;

namespace Helper.Math
{
    public static class MathHelper
    {
        public struct FixedPoint
        {
            public int RawValue;

            // Convert from a whole number (e.g., 100) to 16.16 (6,553,600)
            public static FixedPoint FromInt(int value) => new FixedPoint { RawValue = value << 16 };

            // Get the whole number back
            public int ToInt() => RawValue >> 16;
        }

        // MathHelper.DegreesToRadians(90f)
        public const Single RightAngleRadians = 1.570794993f;

        private const Single DirectionRad = 11.37777777777778f;
        private const Single PiDegrees = 0.0174532777777778f;
	    private const Single Epsilon = 1.192092896e-07f;

		public static Boolean FloatEqual(Single a, Single b)
		{
			return (System.Math.Abs(a - b) <= Epsilon * System.Math.Max(System.Math.Abs(a), System.Math.Abs(b)));
		}

        public static Single DegreesToRadians(Single degrees)
        {
            return degrees * 0.0174532777f;
        }

        public static Single DirectionToRadians(Single direction)
        {
            //return (direction / DirectionRad) * PiDegrees;
            return direction * (2f * (float)System.Math.PI / 4096f);
        }

        public static ushort ShortestAngularDifference(ushort a, ushort b)
        {
            int diff = System.Math.Abs((int)a - (int)b);
            return (ushort)System.Math.Min(diff, 4096 - diff);
        }

        public static float DirectionToDegrees(ushort direction)
        {
            return direction / DirectionRad;
        }
        public static bool AreCloseEnough(ushort value1, ushort value2, ushort tolerance)
        {
            // The difference might be negative if value2 > value1, so we cast to a larger
            // signed integer type (like int) to avoid potential overflow during subtraction,
            // then use Math.Abs.
            int difference = System.Math.Abs((int)value1 - (int)value2);
            int shortest = System.Math.Min(difference, 4096 - difference); // Wrap-around
            return difference <= tolerance;
        }
        public static Single DirectionToRadians(ushort direction)
        {
            //return (direction / DirectionRad) * PiDegrees;
            return direction * (float)(2f * System.Math.PI / 4096f);
        }

        public static Single RadiansToDirection(Single radians)
        {
            return ((radians * DirectionRad) * DirectionRad) * 5.035766f;

        }
        public static ushort URadiansToDirection(float radians)
        {
            //radians = radians % (float)(2f * System.Math.PI);
            //if (radians < 0) radians += (float)(2f * System.Math.PI);
            //return (ushort)((radians * 11.37777777777778f * 11.37777777777778f * 5.035766f) % 65536f);
            // Normalize to 0 -> 2PI
            radians = radians % (float)(2f * System.Math.PI);
            if (radians < 0) radians += (float)(2f * System.Math.PI);

            // Convert to 0 -> 4096
            return (ushort)((radians * (4096.0 / (2.0 * System.Math.PI))) % 4096);
        }
        public static int FastDistance(int x, int y)
        {
            int AbsX = System.Math.Abs(x);
            int AbsY = System.Math.Abs(y);

            if ( AbsX <= AbsY )
            {
                return AbsY + (AbsX >> 1);
            }
            else
            {
                return AbsX + (AbsY >> 1);
            }
        }
        public static Matrix CreateMatrixFromAxisAngle(Vector3 axis, Single angle)
        {
            Matrix matrix;

            Single x = axis.X;
            Single y = axis.Y;
            Single z = axis.Z;
			Single num2 = (Single)System.Math.Sin(angle);
			Single num = (Single)System.Math.Cos(angle);
            Single num11 = x * x;
            Single num10 = y * y;
            Single num9 = z * z;
            Single num8 = x * y;
            Single num7 = x * z;
            Single num6 = y * z;
            matrix.M11 = num11 + (num * (1f - num11));
            matrix.M12 = (num8 - (num * num8)) + (num2 * z);
            matrix.M13 = (num7 - (num * num7)) - (num2 * y);
            matrix.M14 = 0f;
            matrix.M21 = (num8 - (num * num8)) - (num2 * z);
            matrix.M22 = num10 + (num * (1f - num10));
            matrix.M23 = (num6 - (num * num6)) + (num2 * x);
            matrix.M24 = 0f;
            matrix.M31 = (num7 - (num * num7)) + (num2 * y);
            matrix.M32 = (num6 - (num * num6)) - (num2 * x);
            matrix.M33 = num9 + (num * (1f - num9));
            matrix.M34 = 0f;
            matrix.M41 = 0f;
            matrix.M42 = 0f;
            matrix.M43 = 0f;
            matrix.M44 = 1f;

            return matrix;
        }
        public static ushort VectorToDirection(Vector3 velocity)
        {
            // Use Atan2 to get the angle. 
            // In your system (VX = -Sin, VY = Cos), Atan2 needs (-X, Y) 
            // to place 0 radians at South.
            float radians = (float)System.Math.Atan2(-velocity.X, velocity.Y);

            float circle = (float)(System.Math.PI * 2.0);

            // Normalize to 0 -> 2PI range
            float normalized = (radians % circle + circle) % circle;

            // Map 0-2PI to 0-4096
            return (ushort)((normalized / circle) * 4096.0f);
        }
    }
}
