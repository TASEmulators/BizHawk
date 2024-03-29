﻿using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// This class hold a collection <see cref="Watch"/>
	/// Different memory domain can be mixed
	/// </summary>
	public sealed partial class WatchList
	{
		private class WatchEqualityComparer : IEqualityComparer<Watch>
		{
			/// <summary>
			/// Determines if two <see cref="Watch"/> are equals
			/// </summary>
			/// <param name="x">First <see cref="Watch"/></param>
			/// <param name="y">Second <see cref="Watch"/></param>
			/// <returns>True if <see cref="Watch"/> are equal; otherwise, false</returns>
			public bool Equals(Watch x, Watch y)
			{
				if (x is null)
				{
					return y is null;
				}

				if (y is null)
				{
					return false;
				}

				return ReferenceEquals(x, y);
			}

			/// <summary>
			/// Gets the hash value of specified <see cref="Watch"/>
			/// </summary>
			/// <param name="obj">Watch to get hash</param>
			/// <returns>int that can serves as a unique representation of current Watch</returns>
			public int GetHashCode(Watch obj) => obj.GetHashCode();
		}
	}
}
