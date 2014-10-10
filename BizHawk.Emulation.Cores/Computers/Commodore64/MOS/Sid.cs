using System;

using BizHawk.Common;
using BizHawk.Emulation.Common;

#pragma warning disable 649 //adelikat: Disable dumb warnings until this file is complete
#pragma warning disable 169 //adelikat: Disable dumb warnings until this file is complete
#pragma warning disable 219 //adelikat: Disable dumb warnings until this file is complete

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	sealed public partial class Sid
	{

		// ------------------------------------

		public SpeexResampler resampler;

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

			resampler = new SpeexResampler(0, cyclesNum, sampleRate * cyclesDen, cyclesNum, sampleRate * cyclesDen, null, null);
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
