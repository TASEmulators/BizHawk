using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	// http://wiki.nesdev.com/w/index.php/Namco_163_audio
	public class Namco163Audio : IDisposable
	{
		//ByteBuffer ram = new ByteBuffer(0x80);
		byte[] ram = new byte[0x80];
		int addr;
		bool autoincrement;

		public void Dispose()
		{
			//ram.Dispose();
			ram = null;
			resampler.Dispose();
			resampler = null;
		}

		/// <summary>
		/// F800:FFFF
		/// </summary>
		/// <param name="value"></param>
		public void WriteAddr(byte value)
		{
			addr = value & 0x7f;
			autoincrement = (value & 0x80) != 0;
		}

		/// <summary>
		/// 4800:4FFF
		/// </summary>
		/// <param name="value"></param>
		public void WriteData(byte value)
		{
			ram[addr] = value;
			if (autoincrement)
			{
				addr++;
				addr &= 0x7f;
			}
		}

		/// <summary>
		/// 4800:4FFF
		/// </summary>
		/// <returns></returns>
		public byte ReadData()
		{
			byte ret = ram[addr];
			if (autoincrement)
			{
				addr++;
				addr &= 0x7f;
			}
			return ret;
		}

		/// <summary>
		/// last channel clocked
		/// </summary>
		int ch;

		// output buffer; not savestated
		//short[] samplebuff = new short[2048];
		//int samplebuffpos;

		/// <summary>
		/// 119318hz (CPU / 15)
		/// </summary>
		public void Clock()
		{
			ch--;
			int lastch = 8 - (ram[0x7f] >> 4 & 7);
			if (ch < lastch)
				ch = 8;

			byte samp = ClockChannel(ch);

			//samplebuff[samplebuffpos++] = samp;
			//samplebuffpos &= 2047;
			short ss = (short)(samp * 150 - 18000);
			resampler.EnqueueSample(ss, ss);
		}

		byte ClockChannel(int ch)
		{
			// channel regs are at [b..b+7]
			int b = ch * 8 + 56;

			// not that bad really, and best to do exactly as specified because
			// the results (phase) can be read back
			int phase = ram[b + 1] | ram[b + 3] << 8 | ram[b + 5] << 16;
			int freq = ram[b] | ram[b + 2] << 8 | ram[b + 4] << 16 & 0x030000;
			int length = 256 - (ram[b + 4] & 0xfc);
			phase = (phase + freq) % (length << 16);

			int pos = (phase >> 16) + ram[b + 6];
			pos &= 0xff;
			int sample = ram[pos / 2] >> (pos & 1) * 4 & 0xf;
			byte ret = (byte)(sample * (ram[b + 7] & 0x0f));
			// writeback phase
			ram[b + 5] = (byte)(phase >> 16);
			ram[b + 3] = (byte)(phase >> 8);
			ram[b + 1] = (byte)phase;
			return ret;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync("ram", ref ram, false);
			ser.Sync("addr", ref addr);
			ser.Sync("autoincrement", ref autoincrement);
			ser.Sync("ch", ref ch);
		}

		Sound.Utilities.SpeexResampler resampler; 
		Sound.Utilities.DCFilter dc;
		Sound.MetaspuAsync metaspu;

		public Namco163Audio()
		{
			resampler = new Sound.Utilities.SpeexResampler(2, 119318, 44100, 119318, 44100, null, null);
			dc = Sound.Utilities.DCFilter.DetatchedMode(4096);
			metaspu = new Sound.MetaspuAsync(resampler, Sound.ESynchMethod.ESynchMethod_V);
		}

		public void ApplyCustomAudio(short[] samples)
		{
			short[] tmp = new short[samples.Length];
			metaspu.GetSamples(tmp);
			for (int i = 0; i < samples.Length; i++)
			{
				int samp = samples[i] + tmp[i];
				if (samp > 32767)
					samples[i] = 32767;
				else if (samp < -32768)
					samples[i] = -32768;
				else
					samples[i] = (short)samp;
			}
			dc.PushThroughSamples(samples, samples.Length);
		}

		// the same junk used in FDSAudio
		// the problem here is, the raw 120khz output contains significant amounts of crap that gets
		// massively garbaged up by this resampling
		/*
		public void ApplyCustomAudio(short[] samples)
		{
			for (int i = 0; i < samples.Length; i += 2)
			{
				// worst imaginable resampling
				int pos = i * samplebuffpos / samples.Length;
				int samp = samplebuff[pos] * 50 - 12096;
				samp += samples[i];
				if (samp > 32767)
					samples[i] = 32767;
				else if (samp < -32768)
					samples[i] = -32768;
				else
					samples[i] = (short)samp;

				// NES audio is mono, so this should be identical anyway
				samples[i + 1] = samples[i];
			}
			samplebuffpos = 0;

			dc.PushThroughSamples(samples, samples.Length);
		}
		*/


	}
}
