using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class TIA
	{
		public class Audio
		{
			// noise/division control
			public byte AUDC = 0;

			// frequency divider
			public byte AUDF = 1;

			// volume
			public byte AUDV = 0;

			// 2 state counter
			private bool _sr1 = true;

			// 4 bit shift register
			private int _sr4 = 0x0f;

			// 5 bit shift register
			private int _sr5 = 0x1f;

			// 3 state counter
			private int _sr3 = 2;

			// counter based off AUDF
			private byte _freqcnt;

			// latched audio value
			private bool _on = true;

			private bool Run3()
			{
				_sr3++;
				if (_sr3 == 3)
				{
					_sr3 = 0;
					return true;
				}

				return false;
			}

			private bool Run4()
			{
				bool ret = (_sr4 & 1) != 0;
				bool c = (_sr4 & 1) != 0 ^ (_sr4 & 2) != 0;
				_sr4 = (_sr4 >> 1) | (c ? 8 : 0);
				return ret;
			}

			private bool Run5()
			{
				bool ret = (_sr5 & 1) != 0;
				bool c = (_sr5 & 1) != 0 ^ (_sr5 & 4) != 0;
				_sr5 = (_sr5 >> 1) | (c ? 16 : 0);
				return ret;
			}

			private bool One4()
			{
				bool ret = (_sr4 & 1) != 0;
				_sr4 = (_sr4 >> 1) | 8;
				return ret;
			}

			private bool One5()
			{
				bool ret = (_sr5 & 1) != 0;
				_sr5 = (_sr5 >> 1) | 16;
				return ret;
			}

			private bool Run1()
			{
				_sr1 = !_sr1;
				return !_sr1;
			}

			private bool Run9()
			{
				bool ret = (_sr4 & 1) != 0;
				bool c = (_sr5 & 1) != 0 ^ (_sr4 & 1) != 0;
				_sr4 = (_sr4 >> 1) | ((_sr5 & 1) != 0 ? 8 : 0);
				_sr5 = (_sr5 >> 1) | (c ? 16 : 0);
				return ret;
			}

			/// <summary>
			/// call me approx 31k times a second
			/// </summary>
			/// <returns>16 bit audio sample</returns>
			public short Cycle()
			{
				if (++_freqcnt == AUDF)
				{
					_freqcnt = 0;
					switch (AUDC)
					{
						case 0x00:
						case 0x0b:
							// Both have a 1 s
							One5();
							_on = One4();
							break;

						case 0x01:
							// Both run, but the 5 bit is ignored
							_on = Run4();
							Run5();
							break;

						case 0x02:
							if ((_sr5 & 0x0f) == 0 || (_sr5 & 0x0f) == 0x0f)
							{
								_on = Run4();
								break;
							}

							Run5();
							break;

						case 0x03:
							if (Run5())
							{
								_on = Run4();
							}

							break;

						case 0x04:
							Run5();
							One4();
							_on = Run1();
							break;

						case 0x05:
							One5();
							Run4();
							_on = Run1();
							break;

						case 0x06:
						case 0x0a:
							Run4(); // ???
							Run5();
							if ((_sr5 & 0x0f) == 0)
							{
								_on = false;
							}
							else if ((_sr5 & 0x0f) == 0x0f)
							{
								_on = true;
							}

							break;

						case 0x07:
						case 0x09:
							Run4(); // ???
							_on = Run5();
							break;
						case 0x08:
							_on = Run9();
							break;
						case 0x0c:
						case 0x0d:
							if (Run3())
							{
								_on = Run1();
							}

							break;
						case 0x0e:
							if (Run3())
							{
								goto case 0x06;
							}

							break;
						case 0x0f:
							if (Run3())
							{
								goto case 0x07;
							}

							break;
					}
				}

				return (short)(_on ? AUDV * 1092 : 0);
			}

			public void SyncState(Serializer ser)
			{
				ser.Sync("AUDC", ref AUDC);
				ser.Sync("AUDF", ref AUDF);
				ser.Sync("AUDV", ref AUDV);
				ser.Sync("sr1", ref _sr1);
				ser.Sync("sr3", ref _sr3);
				ser.Sync("sr4", ref _sr4);
				ser.Sync("sr5", ref _sr5);
				ser.Sync("freqcnt", ref _freqcnt);
				ser.Sync("on", ref _on);
			}
		}
	}
}
