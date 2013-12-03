using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Represents everything needed for a frame of input
	/// </summary>
	public interface IMovieRecord
	{
		/// <summary>
		/// String representation of the controller input as a series of mnemonics
		/// </summary>
		string Input { get; set;  }

		/// <summary>
		/// Whether or not this was a lag frame, 
		/// where lag is the act of the core failing to poll for input (input on lag frames have no affect)
		/// </summary>
		bool Lagged { get; }

		/// <summary>
		/// A savestate for this frame of input
		/// </summary>
		IEnumerable<byte> State { get; }

	}
}
