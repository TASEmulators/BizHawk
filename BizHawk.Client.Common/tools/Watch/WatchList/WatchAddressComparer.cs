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
		/// based on their address
		/// </summary>
		private struct WatchAddressComparer
		: IEqualityComparer<Watch>,
			IComparer<Watch>
		{
			/// <summary>
			/// Compare two <see cref="Watch"/> between them
			/// and determine wich one comes first.
			/// If they are equals, comapraison will done one the domain and next on size
			/// </summary>
			/// <param name="x">First <see cref="Watch"/></param>
			/// <returns>True if <see cref="Watch"/> are equal; otherwise, false</returns>
			/// <returns></returns>
			public int Compare(Watch x, Watch y)
		{
			if (Equals(x, y))
			{
				return 0;
			}
			else if (x.Address.Equals(y.Address))
			{
				if (x.Domain.Name.Equals(y.Domain.Name))
				{
					return x.Size.CompareTo(y.Size);
				}
				else
				{
					return x.Domain.Name.CompareTo(y.Domain.Name);
				}
			}
			else
			{
				return x.Address.CompareTo(y.Address);
			}
		}

		/// <summary>
		/// Determine if two <see cref="Watch"/> are equals
		/// </summary>
		/// <param name="x">First <see cref="Watch"/></param>
		/// <param name="y">Second <see cref="Watch"/></param>
		/// <returns>True if <see cref="Watch"/> are equal; otherwise, false</returns>
		public bool Equals(Watch x, Watch y)
		{
			if (object.ReferenceEquals(x, null))
			{
				if (object.ReferenceEquals(y, null))
				{
					return true;
				}
				else
				{
					return false;
				}
			}
			else if (object.ReferenceEquals(y, null))
			{
				return false;
			}
			else if (object.ReferenceEquals(x, y))
			{
				return true;
			}
			else
			{
				return x.Address.Equals(y.Address);
			}
		}

		/// <summary>
		/// Get the hash value of specified <see cref="Watch"/>
		/// </summary>
		/// <param name="obj">Watch to get hash</param>
		/// <returns>int that can serves as a unique representation of current Watch</returns>
		public int GetHashCode(Watch obj)
		{
			return obj.GetHashCode();
		}
	}
}
}
