using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Sound.Utilities
{
	/// <summary>
	/// a simple cubic interpolation resampler.  no lowpass.  original code
	/// </summary>
	public class CubicResampler : IStereoResampler
	{
		int[] data = new int[8];

		double mu;
		/// <summary>input rate / output rate</summary>
		double ratio;

		public CubicResampler()
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
					for (int i = 0; i < 6; i++)
						data[i] = data[i + 2];
					data[6] = input.Dequeue();
					data[7] = input.Dequeue();
				}
				if (mu >= 1.0)
					return;

				double mu2 = mu * mu;
				double mu3 = mu2 * mu2;

				int l0 = data[6] - data[4] - data[0] + data[2];
				int l1 = data[0] - data[2] - l0;
				int l2 = data[4] - data[0];
				int l3 = data[2];

				int r0 = data[7] - data[5] - data[1] + data[3];
				int r1 = data[1] - data[3] - r0;
				int r2 = data[5] - data[1];
				int r3 = data[3];

				double ls = l0 * mu3 + l1 * mu2 + l2 * mu + l3;
				double rs = r0 * mu3 + r1 * mu2 + r2 * mu + r3;

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
