using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Sid
	{
		public int Peek(int addr)
		{
			return ReadRegister(addr & 0x1F);
		}

		public void Poke(int addr, int val)
		{
			WriteRegister(addr & 0x1F, val);
		}

		public int Read(int addr)
		{
			addr &= 0x1F;
            var result = 0x00;
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

		private int ReadRegister(int addr)
		{
            var result = 0x00;

			switch (addr)
			{
				case 0x00: result = _voice0.FrequencyLo; break;
				case 0x01: result = _voice0.FrequencyHi; break;
				case 0x02: result = _voice0.PulseWidthLo; break;
				case 0x03: result = _voice0.PulseWidthHi; break;
				case 0x04:
					result = (_envelope0.Gate ? 0x01 : 0x00) |
					         (_voice0.Sync ? 0x02 : 0x00) |
					         (_voice0.RingMod ? 0x04 : 0x00) |
					         (_voice0.Test ? 0x08 : 0x00) |
					         (_voice0.Waveform << 4);
					break;
				case 0x05:
					result = (_envelope0.Attack << 4) |
					         _envelope0.Decay;
					break;
				case 0x06:
					result = (_envelope0.Sustain << 4) |
					         _envelope0.Release;
					break;
				case 0x07: result = _voice1.FrequencyLo; break;
				case 0x08: result = _voice1.FrequencyHi; break;
				case 0x09: result = _voice1.PulseWidthLo; break;
				case 0x0A: result = _voice1.PulseWidthHi; break;
				case 0x0B:
					result = (_envelope1.Gate ? 0x01 : 0x00) |
					         (_voice1.Sync ? 0x02 : 0x00) |
					         (_voice1.RingMod ? 0x04 : 0x00) |
					         (_voice1.Test ? 0x08 : 0x00) |
					         (_voice1.Waveform << 4);
					break;
				case 0x0C:
					result = (_envelope1.Attack << 4) |
					         _envelope1.Decay;
					break;
				case 0x0D:
					result = (_envelope1.Sustain << 4) |
					         _envelope1.Release;
					break;
				case 0x0E: result = _voice2.FrequencyLo; break;
				case 0x0F: result = _voice2.FrequencyHi; break;
				case 0x10: result = _voice2.PulseWidthLo; break;
				case 0x11: result = _voice2.PulseWidthHi; break;
				case 0x12:
					result = (_envelope2.Gate ? 0x01 : 0x00) |
					         (_voice2.Sync ? 0x02 : 0x00) |
					         (_voice2.RingMod ? 0x04 : 0x00) |
					         (_voice2.Test ? 0x08 : 0x00) |
					         (_voice2.Waveform << 4);
					break;
				case 0x13:
					result = (_envelope2.Attack << 4) |
					         _envelope2.Decay;
					break;
				case 0x14:
					result = (_envelope2.Sustain << 4) |
					         _envelope2.Release;
					break;
				case 0x15: result = _filterFrequency & 0x7; break;
				case 0x16: result = (_filterFrequency >> 3) & 0xFF; break;
				case 0x17:
					result = (_filterEnable[0] ? 0x01 : 0x00) |
					         (_filterEnable[1] ? 0x02 : 0x00) |
					         (_filterEnable[2] ? 0x04 : 0x00) |
					         (_filterResonance << 4);
					break;
				case 0x18:
					result = _volume |
					         (_filterSelectLoPass ? 0x10 : 0x00) |
					         (_filterSelectBandPass ? 0x20 : 0x00) |
					         (_filterSelectHiPass ? 0x40 : 0x00) |
					         (_disableVoice3 ? 0x80 : 0x00);
					break;
				case 0x19: result = _potX; break;
				case 0x1A: result = _potY; break;
				case 0x1B: result = _voiceOutput2 >> 4; break;
				case 0x1C: result = _envelopeOutput2; break;
			}

			return result;
		}

		public void Write(int addr, int val)
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

		private void WriteRegister(int addr, int val)
		{
			switch (addr)
			{
				case 0x00: _voice0.FrequencyLo = val; break;
				case 0x01: _voice0.FrequencyHi = val; break;
				case 0x02: _voice0.PulseWidthLo = val; break;
				case 0x03: _voice0.PulseWidthHi = val; break;
				case 0x04: _voice0.Control = val; _envelope0.Gate = (val & 0x01) != 0; break;
				case 0x05: _envelope0.Attack = val >> 4; _envelope0.Decay = val & 0xF; break;
				case 0x06: _envelope0.Sustain = val >> 4; _envelope0.Release = val & 0xF; break;
				case 0x07: _voice1.FrequencyLo = val; break;
				case 0x08: _voice1.FrequencyHi = val; break;
				case 0x09: _voice1.PulseWidthLo = val; break;
				case 0x0A: _voice1.PulseWidthHi = val; break;
				case 0x0B: _voice1.Control = val; _envelope1.Gate = (val & 0x01) != 0; break;
				case 0x0C: _envelope1.Attack = val >> 4; _envelope1.Decay = val & 0xF; break;
				case 0x0D: _envelope1.Sustain = val >> 4; _envelope1.Release = val & 0xF; break;
				case 0x0E: _voice2.FrequencyLo = val; break;
				case 0x0F: _voice2.FrequencyHi = val; break;
				case 0x10: _voice2.PulseWidthLo = val; break;
				case 0x11: _voice2.PulseWidthHi = val; break;
				case 0x12: _voice2.Control = val; _envelope2.Gate = (val & 0x01) != 0; break;
				case 0x13: _envelope2.Attack = val >> 4; _envelope2.Decay = val & 0xF; break;
				case 0x14: _envelope2.Sustain = val >> 4; _envelope2.Release = val & 0xF; break;
				case 0x15: _filterFrequency &= 0x3FF; _filterFrequency |= val & 0x7; break;
				case 0x16: _filterFrequency &= 0x7; _filterFrequency |= val << 3; break;
				case 0x17:
					_filterEnable[0] = (val & 0x1) != 0;
					_filterEnable[1] = (val & 0x2) != 0;
					_filterEnable[2] = (val & 0x4) != 0;
					_filterResonance = val >> 4;
					break;
				case 0x18:
					_volume = val & 0xF;
					_filterSelectLoPass = (val & 0x10) != 0;
					_filterSelectBandPass = (val & 0x20) != 0;
					_filterSelectHiPass = (val & 0x40) != 0;
					_disableVoice3 = (val & 0x40) != 0;
					break;
				case 0x19:
					_potX = val;
					break;
				case 0x1A:
					_potY = val;
					break;
			}
		}
	}
}
