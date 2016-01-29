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
		/// Netsed private class that define how to compare two <see cref="Watch"/>
		/// based on their domain
		/// </summary>
		private sealed class WatchDomainComparer
			: WatchEqualityComparer,
			IComparer<Watch>
		{
			/// <summary>
			/// Compares two <see cref="Watch"/> between them
			/// and determines wich one comes first.
			/// If they are equals, comapraison will done one the address and next on size
			/// </summary>
			/// <param name="x">First <see cref="Watch"/></param>
			///<param name="y">Second <see cref="Watch"/></param>
			/// <returns>0 for equality, 1 if x comes first; -1 if y comes first</returns>
			public int Compare(Watch x, Watch y)
			{
				if (Equals(x, y))
				{
					return 0;
				}
				else if (x.Domain.Name.Equals(y.Domain.Name))
				{
					if (x.Address.Equals(y.Address))
					{
						return x.Size.CompareTo(y.Size);
					}
					else
					{
						return x.Address.CompareTo(y.Address);
					}
				}
				else
				{
					return x.Domain.Name.CompareTo(y.Domain.Name);
				}
			}
		}
	}
}
