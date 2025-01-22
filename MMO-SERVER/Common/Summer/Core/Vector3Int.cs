using HS.Protobuf.Common;
using HS.Protobuf.SceneEntity;
using System;

namespace Common.Summer.Core
{
    //
    // 摘要:
    //     Representation of 3D vectors and points using integers.
    public struct Vector3Int : IEquatable<Vector3Int>, IFormattable
    {
        private int m_X;

        private int m_Y;

        private int m_Z;

        private static readonly Vector3Int s_Zero = new Vector3Int(0, 0, 0);

        private static readonly Vector3Int s_One = new Vector3Int(1, 1, 1);

        private static readonly Vector3Int s_Up = new Vector3Int(0, 1, 0);

        private static readonly Vector3Int s_Down = new Vector3Int(0, -1, 0);

        private static readonly Vector3Int s_Left = new Vector3Int(-1, 0, 0);

        private static readonly Vector3Int s_Right = new Vector3Int(1, 0, 0);

        private static readonly Vector3Int s_Forward = new Vector3Int(0, 0, 1);

        private static readonly Vector3Int s_Back = new Vector3Int(0, 0, -1);

        //
        // 摘要:
        //     X component of the vector.
        public int x
        {
            get
            {
                return m_X;
            }
            set
            {
                m_X = value;
            }
        }

        //
        // 摘要:
        //     Y component of the vector.
        public int y
        {
            get
            {
                return m_Y;
            }
            set
            {
                m_Y = value;
            }
        }

        //
        // 摘要:
        //     Z component of the vector.
        public int z
        {
            get
            {
                return m_Z;
            }
            set
            {
                m_Z = value;
            }
        }

        public int this[int index]
        {
            get
            {
                return index switch
                {
                    0 => x,
                    1 => y,
                    2 => z,
                    _ => throw new IndexOutOfRangeException("Invalid Vector3Int index addressed: " + index + "!"),
                };
            }
            set
            {
                switch (index)
                {
                    case 0:
                        x = value;
                        break;
                    case 1:
                        y = value;
                        break;
                    case 2:
                        z = value;
                        break;
                    default:
                        throw new IndexOutOfRangeException("Invalid Vector3Int index addressed: " + index + "!");
                }
            }
        }

        //
        // 摘要:
        //     Returns the length of this vector (Read Only).
        public float magnitude
        {
            
            get
            {
                return (float)Math.Sqrt(x * x + y * y + z * z);
            }
        }

        //
        // 摘要:
        //     Returns the squared length of this vector (Read Only).
        public int sqrMagnitude
        {
            
            get
            {
                return x * x + y * y + z * z;
            }
        }

        //
        // 摘要:
        //     Shorthand for writing Vector3Int(0, 0, 0).
        public static Vector3Int zero
        {
            
            get
            {
                return s_Zero;
            }
        }

        //
        // 摘要:
        //     Shorthand for writing Vector3Int(1, 1, 1).
        public static Vector3Int one
        {
            
            get
            {
                return s_One;
            }
        }

        //
        // 摘要:
        //     Shorthand for writing Vector3Int(0, 1, 0).
        public static Vector3Int up
        {
            
            get
            {
                return s_Up;
            }
        }

        //
        // 摘要:
        //     Shorthand for writing Vector3Int(0, -1, 0).
        public static Vector3Int down
        {
            
            get
            {
                return s_Down;
            }
        }

        //
        // 摘要:
        //     Shorthand for writing Vector3Int(-1, 0, 0).
        public static Vector3Int left
        {
            
            get
            {
                return s_Left;
            }
        }

        //
        // 摘要:
        //     Shorthand for writing Vector3Int(1, 0, 0).
        public static Vector3Int right
        {
            
            get
            {
                return s_Right;
            }
        }

        //
        // 摘要:
        //     Shorthand for writing Vector3Int(0, 0, 1).
        public static Vector3Int forward
        {
            
            get
            {
                return s_Forward;
            }
        }

        //
        // 摘要:
        //     Shorthand for writing Vector3Int(0, 0, -1).
        public static Vector3Int back
        {
            
            get
            {
                return s_Back;
            }
        }

        //
        // 摘要:
        //     Initializes and returns an instance of a new Vector3Int with x and y components
        //     and sets z to zero.
        //
        // 参数:
        //   x:
        //     The X component of the Vector3Int.
        //
        //   y:
        //     The Y component of the Vector3Int.
        
        public Vector3Int(int x, int y)
        {
            m_X = x;
            m_Y = y;
            m_Z = 0;
        }

        //
        // 摘要:
        //     Initializes and returns an instance of a new Vector3Int with x, y, z components.
        //
        // 参数:
        //   x:
        //     The X component of the Vector3Int.
        //
        //   y:
        //     The Y component of the Vector3Int.
        //
        //   z:
        //     The Z component of the Vector3Int.
        
        public Vector3Int(int x, int y, int z)
        {
            m_X = x;
            m_Y = y;
            m_Z = z;
        }

        //
        // 摘要:
        //     Set x, y and z components of an existing Vector3Int.
        //
        // 参数:
        //   x:
        //
        //   y:
        //
        //   z:
        
        public void Set(int x, int y, int z)
        {
            m_X = x;
            m_Y = y;
            m_Z = z;
        }

        //
        // 摘要:
        //     Returns the distance between a and b.
        //
        // 参数:
        //   a:
        //
        //   b:
        
        public static float Distance(Vector3Int a, Vector3Int b)
        {
            return (a - b).magnitude;
        }

        //
        // 摘要:
        //     Returns a vector that is made from the smallest components of two vectors.
        //
        // 参数:
        //   lhs:
        //
        //   rhs:
        
