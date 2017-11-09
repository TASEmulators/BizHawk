using System;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	// Audio Emulation
	public class Audio : ISoundProvider
	{
		public GBHawk Core { get; set; }

		public const int NR10 = 0;
		public const int NR11 = 1;
		public const int NR12 = 2;
		public const int NR13 = 3;
		public const int NR14 = 4;
		public const int NR21 = 5;
		public const int NR22 = 6;
		public const int NR23 = 7;
		public const int NR24 = 8;
		public const int NR30 = 9;
		public const int NR31 = 10;
		public const int NR32 = 11;
		public const int NR33 = 12;
		public const int NR34 = 13;
		public const int NR41 = 14;
		public const int NR42 = 15;
		public const int NR43 = 16;
		public const int NR44 = 17;
		public const int NR50 = 18;
		public const int NR51 = 19;
		public const int NR52 = 20;

		public static int[] unused_bits = new int[] { 0x80, 0x3F, 0x00, 0xFF, 0xBF,
															0x3F, 0x00, 0xFF, 0xBF,
													  0x7F, 0xFF, 0x9F, 0xFF, 0xBF,
															0xFF, 0x00, 0x00, 0xBF,
													  0x00, 0x00, 0x70};

		public byte[] Audio_Regs = new byte[21];

		public byte[] Wave_RAM = new byte [16];


		public byte ReadReg(int addr)
		{
			byte ret = 0;

			switch (addr)
			{		
				case 0xFF10: ret = (byte)(Audio_Regs[NR10] | unused_bits[NR10]);		break; // NR10 (sweep)
				case 0xFF11: ret = (byte)(Audio_Regs[NR11] | unused_bits[NR11]);		break; // NR11 (sound length / wave pattern duty %)
				case 0xFF12: ret = (byte)(Audio_Regs[NR12] | unused_bits[NR12]);		break; // NR12 (envelope)
				case 0xFF13: ret = (byte)(Audio_Regs[NR13] | unused_bits[NR13]);		break; // NR13 (freq low)
				case 0xFF14: ret = (byte)(Audio_Regs[NR14] | unused_bits[NR14]);		break; // NR14 (freq hi)
				case 0xFF16: ret = (byte)(Audio_Regs[NR21] | unused_bits[NR21]);		break; // NR21 (sound length / wave pattern duty %)
				case 0xFF17: ret = (byte)(Audio_Regs[NR22] | unused_bits[NR22]);		break; // NR22 (envelope)
				case 0xFF18: ret = (byte)(Audio_Regs[NR23] | unused_bits[NR23]);		break; // NR23 (freq low)
				case 0xFF19: ret = (byte)(Audio_Regs[NR24] | unused_bits[NR24]);		break; // NR24 (freq hi)
				case 0xFF1A: ret = (byte)(Audio_Regs[NR30] | unused_bits[NR30]);		break; // NR30 (on/off)
				case 0xFF1B: ret = (byte)(Audio_Regs[NR31] | unused_bits[NR31]);		break; // NR31 (length)
				case 0xFF1C: ret = (byte)(Audio_Regs[NR32] | unused_bits[NR32]);		break; // NR32 (level output)
				case 0xFF1D: ret = (byte)(Audio_Regs[NR33] | unused_bits[NR33]);		break; // NR33 (freq low)
				case 0xFF1E: ret = (byte)(Audio_Regs[NR34] | unused_bits[NR34]);		break; // NR34 (freq hi)
				case 0xFF20: ret = (byte)(Audio_Regs[NR41] | unused_bits[NR41]);		break; // NR41 (sweep)
				case 0xFF21: ret = (byte)(Audio_Regs[NR42] | unused_bits[NR42]);		break; // NR42 (sweep)
				case 0xFF22: ret = (byte)(Audio_Regs[NR43] | unused_bits[NR43]);		break; // NR43 (sweep)
				case 0xFF23: ret = (byte)(Audio_Regs[NR44] | unused_bits[NR44]);		break; // NR44 (sweep)
				case 0xFF24: ret = (byte)(Audio_Regs[NR50] | unused_bits[NR50]);		break; // NR50 (sweep)
				case 0xFF25: ret = (byte)(Audio_Regs[NR51] | unused_bits[NR51]);		break; // NR51 (sweep)
				case 0xFF26: ret = (byte)(Audio_Regs[NR52] | unused_bits[NR52]);		break; // NR52 (sweep)

				// wave ram table
				case 0xFF30:
				case 0xFF31:
				case 0xFF32:
				case 0xFF33:
				case 0xFF34:
				case 0xFF35:
				case 0xFF36:
				case 0xFF37:
				case 0xFF38:
				case 0xFF39:
				case 0xFF3A:
				case 0xFF3B:
				case 0xFF3C:
				case 0xFF3D:
				case 0xFF3E:
				case 0xFF3F:
					ret = Wave_RAM[addr & 0x0F];
					break;

			}

			return ret;
		}

		public void WriteReg(int addr, byte value)
		{
			switch (addr)
			{
				case 0xFF10: Audio_Regs[NR10] = value;		break; // NR10 (sweep)
				case 0xFF11: Audio_Regs[NR11] = value;		break; // NR11 (sound length / wave pattern duty %)
				case 0xFF12: Audio_Regs[NR12] = value;		break; // NR12 (envelope)
				case 0xFF13: Audio_Regs[NR13] = value;		break; // NR13 (freq low)
				case 0xFF14: Audio_Regs[NR14] = value;		break; // NR14 (freq hi)
				case 0xFF16: Audio_Regs[NR21] = value;		break; // NR21 (sound length / wave pattern duty %)
				case 0xFF17: Audio_Regs[NR22] = value;		break; // NR22 (envelope)
				case 0xFF18: Audio_Regs[NR23] = value;		break; // NR23 (freq low)
				case 0xFF19: Audio_Regs[NR24] = value;		break; // NR24 (freq hi)
				case 0xFF1A: Audio_Regs[NR30] = value;		break; // NR30 (on/off)
				case 0xFF1B: Audio_Regs[NR31] = value;		break; // NR31 (length)
				case 0xFF1C: Audio_Regs[NR32] = value;		break; // NR32 (level output)
				case 0xFF1D: Audio_Regs[NR33] = value;		break; // NR33 (freq low)
				case 0xFF1E: Audio_Regs[NR34] = value;		break; // NR34 (freq hi)
				case 0xFF20: Audio_Regs[NR41] = value;		break; // NR41 (sweep)
				case 0xFF21: Audio_Regs[NR42] = value;		break; // NR42 (sweep)
				case 0xFF22: Audio_Regs[NR43] = value;		break; // NR43 (sweep)
				case 0xFF23: Audio_Regs[NR44] = value;		break; // NR44 (sweep)
				case 0xFF24: Audio_Regs[NR50] = value;		break; // NR50 (sweep)
				case 0xFF25: Audio_Regs[NR51] = value;		break; // NR51 (sweep)
				case 0xFF26: Audio_Regs[NR52] = value;		break; // NR52 (sweep)

				// wave ram table
				case 0xFF30:
				case 0xFF31:
				case 0xFF32:
				case 0xFF33:
				case 0xFF34:
				case 0xFF35:
				case 0xFF36:
				case 0xFF37:
				case 0xFF38:
				case 0xFF39:
				case 0xFF3A:
				case 0xFF3B:
				case 0xFF3C:
				case 0xFF3D:
				case 0xFF3E:
				case 0xFF3F:
					Wave_RAM[addr & 0x0F] = value;
					break;

			}
		}

		public void tick()
		{

		}

		public void reset()
		{
			Wave_RAM = new byte[16];

			Audio_Regs = new byte[21];
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync("Audio_Regs", ref Audio_Regs, false);
			ser.Sync("Wave_Ram", ref Wave_RAM, false);

		}

		#region audio

		public bool CanProvideAsync => false;

		public int _spf;
		public int AudioClocks;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Only Sync mode is supported.");
			}
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			short[] ret = new short[_spf * 2];
			nsamp = _spf;
			GetSamples(ret);
			samples = ret;
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async is not available");
		}

		public void DiscardSamples()
		{
			AudioClocks = 0;
		}

		// Exposing this as GetSamplesAsync would allow this to provide async sound
		// However, it does nothing special for async sound so I don't see a point
		private void GetSamples(short[] samples)
		{

		}

		#endregion
	}
}