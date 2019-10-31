using System;

using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.O2Hawk
{
	// Audio Emulation
	public class Audio : ISoundProvider
	{
		public O2Hawk Core { get; set; }

		private BlipBuffer _blip_L = new BlipBuffer(15000);
		private BlipBuffer _blip_R = new BlipBuffer(15000);

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


		public byte[] Audio_Regs = new byte[21];

		// Contol Variables
		public bool AUD_CTRL_vin_L_en;
		public bool AUD_CTRL_vin_R_en;
		public bool AUD_CTRL_sq1_L_en;
		public bool AUD_CTRL_sq2_L_en;
		public bool AUD_CTRL_wave_L_en;
		public bool AUD_CTRL_noise_L_en;
		public bool AUD_CTRL_sq1_R_en;
		public bool AUD_CTRL_sq2_R_en;
		public bool AUD_CTRL_wave_R_en;
		public bool AUD_CTRL_noise_R_en;
		public bool AUD_CTRL_power;
		public byte AUD_CTRL_vol_L;
		public byte AUD_CTRL_vol_R;

		public byte sample;

		public uint master_audio_clock;

		public int latched_sample_L, latched_sample_R;

		public byte ReadReg(int addr)
		{
			byte ret = 0;

			switch (addr)
			{
				case 0xFF10: ret = (byte)(Audio_Regs[NR10]); break; // NR10 (sweep)
				case 0xFF11: ret = (byte)(Audio_Regs[NR11]); break; // NR11 (sound length / wave pattern duty %)
				case 0xFF12: ret = (byte)(Audio_Regs[NR12]); break; // NR12 (envelope)
				case 0xFF13: ret = (byte)(Audio_Regs[NR13]); break; // NR13 (freq low)
			}

			return ret;
		}

		public void WriteReg(int addr, byte value)
		{

		}

		public void tick()
		{

			// add up components to each channel
			int L_final = 0;
			int R_final = 0;

			if (AUD_CTRL_sq1_L_en) { L_final += 0; }


			if (AUD_CTRL_sq1_R_en) { R_final += 0; }

			L_final *= (AUD_CTRL_vol_L + 1) * 40;
			R_final *= (AUD_CTRL_vol_R + 1) * 40;

			if (L_final != latched_sample_L)
			{
			_blip_L.AddDelta(master_audio_clock, L_final - latched_sample_L);
			latched_sample_L = L_final;
			}

			if (R_final != latched_sample_R)
			{
			_blip_R.AddDelta(master_audio_clock, R_final - latched_sample_R);
			latched_sample_R = R_final;
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
			Audio_Regs = new byte[21];

			master_audio_clock = 0;

			sample = 0;

			_blip_L.SetRates(4194304, 44100);
			_blip_R.SetRates(4194304, 44100);
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(Audio_Regs), ref Audio_Regs, false);

			ser.Sync(nameof(master_audio_clock), ref master_audio_clock);

			ser.Sync(nameof(sample), ref sample);
			ser.Sync(nameof(latched_sample_L), ref latched_sample_L);
			ser.Sync(nameof(latched_sample_R), ref latched_sample_R);

			ser.Sync(nameof(AUD_CTRL_vin_L_en), ref AUD_CTRL_vin_L_en);
			ser.Sync(nameof(AUD_CTRL_vin_R_en), ref AUD_CTRL_vin_R_en);
			ser.Sync(nameof(AUD_CTRL_sq1_L_en), ref AUD_CTRL_sq1_L_en);
			ser.Sync(nameof(AUD_CTRL_sq2_L_en), ref AUD_CTRL_sq2_L_en);
			ser.Sync(nameof(AUD_CTRL_wave_L_en), ref AUD_CTRL_wave_L_en);
			ser.Sync(nameof(AUD_CTRL_noise_L_en), ref AUD_CTRL_noise_L_en);
			ser.Sync(nameof(AUD_CTRL_sq1_R_en), ref AUD_CTRL_sq1_R_en);
			ser.Sync(nameof(AUD_CTRL_sq2_R_en), ref AUD_CTRL_sq2_R_en);
			ser.Sync(nameof(AUD_CTRL_wave_R_en), ref AUD_CTRL_wave_R_en);
			ser.Sync(nameof(AUD_CTRL_noise_R_en), ref AUD_CTRL_noise_R_en);
			ser.Sync(nameof(AUD_CTRL_power), ref AUD_CTRL_power);
			ser.Sync(nameof(AUD_CTRL_vol_L), ref AUD_CTRL_vol_L);
			ser.Sync(nameof(AUD_CTRL_vol_R), ref AUD_CTRL_vol_R);
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
			_blip_L.EndFrame(master_audio_clock);
			_blip_R.EndFrame(master_audio_clock);

			nsamp = _blip_L.SamplesAvailable();

			samples = new short[nsamp * 2];

			if (nsamp != 0)
			{
				_blip_L.ReadSamplesLeft(samples, nsamp);
				_blip_R.ReadSamplesRight(samples, nsamp);
			}

			master_audio_clock = 0;
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async is not available");
		}

		public void DiscardSamples()
		{
			_blip_L.Clear();
			_blip_R.Clear();
			master_audio_clock = 0;
		}

		private void GetSamples(short[] samples)
		{

		}

		public void DisposeSound()
		{
			_blip_L.Clear();
			_blip_R.Clear();
			_blip_L.Dispose();
			_blip_R.Dispose();
			_blip_L = null;
			_blip_R = null;
		}

		#endregion
	}
}