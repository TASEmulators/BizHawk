using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Sound
{
	class VRC6 : ISoundProvider
	{
		public void DiscardSamples() { }
		public int MaxVolume { get; set; }
		public void GetSamples(short[] samples)
		{
			for (int i = 0; i < samples.Length; )
			{
				short val = 0;
				val = (short)(Pulse1.RenderSample() << 4);
				val += (short)(Pulse2.RenderSample() << 7);
				val += (short)(Sawtooth.RenderSample() << 7);
				samples[i++] = val;
				samples[i++] = val;
			}
		}

		public VRC6()
		{
			MaxVolume = (short.MaxValue / 3);
		}

		public void SyncState(Serializer ser)
		{
			Pulse1.SyncState(ser);
			Pulse2.SyncState(ser);
			Sawtooth.SyncState(ser);
		}

		public void Write9000(byte value)
		{
			Pulse1.Write9000(value);
		}

		public void Write9001(byte value)
		{
			Pulse1.Write9001(value);
		}

		public void Write9002(byte value)
		{
			Pulse1.Write9002(value);
		}

		public void WriteA000(byte value)
		{
			Pulse2.WriteA000(value);
		}

		public void WriteA001(byte value)
		{
			Pulse2.WriteA001(value);
		}

		public void WriteA002(byte value)
		{
			Pulse2.WriteA002(value);
		}

		public void WriteB000(byte value)
		{
			Sawtooth.WriteB000(value);
		}

		public void WriteB001(byte value)
		{
			Sawtooth.WriteB001(value);
		}

		public void WriteB002(byte value)
		{
			Sawtooth.WriteB002(value);
		}

		private Chn_VRC6Pulse1 Pulse1 = new Chn_VRC6Pulse1();
		private Chn_VRC6Pulse2 Pulse2 = new Chn_VRC6Pulse2();
		private Chn_VRC6Sawtooth Sawtooth = new Chn_VRC6Sawtooth();

		public class Chn_VRC6Pulse1
		{
			byte _Volume = 0;
			double DutyPercentage = 0;
			int _DutyCycle = 0;
			int _FreqTimer = 0;
			bool _Enabled = false;
			double _Frequency = 0;
			double _SampleCount = 0;
			double _RenderedLength = 0;
			bool WaveStatus = false;
			short OUT = 0;

			public void SyncState(Serializer ser)
			{
				ser.Sync("_Volume", ref _Volume);
				//ser.Sync("DutyPercentage", ref DutyPercentage);
				ser.Sync("_DutyCycle", ref _DutyCycle);
				ser.Sync("_FreqTimer", ref _FreqTimer);
				ser.Sync("_Enabled", ref _Enabled);
				//ser.Sync("_Frequency", ref _Frequency);
				//ser.Sync("_SampleCount", ref _SampleCount);
				//ser.Sync("_RenderedLength", ref _RenderedLength);
				ser.Sync("WaveStatus", ref WaveStatus);
				ser.Sync("OUT", ref OUT);
			}

			public short RenderSample()
			{
				if (_Enabled)
				{
					_SampleCount++;
					if (WaveStatus && (_SampleCount > (_RenderedLength * DutyPercentage)))
					{
						_SampleCount -= _RenderedLength * DutyPercentage;
						WaveStatus = !WaveStatus;
					}
					else if (!WaveStatus && (_SampleCount > (_RenderedLength * (1.0 - DutyPercentage))))
					{
						_SampleCount -= _RenderedLength * (1.0 - DutyPercentage);
						WaveStatus = !WaveStatus;
					}
					if (WaveStatus)
						OUT = (short)(-_Volume);
					else
						OUT = (short)(_Volume);

					return OUT;
				}
				return 0;
			}
			public void Write9000(byte data)
			{
				_Volume = (byte)(data & 0x0F);//Bit 0 - 3
				_DutyCycle = (data >> 4); //Bit 4 - 7
				if (_DutyCycle == 0)
					DutyPercentage = 0.6250;
				else if (_DutyCycle == 1)
					DutyPercentage = 0.1250;
				else if (_DutyCycle == 2)
					DutyPercentage = 0.1875;
				else if (_DutyCycle == 3)
					DutyPercentage = 0.2500;
				else if (_DutyCycle == 4)
					DutyPercentage = 0.3125;
				else if (_DutyCycle == 5)
					DutyPercentage = 0.3750;
				else if (_DutyCycle == 6)
					DutyPercentage = 0.4375;
				else if (_DutyCycle == 7)
					DutyPercentage = 0.5000;
				else
					DutyPercentage = 1.0;
			}
			public void Write9001(byte data)
			{
				_FreqTimer = (_FreqTimer & 0x0F00) | data;
				//Update freq
				_Frequency = 1790000 / 16 / (_FreqTimer + 1);
				_RenderedLength = 44100 / _Frequency;
			}
			public void Write9002(byte data)
			{
				_FreqTimer = (_FreqTimer & 0x00FF) | ((data & 0x0F) << 8);
				_Enabled = (data & 0x80) != 0;
				//Update freq
				_Frequency = 1790000 / 16 / (_FreqTimer + 1);
				_RenderedLength = 44100 / _Frequency;
			}
		}

		public class Chn_VRC6Pulse2
		{
			byte _Volume = 0;
			double DutyPercentage = 0;
			int _DutyCycle = 0;
			int _FreqTimer = 0;
			bool _Enabled = false;
			double _Frequency = 0;
			double _SampleCount = 0;
			double _RenderedLength = 0;
			bool WaveStatus = false;
			short OUT = 0;

			public void SyncState(Serializer ser)
			{
				ser.Sync("_Volume", ref _Volume);
				//ser.Sync("DutyPercentage", ref DutyPercentage);
				ser.Sync("_DutyCycle", ref _DutyCycle);
				ser.Sync("_FreqTimer", ref _FreqTimer);
				ser.Sync("_Enabled", ref _Enabled);
				//ser.Sync("_Frequency", ref _Frequency);
				//ser.Sync("_SampleCount", ref _SampleCount);
				//ser.Sync("_RenderedLength", ref _RenderedLength);
				ser.Sync("WaveStatus", ref WaveStatus);
				ser.Sync("OUT", ref OUT);
			}

			public short RenderSample()
			{
				if (_Enabled)
				{
					_SampleCount++;
					if (WaveStatus && (_SampleCount > (_RenderedLength * DutyPercentage)))
					{
						_SampleCount -= _RenderedLength * DutyPercentage;
						WaveStatus = !WaveStatus;
					}
					else if (!WaveStatus && (_SampleCount > (_RenderedLength * (1.0 - DutyPercentage))))
					{
						_SampleCount -= _RenderedLength * (1.0 - DutyPercentage);
						WaveStatus = !WaveStatus;
					}
					if (WaveStatus)
						OUT = (short)(-_Volume);
					else
						OUT = (short)(_Volume);

					return OUT;
				}
				return 0;
			}
			public void WriteA000(byte data)
			{
				_Volume = (byte)(data & 0x0F);//Bit 0 - 3
				_DutyCycle = (data >> 4); //Bit 4 - 7
				if (_DutyCycle == 0)
					DutyPercentage = 0.6250;
				else if (_DutyCycle == 1)
					DutyPercentage = 0.1250;
				else if (_DutyCycle == 2)
					DutyPercentage = 0.1875;
				else if (_DutyCycle == 3)
					DutyPercentage = 0.2500;
				else if (_DutyCycle == 4)
					DutyPercentage = 0.3125;
				else if (_DutyCycle == 5)
					DutyPercentage = 0.3750;
				else if (_DutyCycle == 6)
					DutyPercentage = 0.4375;
				else if (_DutyCycle == 7)
					DutyPercentage = 0.5000;
				else
					DutyPercentage = 1.0;
			}
			public void WriteA001(byte data)
			{
				_FreqTimer = (_FreqTimer & 0x0F00) | data;
				//Update freq
				_Frequency = 1790000 / 16 / (_FreqTimer + 1);
				_RenderedLength = 44100 / _Frequency;
			}
			public void WriteA002(byte data)
			{
				_FreqTimer = (_FreqTimer & 0x00FF) | ((data & 0x0F) << 8);
				_Enabled = (data & 0x80) != 0;
				//Update freq
				_Frequency = 1790000 / 16 / (_FreqTimer + 1);
				_RenderedLength = 44100 / _Frequency;
			}
		}

		public class Chn_VRC6Sawtooth
		{
			byte AccumRate = 0;
			byte AccumStep = 0;
			byte Accum = 0;
			int _FreqTimer = 0;
			bool _Enabled = false;
			double _Frequency = 0;
			double _SampleCount = 0;
			double _RenderedLength = 0;
			short OUT = 0;

			public void SyncState(Serializer ser)
			{
				ser.Sync("AccumRate", ref AccumRate);
				ser.Sync("AccumStep", ref AccumStep);
				ser.Sync("Accum", ref Accum);
				ser.Sync("_FreqTimer", ref _FreqTimer);
				ser.Sync("_Enabled", ref _Enabled);
				//ser.Sync("_Frequency", ref _Frequency);
				//ser.Sync("_SampleCount", ref _SampleCount);
				//ser.Sync("_RenderedLength", ref _RenderedLength);
				ser.Sync("OUT", ref OUT);
			}

			public short RenderSample()
			{
				if (_Enabled)
				{
					_SampleCount++;
					if (_SampleCount >= _RenderedLength)
					{
						_SampleCount -= _RenderedLength;
						AccumStep++;
						if ((AccumStep & 2) != 0)
							Accum += AccumRate;
						if (AccumStep >= 14)
							AccumStep = Accum = 0;

						OUT = (short)(Accum >> 3);

					}
					return (short)((OUT - 5));
				}
				return 0;
			}
			public void WriteB000(byte data)
			{
				AccumRate = (byte)(data & 0x3F);
			}
			public void WriteB001(byte data)
			{
				_FreqTimer = (_FreqTimer & 0x0F00) | data;
				//Update freq
				_Frequency = 1790000 / (_FreqTimer + 1);
				_RenderedLength = 44100 / _Frequency;
			}
			public void WriteB002(byte data)
			{
				_FreqTimer = (_FreqTimer & 0x00FF) | ((data & 0x0F) << 8);
				_Enabled = (data & 0x80) != 0;
				//Update freq
				_Frequency = 1790000 / (_FreqTimer + 1);
				_RenderedLength = 44100 / _Frequency;
			}
		}
	}
}
