using System.Runtime.CompilerServices;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	public sealed partial class TIA
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
			private bool sr1 = true;

			// 4 bit shift register
			private int sr4 = 0x0f;

			// 5 bit shift register
			private int sr5 = 0x1f;

			// 9 bit shift register
			private int sr9 = 0x1ff;

			// 3 state counter
			private int sr3 = 2;

			// counter based off AUDF
			private byte freqcnt;

			// latched audio value
			private bool on = true;

			private bool Run3()
			{
				sr3++;
				if (sr3 == 3)
				{
					sr3 = 0;
					return true;
				}

				return false;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private bool Run4()
			{
				bool ret = (sr4 & 1) != 0;
				bool c = ((sr4 & 1) != 0) ^ ((sr4 & 2) != 0);
				sr4 = (sr4 >> 1) | (c ? 8 : 0);
				return ret;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private bool Run5()
			{
				bool ret = (sr5 & 1) != 0;
				bool c = ((sr5 & 1) != 0) ^ ((sr5 & 4) != 0);
				sr5 = (sr5 >> 1) | (c ? 16 : 0);
				return ret;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private bool One4()
			{
				bool ret = (sr4 & 1) != 0;
				sr4 = (sr4 >> 1) | 8;
				return ret;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			private bool One5()
			{
				bool ret = (sr5 & 1) != 0;
				sr5 = (sr5 >> 1) | 16;
				return ret;
			}

			private bool Run1()
			{
				sr1 = !sr1;
				return !sr1;
			}

			private bool Run9()
			{
				bool ret = (sr9 & 1) != 0;
				bool c = ((sr9 & 1) != 0) ^ ((sr9 & 16) != 0);
				sr9 = (sr9 >> 1) | (c ? 256 : 0);
				return ret;
			}

			/// <summary>
			/// call me approx 31k times a second
			/// </summary>
			/// <returns>16 bit audio sample</returns>
			public short Cycle()
			{
				if (++freqcnt >= AUDF)
				{
					freqcnt = 0;
					switch (AUDC)
					{
						case 0x00:
						case 0x0b:
							// Both have a 1 s
							One5();
							on = One4();
							break;
						case 0x01:
							// Both run, but the 5 bit is ignored
							on = Run4();
							Run5();
							break;
						case 0x02:
							if ((sr5 == 31) || (sr5 == 16))
							{
								on = Run4();
							}
							Run5();
							break;
						case 0x03:
							if (Run5())
							{
								on = Run4();
							}
							break;
						case 0x04:
							Run5();
							One4();
							on = Run1();
							break;
						case 0x05:
							One5();
							Run4();
							on = Run1();
							break;
						case 0x06:
						case 0x0a:
							Run5();
							if (sr5 == 16)
							{
								on = false;
							}
							else if (sr5 == 31)
							{
								on = true;
							}
							break;
						case 0x07:
						case 0x09:
							on = Run5();
							break;
						case 0x08:
							on = Run9();
							break;
						case 0x0c:
						case 0x0d:
							if (Run3())
							{
								on = Run1();
							}
							break;
						case 0x0e:
							if (Run3())
							{
								goto case 0x06;
							}
							break;
						case 0x0f:
							// poly5 output to div 6
							if (Run5())
							{
								if (Run3())
								{
									on = Run1();
								}
							}
							break;
					}
				}

				return (short)(on ? AUDV * 1092 : 0);
			}

			public void SyncState(Serializer ser)
			{
				ser.Sync(nameof(AUDC), ref AUDC);
				ser.Sync(nameof(AUDF), ref AUDF);
				ser.Sync(nameof(AUDV), ref AUDV);
				ser.Sync(nameof(sr1), ref sr1);
				ser.Sync(nameof(sr3), ref sr3);
				ser.Sync(nameof(sr4), ref sr4);
				ser.Sync(nameof(sr5), ref sr5);
				ser.Sync(nameof(sr9), ref sr9);
				ser.Sync(nameof(freqcnt), ref freqcnt);
				ser.Sync(nameof(on), ref on);
			}

			public void Reset()
			{
				AUDC = 0;
				AUDF = 1;
				AUDV = 0;
				sr1 = true;
				sr4 = 0x0f;
				sr5 = 0x1f;
				sr9 = 0x1ff;
				sr3 = 2;
				freqcnt = 0;
				on = true;
			}
		}
	}
}
