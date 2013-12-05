using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Represents everything needed for a frame of input
	/// </summary>
	public interface IMovieRecord
	{
		/// <summary>
		/// Gets or sets the string representation of the controller input as a series of mnemonics
		/// </summary>
		string Input { get; set;  }

		/// <summary>
		/// Gets a value indicating whether or not this was a lag frame, 
		/// where lag is the act of the core failing to poll for input (input on lag frames have no affect)
		/// </summary>
		bool Lagged { get; }

		/// <summary>
		/// Gets the Savestate for this frame of input
		/// </summary>
		IEnumerable<byte> State { get; }
	}
}
