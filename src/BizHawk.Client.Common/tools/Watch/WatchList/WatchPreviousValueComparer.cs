using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// This class hold a collection <see cref="Watch"/>
	/// Different memory domain can be mixed
	/// </summary>
	public sealed partial class WatchList
	{
		/// <summary>
		/// Nested private class that define how to compare two <see cref="Watch"/>
		/// based on their previous value
		/// </summary>
		private sealed class WatchPreviousValueComparer
			: WatchEqualityComparer, IComparer<Watch>
		{
			/// <summary>
			/// Compares two <see cref="Watch"/> between them
			/// and determines which one comes first.
			/// If they are equals, comparison will done one the address and next on size
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

				if (x.Previous.Equals(y.Previous))
				{
					if (x.Address.Equals(y.Address))
					{
						return x.Size.CompareTo(y.Size);
					}

					return x.Address.CompareTo(y.Address);
				}

				return x.Previous.CompareTo(y.Previous);
			}
		}
	}
}
