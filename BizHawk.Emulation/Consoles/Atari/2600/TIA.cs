using System;
using System.Globalization;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.Emulation.Consoles.Atari
{
	// Emulates the TIA
	public partial class TIA : IVideoProvider, ISoundProvider
	{
		Atari2600 core;

		public bool frameComplete;
		byte hsyncCnt = 0;
		int capChargeStart = 0;
		bool capCharging = false;

		static byte CXP0 = 0x01;
		static byte CXP1 = 0x02;
		static byte CXM0 = 0x04;
		static byte CXM1 = 0x08;
		static byte CXPF = 0x10;
		static byte CXBL = 0x20;

		struct missileData
		{
			public bool enabled;
			public bool resetToPlayer;
			public byte hPosCnt;
			public byte size;
			public byte number;
			public byte HM;
			public byte collisions;

			public bool tick()
			{
				bool result = false;

				// At hPosCnt == 0, start drawing the missile, if enabled
				if (hPosCnt < (1 << size))
				{
					if (enabled && !resetToPlayer)
					{
						// Draw the missile
						result = true;
					}
				}
				
				if ((number & 0x07) == 0x01 || ((number & 0x07) == 0x03))
				{
					if (hPosCnt >= 16 && hPosCnt <= (16 + (1 << size) - 1) )
					{
						if (enabled && !resetToPlayer)
						{
							// Draw the missile
							result = true;
						}
					}
				}

				if (((number & 0x07) == 0x02 || ((number & 0x07) == 0x03) || ((number & 0x07) == 0x06)))
				{
					if (hPosCnt >= 32 && hPosCnt <= (32 + (1 << size) - 1) )
					{
						if (enabled && !resetToPlayer)
						{
							// Draw the missile
							result = true;
						}
					}
				}

				if ((number & 0x07) == 0x04 || (number & 0x07) == 0x06)
				{
					if (hPosCnt >= 64 && hPosCnt <= (64 + (1 << size) - 1) )
					{
						if (enabled && !resetToPlayer)
						{
							// Draw the missile
							result = true;
						}
					}
				}

				// Increment the counter
				hPosCnt++;
				// Counter loops at 160 
				hPosCnt %= 160;

				return result;
			}

			public void SyncState(Serializer ser)
			{
				ser.Sync("enabled", ref enabled);
				ser.Sync("resetToPlayer", ref resetToPlayer);
				ser.Sync("hPosCnt", ref hPosCnt);
				ser.Sync("size", ref size);
				ser.Sync("number", ref number);
				ser.Sync("HM", ref HM);
				ser.Sync("collisions", ref collisions);
			}
		}

		struct playerData
		{
			public missileData missile;
			public byte grp;
			public byte dgrp;
			public byte color;
			public byte hPosCnt;
			public byte scanCnt;
			public byte scanStrchCnt;
			public byte HM;
			public bool reflect;
			public bool delay;
			public byte nusiz;
			public bool reset;
			public byte resetCnt;
			public byte collisions;

			public bool tick()
			{
				bool result = false;
				if (scanCnt < 8)
				{
					// Make the mask to check the graphic
					byte playerMask = (byte)(1 << (8 - 1 - scanCnt));

					// Reflect it if needed
					if (reflect)
					{
						playerMask = (byte)reverseBits(playerMask, 8);
					}

					// Check the graphic (depending on delay)
					if (!delay)
					{
						if ((grp & playerMask) != 0)
						{
							result = true;
						}
					}
					else
					{
						if ((dgrp & playerMask) != 0)
						{
							result = true;
						}
					}

					// Reset missile, if desired
					if (scanCnt == 0x04 && hPosCnt <= 16 && missile.resetToPlayer)
					{
						missile.hPosCnt = 0;
					}

					// Increment counter
					// When this reaches 8, we've run out of pixel

					// If we're drawing a stretched player, only incrememnt the
					// counter every 2 or 4 clocks

					// Double size player
					if ((nusiz & 0x07) == 0x05)
					{
						scanStrchCnt++;
						scanStrchCnt %= 2;
					}
					// Quad size player
					else if ((nusiz & 0x07) == 0x07)
					{
						scanStrchCnt++;
						scanStrchCnt %= 4;
					}
					// Single size player
					else
					{
						scanStrchCnt = 0;
					}

					if (scanStrchCnt == 0)
					{
						scanCnt++;
					}

				}

				// At counter position 0 we should start drawing, a pixel late
				// Set the scan counter at 0, and at the next pixel the graphic will start drawing
				if (hPosCnt == 0 && !reset)
				{
					scanCnt = 0;
					if ((nusiz & 0x07) == 0x05 || (nusiz & 0x07) == 0x07)
					{
						scanStrchCnt = 0;
					}
				}

				if (hPosCnt == 16 && ((nusiz & 0x07) == 0x01 || ((nusiz & 0x07) == 0x03)))
				{
					scanCnt = 0;
				}

				if (hPosCnt == 32 && ((nusiz & 0x07) == 0x02 || ((nusiz & 0x07) == 0x03) || ((nusiz & 0x07) == 0x06)))
				{
					scanCnt = 0;
				}

				if (hPosCnt == 64 && ((nusiz & 0x07) == 0x04 || ((nusiz & 0x07) == 0x06)))
				{
					scanCnt = 0;
				}

				// Reset is no longer in effect
				reset = false;
				// Increment the counter
				hPosCnt++;
				// Counter loops at 160 
				hPosCnt %= 160;

				if (resetCnt < 4)
				{
					resetCnt++;
				}

				if (resetCnt == 4)
				{
					hPosCnt = 0;
					reset = true;
					resetCnt++;
				}

				return result;
			}

			public void SyncState(Serializer ser)
			{
				missile.SyncState(ser);
				ser.Sync("grp", ref grp);
				ser.Sync("dgrp", ref dgrp);
				ser.Sync("color", ref color);
				ser.Sync("hPosCnt", ref hPosCnt);
				ser.Sync("scanCnt", ref scanCnt);
				ser.Sync("scanStrchCnt", ref scanStrchCnt);
				ser.Sync("HM", ref HM);
				ser.Sync("reflect", ref reflect);
				ser.Sync("delay", ref delay);
				ser.Sync("nusiz", ref nusiz);
				ser.Sync("reset", ref reset);
				ser.Sync("resetCnt", ref resetCnt);
				ser.Sync("collisions", ref collisions);
			}
		};

		struct ballData
		{
			public bool enabled;
			public bool denabled;
			public bool delay;
			public byte size;
			public byte HM;
			public byte hPosCnt;
			
			public byte collisions;

			public bool tick()
			{
				bool result = false;
				if (hPosCnt < (1 << size))
				{
					if (!delay && enabled)
					{
						// Draw the ball!
						result = true;
					}
					else if (delay && denabled)
					{
						// Draw the ball!
						result = true;
					}
				}

				// Increment the counter
				hPosCnt++;
				// Counter loops at 160 
				hPosCnt %= 160;

				return result;
			}

			public void SyncState(Serializer ser)
			{
				ser.Sync("enabled", ref enabled);
				ser.Sync("denabled", ref denabled);
				ser.Sync("delay", ref delay);
				ser.Sync("size", ref size);
				ser.Sync("HM", ref HM);
				ser.Sync("hPosCnt", ref hPosCnt);
				ser.Sync("collisions", ref collisions);
			}

		};

		struct playfieldData
		{
			public UInt32 grp;
			public byte pfColor;
			public byte bkColor;
			public bool reflect;
			public bool score;
			public bool priority;
			public void SyncState(Serializer ser)
			{
				ser.Sync("grp", ref grp);
				ser.Sync("pfColor", ref pfColor);
				ser.Sync("bkColor", ref bkColor);
				ser.Sync("reflect", ref reflect);
				ser.Sync("score", ref score);
				ser.Sync("priority", ref priority);
			}
		};

		struct hmoveData
		{
			public bool hmoveEnabled;
			public bool hmoveJustStarted;
			public bool lateHBlankReset;
			public bool decCntEnabled;

			public bool player0Latch;
			public bool player1Latch;
			public bool missile0Latch;
			public bool missile1Latch;
			public bool ballLatch;

			public byte hmoveDelayCnt;

			public byte hmoveCnt;

			public byte player0Cnt;
			public byte player1Cnt;
			public byte missile0Cnt;
			public byte missile1Cnt;
			public byte ballCnt;

			public void SyncState(Serializer ser)
			{
				ser.Sync("hmoveEnabled", ref hmoveEnabled);
				ser.Sync("hmoveJustStarted", ref hmoveJustStarted);
				ser.Sync("lateHBlankReset", ref lateHBlankReset);
				ser.Sync("decCntEnabled", ref decCntEnabled);
				ser.Sync("player0Latch", ref player0Latch);
				ser.Sync("player1Latch", ref player1Latch);
				ser.Sync("missile0Latch", ref missile0Latch);
				ser.Sync("missile1Latch", ref missile1Latch);
				ser.Sync("ballLatch", ref ballLatch);
				ser.Sync("hmoveDelayCnt", ref hmoveDelayCnt);
				ser.Sync("hmoveCnt", ref hmoveCnt);
				ser.Sync("player0Cnt", ref player0Cnt);
				ser.Sync("player1Cnt", ref player1Cnt);
				ser.Sync("missile0Cnt", ref missile0Cnt);
				ser.Sync("missile1Cnt", ref missile1Cnt);
				ser.Sync("ballCnt", ref ballCnt);
			}
		};

		playerData player0;
		playerData player1;
		playfieldData playField;
		hmoveData hmove;
		ballData ball;


		bool vblankEnabled = false;
		bool vsyncEnabled = false;

		List<uint[]> scanlinesBuffer = new List<uint[]>();
		uint[] scanline = new uint[160];

		UInt32[] palette = new UInt32[]{
		  0x000000, 0, 0x4a4a4a, 0, 0x6f6f6f, 0, 0x8e8e8e, 0,
		  0xaaaaaa, 0, 0xc0c0c0, 0, 0xd6d6d6, 0, 0xececec, 0,
		  0x484800, 0, 0x69690f, 0, 0x86861d, 0, 0xa2a22a, 0,
		  0xbbbb35, 0, 0xd2d240, 0, 0xe8e84a, 0, 0xfcfc54, 0,
		  0x7c2c00, 0, 0x904811, 0, 0xa26221, 0, 0xb47a30, 0,
		  0xc3903d, 0, 0xd2a44a, 0, 0xdfb755, 0, 0xecc860, 0,
		  0x901c00, 0, 0xa33915, 0, 0xb55328, 0, 0xc66c3a, 0,
		  0xd5824a, 0, 0xe39759, 0, 0xf0aa67, 0, 0xfcbc74, 0,
		  0x940000, 0, 0xa71a1a, 0, 0xb83232, 0, 0xc84848, 0,
		  0xd65c5c, 0, 0xe46f6f, 0, 0xf08080, 0, 0xfc9090, 0,
		  0x840064, 0, 0x97197a, 0, 0xa8308f, 0, 0xb846a2, 0,
		  0xc659b3, 0, 0xd46cc3, 0, 0xe07cd2, 0, 0xec8ce0, 0,
		  0x500084, 0, 0x68199a, 0, 0x7d30ad, 0, 0x9246c0, 0,
		  0xa459d0, 0, 0xb56ce0, 0, 0xc57cee, 0, 0xd48cfc, 0,
		  0x140090, 0, 0x331aa3, 0, 0x4e32b5, 0, 0x6848c6, 0,
		  0x7f5cd5, 0, 0x956fe3, 0, 0xa980f0, 0, 0xbc90fc, 0,
		  0x000094, 0, 0x181aa7, 0, 0x2d32b8, 0, 0x4248c8, 0,
		  0x545cd6, 0, 0x656fe4, 0, 0x7580f0, 0, 0x8490fc, 0,
		  0x001c88, 0, 0x183b9d, 0, 0x2d57b0, 0, 0x4272c2, 0,
		  0x548ad2, 0, 0x65a0e1, 0, 0x75b5ef, 0, 0x84c8fc, 0,
		  0x003064, 0, 0x185080, 0, 0x2d6d98, 0, 0x4288b0, 0,
		  0x54a0c5, 0, 0x65b7d9, 0, 0x75cceb, 0, 0x84e0fc, 0,
		  0x004030, 0, 0x18624e, 0, 0x2d8169, 0, 0x429e82, 0,
		  0x54b899, 0, 0x65d1ae, 0, 0x75e7c2, 0, 0x84fcd4, 0,
		  0x004400, 0, 0x1a661a, 0, 0x328432, 0, 0x48a048, 0,
		  0x5cba5c, 0, 0x6fd26f, 0, 0x80e880, 0, 0x90fc90, 0,
		  0x143c00, 0, 0x355f18, 0, 0x527e2d, 0, 0x6e9c42, 0,
		  0x87b754, 0, 0x9ed065, 0, 0xb4e775, 0, 0xc8fc84, 0,
		  0x303800, 0, 0x505916, 0, 0x6d762b, 0, 0x88923e, 0,
		  0xa0ab4f, 0, 0xb7c25f, 0, 0xccd86e, 0, 0xe0ec7c, 0,
		  0x482c00, 0, 0x694d14, 0, 0x866a26, 0, 0xa28638, 0,
		  0xbb9f47, 0, 0xd2b656, 0, 0xe8cc63, 0, 0xfce070, 0
		};

		public TIA(Atari2600 core)
		{
			this.core = core;
			player0.scanCnt = 8;
			player1.scanCnt = 8;
		}

		public int[] frameBuffer = new int[320 * 262];
		public int[] GetVideoBuffer() { return frameBuffer; }
        public int VirtualWidth { get { return 320; } }
        public int BufferWidth { get { return 320; } }
		public int BufferHeight { get { return 262; } }
		public int BackgroundColor { get { return 0; } }

		public class audio
		{
			/// <summary>noise/division control</summary>
			public byte AUDC = 0;
			/// <summary>frequency divider</summary>
			public byte AUDF = 1;
			/// <summary>volume</summary>
			public byte AUDV = 0;

			/// <summary>2 state counter</summary>
			bool sr1 = true;
			/// <summary>4 bit shift register</summary>
			int sr4 = 0x0f;
			/// <summary>5 bit shift register</summary>
			int sr5 = 0x1f;
			/// <summary>3 state counter</summary>
			int sr3 = 2;

			/// <summary>counter based off AUDF</summary>
			byte freqcnt = 0;

			/// <summary>latched audio value</summary>
			bool on = true;

			bool run_3()
			{
				sr3++;
				if (sr3 == 3)
				{
					sr3 = 0;
					return true;
				}
				return false;
			}

			bool run_4()
			{
				bool ret = (sr4 & 1) != 0;
				bool c = (sr4 & 1) != 0 ^ (sr4 & 2) != 0;
				sr4 = (sr4 >> 1) | (c ? 8 : 0);
				return ret;
			}

			bool run_5()
			{
				bool ret = (sr5 & 1) != 0;
				bool c = (sr5 & 1) != 0 ^ (sr5 & 4) != 0;
				sr5 = (sr5 >> 1) | (c ? 16 : 0);
				return ret;
			}

			bool one_4()
			{
				bool ret = (sr4 & 1) != 0;
				sr4 = (sr4 >> 1) | 8;
				return ret;
			}
			bool one_5()
			{
				bool ret = (sr5 & 1) != 0;
				sr5 = (sr5 >> 1) | 16;
				return ret;
			}

			bool run_1()
			{
				sr1 = !sr1;
				return !sr1;
			}

			bool run_9()
			{
				bool ret = (sr4 & 1) != 0;
				bool c = (sr5 & 1) != 0 ^ (sr4 & 1) != 0;
				sr4 = (sr4 >> 1) | ((sr5 & 1) != 0 ? 8 : 0);
				sr5 = (sr5 >> 1) | (c ? 16 : 0);
				return ret;
			}

			/// <summary>
			/// call me approx 31k times a second
			/// </summary>
			/// <returns>16 bit audio sample</returns>
			public short cycle()
			{
				if (++freqcnt == AUDF)
				{
					freqcnt = 0;
					switch (AUDC)
					{
						case 0x00:
						case 0x0b:
							// Both have a 1 s
							one_5();
							on = one_4();
							break;

						case 0x01:
							// Both run, but the 5 bit is ignored
							on = run_4();
							run_5();
							break;

						case 0x02:
							if ((sr5 & 0x0f) == 0 || (sr5 & 0x0f) == 0x0f)
							{
								on = run_4();
								break;
							}
							run_5();
							break;

						case 0x03:
							if (run_5())
							{
								on = run_4();
								break;
							}
							break;

						case 0x04:
							run_5();
							one_4();
							on = run_1();
							break;

						case 0x05:
							one_5();
							run_4();
							on = run_1();
							break;

						case 0x06:
						case 0x0a:
							run_4(); // ???
							run_5();
							if ((sr5 & 0x0f) == 0)
								on = false;
							else if ((sr5 & 0x0f) == 0x0f)
								on = true;
							break;

						case 0x07:
						case 0x09:
							run_4(); // ???
							on = run_5();
							break;
						case 0x08:
							on = run_9();
							break;
						case 0x0c:
						case 0x0d:
							if (run_3())
								on = run_1();
							break;
						case 0x0e:
							if (run_3())
								goto case 0x06;
							break;
						case 0x0f:
							if (run_3())
								goto case 0x07;
							break;
					}
				}
				return (short)(on ? AUDV * 1092 : 0);
			}

			public void SyncState(Serializer ser)
			{
				ser.Sync("AUDC", ref AUDC);
				ser.Sync("AUDF", ref AUDF);
				ser.Sync("AUDV", ref AUDV);
				ser.Sync("sr1", ref sr1);
				ser.Sync("sr3", ref sr3);
				ser.Sync("sr4", ref sr4);
				ser.Sync("sr5", ref sr5);
				ser.Sync("freqcnt", ref freqcnt);
				ser.Sync("on", ref on);
			}
		}

		public audio[] AUD = { new audio(), new audio() };

		public void GetSamples(short[] samples)
		{
			short[] moreSamples = new short[523];
			for (int i = 0; i < moreSamples.Length; i++)
			{
				for (int snd = 0; snd < 2; snd++)
				{
					moreSamples[i] += AUD[snd].cycle();
					//moreSamples[i] += (short)(((AUD[snd].sr4 & 0x08) != 0) ? AUD[snd].AUDV * 1092 : 0);
				}
				/*if (++freqDiv == (audioFreqDiv * 2))
				{
					freqDiv = 0;
					myP4 = (byte)(((myP4 & 0x0f) != 0) ? ((myP4 << 1) | ((((myP4 & 0x08) != 0) ? 1 : 0) ^ (((myP4 & 0x04) != 0) ? 1 : 0))) : 1);
				}

				moreSamples[i] = (short)(((myP4 & 0x08) != 0) ? 32767 : 0);
				*/
			}

			for (int i = 0; i < samples.Length / 2; i++)
			{
				samples[i * 2] = moreSamples[(int)(((double)moreSamples.Length / (double)(samples.Length / 2)) * i)];
				samples[i * 2 + 1] = samples[i * 2];
			}
		}
		public void DiscardSamples() { }
		public int MaxVolume { get; set; }


		// Execute TIA cycles
		public void execute(int cycles)
		{
			// Still ignoring cycles...

			// Assume we're on the left side of the screen for now
			bool rightSide = false;

			// ---- Things that happen only in the drawing section ----
			// TODO: Remove this magic number (17). It depends on the HMOVE
			if ((hsyncCnt / 4) >= (hmove.lateHBlankReset ? 19 : 17))
			{
				// TODO: Remove this magic number
				if ((hsyncCnt / 4) >= 37)
				{
					rightSide = true;
				}

				// The bit number of the PF data which we want
				int pfBit = ((hsyncCnt / 4) - 17) % 20;

				// Create the mask for the bit we want
				// Note that bits are arranged 0 1 2 3 4 .. 19
				int pfMask = 1 << (20 - 1 - pfBit);

				// Reverse the mask if on the right and playfield is reflected
				if (rightSide && playField.reflect)
				{
					pfMask = reverseBits(pfMask, 20);
				}

				// Calculate collisions
				byte collisions = 0x00;

				if ((playField.grp & pfMask) != 0)
				{
					collisions |= CXPF;
				}


				// ---- Player 0 ----
				collisions |= (player0.tick() ? CXP0 : (byte)0x00);

				// ---- Missile 0 ----
				collisions |= (player0.missile.tick() ? CXM0 : (byte)0x00);

				// ---- Player 1 ----
				collisions |= (player1.tick() ? CXP1 : (byte)0x00);

				// ---- Missile 0 ----
				collisions |= (player1.missile.tick() ? CXM1 : (byte)0x00);

				// ---- Ball ----
				collisions |= (ball.tick() ? CXBL : (byte)0x00);


				// Pick the pixel color from collisions
				uint pixelColor = 0x000000;
				if (core.CoreInputComm.Atari2600_ShowBG)
				{
					pixelColor = palette[playField.bkColor];
				}

				if ((collisions & CXPF) != 0 && core.CoreInputComm.Atari2600_ShowPF)
				{
					if (playField.score)
					{
						if (!rightSide)
						{
							pixelColor = palette[player0.color];
						}
						else
						{
							pixelColor = palette[player1.color];
						}
					}
					else
					{
						pixelColor = palette[playField.pfColor];
					}
				}

				if ((collisions & CXBL) != 0)
				{
					ball.collisions |= collisions;
					if (core.CoreInputComm.Atari2600_ShowBall) 
					{
						pixelColor = palette[playField.pfColor];
					}
				}

				if ((collisions & CXM1) != 0)
				{
					player1.missile.collisions |= collisions;
					if (core.CoreInputComm.Atari2600_ShowMissle2)
					{
						pixelColor = palette[player1.color];
					}
				}

				if ((collisions & CXP1) != 0)
				{
					player1.collisions |= collisions;
					if (core.CoreInputComm.Atari2600_ShowPlayer2)
					{
						pixelColor = palette[player1.color];
					}
				}

				if ((collisions & CXM0) != 0)
				{
					player0.missile.collisions |= collisions;
					if (core.CoreInputComm.Atari2600_ShowMissle1)
					{
						pixelColor = palette[player0.color];
					}
				}

				if ((collisions & CXP0) != 0)
				{
					player0.collisions |= collisions;
					if (core.CoreInputComm.Atari2600_ShowPlayer1)
					{
						pixelColor = palette[player0.color];
					}
				}

				if (playField.priority && (collisions & CXPF) != 0 && core.CoreInputComm.Atari2600_ShowPF)
				{
					if (playField.score)
					{
						if (!rightSide)
						{
							pixelColor = palette[player0.color];
						}
						else
						{
							pixelColor = palette[player1.color];
						}
					}
					else
					{
						pixelColor = palette[playField.pfColor];
					}
				}

				// Handle vblank
				if (vblankEnabled)
				{
					pixelColor = 0x000000;
				}

				// Add the pixel to the scanline
				// TODO: Remove this magic number (68)
				scanline[hsyncCnt - 68] = pixelColor;
			}


			// ---- Things that happen every time ----

			// Handle HMOVE
			if (hmove.hmoveEnabled)
			{
				// On the first time, set the latches and counters
				if (hmove.hmoveJustStarted)
				{
					hmove.player0Latch = true;
					hmove.player0Cnt = 0;

					hmove.missile0Latch = true;
					hmove.missile0Cnt = 0;

					hmove.player1Latch = true;
					hmove.player1Cnt = 0;

					hmove.missile1Latch = true;
					hmove.missile1Cnt = 0;

					hmove.ballLatch = true;
					hmove.ballCnt = 0;

					hmove.hmoveCnt = 0;

					//hmove.hmoveCnt++;
					hmove.hmoveJustStarted = false;
					hmove.lateHBlankReset = true;
					hmove.decCntEnabled = false;
				}

				if (hmove.decCntEnabled)
				{
					// Actually do stuff only evey 4 pulses
					if (hmove.hmoveCnt == 0)
					{
						// If the latch is still set
						if (hmove.player0Latch)
						{
							// If the move counter still has a bit in common with the HM register
							if (((15 - hmove.player0Cnt) ^ ((player0.HM & 0x07) | ((~(player0.HM & 0x08)) & 0x08))) != 0x0F)
							{
								// "Clock-Stuffing"
								player0.tick();

								// Increase by 1, max of 15
								hmove.player0Cnt++;
								hmove.player0Cnt %= 16;
							}
							else
							{
								hmove.player0Latch = false;
							}
						}

						if (hmove.missile0Latch)
						{
							if (hmove.missile0Cnt == 15)
							{ }

							// If the move counter still has a bit in common with the HM register
							if (((15 - hmove.missile0Cnt) ^ ((player0.missile.HM & 0x07) | ((~(player0.missile.HM & 0x08)) & 0x08))) != 0x0F)
							{
								// "Clock-Stuffing"
								player0.missile.tick();

								// Increase by 1, max of 15
								hmove.missile0Cnt++;
								hmove.missile0Cnt %= 16;
							}
							else
							{
								hmove.missile0Latch = false;
								hmove.missile0Cnt = 0;
							}
						}

						if (hmove.player1Latch)
						{
							// If the move counter still has a bit in common with the HM register
							if (((15 - hmove.player1Cnt) ^ ((player1.HM & 0x07) | ((~(player1.HM & 0x08)) & 0x08))) != 0x0F)
							{
								// "Clock-Stuffing"
								player1.tick();

								// Increase by 1, max of 15
								hmove.player1Cnt++;
								hmove.player1Cnt %= 16;
							}
							else
							{
								hmove.player1Latch = false;
							}
						}

						if (hmove.missile1Latch)
						{
							// If the move counter still has a bit in common with the HM register
							if (((15 - hmove.missile1Cnt) ^ ((player1.missile.HM & 0x07) | ((~(player1.missile.HM & 0x08)) & 0x08))) != 0x0F)
							{
								// "Clock-Stuffing"
								player1.missile.tick();

								// Increase by 1, max of 15
								hmove.missile1Cnt++;
								hmove.missile1Cnt %= 16;
							}
							else
							{
								hmove.missile1Latch = false;
							}
						}

						if (hmove.ballLatch)
						{
							// If the move counter still has a bit in common with the HM register
							if (((15 - hmove.ballCnt) ^ ((ball.HM & 0x07) | ((~(ball.HM & 0x08)) & 0x08))) != 0x0F)
							{
								// "Clock-Stuffing"
								ball.tick();

								// Increase by 1, max of 15
								hmove.ballCnt++;
								hmove.ballCnt %= 16;
							}
							else
							{
								hmove.ballLatch = false;
							}
						}

						if (!hmove.player0Latch && !hmove.player1Latch && !hmove.ballLatch && !hmove.missile0Latch && !hmove.missile1Latch)
						{
							hmove.hmoveEnabled = false;
							hmove.decCntEnabled = false;
							hmove.hmoveDelayCnt = 0;
						}
					}
					//hmove.hmoveJustStarted = false;
					hmove.hmoveCnt++;
					hmove.hmoveCnt %= 4;
				}

				if (hmove.hmoveDelayCnt < 6)
				{
					hmove.hmoveDelayCnt++;
				}

				if (hmove.hmoveDelayCnt == 6)
				{
					hmove.hmoveDelayCnt++;
					hmove.hmoveCnt = 0;
					hmove.decCntEnabled = true;
				}
			}

			

			// Increment the hsync counter
			hsyncCnt++;
			hsyncCnt %= 228;

			// End of the line? Add it to the buffer!
			if (hsyncCnt == 0)
			{
				hmove.lateHBlankReset = false;
				scanlinesBuffer.Add(scanline);
				scanline = new uint[160];
			}
			if (scanlinesBuffer.Count >= 1024)
			{
				/* if a rom never toggles vsync, FrameAdvance() will hang while consuming
				 * huge amounts of ram.  this is most certainly due to emulation defects
				 * that need to be fixed; but it's preferable to not crash the emulator
				 * in such situations
				 */
				outputFrame();
				scanlinesBuffer.Clear();
				frameComplete = true;
			}
		}

		// TODO: Remove the magic numbers from this function to allow for a variable height screen
		public void outputFrame()
		{
			for (int row = 0; row < 262; row++)
			{
				for (int col = 0; col < 320; col++)
				{
					if (scanlinesBuffer.Count > row)
					{
						frameBuffer[row * 320 + col] = (int)(scanlinesBuffer[row][col / 2] | 0xFF000000);
					}
					else
					{
						frameBuffer[row * 320 + col] = unchecked((int)0xFF000000);
					}
				}
			}
		}

		public byte ReadMemory(ushort addr)
		{
			ushort maskedAddr = (ushort)(addr & 0x000F);
			if (maskedAddr == 0x00) // CXM0P
			{
				return (byte)((((player0.missile.collisions & CXP1) != 0) ? 0x80 : 0x00) | (((player0.missile.collisions & CXP0) != 0) ? 0x40 : 0x00));
			}
			else if (maskedAddr == 0x01) // CXM1P
			{
				return (byte)((((player1.missile.collisions & CXP0) != 0) ? 0x80 : 0x00) | (((player1.missile.collisions & CXP1) != 0) ? 0x40 : 0x00));
			}
			else if (maskedAddr == 0x02) // CXP0FB
			{
				return (byte)((((player0.collisions & CXPF) != 0) ? 0x80 : 0x00) | (((player0.collisions & CXBL) != 0) ? 0x40 : 0x00));
			}
			else if (maskedAddr == 0x03) // CXP1FB
			{
				return (byte)((((player1.collisions & CXPF) != 0) ? 0x80 : 0x00) | (((player1.collisions & CXBL) != 0) ? 0x40 : 0x00));
			}
			else if (maskedAddr == 0x04) // CXM0FB
			{
				return (byte)((((player0.missile.collisions & CXPF) != 0) ? 0x80 : 0x00) | (((player0.missile.collisions & CXBL) != 0) ? 0x40 : 0x00));
			}
			else if (maskedAddr == 0x05) // CXM1FB
			{
				return (byte)((((player1.missile.collisions & CXPF) != 0) ? 0x80 : 0x00) | (((player1.missile.collisions & CXBL) != 0) ? 0x40 : 0x00));
			}
			else if (maskedAddr == 0x06) // CXBLPF
			{
				return (byte)(((ball.collisions & CXPF) != 0) ? 0x80 : 0x00);
			}
			else if (maskedAddr == 0x07) // CXPPMM
			{
				return (byte)((((player0.collisions & CXP1) != 0) ? 0x80 : 0x00) | (((player0.missile.collisions & CXM1) != 0) ? 0x40 : 0x00));
			}
			else if (maskedAddr == 0x08) // INPT0
			{
				// Changing the hard coded value will change the paddle position. The range seems to be roughly 0-56000 according to values from stella
				// 6105 roughly centers the paddle in Breakout
				if (capCharging && core.cpu.TotalExecutedCycles - capChargeStart >= 6105)
				{
					return 0x80;
				}
				else
				{
					return 0x00;
				}
			}
			else if (maskedAddr == 0x0C) // INPT4
			{
				return (byte)((core.ReadControls1() & 0x08) != 0 ? 0x80 : 0x00);
			}
			else if (maskedAddr == 0x0D) // INPT5
			{
				return (byte)((core.ReadControls2() & 0x08) != 0 ? 0x80 : 0x00);
			}

			return 0x00;
		}

		public void WriteMemory(ushort addr, byte value)
		{
			ushort maskedAddr = (ushort)(addr & 0x3f);

			if (maskedAddr == 0x00) // VSYNC
			{
				if ((value & 0x02) != 0)
				{
					// Frame is complete, output to buffer
					vsyncEnabled = true;
				}
				else if (vsyncEnabled)
				{
					// When VSYNC is disabled, this will be the first line of the new frame

					// write to frame buffer
					outputFrame();
					// Clear all from last frame
					scanlinesBuffer.Clear();
					//Frame is done
					frameComplete = true;

					vsyncEnabled = false;

					// Do not reset hsync, since we're on the first line of the new frame
					//hsyncCnt = 0;
				}
			}
			else if (maskedAddr == 0x01) // VBLANK
			{
				vblankEnabled = (value & 0x02) != 0;
				capCharging = (value & 0x80) == 0;
				if ((value & 0x80) == 0)
				{
					capChargeStart = core.cpu.TotalExecutedCycles;
				}
			}
			else if (maskedAddr == 0x02) // WSYNC
			{
				int count = 0;
				while (hsyncCnt > 0)
				{
					count++;
					execute(1);

					// Add a cycle to the cpu every 3 TIA clocks (corrects timer error in M6532)
					if (count % 3 == 0)
					{
						core.m6532.timer.tick();
					}
				}
			}
			else if (maskedAddr == 0x04) // NUSIZ0
			{
				player0.nusiz = (byte)(value & 0x37);
				player0.missile.size = (byte)((value & 0x30) >> 4);
				player0.missile.number = (byte)(value & 0x07);
			}
			else if (maskedAddr == 0x05) // NUSIZ1
			{
				player1.nusiz = (byte)(value & 0x37);
				player1.missile.size = (byte)((value & 0x30) >> 4);
				player1.missile.number = (byte)(value & 0x07);
			}
			else if (maskedAddr == 0x06) // COLUP0
			{
				player0.color = (byte)(value & 0xFE);
			}
			else if (maskedAddr == 0x07) // COLUP1
			{
				player1.color = (byte)(value & 0xFE);
			}
			else if (maskedAddr == 0x08) // COLUPF
			{
				playField.pfColor = (byte)(value & 0xFE);
			}
			else if (maskedAddr == 0x09) // COLUBK
			{
				playField.bkColor = (byte)(value & 0xFE);
			}
			else if (maskedAddr == 0x0A) // CTRLPF
			{
				playField.reflect = (value & 0x01) != 0;
				playField.priority = (value & 0x04) != 0;

				ball.size = (byte)((value & 0x30) >> 4);
			}
			else if (maskedAddr == 0x0B) // REFP0
			{
				player0.reflect = ((value & 0x08) != 0);
			}
			else if (maskedAddr == 0x0C) // REFP1
			{
				player1.reflect = ((value & 0x08) != 0);
			}
			else if (maskedAddr == 0x0D) // PF0
			{
				playField.grp = (UInt32)((playField.grp & 0x0FFFF) + ((reverseBits(value,8) & 0x0F) << 16));
			}
			else if (maskedAddr == 0x0E) // PF1
			{
				playField.grp = (UInt32)((playField.grp & 0xF00FF) + (value << 8));
			}
			else if (maskedAddr == 0x0F) // PF2
			{
				playField.grp = (UInt32)((playField.grp & 0xFFF00) + reverseBits(value,8));
			}
			else if (maskedAddr == 0x10) // RESP0
			{
				player0.resetCnt = 0;
			}
			else if (maskedAddr == 0x11) // RESP1
			{
				player1.resetCnt = 0;
			}
			else if (maskedAddr == 0x12) // RESM0
			{
				player0.missile.hPosCnt = (byte)(hsyncCnt < 68 ? 160 - 2 : 160 - 4);
			}
			else if (maskedAddr == 0x13) // RESM1
			{
				player1.missile.hPosCnt = (byte)(hsyncCnt < 68 ? 160 - 2 : 160 - 4);
			}
			else if (maskedAddr == 0x14) // RESBL
			{
				ball.hPosCnt = (byte)(hsyncCnt < 68 ? 160 - 2 : 160 - 4);
			}
			else if (maskedAddr == 0x15) // AUDC0
			{
				AUD[0].AUDC = (byte)(value & 15);
			}
			else if (maskedAddr == 0x16) // AUDC1
			{
				AUD[1].AUDC = (byte)(value & 15);
			}
			else if (maskedAddr == 0x17) // AUDF0
			{
				AUD[0].AUDF = (byte)((value & 31) + 1);
			}
			else if (maskedAddr == 0x18) // AUDF1
			{
				AUD[1].AUDF = (byte)((value & 31) + 1);
			}
			else if (maskedAddr == 0x19) // AUDV0
			{
				AUD[0].AUDV = (byte)(value & 15);
			}
			else if (maskedAddr == 0x1A) // AUDV1
			{
				AUD[1].AUDV = (byte)(value & 15);
			}
			else if (maskedAddr == 0x1B) // GRP0
			{
				player0.grp = value;
				player1.dgrp = player1.grp;
			}
			else if (maskedAddr == 0x1C) // GRP1
			{
				player1.grp = value;
				player0.dgrp = player0.grp;

				// TODO: Find a game that uses this functionality and test it
				ball.denabled = ball.enabled;
			}
			else if (maskedAddr == 0x1D) // ENAM0
			{
				player0.missile.enabled = (value & 0x02) != 0;
			}
			else if (maskedAddr == 0x1E) // ENAM1
			{
				player1.missile.enabled = (value & 0x02) != 0;
			}
			else if (maskedAddr == 0x1F) // ENABL
			{
				ball.enabled = (value & 0x02) != 0;
			}
			else if (maskedAddr == 0x20) // HMP0
			{
				player0.HM = (byte)((value & 0xF0) >> 4);
			}
			else if (maskedAddr == 0x21) // HMP1
			{
				player1.HM = (byte)((value & 0xF0) >> 4);
			}
			else if (maskedAddr == 0x22) // HMM0
			{
				player0.missile.HM = (byte)((value & 0xF0) >> 4);
			}
			else if (maskedAddr == 0x23) // HMM1
			{
				player1.missile.HM = (byte)((value & 0xF0) >> 4);
			}
			else if (maskedAddr == 0x24) // HMBL
			{
				ball.HM = (byte)((value & 0xF0) >> 4);
			}
			else if (maskedAddr == 0x25) // VDELP0
			{
				player0.delay = (value & 0x01) != 0;
			}
			else if (maskedAddr == 0x26) // VDELP1
			{
				player1.delay = (value & 0x01) != 0;
			}
			else if (maskedAddr == 0x27) // VDELBL
			{
				ball.delay = (value & 0x01) != 0;
			}
			else if (maskedAddr == 0x28) // RESMP0
			{
				player0.missile.resetToPlayer = (value & 0x02) != 0;
			}
			else if (maskedAddr == 0x29) // RESMP1
			{
				player1.missile.resetToPlayer = (value & 0x02) != 0;
			}
			else if (maskedAddr == 0x2A) // HMOVE
			{
				hmove.hmoveEnabled = true;
				hmove.hmoveJustStarted = true;
				hmove.hmoveDelayCnt = 0;
			}
			else if (maskedAddr == 0x2B) // HMCLR
			{
				player0.HM = 0;
				player0.missile.HM = 0;
				player1.HM = 0;
				player1.missile.HM = 0;
				ball.HM = 0;
			}
			else if (maskedAddr == 0x2C) // CXCLR
			{
				player0.collisions = 0;
				player0.missile.collisions = 0;
				player1.collisions = 0;
				player1.missile.collisions = 0;
				ball.collisions = 0;
			}
		}

		static int reverseBits(int value, int bits)
		{
			int result = 0;
			for (int i = 0; i < bits; i++)
			{
				result = (result << 1) | ((value >> i) & 0x01);
			}
			return result;
		}

		public void SyncState(Serializer ser)
		{
			ball.SyncState(ser);
			hmove.SyncState(ser);
			ser.Sync("hsyncCnt", ref hsyncCnt);
			player0.SyncState(ser);
			player1.SyncState(ser);
			playField.SyncState(ser);
			//ser.Sync("scanline", ref scanline);
		}
	}
}