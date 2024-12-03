using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace GameServer
{

#if !(UNITY_EDITOR || UNITY_STANDALONE || UNITY_ANDROID || UNITY_IOS)

    public struct Mathf
    {
        //
        // 摘要:
        //     The well-known 3.14159265358979... value (Read Only).
        public const float PI = MathF.PI;

        //
        // 摘要:
        //     A representation of positive infinity (Read Only).
        public const float Infinity = float.PositiveInfinity;

        //
        // 摘要:
        //     A representation of negative infinity (Read Only).
        public const float NegativeInfinity = float.NegativeInfinity;

        //
        // 摘要:
        //     Degrees-to-radians conversion constant (Read Only).
        public const float Deg2Rad = MathF.PI / 180f;

        //
        // 摘要:
        //     Radians-to-degrees conversion constant (Read Only).
        public const float Rad2Deg = 57.29578f;

        internal const int kMaxDecimals = 15;

        //
        // 摘要:
        //     A tiny floating point value (Read Only).
        public static readonly float Epsilon = float.Epsilon;

        /// <summary>
        /// 两个值近似相等
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static bool Similar(float a, float b, float tolerance = 10e-6f)
        {
            return MathF.Abs(a - b) < tolerance; //0.00001
        }

        //
        // 摘要:
        //     Returns the sine of angle f.
        //
        // 参数:
        //   f:
        //     The input angle, in radians.
        //
        // 返回结果:
        //     The return value between -1 and +1.
        public static float Sin(float f)
        {
            return (float)Math.Sin(f);
        }

        //
        // 摘要:
        //     Returns the cosine of angle f.
        //
        // 参数:
        //   f:
        //     The input angle, in radians.
        //
        // 返回结果:
        //     The return value between -1 and 1.
        public static float Cos(float f)
        {
            return (float)Math.Cos(f);
        }

        //
        // 摘要:
        //     Returns the tangent of angle f in radians.
        //
        // 参数:
        //   f:
        public static float Tan(float f)
        {
            return (float)Math.Tan(f);
        }

        //
        // 摘要:
        //     Returns the arc-sine of f - the angle in radians whose sine is f.
        //
        // 参数:
        //   f:
        public static float Asin(float f)
        {
            return (float)Math.Asin(f);
        }

        //
        // 摘要:
        //     Returns the arc-cosine of f - the angle in radians whose cosine is f.
        //
        // 参数:
        //   f:
        public static float Acos(float f)
        {
            return (float)Math.Acos(f);
        }

        //
        // 摘要:
        //     Returns the arc-tangent of f - the angle in radians whose tangent is f.
        //
        // 参数:
        //   f:
        public static float Atan(float f)
        {
            return (float)Math.Atan(f);
        }

        //
        // 摘要:
        //     Returns the angle in radians whose Tan is y/x.
        //
        // 参数:
        //   y:
        //
        //   x:
        public static float Atan2(float y, float x)
        {
            return (float)Math.Atan2(y, x);
        }

        //
        // 摘要:
        //     Returns square root of f.
        //
        // 参数:
        //   f:
        public static float Sqrt(float f)
        {
            return (float)Math.Sqrt(f);
        }

        //
        // 摘要:
        //     Returns the absolute value of f.
        //
        // 参数:
        //   f:
        public static float Abs(float f)
        {
            return Math.Abs(f);
        }

        //
        // 摘要:
        //     Returns the absolute value of value.
        //
        // 参数:
        //   value:
        public static int Abs(int value)
        {
            return Math.Abs(value);
        }

        //
        // 摘要:
        //     Returns the smallest of two or more values.
        //
        // 参数:
        //   a:
        //
        //   b:
        //
        //   values:
        public static float Min(float a, float b)
        {
            return a < b ? a : b;
        }

        //
        // 摘要:
        //     Returns the smallest of two or more values.
        //
        // 参数:
        //   a:
        //
        //   b:
        //
        //   values:
        public static float Min(params float[] values)
        {
            int num = values.Length;
            if (num == 0)
            {
                return 0f;
            }

            float num2 = values[0];
            for (int i = 1; i < num; i++)
            {
                if (values[i] < num2)
                {
                    num2 = values[i];
                }
            }

            return num2;
        }

        //
        // 摘要:
        //     Returns the smallest of two or more values.
        //
        // 参数:
        //   a:
        //
        //   b:
        //
        //   values:
        public static int Min(int a, int b)
        {
            return a < b ? a : b;
        }

        //
        // 摘要:
        //     Returns the smallest of two or more values.
        //
        // 参数:
        //   a:
        //
        //   b:
        //
        //   values:
        public static int Min(params int[] values)
        {
            int num = values.Length;
            if (num == 0)
            {
                return 0;
            }

            int num2 = values[0];
            for (int i = 1; i < num; i++)
            {
                if (values[i] < num2)
                {
                    num2 = values[i];
                }
            }

            return num2;
        }

        //
        // 摘要:
        //     Returns largest of two or more values.
        //
        // 参数:
        //   a:
        //
        //   b:
        //
        //   values:
        public static float Max(float a, float b)
        {
            return a > b ? a : b;
        }

        //
        // 摘要:
        //     Returns largest of two or more values.
        //
        // 参数:
        //   a:
        //
        //   b:
        //
        //   values:
        public static float Max(params float[] values)
        {
            int num = values.Length;
            if (num == 0)
            {
                return 0f;
            }

            float num2 = values[0];
            for (int i = 1; i < num; i++)
            {
                if (values[i] > num2)
                {
                    num2 = values[i];
                }
            }

            return num2;
        }

        //
        // 摘要:
        //     Returns the largest of two or more values.
        //
        // 参数:
        //   a:
        //
        //   b:
        //
        //   values:
        public static int Max(int a, int b)
        {
            return a > b ? a : b;
        }

        //
        // 摘要:
        //     Returns the largest of two or more values.
        //
        // 参数:
        //   a:
        //
        //   b:
        //
        //   values:
        public static int Max(params int[] values)
        {
            int num = values.Length;
            if (num == 0)
            {
                return 0;
            }

            int num2 = values[0];
            for (int i = 1; i < num; i++)
            {
                if (values[i] > num2)
                {
                    num2 = values[i];
                }
            }

            return num2;
        }

        //
        // 摘要:
        //     Returns f raised to power p.
        //
        // 参数:
        //   f:
        //
        //   p:
        public static float Pow(float f, float p)
        {
            return (float)Math.Pow(f, p);
        }

        //
        // 摘要:
        //     Returns e raised to the specified power.
        //
        // 参数:
        //   power:
        public static float Exp(float power)
        {
            return (float)Math.Exp(power);
        }

        //
        // 摘要:
        //     Returns the logarithm of a specified number in a specified base.
        //
        // 参数:
        //   f:
        //
        //   p:
        public static float Log(float f, float p)
        {
            return (float)Math.Log(f, p);
        }

        //
        // 摘要:
        //     Returns the natural (base e) logarithm of a specified number.
        //
        // 参数:
        //   f:
        public static float Log(float f)
        {
            return (float)Math.Log(f);
        }

        //
        // 摘要:
        //     Returns the base 10 logarithm of a specified number.
        //
        // 参数:
        //   f:
        public static float Log10(float f)
        {
            return (float)Math.Log10(f);
        }

        //
        // 摘要:
        //     Returns the smallest integer greater to or equal to f.
        //
        // 参数:
        //   f:
        public static float Ceil(float f)
        {
            return (float)Math.Ceiling(f);
        }

        //
        // 摘要:
        //     Returns the largest integer smaller than or equal to f.
        //
        // 参数:
        //   f:
        public static float Floor(float f)
        {
            return (float)Math.Floor(f);
        }

        //
        // 摘要:
        //     Returns f rounded to the nearest integer.
        //
        // 参数:
        //   f:
        public static float Round(float f)
        {
            return (float)Math.Round(f);
        }

        //
        // 摘要:
        //     Returns the smallest integer greater to or equal to f.
        //
        // 参数:
        //   f:
        public static int CeilToInt(float f)
        {
            return (int)Math.Ceiling(f);
        }

        //
        // 摘要:
        //     Returns the largest integer smaller to or equal to f.
        //
        // 参数:
        //   f:
        public static int FloorToInt(float f)
        {
            return (int)Math.Floor(f);
        }

        //
        // 摘要:
        //     Returns f rounded to the nearest integer.
        //
        // 参数:
        //   f:
        public static int RoundToInt(float f)
        {
            return (int)Math.Round(f);
        }

        //
        // 摘要:
        //     Returns the sign of f.
        //
        // 参数:
        //   f:
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Sign(float f)
        {
            return f >= 0f ? 1f : -1f;
        }

        //
        // 摘要:
        //     Clamps the given value between the given minimum float and maximum float values.
        //     Returns the given value if it is within the minimum and maximum range.
        //
        // 参数:
        //   value:
        //     The floating point value to restrict inside the range defined by the minimum
        //     and maximum values.
        //
        //   min:
        //     The minimum floating point value to compare against.
        //
        //   max:
        //     The maximum floating point value to compare against.
        //
        // 返回结果:
        //     The float result between the minimum and maximum values.
        public static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                value = min;
            }
            else if (value > max)
            {
                value = max;
            }

            return value;
        }

        //
        // 摘要:
        //     Clamps the given value between a range defined by the given minimum integer and
        //     maximum integer values. Returns the given value if it is within min and max.
        //
        //
        // 参数:
        //   value:
        //     The integer point value to restrict inside the min-to-max range.
        //
        //   min:
        //     The minimum integer point value to compare against.
        //
        //   max:
        //     The maximum integer point value to compare against.
        //
        // 返回结果:
        //     The int result between min and max values.
        public static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                value = min;
            }
            else if (value > max)
            {
                value = max;
            }

            return value;
        }

        //
        // 摘要:
        //     Clamps value between 0 and 1 and returns value.
        //
        // 参数:
        //   value:
        public static float Clamp01(float value)
        {
            if (value < 0f)
            {
                return 0f;
            }

            if (value > 1f)
            {
                return 1f;
            }

            return value;
        }

        //
        // 摘要:
        //     Linearly interpolates between a and b by t.
        //
        // 参数:
        //   a:
        //     The start value.
        //
        //   b:
        //     The end value.
        //
        //   t:
        //     The interpolation value between the two floats.
        //
        // 返回结果:
        //     The interpolated float result between the two float values.
        public static float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Clamp01(t);
        }

        //
        // 摘要:
        //     Linearly interpolates between a and b by t with no limit to t.
        //
        // 参数:
        //   a:
        //     The start value.
        //
        //   b:
        //     The end value.
        //
        //   t:
        //     The interpolation between the two floats.
        //
        // 返回结果:
        //     The float value as a result from the linear interpolation.
        public static float LerpUnclamped(float a, float b, float t)
        {
            return a + (b - a) * t;
        }

        //
        // 摘要:
        //     Same as Lerp but makes sure the values interpolate correctly when they wrap around
        //     360 degrees.
        //
        // 参数:
        //   a:
        //     The start angle. A float expressed in degrees.
        //
        //   b:
        //     The end angle. A float expressed in degrees.
        //
        //   t:
        //     The interpolation value between the start and end angles. This value is clamped
        //     to the range [0, 1].
        //
        // 返回结果:
        //     Returns the interpolated float result between angle a and angle b, based on the
        //     interpolation value t.
        public static float LerpAngle(float a, float b, float t)
        {
            float num = Repeat(b - a, 360f);
            if (num > 180f)
            {
                num -= 360f;
            }

            return a + num * Clamp01(t);
        }

        //
        // 摘要:
        //     Moves a value current towards target.
        //
        // 参数:
        //   current:
        //     The current value.
        //
        //   target:
        //     The value to move towards.
        //
        //   maxDelta:
        //     The maximum change that should be applied to the value.
        public static float MoveTowards(float current, float target, float maxDelta)
        {
            if (Abs(target - current) <= maxDelta)
            {
                return target;
            }

            return current + Sign(target - current) * maxDelta;
        }

        //
        // 摘要:
        //     Same as MoveTowards but makes sure the values interpolate correctly when they
        //     wrap around 360 degrees.
        //
        // 参数:
        //   current:
        //
        //   target:
        //
        //   maxDelta:
        public static float MoveTowardsAngle(float current, float target, float maxDelta)
        {
            float num = DeltaAngle(current, target);
            if (0f - maxDelta < num && num < maxDelta)
            {
                return target;
            }

            target = current + num;
            return MoveTowards(current, target, maxDelta);
        }

        //
        // 摘要:
        //     Interpolates between min and max with smoothing at the limits.
        //
        // 参数:
        //   from:
        //
        //   to:
        //
        //   t:
        public static float SmoothStep(float from, float to, float t)
        {
            t = Clamp01(t);
            t = -2f * t * t * t + 3f * t * t;
            return to * t + from * (1f - t);
        }

        public static float Gamma(float value, float absmax, float gamma)
        {
            bool flag = value < 0f;
            float num = Abs(value);
            if (num > absmax)
            {
                return flag ? 0f - num : num;
            }

            float num2 = Pow(num / absmax, gamma) * absmax;
            return flag ? 0f - num2 : num2;
        }

        //
        // 摘要:
        //     Compares two floating point values and returns true if they are similar.
        //
        // 参数:
        //   a:
        //
        //   b:
        public static bool Approximately(float a, float b)
        {
            return Abs(b - a) < Max(1E-06f * Max(Abs(a), Abs(b)), Epsilon * 8f);
        }


        public static float SmoothDamp(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            smoothTime = Max(0.0001f, smoothTime);
            float num = 2f / smoothTime;
            float num2 = num * deltaTime;
            float num3 = 1f / (1f + num2 + 0.48f * num2 * num2 + 0.235f * num2 * num2 * num2);
            float value = current - target;
            float num4 = target;
            float num5 = maxSpeed * smoothTime;
            value = Clamp(value, 0f - num5, num5);
            target = current - value;
            float num6 = (currentVelocity + num * value) * deltaTime;
            currentVelocity = (currentVelocity - num * num6) * num3;
            float num7 = target + (value + num6) * num3;
            if (num4 - current > 0f == num7 > num4)
            {
                num7 = num4;
                currentVelocity = (num7 - num4) / deltaTime;
            }

            return num7;
        }

        public static float SmoothDampAngle(float current, float target, ref float currentVelocity, float smoothTime, float maxSpeed, float deltaTime)
        {
            target = current + DeltaAngle(current, target);
            return SmoothDamp(current, target, ref currentVelocity, smoothTime, maxSpeed, deltaTime);
        }

        //
        // 摘要:
        //     Loops the value t, so that it is never larger than length and never smaller than
        //     0.
        //
        // 参数:
        //   t:
        //
        //   length:
        public static float Repeat(float t, float length)
        {
            return Clamp(t - Floor(t / length) * length, 0f, length);
        }

        //
        // 摘要:
        //     PingPong returns a value that will increment and decrement between the value
        //     0 and length.
        //
        // 参数:
        //   t:
        //
        //   length:
        public static float PingPong(float t, float length)
        {
            t = Repeat(t, length * 2f);
            return length - Abs(t - length);
        }

        //
        // 摘要:
        //     Determines where a value lies between two points.
        //
        // 参数:
        //   a:
        //     The start of the range.
        //
        //   b:
        //     The end of the range.
        //
        //   value:
        //     The point within the range you want to calculate.
        //
        // 返回结果:
        //     A value between zero and one, representing where the "value" parameter falls
        //     within the range defined by a and b.
        public static float InverseLerp(float a, float b, float value)
        {
            if (a != b)
            {
                return Clamp01((value - a) / (b - a));
            }

            return 0f;
        }

        //
        // 摘要:
        //     Calculates the shortest difference between two given angles given in degrees.
        //
        //
        // 参数:
        //   current:
        //
        //   target:
        public static float DeltaAngle(float current, float target)
        {
            float num = Repeat(target - current, 360f);
            if (num > 180f)
            {
                num -= 360f;
            }

            return num;
        }

        internal static bool LineIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, ref Vector2 result)
        {
            float num = p2.x - p1.x;
            float num2 = p2.y - p1.y;
            float num3 = p4.x - p3.x;
            float num4 = p4.y - p3.y;
            float num5 = num * num4 - num2 * num3;
            if (num5 == 0f)
            {
                return false;
            }

            float num6 = p3.x - p1.x;
            float num7 = p3.y - p1.y;
            float num8 = (num6 * num4 - num7 * num3) / num5;
            result.x = p1.x + num8 * num;
            result.y = p1.y + num8 * num2;
            return true;
        }

        internal static bool LineSegmentIntersection(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4, ref Vector2 result)
        {
            float num = p2.x - p1.x;
            float num2 = p2.y - p1.y;
            float num3 = p4.x - p3.x;
            float num4 = p4.y - p3.y;
            float num5 = num * num4 - num2 * num3;
            if (num5 == 0f)
            {
                return false;
            }

            float num6 = p3.x - p1.x;
            float num7 = p3.y - p1.y;
            float num8 = (num6 * num4 - num7 * num3) / num5;
            if (num8 < 0f || num8 > 1f)
            {
                return false;
            }

            float num9 = (num6 * num2 - num7 * num) / num5;
            if (num9 < 0f || num9 > 1f)
            {
                return false;
            }

            result.x = p1.x + num8 * num;
            result.y = p1.y + num8 * num2;
            return true;
        }

        internal static long RandomToLong(System.Random r)
        {
            byte[] array = new byte[8];
            r.NextBytes(array);
            return (long)(BitConverter.ToUInt64(array, 0) & 0x7FFFFFFFFFFFFFFFL);
        }

        internal static float ClampToFloat(double value)
        {
            if (double.IsPositiveInfinity(value))
            {
                return float.PositiveInfinity;
            }

            if (double.IsNegativeInfinity(value))
            {
                return float.NegativeInfinity;
            }

            if (value < -3.4028234663852886E+38)
            {
                return float.MinValue;
            }

            if (value > 3.4028234663852886E+38)
            {
                return float.MaxValue;
            }

            return (float)value;
        }

        internal static int ClampToInt(long value)
        {
            if (value < int.MinValue)
            {
                return int.MinValue;
            }

            if (value > int.MaxValue)
            {
                return int.MaxValue;
            }

            return (int)value;
        }

        internal static uint ClampToUInt(long value)
        {
            if (value < 0)
            {
                return 0u;
            }

            if (value > uint.MaxValue)
            {
                return uint.MaxValue;
            }

            return (uint)value;
        }

        internal static float RoundToMultipleOf(float value, float roundingValue)
        {
            if (roundingValue == 0f)
            {
                return value;
            }

            return Round(value / roundingValue) * roundingValue;
        }

        internal static float GetClosestPowerOfTen(float positiveNumber)
        {
            if (positiveNumber <= 0f)
            {
                return 1f;
            }

            return Pow(10f, RoundToInt(Log10(positiveNumber)));
        }

        internal static int GetNumberOfDecimalsForMinimumDifference(float minDifference)
        {
            return Clamp(-FloorToInt(Log10(Abs(minDifference))), 0, 15);
        }

        internal static int GetNumberOfDecimalsForMinimumDifference(double minDifference)
        {
            return (int)Math.Max(0.0, 0.0 - Math.Floor(Math.Log10(Math.Abs(minDifference))));
        }

        internal static float RoundBasedOnMinimumDifference(float valueToRound, float minDifference)
        {
            if (minDifference == 0f)
            {
                return DiscardLeastSignificantDecimal(valueToRound);
            }

            return (float)Math.Round(valueToRound, GetNumberOfDecimalsForMinimumDifference(minDifference), MidpointRounding.AwayFromZero);
        }

        internal static double RoundBasedOnMinimumDifference(double valueToRound, double minDifference)
        {
            if (minDifference == 0.0)
            {
                return DiscardLeastSignificantDecimal(valueToRound);
            }

            return Math.Round(valueToRound, GetNumberOfDecimalsForMinimumDifference(minDifference), MidpointRounding.AwayFromZero);
        }

        internal static float DiscardLeastSignificantDecimal(float v)
        {
            int digits = Clamp((int)(5f - Log10(Abs(v))), 0, 15);
            return (float)Math.Round(v, digits, MidpointRounding.AwayFromZero);
        }

        internal static double DiscardLeastSignificantDecimal(double v)
        {
            int digits = Math.Max(0, (int)(5.0 - Math.Log10(Math.Abs(v))));
            try
            {
                return Math.Round(v, digits);
            }
            catch (ArgumentOutOfRangeException)
            {
                return 0.0;
            }
        }

    }

#endif

}

