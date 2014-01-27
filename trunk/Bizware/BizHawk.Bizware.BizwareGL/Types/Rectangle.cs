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
using System.Globalization;
using System.ComponentModel;


namespace BizHawk.Bizware.BizwareGL
{


    public struct Rectangle : IEquatable<Rectangle>
    {
        #region Private Fields

        private static Rectangle emptyRectangle = new Rectangle();

        #endregion Private Fields


        #region Public Fields

        public int X;
        public int Y;
        public int Width;
        public int Height;

        #endregion Public Fields


        #region Public Properties

        public Point Center
        {
            get
            {
                return new Point(this.X + (this.Width / 2), this.Y + (this.Height / 2));
            }
        }

        public Point Location
        {
            get
            {
                return new Point(this.X, this.Y);
            }
            set
            {
                this.X = value.X;
                this.Y = value.Y;
            }
        }

        public bool IsEmpty
        {
            get
            {
                return ((((this.Width == 0) && (this.Height == 0)) && (this.X == 0)) && (this.Y == 0));
            }
        }

        public static Rectangle Empty
        {
            get { return emptyRectangle; }
        }

        public int Left
        {
            get { return this.X; }
        }

        public int Right
        {
            get { return (this.X + this.Width); }
        }

        public int Top
        {
            get { return this.Y; }
        }

        public int Bottom
        {
            get { return (this.Y + this.Height); }
        }

        #endregion Public Properties


        #region Constructors

        public Rectangle(int x, int y, int width, int height)
        {
            this.X = x;
            this.Y = y;
            this.Width = width;
            this.Height = height;
        }

        #endregion Constructors


        #region Public Methods

        public static Rectangle Union(Rectangle value1, Rectangle value2)
        {
            throw new NotImplementedException();
        }

        public static void Union(ref Rectangle value1, ref Rectangle value2, out Rectangle result)
        {
            throw new NotImplementedException();
        }

        public static Rectangle Intersect(Rectangle value1, Rectangle value2)
        {
            throw new NotImplementedException();
        }

        public void Intersects(ref Rectangle value, out bool result)
        {
            result = (((value.X < (this.X + this.Width)) && (this.X < (value.X + value.Width))) && (value.Y < (this.Y + this.Height))) && (this.Y < (value.Y + value.Height));
        }

        public bool Contains(Point value)
        {
            return ((((this.X <= value.X) && (value.X < (this.X + this.Width))) && (this.Y <= value.Y)) && (value.Y < (this.Y + this.Height)));
        }

        public bool Contains(Rectangle value)
        {
            return ((((this.X <= value.X) && ((value.X + value.Width) <= (this.X + this.Width))) && (this.Y <= value.Y)) && ((value.Y + value.Height) <= (this.Y + this.Height)));
        }

        public void Contains(ref Rectangle value, out bool result)
        {
            result = (((this.X <= value.X) && ((value.X + value.Width) <= (this.X + this.Width))) && (this.Y <= value.Y)) && ((value.Y + value.Height) <= (this.Y + this.Height));
        }

        public bool Contains(int x, int y)
        {
            return ((((this.X <= x) && (x < (this.X + this.Width))) && (this.Y <= y)) && (y < (this.Y + this.Height)));
        }

        public void Contains(ref Point value, out bool result)
        {
            result = (((this.X <= value.X) && (value.X < (this.X + this.Width))) && (this.Y <= value.Y)) && (value.Y < (this.Y + this.Height));
        }

        public static void Intersect(ref Rectangle value1, ref Rectangle value2, out Rectangle result)
        {
            throw new NotImplementedException();
        }

        public static bool operator ==(Rectangle a, Rectangle b)
        {
            return ((a.X == b.X) && (a.Y == b.Y) && (a.Width == b.Width) && (a.Height == b.Height));
        }

        public static bool operator !=(Rectangle a, Rectangle b)
        {
            return !(a == b);
        }

		/// <summary>
		/// Moves Rectangle for both Point values.
		/// </summary>
		/// <param name="offset">
		/// A <see cref="Point"/>
		/// </param>
        public void Offset(Point offset)
        {
            X += offset.X;
            Y += offset.Y;
        }

		/// <summary>
		/// Moves rectangle for both values.
		/// </summary>
		/// <param name="offsetX">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="offsetY">
		/// A <see cref="System.Int32"/>
		/// </param>
        public void Offset(int offsetX, int offsetY)
        {
            X += offsetX;
            Y += offsetY;
        }

		/// <summary>
		/// Grows the Rectangle. Down right point is in the same position.
		/// </summary>
		/// <param name="horizontalValue">
		/// A <see cref="System.Int32"/>
		/// </param>
		/// <param name="verticalValue">
		/// A <see cref="System.Int32"/>
		/// </param>
        public void Inflate(int horizontalValue, int verticalValue)
        {
            X -= horizontalValue;
            Y -= verticalValue;
            Width += horizontalValue * 2;
            Height += verticalValue * 2;
        }
		
		/// <summary>
		/// It checks if two rectangle intersects.
		/// </summary>
		/// <param name="rect">
		/// A <see cref="Rectangle"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
		public bool Intersects(Rectangle rect)
		{
			if(this.X <= rect.X)
			{
				if((this.X + this.Width) > rect.X)
				{
					if(this.Y < rect.Y)
					{
						if((this.Y + this.Height) > rect.Y)
							return true;
					}
					else
					{
						if((rect.Y + rect.Height) > this.Y)
							return true;
					}
				}
			}
			else
			{
				if((rect.X + rect.Width) > this.X)
				{
					if(this.Y < rect.Y)
					{
						if((this.Y + this.Height) > rect.Y)
							return true;
					}
					else
					{
						if((rect.Y + rect.Height) > this.Y)
							return true;
					}
				}
			}
			return false;
		}


        public bool Equals(Rectangle other)
        {
            return this == other;
        }

		/// <summary>
		/// Returns true if recangles are same
		/// </summary>
		/// <param name="obj">
		/// A <see cref="System.Object"/>
		/// </param>
		/// <returns>
		/// A <see cref="System.Boolean"/>
		/// </returns>
        public override bool Equals(object obj)
        {
            return (obj is Rectangle) ? this == ((Rectangle)obj) : false;
        }

        public override string ToString()
        {
            return string.Format("{{X:{0} Y:{1} Width:{2} Height:{3}}}", X, Y, Width, Height);
        }

        public override int GetHashCode()
        {
            return (this.X ^ this.Y ^ this.Width ^ this.Height);
        }

        #endregion Public Methods
    }
}