        public static Vector3Int Min(Vector3Int lhs, Vector3Int rhs)
        {
            return new Vector3Int(Math.Min(lhs.x, rhs.x), Math.Min(lhs.y, rhs.y), Math.Min(lhs.z, rhs.z));
        }

        //
        // 摘要:
        //     Returns a vector that is made from the largest components of two vectors.
        //
        // 参数:
        //   lhs:
        //
        //   rhs:
        
        public static Vector3Int Max(Vector3Int lhs, Vector3Int rhs)
        {
            return new Vector3Int(Math.Max(lhs.x, rhs.x), Math.Max(lhs.y, rhs.y), Math.Max(lhs.z, rhs.z));
        }

        //
        // 摘要:
        //     Multiplies two vectors component-wise.
        //
        // 参数:
        //   a:
        //
        //   b:
        
        public static Vector3Int Scale(Vector3Int a, Vector3Int b)
        {
            return new Vector3Int(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        //
        // 摘要:
        //     Multiplies every component of this vector by the same component of scale.
        //
        // 参数:
        //   scale:
        
        public void Scale(Vector3Int scale)
        {
            x *= scale.x;
            y *= scale.y;
            z *= scale.z;
        }

        //
        // 摘要:
        //     Clamps the Vector3Int to the bounds given by min and max.
        //
        // 参数:
        //   min:
        //
        //   max:
        
        public void Clamp(Vector3Int min, Vector3Int max)
        {
            x = Math.Max(min.x, x);
            x = Math.Min(max.x, x);
            y = Math.Max(min.y, y);
            y = Math.Min(max.y, y);
            z = Math.Max(min.z, z);
            z = Math.Min(max.z, z);
        }

        
        
        public static Vector3Int operator +(Vector3Int a, Vector3Int b)
        {
            return new Vector3Int(a.x + b.x, a.y + b.y, a.z + b.z);
        }

        
        public static Vector3Int operator -(Vector3Int a, Vector3Int b)
        {
            return new Vector3Int(a.x - b.x, a.y - b.y, a.z - b.z);
        }

        
        public static Vector3Int operator *(Vector3Int a, Vector3Int b)
        {
            return new Vector3Int(a.x * b.x, a.y * b.y, a.z * b.z);
        }

        
        public static Vector3Int operator -(Vector3Int a)
        {
            return new Vector3Int(-a.x, -a.y, -a.z);
        }

        
        public static Vector3Int operator *(Vector3Int a, int b)
        {
            return new Vector3Int(a.x * b, a.y * b, a.z * b);
        }

        
        public static Vector3Int operator *(int a, Vector3Int b)
        {
            return new Vector3Int(a * b.x, a * b.y, a * b.z);
        }

        
        public static Vector3Int operator /(Vector3Int a, int b)
        {
            return new Vector3Int(a.x / b, a.y / b, a.z / b);
        }

        
        public static bool operator ==(Vector3Int lhs, Vector3Int rhs)
        {
            return lhs.x == rhs.x && lhs.y == rhs.y && lhs.z == rhs.z;
        }

        
        public static bool operator !=(Vector3Int lhs, Vector3Int rhs)
        {
            return !(lhs == rhs);
        }





        //
        // 摘要:
        //     Returns true if the objects are equal.
        //
        // 参数:
        //   other:

        public override bool Equals(object other)
        {
            if (!(other is Vector3Int))
            {
                return false;
            }

            return Equals((Vector3Int)other);
        }

        
        public bool Equals(Vector3Int other)
        {
            return this == other;
        }

        //
        // 摘要:
        //     Gets the hash code for the Vector3Int.
        //
        // 返回结果:
        //     The hash code of the Vector3Int.
        
        public override int GetHashCode()
        {
            int hashCode = y.GetHashCode();
            int hashCode2 = z.GetHashCode();
            return x.GetHashCode() ^ (hashCode << 4) ^ (hashCode >> 28) ^ (hashCode2 >> 4) ^ (hashCode2 << 28);
        }

        //
        // 摘要:
        //     Returns a formatted string for this vector.
        //
        // 参数:
        //   format:
        //     A numeric format string.
        //
        //   formatProvider:
        //     An object that specifies culture-specific formatting.
        public override string ToString()
        {
            return ToString(null, null);
        }

        //
        // 摘要:
        //     Returns a formatted string for this vector.
        //
        // 参数:
        //   format:
        //     A numeric format string.
        //
        //   formatProvider:
        //     An object that specifies culture-specific formatting.
        public string ToString(string format)
        {
            return ToString(format, null);
        }

        //
        // 摘要:
        //     Returns a formatted string for this vector.
        //
        // 参数:
        //   format:
        //     A numeric format string.
        //
        //   formatProvider:
        //     An object that specifies culture-specific formatting.
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "({0}, {1}, {2})", x.ToString(format, formatProvider), y.ToString(format, formatProvider), z.ToString(format, formatProvider));
        }


        //vector3int和Vec3转换
        public static implicit operator Vector3Int(Vec3 v)
        {
            return new Vector3Int() { x = v.X, y = v.Y, z = v.Z };
        }
        public static implicit operator Vec3(Vector3Int v)
        {
            return new Vec3() { X = v.x, Y = v.y, Z = v.z };
        }

        public static implicit operator Vector3Int(NetVector3 v)
        {
            return new Vector3Int() { x = v.X, y = v.Y, z = v.Z };
        }
        public static implicit operator NetVector3(Vector3Int v)
        {
            return new NetVector3() { X = v.x, Y = v.y, Z = v.z };
        }

    }
}
