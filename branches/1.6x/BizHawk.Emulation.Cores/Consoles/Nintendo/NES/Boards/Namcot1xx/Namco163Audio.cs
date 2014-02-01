using System;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	// http://wiki.nesdev.com/w/index.php/Namco_163_audio
	public sealed class Namco163Audio
	{
		byte[] ram = new byte[0x80];
		int addr;
		bool autoincrement;

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
		int latchout = 0;

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
			int s = samp * 150;
			int delta = latchout - s;
			latchout = s;
			enqueuer(delta);
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

		Action<int> enqueuer;

		public Namco163Audio(Action<int> enqueuer)
		{
			this.enqueuer = enqueuer;
		}

		// the sound ram can be uesd for arbitrary load\store of data,
		// and can be batteryed, and some games actually did this
		public byte[] GetSaveRam()
		{
			return ram;
		}
	}
}
