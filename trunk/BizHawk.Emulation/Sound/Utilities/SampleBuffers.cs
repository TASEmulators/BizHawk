using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Sound.Utilities
{
	/******************************************************************************
	 *
	 * libresample4j
	 * Copyright (c) 2009 Laszlo Systems, Inc. All Rights Reserved.
	 *
	 * libresample4j is a Java port of Dominic Mazzoni's libresample 0.1.3,
	 * which is in turn based on Julius Smith's Resample 1.7 library.
	 *      http://www-ccrma.stanford.edu/~jos/resample/
	 *
	 * License: LGPL -- see the file LICENSE.txt for more information
	 *
	 *****************************************************************************/

	/// <summary>
	/// Callback for producing and consuming samples. Enables on-the-fly conversion between sample types
	/// (signed 16-bit integers to floats, for example) and/or writing directly to an output stream.
	/// </summary>
	public interface SampleBuffers
	{
		/// <summary>number of input samples available</summary>
		int getInputBufferLength();

		/// <summary>number of samples the output buffer has room for</summary>
		int getOutputBufferLength();

		/// <summary>
		/// Copy <code>length</code> samples from the input buffer to the given array, starting at the given offset.
		/// Samples should be in the range -1.0f to 1.0f.
		/// </summary>
		/// <param name="array">array to hold samples from the input buffer</param>
		/// <param name="offset">start writing samples here</param>
		/// <param name="length">write this many samples</param>
		void produceInput(float[] array, int offset, int length);

		/// <summary>
		/// Copy <code>length</code> samples from the given array to the output buffer, starting at the given offset.
		/// </summary>
		/// <param name="array">array to read from</param>
		/// <param name="offset">start reading samples here</param>
		/// <param name="length">read this many samples</param>
		void consumeOutput(float[] array, int offset, int length);
	}
}
