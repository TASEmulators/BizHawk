using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Sound.Utilities
{
	/// <summary>
	/// describes an audio resampler that works with stereo streams of shorts (interleaved)
	/// </summary>
	public interface IStereoResampler
	{
		/// <summary>
		/// start a resampling session, with the given conversion rate
		/// </summary>
		/// <param name="ratio">outrate / inrate</param>
		void StartSession(double ratio);

		/// <summary>
		/// process any available input
		/// </summary>
		/// <param name="input">input samples.  all might not be consumed unless finish == true</param>
		/// <param name="output">where to put output samples.</param>
		/// <param name="finish">if true, consume all input and end session</param>
		void ResampleChunk(Queue<short> input, Queue<short> output, bool finish);
	}
}
