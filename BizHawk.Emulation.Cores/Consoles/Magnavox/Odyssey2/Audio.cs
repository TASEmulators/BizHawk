using System;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.O2Hawk
{
	// Audio Emulation (a 24 bit shift register plus a control register)
	public class Audio : ISoundProvider
	{
		public O2Hawk Core { get; set; }

		private BlipBuffer _blip_C = new BlipBuffer(15000);

		public byte sample;

		public byte shift_0, shift_1, shift_2, aud_ctrl;

		public uint master_audio_clock;

		public int tick_cnt, output_bit;

		public int latched_sample_C;

		public byte ReadReg(int addr)
		{
			byte ret = 0;

			switch (addr)
			{
				case 0xA7: ret = shift_0; break;
				case 0xA8: ret = shift_1; break;
				case 0xA9: ret = shift_2; break;
				case 0xAA: ret = aud_ctrl; break;
			}

			return ret;
		}

		public void WriteReg(int addr, byte value)
		{
			switch (addr)
			{
				case 0xA7: shift_0 = value; break;
				case 0xA8: shift_1 = value; break;
				case 0xA9: shift_2 = value; break;
				case 0xAA: aud_ctrl = value; break;
			}

		}

		public void tick()
		{
			int C_final = 0;

			if (aud_ctrl.Bit(7))
			{
				tick_cnt++;
				if (tick_cnt > (aud_ctrl.Bit(5) ? 455 : 1820))
				{
					tick_cnt = 0;

					output_bit = (shift_0 >> 1) & 1;

					shift_0 = (byte)((shift_0 >> 1) | ((shift_1 & 1) << 3));
					shift_1 = (byte)((shift_1 >> 1) | ((shift_2 & 1) << 3));

					if (aud_ctrl.Bit(6))
					{
						shift_2 = (byte)((shift_2 >> 1) | ((output_bit) << 3));
					}
					else
					{
						shift_0 = (byte)(shift_2 >> 1);
					}
				}

				C_final = output_bit;
				C_final *= ((aud_ctrl & 0xF) + 1) * 40;
			}

			if (C_final != latched_sample_C)
			{
			_blip_C.AddDelta(master_audio_clock, C_final - latched_sample_C);
			latched_sample_C = C_final;
			}

			master_audio_clock++;
		}

		public void power_off()
		{
			for (int i = 0; i < 0x16; i++)
			{
				WriteReg(0xFF10 + i, 0);
			}
		}

		public void Reset()
		{
			master_audio_clock = 0;

			sample = 0;

			_blip_C.SetRates(4194304, 44100);
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(master_audio_clock), ref master_audio_clock);

			ser.Sync(nameof(sample), ref sample);
			ser.Sync(nameof(latched_sample_C), ref latched_sample_C);

			ser.Sync(nameof(aud_ctrl), ref aud_ctrl);
			ser.Sync(nameof(shift_0), ref shift_0);
			ser.Sync(nameof(shift_1), ref shift_1);
			ser.Sync(nameof(shift_2), ref shift_2);
			ser.Sync(nameof(tick_cnt), ref tick_cnt);
			ser.Sync(nameof(output_bit), ref output_bit);
		}

		#region audio

		public bool CanProvideAsync => false;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Only Sync mode is supported_");
			}
		}

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			_blip_C.EndFrame(master_audio_clock);

			nsamp = _blip_C.SamplesAvailable();

			samples = new short[nsamp * 2];

			if (nsamp != 0)
			{
				_blip_C.ReadSamples(samples, nsamp, false);
			}

			master_audio_clock = 0;
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async is not available");
		}

		public void DiscardSamples()
		{
			_blip_C.Clear();
			master_audio_clock = 0;
		}

		private void GetSamples(short[] samples)
		{

		}

		public void DisposeSound()
		{
			_blip_C.Clear();
			_blip_C.Dispose();
			_blip_C = null;
		}

		#endregion
	}
}