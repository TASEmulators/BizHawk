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
		/// based on their address
		/// </summary>
		private sealed class WatchAddressComparer
			: WatchEqualityComparer, IComparer<Watch>
		{
			/// <summary>
			/// Compares two <see cref="Watch"/> between them
			/// and determines which one comes first.
			/// If they are equals, comparison will done one the domain and next on size
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

				if (x.Address.Equals(y.Address))
				{
					if (x.Domain.Name.Equals(y.Domain.Name))
					{
						return x.Size.CompareTo(y.Size);
					}

					return x.Domain.Name.CompareTo(y.Domain.Name);
				}

				return x.Address.CompareTo(y.Address);
			}
		}
	}
}
