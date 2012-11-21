using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class Sid : ISoundProvider
	{
		public void SyncState(Serializer ser)
		{
			// voices
			for (int i = 0; i < 3; i++)
			{
				string iTag = i.ToString();
				ser.Sync("GENACCUM" + iTag, ref regs.Voices[i].Generator.accumulator);
				ser.Sync("GENFOTTL" + iTag, ref regs.Voices[i].Generator.floatingOutputTtl);
				ser.Sync("GENMSBRISING" + iTag, ref regs.Voices[i].Generator.msbRising);
				ser.Sync("GENNOISEOUT" + iTag, ref regs.Voices[i].Generator.noiseOutput);
				ser.Sync("GENPULSEOUT" + iTag, ref regs.Voices[i].Generator.pulseOutput);
				ser.Sync("GENSR" + iTag, ref regs.Voices[i].Generator.shiftRegister);
				ser.Sync("GENSRDELAY" + iTag, ref regs.Voices[i].Generator.shiftRegisterDelay);
				ser.Sync("GENSRRESETDELAY" + iTag, ref regs.Voices[i].Generator.shiftRegisterResetDelay);
				ser.Sync("GENWAVEFORMOUT" + iTag, ref regs.Voices[i].Generator.waveformOutput);

				ser.Sync("ENVCOUNTER" + iTag, ref regs.Voices[i].Envelope.envelopeCounter);
				ser.Sync("ENVENABLE" + iTag, ref regs.Voices[i].Envelope.envelopeProcessEnabled);
				ser.Sync("ENVEXPCOUNTER" + iTag, ref regs.Voices[i].Envelope.exponentialCounter);
				ser.Sync("ENVEXPCOUNTERPERIOD" + iTag, ref regs.Voices[i].Envelope.exponentialCounterPeriod);
				ser.Sync("ENVFREEZE" + iTag, ref regs.Voices[i].Envelope.freeze);
				ser.Sync("ENVLFSR" + iTag, ref regs.Voices[i].Envelope.lfsr);
				ser.Sync("ENVRATE" + iTag, ref regs.Voices[i].Envelope.rate);

				byte control = regs.Voices[i].Generator.Control;
				int freq = regs.Voices[i].Generator.Frequency;
				int pw = regs.Voices[i].Generator.PulseWidth;
				int attack = regs.Voices[i].Envelope.Attack;
				int decay = regs.Voices[i].Envelope.Decay;
				int sustain = regs.Voices[i].Envelope.Sustain;
				int release = regs.Voices[i].Envelope.Release;
				bool gate = regs.Voices[i].Envelope.Gate;
				int state = (int)regs.Voices[i].Envelope.state;

				ser.Sync("GENCONTROL" + iTag, ref control);
				ser.Sync("GENFREQ" + iTag, ref freq);
				ser.Sync("GENPW" + iTag, ref pw);
				ser.Sync("ENVATTACK" + iTag, ref attack);
				ser.Sync("ENVDECAY" + iTag, ref decay);
				ser.Sync("ENVSUSTAIN" + iTag, ref sustain);
				ser.Sync("ENVRELEASE" + iTag, ref release);
				ser.Sync("ENVGATE" + iTag, ref gate);
				ser.Sync("ENVSTATE" + iTag, ref state);

				if (ser.IsReader)
				{
					regs.Voices[i].Generator.SetState(control, freq, pw);
					regs.Voices[i].Envelope.SetState(attack, decay, sustain, release, gate, (EnvelopeGenerator.EnvelopeState)state);
				}
			}

			// regs
			ser.Sync("BP", ref regs.BP);
			ser.Sync("D3", ref regs.D3);
			ser.Sync("FC", ref regs.FC);
			ser.Sync("FILT0", ref regs.FILT[0]);
			ser.Sync("FILT1", ref regs.FILT[1]);
			ser.Sync("FILT2", ref regs.FILT[2]);
			ser.Sync("FILTEX", ref regs.FILTEX);
			ser.Sync("HP", ref regs.HP);
			ser.Sync("LP", ref regs.LP);
			ser.Sync("POTX", ref regs.POTX);
			ser.Sync("POTY", ref regs.POTY);
			ser.Sync("RES", ref regs.RES);
			ser.Sync("VOL", ref regs.VOL);

			// vars
			ser.Sync("CLOCK", ref clock);
			ser.Sync("CYCLESPERSAMPLE", ref cyclesPerSample);
			ser.Sync("OUTPUT", ref output);
		}
	}
}
