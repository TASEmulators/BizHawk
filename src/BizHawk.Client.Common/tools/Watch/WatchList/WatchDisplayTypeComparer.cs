using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// This class hold a collection <see cref="Watch"/>
	/// </summary>
	public sealed partial class WatchList
	{
		/// <summary>
		/// Nested private class that defines how to compare two <see cref="Watch"/>es based on their full display type
		/// </summary>
		private sealed class WatchFullDisplayTypeComparer
			: WatchEqualityComparer, IComparer<Watch>
		{
			/// <summary>
			/// Compares two <see cref="Watch"/>es and determines which has greater size.
			/// If they are equal, comparison will done on the display type and then on endianness
			/// </summary>
			/// <param name="x">First <see cref="Watch"/></param>
			/// <param name="y">Second <see cref="Watch"/></param>
			/// <returns>0 for equality, 1 if x comes first; -1 if y comes first</returns>
			public int Compare(Watch x, Watch y)
			{
				if (Equals(x, y))
				{
					return 0;
				}

				if (x.Size.Equals(y.Size))
				{
					if (x.Type.Equals(y.Type))
					{
						return x.BigEndian.CompareTo(y.BigEndian);
					}

					return x.Type.CompareTo(y.Type);
				}

				return x.Size.CompareTo(y.Size);
			}
		}
	}
}
