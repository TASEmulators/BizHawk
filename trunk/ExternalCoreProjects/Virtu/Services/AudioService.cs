using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Jellyfish.Library;

namespace Jellyfish.Virtu.Services
{
	/// <summary>
	/// this isn't really a "service" anymore, just a helper for the speaker class
	/// </summary>
    public class AudioService
    {
		public AudioService() { }

        public void Output(int data) // machine thread
        {
			data = (int)(data * 0.2);
			if (pos < buff.Length - 2)
			{
				buff[pos++] = (short)data;
				buff[pos++] = (short)data;
			}
        }

		private short[] buff = new short[4096];
		private int pos = 0;

		public void Clear()
		{
			pos = 0;
		}

		public void GetSamples(out short[] samples, out int nsamp)
		{
			samples = buff;
			nsamp = pos / 2;
			pos = 0;
		}
    }
}
