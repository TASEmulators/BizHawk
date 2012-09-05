using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Sound.Utilities
{
	// a simple linear resampler
	public class LinearResampler : IStereoResampler
	{
		short[] data = new short[4];

		double mu;
		/// <summary>input rate / output rate</summary>
		double ratio;

		public LinearResampler()
		{
		}

		public void StartSession(double ratio)
		{
			this.ratio = 1.0 / ratio;
			mu = 0.0;
			for (int i = 0; i < data.Length; i++)
				data[i] = 0;
		}

		public void ResampleChunk(Queue<short> input, Queue<short> output, bool finish)
		{
			while (true)
			{
				while (mu >= 1.0 && input.Count >= 2)
				{
					mu -= 1.0;
					data[0] = data[2];
					data[1] = data[3];
					data[2] = input.Dequeue();
					data[3] = input.Dequeue();
				}
				if (mu >= 1.0)
					return;

				double ls = data[0] * (1.0 - mu) + data[1] * mu;
				double rs = data[1] * (1.0 - mu) + data[3] * mu;

				short l, r;

				if (ls > 32767.0)
					l = 32767;
				else if (ls < -32768.0)
					l = -32768;
				else
					l = (short)ls;

				if (rs > 32767.0)
					r = 32767;
				else if (ls < -32768.0)
					r = -32768;
				else
					r = (short)ls;

				output.Enqueue(l);
				output.Enqueue(r);

				mu += ratio;
			}
		}
	}
}
