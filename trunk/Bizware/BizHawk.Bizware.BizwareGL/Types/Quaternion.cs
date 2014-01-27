#region License
/*
MIT License
Copyright © 2006 The Mono.Xna Team

All rights reserved.

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/
#endregion License

using System;
using System.ComponentModel;
using System.Text;

namespace BizHawk.Bizware.BizwareGL
{
    [Serializable]
    public struct Quaternion : IEquatable<Quaternion>
    {
        public float X;
        public float Y;
        public float Z;
        public float W;
        static Quaternion identity = new Quaternion(0, 0, 0, 1);

        
        public Quaternion(float x, float y, float z, float w)
        {
            this.X = x;
            this.Y = y;
            this.Z = z;
            this.W = w;
        }
        
        
        public Quaternion(Vector3 vectorPart, float scalarPart)
        {
            this.X = vectorPart.X;
            this.Y = vectorPart.Y;
            this.Z = vectorPart.Z;
            this.W = scalarPart;
        }

        public static Quaternion Identity
        {
            get{ return identity; }
        }


        public static Quaternion Add(Quaternion quaternion1, Quaternion quaternion2)
        {
            quaternion1.X += quaternion2.X;
            quaternion1.Y += quaternion2.Y;
            quaternion1.Z += quaternion2.Z;
            quaternion1.W += quaternion2.W;
            return quaternion1;
        }


        public static void Add(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result)
        {
            result.W = quaternion1.W + quaternion2.W;
            result.X = quaternion1.X + quaternion2.X;
            result.Y = quaternion1.Y + quaternion2.Y;
            result.Z = quaternion1.Z + quaternion2.Z;
        }

        public static Quaternion Concatenate(Quaternion value1, Quaternion value2)
        {
            Quaternion quaternion;
            quaternion.X = ((value2.X * value1.W) + (value1.X * value2.W)) + (value2.Y * value1.Z) - (value2.Z * value1.Y);
            quaternion.Y = ((value2.Y * value1.W) + (value1.Y * value2.W)) + (value2.Z * value1.X) - (value2.X * value1.Z);
            quaternion.Z = ((value2.Z * value1.W) + (value1.Z * value2.W)) + (value2.X * value1.Y) - (value2.Y * value1.X);
            quaternion.W = (value2.W * value1.W) - ((value2.X * value1.X) + (value2.Y * value1.Y)) + (value2.Z * value1.Z);
            return quaternion;
        }

        public void Conjugate()
        {
            this.X = -this.X;
            this.Y = -this.Y;
            this.Z = -this.Z;
        }

        public static Quaternion Conjugate(Quaternion value)
        {
            Quaternion quaternion;
            quaternion.X = -value.X;
            quaternion.Y = -value.Y;
            quaternion.Z = -value.Z;
            quaternion.W = value.W;
            return quaternion;
        }

        public static void Conjugate(ref Quaternion value, out Quaternion result)
        {
            result.X = -value.X;
            result.Y = -value.Y;
            result.Z = -value.Z;
            result.W = value.W;
        }

        public static void Concatenate(ref Quaternion value1, ref Quaternion value2, out Quaternion result)
        {
            result.X = ((value2.X * value1.W) + (value1.X * value2.W)) + (value2.Y * value1.Z) - (value2.Z * value1.Y);
            result.Y = ((value2.Y * value1.W) + (value1.Y * value2.W)) + (value2.Z * value1.X) - (value2.X * value1.Z);
            result.Z = ((value2.Z * value1.W) + (value1.Z * value2.W)) + (value2.X * value1.Y) - (value2.Y * value1.X);
            result.W = (value2.W * value1.W) - ((value2.X * value1.X) + (value2.Y * value1.Y)) + (value2.Z * value1.Z);
        }

        public static Quaternion CreateFromYawPitchRoll(float yaw, float pitch, float roll)
        {
            Quaternion quaternion;
            quaternion.X = (((float)Math.Cos((double)(yaw * 0.5f)) * (float)Math.Sin((double)(pitch * 0.5f))) * (float)Math.Cos((double)(roll * 0.5f))) + (((float)Math.Sin((double)(yaw * 0.5f)) * (float)Math.Cos((double)(pitch * 0.5f))) * (float)Math.Sin((double)(roll * 0.5f)));
            quaternion.Y = (((float)Math.Sin((double)(yaw * 0.5f)) * (float)Math.Cos((double)(pitch * 0.5f))) * (float)Math.Cos((double)(roll * 0.5f))) - (((float)Math.Cos((double)(yaw * 0.5f)) * (float)Math.Sin((double)(pitch * 0.5f))) * (float)Math.Sin((double)(roll * 0.5f)));
            quaternion.Z = (((float)Math.Cos((double)(yaw * 0.5f)) * (float)Math.Cos((double)(pitch * 0.5f))) * (float)Math.Sin((double)(roll * 0.5f))) - (((float)Math.Sin((double)(yaw * 0.5f)) * (float)Math.Sin((double)(pitch * 0.5f))) * (float)Math.Cos((double)(roll * 0.5f)));
            quaternion.W = (((float)Math.Cos((double)(yaw * 0.5f)) * (float)Math.Cos((double)(pitch * 0.5f))) * (float)Math.Cos((double)(roll * 0.5f))) + (((float)Math.Sin((double)(yaw * 0.5f)) * (float)Math.Sin((double)(pitch * 0.5f))) * (float)Math.Sin((double)(roll * 0.5f)));
            return quaternion;
        }

        public static void CreateFromYawPitchRoll(float yaw, float pitch, float roll, out Quaternion result)
        {
            result.X = (((float)Math.Cos((double)(yaw * 0.5f)) * (float)Math.Sin((double)(pitch * 0.5f))) * (float)Math.Cos((double)(roll * 0.5f))) + (((float)Math.Sin((double)(yaw * 0.5f)) * (float)Math.Cos((double)(pitch * 0.5f))) * (float)Math.Sin((double)(roll * 0.5f)));
            result.Y = (((float)Math.Sin((double)(yaw * 0.5f)) * (float)Math.Cos((double)(pitch * 0.5f))) * (float)Math.Cos((double)(roll * 0.5f))) - (((float)Math.Cos((double)(yaw * 0.5f)) * (float)Math.Sin((double)(pitch * 0.5f))) * (float)Math.Sin((double)(roll * 0.5f)));
            result.Z = (((float)Math.Cos((double)(yaw * 0.5f)) * (float)Math.Cos((double)(pitch * 0.5f))) * (float)Math.Sin((double)(roll * 0.5f))) - (((float)Math.Sin((double)(yaw * 0.5f)) * (float)Math.Sin((double)(pitch * 0.5f))) * (float)Math.Cos((double)(roll * 0.5f)));
            result.W = (((float)Math.Cos((double)(yaw * 0.5f)) * (float)Math.Cos((double)(pitch * 0.5f))) * (float)Math.Cos((double)(roll * 0.5f))) + (((float)Math.Sin((double)(yaw * 0.5f)) * (float)Math.Sin((double)(pitch * 0.5f))) * (float)Math.Sin((double)(roll * 0.5f)));
        }

        public static Quaternion CreateFromAxisAngle(Vector3 axis, float angle)
        {
            float sin_a = (float)Math.Sin(angle / 2.0f);
            return new Quaternion(axis.X * sin_a,axis.Y * sin_a,axis.Z * sin_a,(float)Math.Cos(angle / 2.0f));
        }


        public static void CreateFromAxisAngle(ref Vector3 axis, float angle, out Quaternion result)
        {
            float sin_a = (float)Math.Sin(angle / 2.0f);
            result.X = axis.X * sin_a;
            result.Y = axis.Y * sin_a;
            result.Z = axis.Z * sin_a;
            result.W = (float)Math.Cos(angle / 2.0f);
        }


        public static Quaternion CreateFromRotationMatrix(Matrix matrix)
        {
            Quaternion result;
            if ((matrix.M11 + matrix.M22 + matrix.M33) > 0.0F)
            {
                float M1 = (float)System.Math.Sqrt((double)(matrix.M11 + matrix.M22 + matrix.M33 + 1.0F));
                result.W = M1 * 0.5F;
                M1 = 0.5F / M1;
                result.X = (matrix.M23 - matrix.M32) * M1;
                result.Y = (matrix.M31 - matrix.M13) * M1;
                result.Z = (matrix.M12 - matrix.M21) * M1;
                return result;
            }
            if ((matrix.M11 >= matrix.M22) && (matrix.M11 >= matrix.M33))
            {
                float M2 = (float)System.Math.Sqrt((double)(1.0F + matrix.M11 - matrix.M22 - matrix.M33));
                float M3 = 0.5F / M2;
                result.X = 0.5F * M2;
                result.Y = (matrix.M12 + matrix.M21) * M3;
                result.Z = (matrix.M13 + matrix.M31) * M3;
                result.W = (matrix.M23 - matrix.M32) * M3;
                return result;
            }
            if (matrix.M22 > matrix.M33)
            {
                float M4 = (float)System.Math.Sqrt((double)(1.0F + matrix.M22 - matrix.M11 - matrix.M33));
                float M5 = 0.5F / M4;
                result.X = (matrix.M21 + matrix.M12) * M5;
                result.Y = 0.5F * M4;
                result.Z = (matrix.M32 + matrix.M23) * M5;
                result.W = (matrix.M31 - matrix.M13) * M5;
                return result;
            }
            float M6 = (float)System.Math.Sqrt((double)(1.0F + matrix.M33 - matrix.M11 - matrix.M22));
            float M7 = 0.5F / M6;
            result.X = (matrix.M31 + matrix.M13) * M7;
            result.Y = (matrix.M32 + matrix.M23) * M7;
            result.Z = 0.5F * M6;
            result.W = (matrix.M12 - matrix.M21) * M7;
            return result;
        }


        public static void CreateFromRotationMatrix(ref Matrix matrix, out Quaternion result)
        {
            if ((matrix.M11 + matrix.M22 + matrix.M33) > 0.0F)
            {
                float M1 = (float)System.Math.Sqrt((double)(matrix.M11 + matrix.M22 + matrix.M33 + 1.0F));
                result.W = M1 * 0.5F;
                M1 = 0.5F / M1;
                result.X = (matrix.M23 - matrix.M32) * M1;
                result.Y = (matrix.M31 - matrix.M13) * M1;
                result.Z = (matrix.M12 - matrix.M21) * M1;
                return;
            }
            if ((matrix.M11 >= matrix.M22) && (matrix.M11 >= matrix.M33))
            {
                float M2 = (float)System.Math.Sqrt((double)(1.0F + matrix.M11 - matrix.M22 - matrix.M33));
                float M3 = 0.5F / M2;
                result.X = 0.5F * M2;
                result.Y = (matrix.M12 + matrix.M21) * M3;
                result.Z = (matrix.M13 + matrix.M31) * M3;
                result.W = (matrix.M23 - matrix.M32) * M3;
                return;
            }
            if (matrix.M22 > matrix.M33)
            {
                float M4 = (float)System.Math.Sqrt((double)(1.0F + matrix.M22 - matrix.M11 - matrix.M33));
                float M5 = 0.5F / M4;
                result.X = (matrix.M21 + matrix.M12) * M5;
                result.Y = 0.5F * M4;
                result.Z = (matrix.M32 + matrix.M23) * M5;
                result.W = (matrix.M31 - matrix.M13) * M5;
                return;
            }
            float M6 = (float)System.Math.Sqrt((double)(1.0F + matrix.M33 - matrix.M11 - matrix.M22));
            float M7 = 0.5F / M6;
            result.X = (matrix.M31 + matrix.M13) * M7;
            result.Y = (matrix.M32 + matrix.M23) * M7;
            result.Z = 0.5F * M6;
            result.W = (matrix.M12 - matrix.M21) * M7;
        }


        public static Quaternion Divide(Quaternion quaternion1, Quaternion quaternion2)
        {
            Quaternion result;

            float w5 = 1.0F / ((quaternion2.X * quaternion2.X) + (quaternion2.Y * quaternion2.Y) + (quaternion2.Z * quaternion2.Z) + (quaternion2.W * quaternion2.W));
            float w4 = -quaternion2.X * w5;
            float w3 = -quaternion2.Y * w5;
            float w2 = -quaternion2.Z * w5;
            float w1 = quaternion2.W * w5;

            result.X = (quaternion1.X * w1) + (w4 * quaternion1.W) + ((quaternion1.Y * w2) - (quaternion1.Z * w3));
            result.Y = (quaternion1.Y * w1) + (w3 * quaternion1.W) + ((quaternion1.Z * w4) - (quaternion1.X * w2));
            result.Z = (quaternion1.Z * w1) + (w2 * quaternion1.W) + ((quaternion1.X * w3) - (quaternion1.Y * w4));
            result.W = (quaternion1.W * quaternion2.W * w5) - ((quaternion1.X * w4) + (quaternion1.Y * w3) + (quaternion1.Z * w2));
            return result;
        }


        public static void Divide(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result)
        {
            float w5 = 1.0F / ((quaternion2.X * quaternion2.X) + (quaternion2.Y * quaternion2.Y) + (quaternion2.Z * quaternion2.Z) + (quaternion2.W * quaternion2.W));
            float w4 = -quaternion2.X * w5;
            float w3 = -quaternion2.Y * w5;
            float w2 = -quaternion2.Z * w5;
            float w1 = quaternion2.W * w5;

            result.X = (quaternion1.X * w1) + (w4 * quaternion1.W) + ((quaternion1.Y * w2) - (quaternion1.Z * w3));
            result.Y = (quaternion1.Y * w1) + (w3 * quaternion1.W) + ((quaternion1.Z * w4) - (quaternion1.X * w2));
            result.Z = (quaternion1.Z * w1) + (w2 * quaternion1.W) + ((quaternion1.X * w3) - (quaternion1.Y * w4));
            result.W = (quaternion1.W * quaternion2.W * w5) - ((quaternion1.X * w4) + (quaternion1.Y * w3) + (quaternion1.Z * w2));
        }


        public static float Dot(Quaternion quaternion1, Quaternion quaternion2)
        {
            return (quaternion1.X * quaternion2.X) + (quaternion1.Y * quaternion2.Y) + (quaternion1.Z * quaternion2.Z) + (quaternion1.W * quaternion2.W);
        }


        public static void Dot(ref Quaternion quaternion1, ref Quaternion quaternion2, out float result)
        {
           result = (quaternion1.X * quaternion2.X) + (quaternion1.Y * quaternion2.Y) + (quaternion1.Z * quaternion2.Z) + (quaternion1.W * quaternion2.W);
        }


        public override bool Equals(object obj)
        {
            return (obj is Quaternion) ? this == (Quaternion)obj : false;
        }


        public bool Equals(Quaternion other)
        {
            if ((X == other.X) && (Y == other.Y) && (Z == other.Z))
                return W == other.W;
            return false;   
        }


        public override int GetHashCode()
        {
            return X.GetHashCode() + Y.GetHashCode() + Z.GetHashCode() + W.GetHashCode();
        }


        public static Quaternion Inverse(Quaternion quaternion)
        {
            Quaternion result;
            float m1 = 1.0F / ((quaternion.X * quaternion.X) + (quaternion.Y * quaternion.Y) + (quaternion.Z * quaternion.Z) + (quaternion.W * quaternion.W));
            result.X = -quaternion.X * m1;
            result.Y = -quaternion.Y * m1;
            result.Z = -quaternion.Z * m1;
            result.W = quaternion.W * m1;
            return result;
        }


        public static void Inverse(ref Quaternion quaternion, out Quaternion result)
        {
            float m1 = 1.0F / ((quaternion.X * quaternion.X) + (quaternion.Y * quaternion.Y) + (quaternion.Z * quaternion.Z) + (quaternion.W * quaternion.W));
            result.X = -quaternion.X * m1;
            result.Y = -quaternion.Y * m1;
            result.Z = -quaternion.Z * m1;
            result.W = quaternion.W * m1;
        }


        public float Length()
        {
            return (float)System.Math.Sqrt((double)((X * X) + (Y * Y) + (Z * Z) + (W * W)));
        }


        public float LengthSquared()
        {
            return (X * X) + (Y * Y) + (Z * Z) + (W * W);
        }


        public static Quaternion Lerp(Quaternion quaternion1, Quaternion quaternion2, float amount)
        {
            Quaternion result;
            float f2 = 1.0F - amount;
            if (((quaternion1.X * quaternion2.X) + (quaternion1.Y * quaternion2.Y) + (quaternion1.Z * quaternion2.Z) + (quaternion1.W * quaternion2.W)) >= 0.0F)
            {
                result.X = (f2 * quaternion1.X) + (amount * quaternion2.X);
                result.Y = (f2 * quaternion1.Y) + (amount * quaternion2.Y);
                result.Z = (f2 * quaternion1.Z) + (amount * quaternion2.Z);
                result.W = (f2 * quaternion1.W) + (amount * quaternion2.W);
            }
            else
            {
                result.X = (f2 * quaternion1.X) - (amount * quaternion2.X);
                result.Y = (f2 * quaternion1.Y) - (amount * quaternion2.Y);
                result.Z = (f2 * quaternion1.Z) - (amount * quaternion2.Z);
                result.W = (f2 * quaternion1.W) - (amount * quaternion2.W);
            }
            float f4 = (result.X * result.X) + (result.Y * result.Y) + (result.Z * result.Z) + (result.W * result.W);
            float f3 = 1.0F / (float)System.Math.Sqrt((double)f4);
            result.X *= f3;
            result.Y *= f3;
            result.Z *= f3;
            result.W *= f3;
            return result;
        }


        public static void Lerp(ref Quaternion quaternion1, ref Quaternion quaternion2, float amount, out Quaternion result)
        {
            float m2 = 1.0F - amount;
            if (((quaternion1.X * quaternion2.X) + (quaternion1.Y * quaternion2.Y) + (quaternion1.Z * quaternion2.Z) + (quaternion1.W * quaternion2.W)) >= 0.0F)
            {
                result.X = (m2 * quaternion1.X) + (amount * quaternion2.X);
                result.Y = (m2 * quaternion1.Y) + (amount * quaternion2.Y);
                result.Z = (m2 * quaternion1.Z) + (amount * quaternion2.Z);
                result.W = (m2 * quaternion1.W) + (amount * quaternion2.W);
            }
            else
            {
                result.X = (m2 * quaternion1.X) - (amount * quaternion2.X);
                result.Y = (m2 * quaternion1.Y) - (amount * quaternion2.Y);
                result.Z = (m2 * quaternion1.Z) - (amount * quaternion2.Z);
                result.W = (m2 * quaternion1.W) - (amount * quaternion2.W);
            }
            float m4 = (result.X * result.X) + (result.Y * result.Y) + (result.Z * result.Z) + (result.W * result.W);
            float m3 = 1.0F / (float)System.Math.Sqrt((double)m4);
            result.X *= m3;
            result.Y *= m3;
            result.Z *= m3;
            result.W *= m3;
        }


        public static Quaternion Slerp(Quaternion quaternion1, Quaternion quaternion2, float amount)
        {
            Quaternion result;
            float q2, q3;

            float q4 = (quaternion1.X * quaternion2.X) + (quaternion1.Y * quaternion2.Y) + (quaternion1.Z * quaternion2.Z) + (quaternion1.W * quaternion2.W);
            bool flag = false;
            if (q4 < 0.0F)
            {
                flag = true;
                q4 = -q4;
            }
            if (q4 > 0.999999F)
            {
                q3 = 1.0F - amount;
                q2 = flag ? -amount : amount;
            }
            else
            {
                float q5 = (float)System.Math.Acos((double)q4);
                float q6 = (float)(1.0 / System.Math.Sin((double)q5));
                q3 = (float)System.Math.Sin((double)((1.0F - amount) * q5)) * q6;
                q2 = flag ? (float)-System.Math.Sin((double)(amount * q5)) * q6 : (float)System.Math.Sin((double)(amount * q5)) * q6;
            }
            result.X = (q3 * quaternion1.X) + (q2 * quaternion2.X);
            result.Y = (q3 * quaternion1.Y) + (q2 * quaternion2.Y);
            result.Z = (q3 * quaternion1.Z) + (q2 * quaternion2.Z);
            result.W = (q3 * quaternion1.W) + (q2 * quaternion2.W);
            return result;
        }


        public static void Slerp(ref Quaternion quaternion1, ref Quaternion quaternion2, float amount, out Quaternion result)
        {
            float q2, q3;

            float q4 = (quaternion1.X * quaternion2.X) + (quaternion1.Y * quaternion2.Y) + (quaternion1.Z * quaternion2.Z) + (quaternion1.W * quaternion2.W);
            bool flag = false;
            if (q4 < 0.0F)
            {
                flag = true;
                q4 = -q4;
            }
            if (q4 > 0.999999F)
            {
                q3 = 1.0F - amount;
                q2 = flag ? -amount : amount;
            }
            else
            {
                float q5 = (float)System.Math.Acos((double)q4);
                float q6 = (float)(1.0 / System.Math.Sin((double)q5));
                q3 = (float)System.Math.Sin((double)((1.0F - amount) * q5)) * q6;
                q2 = flag ? (float)-System.Math.Sin((double)(amount * q5)) * q6 : (float)System.Math.Sin((double)(amount * q5)) * q6;
            }
            result.X = (q3 * quaternion1.X) + (q2 * quaternion2.X);
            result.Y = (q3 * quaternion1.Y) + (q2 * quaternion2.Y);
            result.Z = (q3 * quaternion1.Z) + (q2 * quaternion2.Z);
            result.W = (q3 * quaternion1.W) + (q2 * quaternion2.W);
        }


        public static Quaternion Subtract(Quaternion quaternion1, Quaternion quaternion2)
        {
            quaternion1.X -= quaternion2.X;
            quaternion1.Y -= quaternion2.Y;
            quaternion1.Z -= quaternion2.Z;
            quaternion1.W -= quaternion2.W;
            return quaternion1;
        }


        public static void Subtract(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result)
        {
            result.X = quaternion1.X - quaternion2.X;
            result.Y = quaternion1.Y - quaternion2.Y;
            result.Z = quaternion1.Z - quaternion2.Z;
            result.W = quaternion1.W - quaternion2.W;
        }


        public static Quaternion Multiply(Quaternion quaternion1, Quaternion quaternion2)
        {
            Quaternion result;
            float f12 = (quaternion1.Y * quaternion2.Z) - (quaternion1.Z * quaternion2.Y);
            float f11 = (quaternion1.Z * quaternion2.X) - (quaternion1.X * quaternion2.Z);
            float f10 = (quaternion1.X * quaternion2.Y) - (quaternion1.Y * quaternion2.X);
            float f9 = (quaternion1.X * quaternion2.X) + (quaternion1.Y * quaternion2.Y) + (quaternion1.Z * quaternion2.Z);
            result.X = (quaternion1.X * quaternion2.W) + (quaternion2.X * quaternion1.W) + f12;
            result.Y = (quaternion1.Y * quaternion2.W) + (quaternion2.Y * quaternion1.W) + f11;
            result.Z = (quaternion1.Z * quaternion2.W) + (quaternion2.Z * quaternion1.W) + f10;
            result.W = (quaternion1.W * quaternion2.W) - f9;
            return result;
        }


        public static Quaternion Multiply(Quaternion quaternion1, float scaleFactor)
        {
            quaternion1.X *= scaleFactor;
            quaternion1.Y *= scaleFactor;
            quaternion1.Z *= scaleFactor;
            quaternion1.W *= scaleFactor;
            return quaternion1;
        }


        public static void Multiply(ref Quaternion quaternion1, float scaleFactor, out Quaternion result)
        {
            result.X = quaternion1.X * scaleFactor;
            result.Y = quaternion1.Y * scaleFactor;
            result.Z = quaternion1.Z * scaleFactor;
            result.W = quaternion1.W * scaleFactor;
        }


        public static void Multiply(ref Quaternion quaternion1, ref Quaternion quaternion2, out Quaternion result)
        {
            float f12 = (quaternion1.Y * quaternion2.Z) - (quaternion1.Z * quaternion2.Y);
            float f11 = (quaternion1.Z * quaternion2.X) - (quaternion1.X * quaternion2.Z);
            float f10 = (quaternion1.X * quaternion2.Y) - (quaternion1.Y * quaternion2.X);
            float f9 = (quaternion1.X * quaternion2.X) + (quaternion1.Y * quaternion2.Y) + (quaternion1.Z * quaternion2.Z);
            result.X = (quaternion1.X * quaternion2.W) + (quaternion2.X * quaternion1.W) + f12;
            result.Y = (quaternion1.Y * quaternion2.W) + (quaternion2.Y * quaternion1.W) + f11;
            result.Z = (quaternion1.Z * quaternion2.W) + (quaternion2.Z * quaternion1.W) + f10;
            result.W = (quaternion1.W * quaternion2.W) - f9;
        }


        public static Quaternion Negate(Quaternion quaternion)
        {
            Quaternion result;
            result.X = -quaternion.X;
            result.Y = -quaternion.Y;
            result.Z = -quaternion.Z;
            result.W = -quaternion.W;
            return result;
        }


        public static void Negate(ref Quaternion quaternion, out Quaternion result)
        {
            result.X = -quaternion.X;
            result.Y = -quaternion.Y;
            result.Z = -quaternion.Z;
            result.W = -quaternion.W;
        }


        public void Normalize()
        {
            float f1 = 1.0F / (float)System.Math.Sqrt((double)((this.X * this.X) + (this.Y * this.Y) + (this.Z * this.Z) + (this.W * this.W)));
            this.X *= f1;
            this.Y *= f1;
            this.Z *= f1;
            this.W *= f1;
        }


        public static Quaternion Normalize(Quaternion quaternion)
        {
            Quaternion result;
            float f1 = 1.0F / (float)System.Math.Sqrt((double)((quaternion.X * quaternion.X) + (quaternion.Y * quaternion.Y) + (quaternion.Z * quaternion.Z) + (quaternion.W * quaternion.W)));
            result.X = quaternion.X * f1;
            result.Y = quaternion.Y * f1;
            result.Z = quaternion.Z * f1;
            result.W = quaternion.W * f1;
            return result;
        }


        public static void Normalize(ref Quaternion quaternion, out Quaternion result)
        {
            float f1 = 1.0F / (float)System.Math.Sqrt((double)((quaternion.X * quaternion.X) + (quaternion.Y * quaternion.Y) + (quaternion.Z * quaternion.Z) + (quaternion.W * quaternion.W)));
            result.X = quaternion.X * f1;
            result.Y = quaternion.Y * f1;
            result.Z = quaternion.Z * f1;
            result.W = quaternion.W * f1;
        }


        public static Quaternion operator +(Quaternion quaternion1, Quaternion quaternion2)
        {
            quaternion1.X += quaternion2.X;
            quaternion1.Y += quaternion2.Y;
            quaternion1.Z += quaternion2.Z;
            quaternion1.W += quaternion2.W;
            return quaternion1;
        }


        public static Quaternion operator /(Quaternion quaternion1, Quaternion quaternion2)
        {
            Quaternion result;

            float w5 = 1.0F / ((quaternion2.X * quaternion2.X) + (quaternion2.Y * quaternion2.Y) + (quaternion2.Z * quaternion2.Z) + (quaternion2.W * quaternion2.W));
            float w4 = -quaternion2.X * w5;
            float w3 = -quaternion2.Y * w5;
            float w2 = -quaternion2.Z * w5;
            float w1 = quaternion2.W * w5;

            result.X = (quaternion1.X * w1) + (w4 * quaternion1.W) + ((quaternion1.Y * w2) - (quaternion1.Z * w3));
            result.Y = (quaternion1.Y * w1) + (w3 * quaternion1.W) + ((quaternion1.Z * w4) - (quaternion1.X * w2));
            result.Z = (quaternion1.Z * w1) + (w2 * quaternion1.W) + ((quaternion1.X * w3) - (quaternion1.Y * w4));
            result.W = (quaternion1.W * quaternion2.W * w5) - ((quaternion1.X * w4) + (quaternion1.Y * w3) + (quaternion1.Z * w2));
            return result;
        }


        public static bool operator ==(Quaternion quaternion1, Quaternion quaternion2)
        {
            return quaternion1.X == quaternion2.X
                && quaternion1.Y == quaternion2.Y
                && quaternion1.Z == quaternion2.Z
                && quaternion1.W == quaternion2.W;
        }


        public static bool operator !=(Quaternion quaternion1, Quaternion quaternion2)
        {
            return quaternion1.X != quaternion2.X
                || quaternion1.Y != quaternion2.Y
                || quaternion1.Z != quaternion2.Z
                || quaternion1.W != quaternion2.W;
        }


        public static Quaternion operator *(Quaternion quaternion1, Quaternion quaternion2)
        {
            Quaternion result;
            float f12 = (quaternion1.Y * quaternion2.Z) - (quaternion1.Z * quaternion2.Y);
            float f11 = (quaternion1.Z * quaternion2.X) - (quaternion1.X * quaternion2.Z);
            float f10 = (quaternion1.X * quaternion2.Y) - (quaternion1.Y * quaternion2.X);
            float f9 = (quaternion1.X * quaternion2.X) + (quaternion1.Y * quaternion2.Y) + (quaternion1.Z * quaternion2.Z);
            result.X = (quaternion1.X * quaternion2.W) + (quaternion2.X * quaternion1.W) + f12;
            result.Y = (quaternion1.Y * quaternion2.W) + (quaternion2.Y * quaternion1.W) + f11;
            result.Z = (quaternion1.Z * quaternion2.W) + (quaternion2.Z * quaternion1.W) + f10;
            result.W = (quaternion1.W * quaternion2.W) - f9;
            return result;
        }


        public static Quaternion operator *(Quaternion quaternion1, float scaleFactor)
        {
            quaternion1.X *= scaleFactor;
            quaternion1.Y *= scaleFactor;
            quaternion1.Z *= scaleFactor;
            quaternion1.W *= scaleFactor;
            return quaternion1;
        }


        public static Quaternion operator -(Quaternion quaternion1, Quaternion quaternion2)
        {
            quaternion1.X -= quaternion2.X;
            quaternion1.Y -= quaternion2.Y;
            quaternion1.Z -= quaternion2.Z;
            quaternion1.W -= quaternion2.W;
            return quaternion1;
        }


        public static Quaternion operator -(Quaternion quaternion)
        {
            quaternion.X = -quaternion.X;
            quaternion.Y = -quaternion.Y;
            quaternion.Z = -quaternion.Z;
            quaternion.W = -quaternion.W;
            return quaternion;
        }


        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(32);
            sb.Append("{X:");
            sb.Append(this.X);
            sb.Append(" Y:");
            sb.Append(this.Y);
            sb.Append(" Z:");
            sb.Append(this.Z);
            sb.Append(" W:");
            sb.Append(this.W);
            sb.Append("}");
            return sb.ToString();
        }

    }
}
