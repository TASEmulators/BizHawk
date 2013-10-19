using System;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	sealed public partial class Sid
	{
		// ------------------------------------


		// ------------------------------------

		public Sound.Utilities.SpeexResampler resampler;

		static int[] syncNextTable = new int[] { 1, 2, 0 };
		static int[] syncPrevTable = new int[] { 2, 0, 1 };

        int cachedCycles;
		bool disableVoice3;
		int[] envelopeOutput;
		Envelope[] envelopes;
		bool[] filterEnable;
		int filterFrequency;
		int filterResonance;
		bool filterSelectBandPass;
		bool filterSelectLoPass;
		bool filterSelectHiPass;
        int mixer;
        int potCounter;
		int potX;
        int potY;
        short sample;
        int[] voiceOutput;
		Voice[] voices;
		int volume;
		int[][] waveformTable;

		public Func<byte> ReadPotX;
		public Func<byte> ReadPotY;

		public Sid(int[][] newWaveformTable, int newSampleRate, Region newRegion)
		{
			uint cyclesPerSec = 0;
            uint cyclesNum;
            uint cyclesDen;
            uint sampleRate = 44100;

			switch (newRegion)
			{
                case Region.NTSC: cyclesNum = 14318181; cyclesDen = 14; break;
				case Region.PAL: cyclesNum = 17734472; cyclesDen = 18; break;
                default: return;
			}

			waveformTable = newWaveformTable;

			envelopes = new Envelope[3];
			for (int i = 0; i < 3; i++)
				envelopes[i] = new Envelope();
			envelopeOutput = new int[3];

			voices = new Voice[3];
			for (int i = 0; i < 3; i++)
				voices[i] = new Voice(newWaveformTable);
			voiceOutput = new int[3];

			filterEnable = new bool[3];
			for (int i = 0; i < 3; i++)
				filterEnable[i] = false;

            resampler = new Sound.Utilities.SpeexResampler(0, cyclesNum, sampleRate * cyclesDen, cyclesNum, sampleRate * cyclesDen, null, null);
		}

		public void Dispose()
		{
			if (resampler != null)
			{
				resampler.Dispose();
				resampler = null;
			}
		}

		// ------------------------------------

		public void HardReset()
		{
			for (int i = 0; i < 3; i++)
			{
				envelopes[i].HardReset();
				voices[i].HardReset();
			}
			potCounter = 0;
			potX = 0;
			potY = 0;
		}

		// ------------------------------------

        public void ExecutePhase2()
		{
            cachedCycles++;

            // potentiometer values refresh every 512 cycles
            if (potCounter == 0)
            {
                potCounter = 512;
                potX = ReadPotX();
                potY = ReadPotY();
                Flush(); //this is here unrelated to the pots, just to keep the buffer somewhat loaded
            }
            potCounter--;
        }

        public void Flush()
        {
            while (cachedCycles > 0)
            {
                // process voices and envelopes
                voices[0].ExecutePhase2();
                voices[1].ExecutePhase2();
                voices[2].ExecutePhase2();
                envelopes[0].ExecutePhase2();
                envelopes[1].ExecutePhase2();
                envelopes[2].ExecutePhase2();

                // process sync
                for (int i = 0; i < 3; i++)
                    voices[i].Synchronize(voices[syncNextTable[i]], voices[syncPrevTable[i]]);

                // get output
                voiceOutput[0] = voices[0].Output(voices[2]);
                voiceOutput[1] = voices[1].Output(voices[0]);
                voiceOutput[2] = voices[2].Output(voices[1]);
                envelopeOutput[0] = envelopes[0].Level;
                envelopeOutput[1] = envelopes[1].Level;
                envelopeOutput[2] = envelopes[2].Level;

                mixer = ((voiceOutput[0] * envelopeOutput[0]) >> 7);
                mixer += ((voiceOutput[1] * envelopeOutput[1]) >> 7);
                mixer += ((voiceOutput[2] * envelopeOutput[2]) >> 7);
                mixer = (mixer * volume) >> 4;

                sample = (short)mixer;
                resampler.EnqueueSample(sample, sample);
                cachedCycles--;
            }
        }

		// ------------------------------------

		public byte Peek(int addr)
		{
			return ReadRegister((addr & 0x1F));
		}

		public void Poke(int addr, byte val)
		{
			WriteRegister((addr & 0x1F), val);
		}

		public byte Read(int addr)
		{
			addr &= 0x1F;
			byte result = 0x00;
			switch (addr)
			{
				case 0x19:
				case 0x1A:
				case 0x1B:
				case 0x1C:
                    Flush();
					result = ReadRegister(addr);
					break;
			}
			return result;
		}

		private byte ReadRegister(int addr)
		{
			byte result = 0x00;

			switch (addr)
			{
				case 0x00: result = (byte)voices[0].FrequencyLo; break;
				case 0x01: result = (byte)voices[0].FrequencyHi; break;
				case 0x02: result = (byte)voices[0].PulseWidthLo; break;
				case 0x03: result = (byte)voices[0].PulseWidthHi; break;
				case 0x04:
					result = (byte)(
						(envelopes[0].Gate ? 0x01 : 0x00) |
						(voices[0].Sync ? 0x02 : 0x00) |
						(voices[0].RingMod ? 0x04 : 0x00) |
						(voices[0].Test ? 0x08 : 0x00) |
						(byte)(voices[0].Waveform << 4)
						);
					break;
				case 0x05:
					result = (byte)(
						(envelopes[0].Attack << 4) |
						(envelopes[0].Decay)
						);
					break;
				case 0x06: 
					result = (byte)(
						(envelopes[0].Sustain << 4) |
						(envelopes[0].Release)
						);
					break;
				case 0x07: result = (byte)voices[1].FrequencyLo; break;
				case 0x08: result = (byte)voices[1].FrequencyHi; break;
				case 0x09: result = (byte)voices[1].PulseWidthLo; break;
				case 0x0A: result = (byte)voices[1].PulseWidthHi; break;
				case 0x0B:
					result = (byte)(
						(envelopes[1].Gate ? 0x01 : 0x00) |
						(voices[1].Sync ? 0x02 : 0x00) |
						(voices[1].RingMod ? 0x04 : 0x00) |
						(voices[1].Test ? 0x08 : 0x00) |
						(byte)(voices[1].Waveform << 4)
						);
					break;
				case 0x0C:
					result = (byte)(
						(envelopes[1].Attack << 4) |
						(envelopes[1].Decay)
						);
					break;
				case 0x0D:
					result = (byte)(
						(envelopes[1].Sustain << 4) |
						(envelopes[1].Release)
						);
					break;
				case 0x0E: result = (byte)voices[2].FrequencyLo; break;
				case 0x0F: result = (byte)voices[2].FrequencyHi; break;
				case 0x10: result = (byte)voices[2].PulseWidthLo; break;
				case 0x11: result = (byte)voices[2].PulseWidthHi; break;
				case 0x12:
					result = (byte)(
						(envelopes[2].Gate ? 0x01 : 0x00) |
						(voices[2].Sync ? 0x02 : 0x00) |
						(voices[2].RingMod ? 0x04 : 0x00) |
						(voices[2].Test ? 0x08 : 0x00) |
						(byte)(voices[2].Waveform << 4)
						);
					break;
				case 0x13:
					result = (byte)(
						(envelopes[2].Attack << 4) |
						(envelopes[2].Decay)
						);
					break;
				case 0x14:
					result = (byte)(
						(envelopes[2].Sustain << 4) |
						(envelopes[2].Release)
						);
					break;
				case 0x15: result = (byte)(filterFrequency & 0x7); break;
				case 0x16: result = (byte)((filterFrequency >> 3) & 0xFF); break;
				case 0x17:
					result = (byte)(
						(filterEnable[0] ? 0x01 : 0x00) |
						(filterEnable[1] ? 0x02 : 0x00) |
						(filterEnable[2] ? 0x04 : 0x00) |
						(byte)(filterResonance << 4)
						);
					break;
				case 0x18:
					result = (byte)(
						(byte)volume |
						(filterSelectLoPass ? 0x10 : 0x00) |
						(filterSelectBandPass ? 0x20 : 0x00) |
						(filterSelectHiPass ? 0x40 : 0x00) |
						(disableVoice3 ? 0x80 : 0x00)
						);
					break;
				case 0x19: result = (byte)potX; break;
				case 0x1A: result = (byte)potY;	break;
				case 0x1B: result = (byte)(voiceOutput[2] >> 4); break;
				case 0x1C: result = (byte)(envelopeOutput[2]); break;
			}

			return result;
		}

		public void Write(int addr, byte val)
		{
			addr &= 0x1F;
			switch (addr)
			{
				case 0x19:
				case 0x1A:
				case 0x1B:
				case 0x1C:
				case 0x1D:
				case 0x1E:
				case 0x1F:
					// can't write to these
					break;
				default:
                    Flush();
					WriteRegister(addr, val);
					break;
			}
		}

		private void WriteRegister(int addr, byte val)
		{
			switch (addr)
			{
				case 0x00: voices[0].FrequencyLo = val; break;
				case 0x01: voices[0].FrequencyHi = val; break;
				case 0x02: voices[0].PulseWidthLo = val; break;
				case 0x03: voices[0].PulseWidthHi = val; break;
				case 0x04: voices[0].Control = val; envelopes[0].Gate = ((val & 0x01) != 0); break;
				case 0x05: envelopes[0].Attack = (val >> 4); envelopes[0].Decay = (val & 0xF); break;
				case 0x06: envelopes[0].Sustain = (val >> 4); envelopes[0].Release = (val & 0xF); break;
				case 0x07: voices[1].FrequencyLo = val; break;
				case 0x08: voices[1].FrequencyHi = val; break;
				case 0x09: voices[1].PulseWidthLo = val; break;
				case 0x0A: voices[1].PulseWidthHi = val; break;
				case 0x0B: voices[1].Control = val; envelopes[1].Gate = ((val & 0x01) != 0); break;
				case 0x0C: envelopes[1].Attack = (val >> 4); envelopes[1].Decay = (val & 0xF); break;
				case 0x0D: envelopes[1].Sustain = (val >> 4); envelopes[1].Release = (val & 0xF); break;
				case 0x0E: voices[2].FrequencyLo = val; break;
				case 0x0F: voices[2].FrequencyHi = val; break;
				case 0x10: voices[2].PulseWidthLo = val; break;
				case 0x11: voices[2].PulseWidthHi = val; break;
				case 0x12: voices[2].Control = val; envelopes[2].Gate = ((val & 0x01) != 0); break;
				case 0x13: envelopes[2].Attack = (val >> 4); envelopes[2].Decay = (val & 0xF); break;
				case 0x14: envelopes[2].Sustain = (val >> 4); envelopes[2].Release = (val & 0xF); break;
				case 0x15: filterFrequency &= 0x3FF; filterFrequency |= (val & 0x7); break;
				case 0x16: filterFrequency &= 0x7; filterFrequency |= val << 3; break;
				case 0x17:
					filterEnable[0] = ((val & 0x1) != 0);
					filterEnable[1] = ((val & 0x2) != 0);
					filterEnable[2] = ((val & 0x4) != 0);
					filterResonance = val >> 4;
					break;
				case 0x18:
					volume = (val & 0xF);
					filterSelectLoPass = ((val & 0x10) != 0);
					filterSelectBandPass = ((val & 0x20) != 0);
					filterSelectHiPass = ((val & 0x40) != 0);
					disableVoice3 = ((val & 0x40) != 0);
					break;
				case 0x19:
					potX = val;
					break;
				case 0x1A:
					potY = val;
					break;
			}
		}

		// ----------------------------------

        public void SyncState(Serializer ser)
		{
            SaveState.SyncObject(ser, this);
            ser.BeginSection("env0");
			envelopes[0].SyncState(ser);
			ser.EndSection();
			ser.BeginSection("wav0");
			voices[0].SyncState(ser);
			ser.EndSection();
			ser.BeginSection("env1");
			envelopes[1].SyncState(ser);
			ser.EndSection();
			ser.BeginSection("wav1");
			voices[1].SyncState(ser);
			ser.EndSection();
			ser.BeginSection("env2");
			envelopes[2].SyncState(ser);
			ser.EndSection();
			ser.BeginSection("wav2");
			voices[2].SyncState(ser);
			ser.EndSection();
		}
	}
}
