using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Components
{
	public sealed class SN76489 : IMixedSoundProvider
	{
		public sealed class Channel
		{
			public ushort Frequency;
			public byte Volume;
			public short[] Wave;
			public bool Noise;
			public byte NoiseType;
			public float WaveOffset;
			public bool Left = true;
			public bool Right = true;

			const int SampleRate = 44100;
			private static readonly byte[] LogScale = { 0, 10, 13, 16, 20, 26, 32, 40, 51, 64, 81, 102, 128, 161, 203, 255 };

			public void Mix(short[] samples, int start, int len, int maxVolume)
			{
				if (Volume == 0) return;

				float adjustedWaveLengthInSamples = SampleRate / (Noise ? (Frequency / (float)Wave.Length) : Frequency);
				float moveThroughWaveRate = Wave.Length / adjustedWaveLengthInSamples;

				int end = start + len;
				for (int i = start; i < end; )
				{
					short value = Wave[(int)WaveOffset];

					samples[i++] += (short)(Left ? (value / 4 * LogScale[Volume] / 0xFF * maxVolume / short.MaxValue) : 0);
					samples[i++] += (short)(Right ? (value / 4 * LogScale[Volume] / 0xFF * maxVolume / short.MaxValue) : 0);
					WaveOffset += moveThroughWaveRate;
					if (WaveOffset >= Wave.Length)
						WaveOffset %= Wave.Length;
				}
			}
		}

		public Channel[] Channels = new Channel[4];
		public byte PsgLatch;

		private readonly Queue<QueuedCommand> commands = new Queue<QueuedCommand>(256);
		int frameStartTime, frameStopTime;

		const int PsgBase = 111861;

		public SN76489()
		{
			MaxVolume = short.MaxValue * 2 / 3;
			Waves.InitWaves();
			for (int i = 0; i < 4; i++)
			{
				Channels[i] = new Channel();
				switch (i)
				{
					case 0:
					case 1:
					case 2:
						Channels[i].Wave = Waves.ImperfectSquareWave;
						break;
					case 3:
						Channels[i].Wave = Waves.NoiseWave;
						Channels[i].Noise = true;
						break;
				}
			}
		}

		public void Reset()
		{
			PsgLatch = 0;
			foreach (var channel in Channels)
			{
				channel.Frequency = 0;
				channel.Volume = 0;
				channel.NoiseType = 0;
				channel.WaveOffset = 0f;
			}
		}

		public void BeginFrame(int cycles)
		{
			while (commands.Count > 0)
			{
				var cmd = commands.Dequeue();
				WritePsgDataImmediate(cmd.Value);
			}
			frameStartTime = cycles;
		}

		public void EndFrame(int cycles)
		{
			frameStopTime = cycles;
		}

		public void WritePsgData(byte value, int cycles)
		{
			commands.Enqueue(new QueuedCommand { Value = value, Time = cycles - frameStartTime });
		}

		void UpdateNoiseType(int value)
		{
			Channels[3].NoiseType = (byte)(value & 0x07);
			switch (Channels[3].NoiseType & 3)
			{
				case 0: Channels[3].Frequency = PsgBase / 16; break;
				case 1: Channels[3].Frequency = PsgBase / 32; break;
				case 2: Channels[3].Frequency = PsgBase / 64; break;
				case 3: Channels[3].Frequency = Channels[2].Frequency; break;
			}
			var newWave = (value & 4) == 0 ? Waves.PeriodicWave16 : Waves.NoiseWave;
			if (newWave != Channels[3].Wave)
			{
				Channels[3].Wave = newWave;
				Channels[3].WaveOffset = 0f;
			}
		}

		void WritePsgDataImmediate(byte value)
		{
			switch (value & 0xF0)
			{
				case 0x80:
				case 0xA0:
				case 0xC0:
					PsgLatch = value;
					break;
				case 0xE0:
					PsgLatch = value;
					UpdateNoiseType(value);
					break;
				case 0x90:
					Channels[0].Volume = (byte)(~value & 15);
					PsgLatch = value;
					break;
				case 0xB0:
					Channels[1].Volume = (byte)(~value & 15);
					PsgLatch = value;
					break;
				case 0xD0:
					Channels[2].Volume = (byte)(~value & 15);
					PsgLatch = value;
					break;
				case 0xF0:
					Channels[3].Volume = (byte)(~value & 15);
					PsgLatch = value;
					break;
				default:
					byte channel = (byte)((PsgLatch & 0x60) >> 5);
					if ((PsgLatch & 16) == 0) // Tone latched
					{
						int f = PsgBase / (((value & 0x03F) * 16) + (PsgLatch & 0x0F) + 1);
						if (f > 15000)
							f = 0; // upper bound of playable frequency
						Channels[channel].Frequency = (ushort)f;
						if ((Channels[3].NoiseType & 3) == 3 && channel == 2)
							Channels[3].Frequency = (ushort)f;
					}
					else
					{ // volume latched
						Channels[channel].Volume = (byte)(~value & 15);
					}
					break;
			}
		}

		byte stereoPanning = 0xFF;
		public byte StereoPanning
		{
			get
			{
				byte value = 0;
				if (Channels[0].Left)  value |= 0x10;
				if (Channels[0].Right) value |= 0x01;
				if (Channels[1].Left)  value |= 0x20;
				if (Channels[1].Right) value |= 0x02;
				if (Channels[2].Left)  value |= 0x40;
				if (Channels[2].Right) value |= 0x04;
				if (Channels[3].Left)  value |= 0x80;
				if (Channels[3].Right) value |= 0x08;
				return value;
			}
			set
			{
				Channels[0].Left  = (value & 0x10) != 0;
				Channels[0].Right = (value & 0x01) != 0;
				Channels[1].Left  = (value & 0x20) != 0;
				Channels[1].Right = (value & 0x02) != 0;
				Channels[2].Left  = (value & 0x40) != 0;
				Channels[2].Right = (value & 0x04) != 0;
				Channels[3].Left  = (value & 0x80) != 0;
				Channels[3].Right = (value & 0x08) != 0;
				stereoPanning = value;
			}
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection("PSG");
			ser.Sync("Volume0", ref Channels[0].Volume);
			ser.Sync("Volume1", ref Channels[1].Volume);
			ser.Sync("Volume2", ref Channels[2].Volume);
			ser.Sync("Volume3", ref Channels[3].Volume);
			ser.Sync("Freq0", ref Channels[0].Frequency);
			ser.Sync("Freq1", ref Channels[1].Frequency);
			ser.Sync("Freq2", ref Channels[2].Frequency);
			ser.Sync("Freq3", ref Channels[3].Frequency);
			ser.Sync("NoiseType", ref Channels[3].NoiseType);
			ser.Sync("PsgLatch", ref PsgLatch);
			ser.Sync("Panning", ref stereoPanning);
			ser.EndSection();

			if (ser.IsReader)
			{
				StereoPanning = stereoPanning;
				UpdateNoiseType(Channels[3].NoiseType);
			}
		}

		public int MaxVolume { get; set; }
		public void DiscardSamples() { commands.Clear(); }
		public void GetSamples(short[] samples)
		{
			int elapsedCycles = frameStopTime - frameStartTime;
			if (elapsedCycles == 0)
				elapsedCycles = 1; // hey it's better than diving by zero

			int start = 0;
			while (commands.Count > 0)
			{
				var cmd = commands.Dequeue();
				int pos = ((cmd.Time * samples.Length) / elapsedCycles) & ~1;
				GetSamplesImmediate(samples, start, pos - start);
				start = pos;
				WritePsgDataImmediate(cmd.Value);
			}
			GetSamplesImmediate(samples, start, samples.Length - start);
		}

		public void GetSamplesImmediate(short[] samples, int start, int len)
		{
			for (int i = 0; i < 4; i++)
				Channels[i].Mix(samples, start, len, MaxVolume);
		}

		class QueuedCommand
		{
			public byte Value;
			public int Time;
		}
	}
}