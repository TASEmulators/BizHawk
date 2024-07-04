using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Components
{
	// Emulates PSG audio unit of a PC Engine / Turbografx-16 / SuperGrafx.
	// It is embedded on the CPU and doesn't have its own part number. None the less, it is emulated separately from the 6280 CPU.

	// Sound refactor TODO: IMixedSoundProvider must inherit ISoundProvider
	// TODo: this provides "fake" sync sound by hardcoding the number of samples
	public sealed class HuC6280PSG : ISoundProvider, IMixedSoundProvider
	{
		private readonly int _spf;
		public class PSGChannel
		{
			public ushort Frequency;
			public byte Panning;
			public byte Volume;
			public bool Enabled;
			public bool NoiseChannel;
			public bool DDA;
			public ushort NoiseFreq;
			public short DDAValue;
			public short[] Wave = new short[32];
			public float SampleOffset;
		}

		public PSGChannel[] Channels = new PSGChannel[8];

		public bool[] UserMute = new bool[8];

		public byte VoiceLatch;
		private byte WaveTableWriteOffset;

		private readonly Queue<QueuedCommand> commands = new Queue<QueuedCommand>(256);
		private long frameStartTime, frameStopTime;

		private const int SampleRate = 44100;
		private const int PsgBase = 3580000;
		private static readonly byte[] LogScale = { 0, 0, 10, 10, 13, 13, 16, 16, 20, 20, 26, 26, 32, 32, 40, 40, 51, 51, 64, 64, 81, 81, 102, 102, 128, 128, 161, 161, 203, 203, 255, 255 };
		private static readonly byte[] VolumeReductionTable = { 0x1F, 0x1D, 0x1B, 0x19, 0x17, 0x15, 0x13, 0x10, 0x0F, 0x0D, 0x0B, 0x09, 0x07, 0x05, 0x03, 0x00 };

		public byte MainVolumeLeft;
		public byte MainVolumeRight;
		public int MaxVolume { get; set; } = short.MaxValue;

		public HuC6280PSG(int spf)
		{
			_spf = spf;
			Waves.InitWaves();
			for (int i = 0; i < 8; i++)
			{
				Channels[i] = new PSGChannel();
			}
		}

		internal void BeginFrame(long cycles)
		{
			while (commands.Count > 0)
			{
				var cmd = commands.Dequeue();
				WritePSGImmediate(cmd.Register, cmd.Value);
			}

			frameStartTime = cycles;
		}

		internal void EndFrame(long cycles)
		{
			frameStopTime = cycles;
		}

		internal void WritePSG(byte register, byte value, long cycles)
		{
			commands.Enqueue(new QueuedCommand { Register = register, Value = value, Time = cycles - frameStartTime });
		}

		private void WritePSGImmediate(int register, byte value)
		{
			register &= 0x0F;
			switch (register)
			{
				case 0: // Set Voice Latch
					VoiceLatch = (byte)(value & 7);
					break;
				case 1: // Global Volume select;
					MainVolumeLeft = (byte)((value >> 4) & 0x0F);
					MainVolumeRight = (byte)(value & 0x0F);
					break;
				case 2: // Frequency LSB
					Channels[VoiceLatch].Frequency &= 0xFF00;
					Channels[VoiceLatch].Frequency |= value;
					break;
				case 3: // Frequency MSB
					Channels[VoiceLatch].Frequency &= 0x00FF;
					Channels[VoiceLatch].Frequency |= (ushort)(value << 8);
					Channels[VoiceLatch].Frequency &= 0x0FFF;
					break;
				case 4: // Voice Volume
					Channels[VoiceLatch].Volume = (byte)(value & 0x1F);
					Channels[VoiceLatch].Enabled = (value & 0x80) != 0;
					Channels[VoiceLatch].DDA = (value & 0x40) != 0;
					if (!Channels[VoiceLatch].Enabled && Channels[VoiceLatch].DDA)
					{
						//for the soudn debugger, this might be a useful indication that a new note has begun.. but not for sure
						WaveTableWriteOffset = 0;
					}
					break;
				case 5: // Panning
					Channels[VoiceLatch].Panning = value;
					break;
				case 6: // Wave data
					if (!Channels[VoiceLatch].DDA)
					{
						Channels[VoiceLatch].Wave[WaveTableWriteOffset++] = (short)((value * 2047) - 32767);
						WaveTableWriteOffset &= 31;
					}
					else
					{
						Channels[VoiceLatch].DDAValue = (short)((value * 2047) - 32767);
					}
					break;
				case 7: // Noise
					Channels[VoiceLatch].NoiseChannel = ((value & 0x80) != 0) && VoiceLatch >= 4;
					if ((value & 0x1F) == 0x1F)
						value &= 0xFE;
					Channels[VoiceLatch].NoiseFreq = (ushort)(PsgBase / (64 * (0x1F - (value & 0x1F))));
					break;
				case 8: // LFO
					// TODO: implement LFO
					break;
				case 9: // LFO Control
					if ((value & 0x80) == 0 && (value & 3) != 0)
					{
						Console.WriteLine("****************      LFO ON !!!!!!!!!!       *****************");
						Channels[1].Enabled = false;
					}
					else
					{
						Channels[1].Enabled = true;
					}
					break;
			}
		}

		public void DiscardSamples() { }

		public bool CanProvideAsync => true;

		public void SetSyncMode(SyncSoundMode mode) => SyncMode = mode;
		public SyncSoundMode SyncMode { get; private set; }

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			if (SyncMode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Must be in sync mode to call a sync method");
			}

			short[] ret = new short[_spf * 2];
			GetSamplesAsync(ret);
			samples = ret;
			nsamp = _spf;
		}

		public void GetSamplesAsync(short[] samples)
		{
			int elapsedCycles = (int)(frameStopTime - frameStartTime);
			int start = 0;
			while (commands.Count > 0)
			{
				var cmd = commands.Dequeue();
				int pos = (int)((cmd.Time * samples.Length) / elapsedCycles) & ~1;
				MixSamples(samples, start, pos - start);
				start = pos;
				WritePSGImmediate(cmd.Register, cmd.Value);
			}
			MixSamples(samples, start, samples.Length - start);
		}

		private void MixSamples(short[] samples, int start, int len)
		{
			for (int i = 0; i < 6; i++)
			{
				if (UserMute[i]) continue;
				MixChannel(samples, start, len, Channels[i]);
			}
		}

		private void MixChannel(short[] samples, int start, int len, PSGChannel channel)
		{
			if (!channel.Enabled) return;
			if (!channel.DDA && channel.Volume == 0) return;

			short[] wave = channel.Wave;
			int freq;

			if (channel.NoiseChannel)
			{
				wave = Waves.NoiseWave;
				freq = channel.NoiseFreq;
			}
			else if (channel.DDA)
			{
				freq = 0;
			}
			else
			{
				if (channel.Frequency <= 1) return;
				freq = PsgBase / (32 * channel.Frequency);
			}

			int globalPanFactorLeft = VolumeReductionTable[MainVolumeLeft];
			int globalPanFactorRight = VolumeReductionTable[MainVolumeRight];
			int channelPanFactorLeft = VolumeReductionTable[channel.Panning >> 4];
			int channelPanFactorRight = VolumeReductionTable[channel.Panning & 0xF];
			int channelVolumeFactor = 0x1f - channel.Volume;

			int volumeLeft = 0x1F - globalPanFactorLeft - channelPanFactorLeft - channelVolumeFactor;
			if (volumeLeft < 0)
				volumeLeft = 0;

			int volumeRight = 0x1F - globalPanFactorRight - channelPanFactorRight - channelVolumeFactor;
			if (volumeRight < 0)
				volumeRight = 0;

			float adjustedWaveLengthInSamples = SampleRate / (channel.NoiseChannel ? freq / (float)(channel.Wave.Length * 128) : freq);
			float moveThroughWaveRate = wave.Length / adjustedWaveLengthInSamples;

			int end = start + len;
			for (int i = start; i < end; )
			{
				channel.SampleOffset %= wave.Length;
				short value = channel.DDA ? channel.DDAValue : wave[(int)channel.SampleOffset];

				samples[i++] += (short)(value * LogScale[volumeLeft] / 255f / 6f * MaxVolume / short.MaxValue);
				samples[i++] += (short)(value * LogScale[volumeRight] / 255f / 6f * MaxVolume / short.MaxValue);

				channel.SampleOffset += moveThroughWaveRate;
				channel.SampleOffset %= wave.Length;
			}
		}

		internal void SyncState(Serializer ser)
		{
			ser.BeginSection("PSG");
			ser.Sync(nameof(MainVolumeLeft), ref MainVolumeLeft);
			ser.Sync(nameof(MainVolumeRight), ref MainVolumeRight);
			ser.Sync(nameof(VoiceLatch), ref VoiceLatch);
			ser.Sync(nameof(WaveTableWriteOffset), ref WaveTableWriteOffset);

			for (int i = 0; i < 6; i++)
			{
				ser.BeginSection("Channel"+i);
				ser.Sync(nameof(PSGChannel.Frequency), ref Channels[i].Frequency);
				ser.Sync(nameof(PSGChannel.Panning), ref Channels[i].Panning);
				ser.Sync(nameof(PSGChannel.Volume), ref Channels[i].Volume);
				ser.Sync(nameof(PSGChannel.Enabled), ref Channels[i].Enabled);
				if (i.In(4, 5))
				{
					ser.Sync(nameof(PSGChannel.NoiseChannel), ref Channels[i].NoiseChannel);
					ser.Sync(nameof(PSGChannel.NoiseFreq), ref Channels[i].NoiseFreq);
				}

				ser.Sync(nameof(PSGChannel.DDA), ref Channels[i].DDA);
				ser.Sync(nameof(PSGChannel.DDAValue), ref Channels[i].DDAValue);
				ser.Sync(nameof(PSGChannel.SampleOffset), ref Channels[i].SampleOffset);
				ser.Sync(nameof(PSGChannel.Wave), ref Channels[i].Wave, false);
				ser.EndSection();
			}

			ser.EndSection();
		}

		private class QueuedCommand
		{
			public byte Register;
			public byte Value;
			public long Time;
		}
	}
}
