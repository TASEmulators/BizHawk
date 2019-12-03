using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class TIA
	{
		public class Audio
		{
			// noise/division control
			public byte AUDC_L = 0;
			public byte AUDC_R = 0;

			// frequency divider
			public byte AUDF_L = 1;
			public byte AUDF_R = 1;

			// volume
			public byte AUDV_L = 0;
			public byte AUDV_R = 0;

			// 2 state counter
			private bool sr1_L = true;
			private bool sr1_R = true;

			// 4 bit shift register
			private int sr4_L = 0x0f;
			private int sr4_R = 0x0f;

			// 5 bit shift register
			private int sr5_L = 0x1f;
			private int sr5_R = 0x1f;

			// 9 bit shift register
			private int sr9_L = 0x1ff;
			private int sr9_R = 0x1ff;

			// 3 state counter
			private int sr3_L = 2;
			private int sr3_R = 2;

			// counter based off AUDF
			private byte freqcnt_L;
			private byte freqcnt_R;

			// latched audio value
			private bool on_L = true;
			private bool on_R = true;

			private bool Run3_L()
			{
				sr3_L++;
				if (sr3_L == 3)
				{
					sr3_L = 0;
					return true;
				}

				return false;
			}

			private bool Run4_L()
			{
				bool ret = (sr4_L & 1) != 0;
				bool c = ((sr4_L & 1) != 0) ^ ((sr4_L & 2) != 0);
				sr4_L = (sr4_L >> 1) | (c ? 8 : 0);
				return ret;
			}

			private bool Run5_L()
			{
				bool ret = (sr5_L & 1) != 0;
				bool c = ((sr5_L & 1) != 0) ^ ((sr5_L & 4) != 0);
				sr5_L = (sr5_L >> 1) | (c ? 16 : 0);
				return ret;
			}

			private bool One4_L()
			{
				bool ret = (sr4_L & 1) != 0;
				sr4_L = (sr4_L >> 1) | 8;
				return ret;
			}

			private bool One5_L()
			{
				bool ret = (sr5_L & 1) != 0;
				sr5_L = (sr5_L >> 1) | 16;
				return ret;
			}

			private bool Run1_L()
			{
				sr1_L = !sr1_L;
				return !sr1_L;
			}

			private bool Run9_L()
			{
				bool ret = (sr9_L & 1) != 0;
				bool c = ((sr9_L & 1) != 0) ^ ((sr9_L & 16) != 0);
				sr9_L = (sr9_L >> 1) | (c ? 256 : 0);
				return ret;
			}

			/// <summary>
			/// call me approx 31k times a second
			/// </summary>
			/// <returns>16 bit audio sample</returns>
			public short Cycle_L()
			{
				if (++freqcnt_L >= AUDF_L)
				{
					freqcnt_L = 0;
					switch (AUDC_L)
					{
						case 0x00:
						case 0x0b:
							// Both have a 1 s
							One5_L();
							on_L = One4_L();
							break;

						case 0x01:
							// Both run, but the 5 bit is ignored
							on_L = Run4_L();
							//Run5();
							break;
						case 0x02:
							if ((sr5_L & 0x0f) == 0 || (sr5_L & 0x0f) == 0x0f)
							{
								on_L = Run4_L();
							}

							Run5_L();
							break;
						case 0x03:
							if (Run5_L())
							{
								on_L = Run4_L();
							}

							break;

						case 0x04:
							Run5_L();
							One4_L();
							on_L = Run1_L();
							break;

						case 0x05:
							One5_L();
							Run4_L();
							on_L = Run1_L();
							break;
							
						case 0x06:
						case 0x0a:
							Run5_L();
							if ((sr5_L & 0x0f) == 0)
							{
								on_L = false;
							}
							else if ((sr5_L & 0x0f) == 0x0f)
							{
								on_L = true;
							}

							break;

						case 0x07:
						case 0x09:
							on_L = Run5_L();
							break;
							
						case 0x08:
							on_L = Run9_L();
							break;
						case 0x0c:
						case 0x0d:
							if (Run3_L())
							{
								on_L = Run1_L();
							}

							break;
						case 0x0e:
							if (Run3_L())
							{
								goto case 0x06;
							}

							break;
						case 0x0f:
							// poly5 output to div 6
							if (Run5_L())
							{
								if (Run3_L())
								{
									on_L = Run1_L();
								}
							}
							break;
					}
				}

				return (short)(on_L ? AUDV_L * 1092 : 0);
			}

			private bool Run3_R()
			{
				sr3_R++;
				if (sr3_R == 3)
				{
					sr3_R = 0;
					return true;
				}

				return false;
			}

			private bool Run4_R()
			{
				bool ret = (sr4_R & 1) != 0;
				bool c = ((sr4_R & 1) != 0) ^ ((sr4_R & 2) != 0);
				sr4_R = (sr4_R >> 1) | (c ? 8 : 0);
				return ret;
			}

			private bool Run5_R()
			{
				bool ret = (sr5_R & 1) != 0;
				bool c = ((sr5_R & 1) != 0) ^ ((sr5_R & 4) != 0);
				sr5_R = (sr5_R >> 1) | (c ? 16 : 0);
				return ret;
			}

			private bool One4_R()
			{
				bool ret = (sr4_R & 1) != 0;
				sr4_R = (sr4_R >> 1) | 8;
				return ret;
			}

			private bool One5_R()
			{
				bool ret = (sr5_R & 1) != 0;
				sr5_R = (sr5_R >> 1) | 16;
				return ret;
			}

			private bool Run1_R()
			{
				sr1_R = !sr1_R;
				return !sr1_R;
			}

			private bool Run9_R()
			{
				bool ret = (sr9_R & 1) != 0;
				bool c = ((sr9_R & 1) != 0) ^ ((sr9_R & 16) != 0);
				sr9_R = (sr9_R >> 1) | (c ? 256 : 0);
				return ret;
			}

			/// <summary>
			/// call me approx 31k times a second
			/// </summary>
			/// <returns>16 bit audio sample</returns>
			public short Cycle_R()
			{
				if (++freqcnt_R >= AUDF_R)
				{
					freqcnt_R = 0;
					switch (AUDC_R)
					{
						case 0x00:
						case 0x0b:
							// Both have a 1 s
							One5_R();
							on_R = One4_R();
							break;

						case 0x01:
							// Both run, but the 5 bit is ignored
							on_R = Run4_R();
							//Run5();
							break;
						case 0x02:
							if ((sr5_R & 0x0f) == 0 || (sr5_R & 0x0f) == 0x0f)
							{
								on_R = Run4_R();
							}

							Run5_R();
							break;
						case 0x03:
							if (Run5_R())
							{
								on_R = Run4_R();
							}

							break;

						case 0x04:
							Run5_R();
							One4_R();
							on_R = Run1_R();
							break;

						case 0x05:
							One5_R();
							Run4_R();
							on_R = Run1_R();
							break;

						case 0x06:
						case 0x0a:
							Run5_R();
							if ((sr5_R & 0x0f) == 0)
							{
								on_R = false;
							}
							else if ((sr5_R & 0x0f) == 0x0f)
							{
								on_R = true;
							}

							break;

						case 0x07:
						case 0x09:
							on_R = Run5_R();
							break;

						case 0x08:
							on_R = Run9_R();
							break;
						case 0x0c:
						case 0x0d:
							if (Run3_R())
							{
								on_R = Run1_R();
							}

							break;
						case 0x0e:
							if (Run3_R())
							{
								goto case 0x06;
							}

							break;
						case 0x0f:
							// poly5 output to div 6
							if (Run5_R())
							{
								if (Run3_R())
								{
									on_R = Run1_R();
								}
							}
							break;
					}
				}

				return (short)(on_R ? AUDV_R * 1092 : 0);
			}

			public void SyncState(Serializer ser)
			{
				ser.Sync(nameof(AUDC_L), ref AUDC_L);
				ser.Sync(nameof(AUDF_L), ref AUDF_L);
				ser.Sync(nameof(AUDV_L), ref AUDV_L);
				ser.Sync(nameof(sr1_L), ref sr1_L);
				ser.Sync(nameof(sr3_L), ref sr3_L);
				ser.Sync(nameof(sr4_L), ref sr4_L);
				ser.Sync(nameof(sr5_L), ref sr5_L);
				ser.Sync(nameof(sr9_L), ref sr9_L);
				ser.Sync(nameof(freqcnt_L), ref freqcnt_L);
				ser.Sync(nameof(on_L), ref on_L);

				ser.Sync(nameof(AUDC_R), ref AUDC_R);
				ser.Sync(nameof(AUDF_R), ref AUDF_R);
				ser.Sync(nameof(AUDV_R), ref AUDV_R);
				ser.Sync(nameof(sr1_R), ref sr1_R);
				ser.Sync(nameof(sr3_R), ref sr3_R);
				ser.Sync(nameof(sr4_R), ref sr4_R);
				ser.Sync(nameof(sr5_R), ref sr5_R);
				ser.Sync(nameof(sr9_R), ref sr9_R);
				ser.Sync(nameof(freqcnt_R), ref freqcnt_R);
				ser.Sync(nameof(on_R), ref on_R);
			}
		}
	}
}
