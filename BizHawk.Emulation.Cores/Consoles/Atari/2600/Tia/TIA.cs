using System;
using System.Collections.Generic;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	// Emulates the TIA
	public partial class TIA : IVideoProvider, ISoundProvider
	{
		private const byte CXP0 = 0x01;
		private const byte CXP1 = 0x02;
		private const byte CXM0 = 0x04;
		private const byte CXM1 = 0x08;
		private const byte CXPF = 0x10;
		private const byte CXBL = 0x20;

		private readonly Atari2600 _core;
		private readonly List<uint[]> _scanlinesBuffer = new List<uint[]>();
		private readonly uint[] _palette = new uint[]
		{
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

		private byte _hsyncCnt;
		private int _capChargeStart;
		private bool _capCharging;
		private bool _vblankEnabled;
		private bool _vsyncEnabled;
		private uint[] _scanline = new uint[160];

		private PlayerData _player0;
		private PlayerData _player1;
		private PlayfieldData _playField;
		private HMoveData _hmove;
		private BallData _ball;

		public int[] FrameBuffer = new int[320 * 262];
		public Audio[] AUD = { new Audio(), new Audio() };

		public TIA(Atari2600 core)
		{
			_core = core;
			_player0.ScanCnt = 8;
			_player1.ScanCnt = 8;
		}

		public void Reset()
		{
			_hsyncCnt = 0;
			_capChargeStart = 0;
			_capCharging = false;
			_vblankEnabled = false;
			_vsyncEnabled = false;
			_scanline = new uint[160];

			_player0 = new PlayerData();
			_player1 = new PlayerData();
			_playField = new PlayfieldData();
			_hmove = new HMoveData();
			_ball = new BallData();

			_player0.ScanCnt = 8;
			_player1.ScanCnt = 8;
		}

		public bool FrameComplete { get; set; }
		public int MaxVolume { get; set; }

		public int VirtualWidth
		{
			get { return 320; }
		}

		public int BufferWidth
		{
			get { return 320; }
		}

		public int BufferHeight
		{
			get { return 262; }
		}

		public int BackgroundColor
		{
			get { return 0; }
		}

		public int[] GetVideoBuffer()
		{
			return FrameBuffer;
		}

		// Execute TIA cycles
		public void Execute(int cycles)
		{
			// Still ignoring cycles...

			// Assume we're on the left side of the screen for now
			var rightSide = false;

			// ---- Things that happen only in the drawing section ----
			// TODO: Remove this magic number (17). It depends on the HMOVE
			if ((_hsyncCnt / 4) >= (_hmove.LateHBlankReset ? 19 : 17))
			{
				// TODO: Remove this magic number
				if ((_hsyncCnt / 4) >= 37)
				{
					rightSide = true;
				}

				// The bit number of the PF data which we want
				int pfBit = ((_hsyncCnt / 4) - 17) % 20;

				// Create the mask for the bit we want
				// Note that bits are arranged 0 1 2 3 4 .. 19
				int pfMask = 1 << (20 - 1 - pfBit);

				// Reverse the mask if on the right and playfield is reflected
				if (rightSide && _playField.Reflect)
				{
					pfMask = ReverseBits(pfMask, 20);
				}

				// Calculate collisions
				byte collisions = 0x00;

				if ((_playField.Grp & pfMask) != 0)
				{
					collisions |= CXPF;
				}


				// ---- Player 0 ----
				collisions |= _player0.Tick() ? CXP0 : (byte)0x00;

				// ---- Missile 0 ----
				collisions |= _player0.Missile.Tick() ? CXM0 : (byte)0x00;

				// ---- Player 1 ----
				collisions |= _player1.Tick() ? CXP1 : (byte)0x00;

				// ---- Missile 0 ----
				collisions |= _player1.Missile.Tick() ? CXM1 : (byte)0x00;

				// ---- Ball ----
				collisions |= _ball.Tick() ? CXBL : (byte)0x00;


				// Pick the pixel color from collisions
				uint pixelColor = 0x000000;
				if (_core.Settings.ShowBG)
				{
					pixelColor = _palette[_playField.BkColor];
				}

				if ((collisions & CXPF) != 0 && _core.Settings.ShowPlayfield)
				{
					if (_playField.Score)
					{
						if (!rightSide)
						{
							pixelColor = _palette[_player0.Color];
						}
						else
						{
							pixelColor = _palette[_player1.Color];
						}
					}
					else
					{
						pixelColor = _palette[_playField.PfColor];
					}
				}

				if ((collisions & CXBL) != 0)
				{
					_ball.Collisions |= collisions;
					if (_core.Settings.ShowBall) 
					{
						pixelColor = _palette[_playField.PfColor];
					}
				}

				if ((collisions & CXM1) != 0)
				{
					_player1.Missile.Collisions |= collisions;
					if (_core.Settings.ShowMissle2)
					{
						pixelColor = _palette[_player1.Color];
					}
				}

				if ((collisions & CXP1) != 0)
				{
					_player1.Collisions |= collisions;
					if (_core.Settings.ShowPlayer2)
					{
						pixelColor = _palette[_player1.Color];
					}
				}

				if ((collisions & CXM0) != 0)
				{
					_player0.Missile.Collisions |= collisions;
					if (_core.Settings.ShowMissle1)
					{
						pixelColor = _palette[_player0.Color];
					}
				}

				if ((collisions & CXP0) != 0)
				{
					_player0.Collisions |= collisions;
					if (_core.Settings.ShowPlayer1)
					{
						pixelColor = _palette[_player0.Color];
					}
				}

				if (_playField.Priority && (collisions & CXPF) != 0 && _core.Settings.ShowPlayfield)
				{
					if (_playField.Score)
					{
						pixelColor = !rightSide ? _palette[_player0.Color] : _palette[_player1.Color];
					}
					else
					{
						pixelColor = _palette[_playField.PfColor];
					}
				}

				// Handle vblank
				if (_vblankEnabled)
				{
					pixelColor = 0x000000;
				}

				// Add the pixel to the scanline
				// TODO: Remove this magic number (68)
				_scanline[_hsyncCnt - 68] = pixelColor;
			}

			// ---- Things that happen every time ----

			// Handle HMOVE
			if (_hmove.HMoveEnabled)
			{
				// On the first time, set the latches and counters
				if (_hmove.HMoveJustStarted)
				{
					_hmove.Player0Latch = true;
					_hmove.Player0Cnt = 0;

					_hmove.Missile0Latch = true;
					_hmove.Missile0Cnt = 0;

					_hmove.Player1Latch = true;
					_hmove.Player1Cnt = 0;

					_hmove.Missile1Latch = true;
					_hmove.Missile1Cnt = 0;

					_hmove.BallLatch = true;
					_hmove.BallCnt = 0;

					_hmove.HMoveCnt = 0;

					_hmove.HMoveJustStarted = false;
					_hmove.LateHBlankReset = true;
					_hmove.DecCntEnabled = false;
				}

				if (_hmove.DecCntEnabled)
				{
					// Actually do stuff only evey 4 pulses
					if (_hmove.HMoveCnt == 0)
					{
						// If the latch is still set
						if (_hmove.Player0Latch)
						{
							// If the move counter still has a bit in common with the HM register
							if (((15 - _hmove.Player0Cnt) ^ ((_player0.HM & 0x07) | ((~(_player0.HM & 0x08)) & 0x08))) != 0x0F)
							{
								// "Clock-Stuffing"
								_player0.Tick();

								// Increase by 1, max of 15
								_hmove.Player0Cnt++;
								_hmove.Player0Cnt %= 16;
							}
							else
							{
								_hmove.Player0Latch = false;
							}
						}

						if (_hmove.Missile0Latch)
						{
							if (_hmove.Missile0Cnt == 15)
							{ }

							// If the move counter still has a bit in common with the HM register
							if (((15 - _hmove.Missile0Cnt) ^ ((_player0.Missile.Hm & 0x07) | ((~(_player0.Missile.Hm & 0x08)) & 0x08))) != 0x0F)
							{
								// "Clock-Stuffing"
								_player0.Missile.Tick();

								// Increase by 1, max of 15
								_hmove.Missile0Cnt++;
								_hmove.Missile0Cnt %= 16;
							}
							else
							{
								_hmove.Missile0Latch = false;
								_hmove.Missile0Cnt = 0;
							}
						}

						if (_hmove.Player1Latch)
						{
							// If the move counter still has a bit in common with the HM register
							if (((15 - _hmove.Player1Cnt) ^ ((_player1.HM & 0x07) | ((~(_player1.HM & 0x08)) & 0x08))) != 0x0F)
							{
								// "Clock-Stuffing"
								_player1.Tick();

								// Increase by 1, max of 15
								_hmove.Player1Cnt++;
								_hmove.Player1Cnt %= 16;
							}
							else
							{
								_hmove.Player1Latch = false;
							}
						}

						if (_hmove.Missile1Latch)
						{
							// If the move counter still has a bit in common with the HM register
							if (((15 - _hmove.Missile1Cnt) ^ ((_player1.Missile.Hm & 0x07) | ((~(_player1.Missile.Hm & 0x08)) & 0x08))) != 0x0F)
							{
								// "Clock-Stuffing"
								_player1.Missile.Tick();

								// Increase by 1, max of 15
								_hmove.Missile1Cnt++;
								_hmove.Missile1Cnt %= 16;
							}
							else
							{
								_hmove.Missile1Latch = false;
							}
						}

						if (_hmove.BallLatch)
						{
							// If the move counter still has a bit in common with the HM register
							if (((15 - _hmove.BallCnt) ^ ((_ball.HM & 0x07) | ((~(_ball.HM & 0x08)) & 0x08))) != 0x0F)
							{
								// "Clock-Stuffing"
								_ball.Tick();

								// Increase by 1, max of 15
								_hmove.BallCnt++;
								_hmove.BallCnt %= 16;
							}
							else
							{
								_hmove.BallLatch = false;
							}
						}

						if (!_hmove.Player0Latch && !_hmove.Player1Latch && !_hmove.BallLatch && !_hmove.Missile0Latch && !_hmove.Missile1Latch)
						{
							_hmove.HMoveEnabled = false;
							_hmove.DecCntEnabled = false;
							_hmove.HMoveDelayCnt = 0;
						}
					}

					_hmove.HMoveCnt++;
					_hmove.HMoveCnt %= 4;
				}

				if (_hmove.HMoveDelayCnt < 6)
				{
					_hmove.HMoveDelayCnt++;
				}

				if (_hmove.HMoveDelayCnt == 6)
				{
					_hmove.HMoveDelayCnt++;
					_hmove.HMoveCnt = 0;
					_hmove.DecCntEnabled = true;
				}
			}

			// Increment the hsync counter
			_hsyncCnt++;
			_hsyncCnt %= 228;

			// End of the line? Add it to the buffer!
			if (_hsyncCnt == 0)
			{
				_hmove.LateHBlankReset = false;
				_scanlinesBuffer.Add(_scanline);
				_scanline = new uint[160];
			}

			if (_scanlinesBuffer.Count >= 1024)
			{
				/* if a rom never toggles vsync, FrameAdvance() will hang while consuming
				 * huge amounts of ram.  this is most certainly due to emulation defects
				 * that need to be fixed; but it's preferable to not crash the emulator
				 * in such situations
				 */
				OutputFrame();
				_scanlinesBuffer.Clear();
				FrameComplete = true;
			}
		}

		// TODO: Remove the magic numbers from this function to allow for a variable height screen
		public void OutputFrame()
		{
			for (int row = 0; row < 262; row++)
			{
				for (int col = 0; col < 320; col++)
				{
					if (_scanlinesBuffer.Count > row)
					{
						FrameBuffer[(row * 320) + col] = (int)(_scanlinesBuffer[row][col / 2] | 0xFF000000);
					}
					else
					{
						FrameBuffer[(row * 320) + col] = unchecked((int)0xFF000000);
					}
				}
			}
		}
		
		public byte ReadMemory(ushort addr, bool peek)
		{
			var maskedAddr = (ushort)(addr & 0x000F);
			if (maskedAddr == 0x00) // CXM0P
			{
				return (byte)((((_player0.Missile.Collisions & CXP1) != 0) ? 0x80 : 0x00) | (((_player0.Missile.Collisions & CXP0) != 0) ? 0x40 : 0x00));
			}
			
			if (maskedAddr == 0x01) // CXM1P
			{
				return (byte)((((_player1.Missile.Collisions & CXP0) != 0) ? 0x80 : 0x00) | (((_player1.Missile.Collisions & CXP1) != 0) ? 0x40 : 0x00));
			}
			
			if (maskedAddr == 0x02) // CXP0FB
			{
				return (byte)((((_player0.Collisions & CXPF) != 0) ? 0x80 : 0x00) | (((_player0.Collisions & CXBL) != 0) ? 0x40 : 0x00));
			}
			
			if (maskedAddr == 0x03) // CXP1FB
			{
				return (byte)((((_player1.Collisions & CXPF) != 0) ? 0x80 : 0x00) | (((_player1.Collisions & CXBL) != 0) ? 0x40 : 0x00));
			}
			
			if (maskedAddr == 0x04) // CXM0FB
			{
				return (byte)((((_player0.Missile.Collisions & CXPF) != 0) ? 0x80 : 0x00) | (((_player0.Missile.Collisions & CXBL) != 0) ? 0x40 : 0x00));
			}
			
			if (maskedAddr == 0x05) // CXM1FB
			{
				return (byte)((((_player1.Missile.Collisions & CXPF) != 0) ? 0x80 : 0x00) | (((_player1.Missile.Collisions & CXBL) != 0) ? 0x40 : 0x00));
			}
			
			if (maskedAddr == 0x06) // CXBLPF
			{
				return (byte)(((_ball.Collisions & CXPF) != 0) ? 0x80 : 0x00);
			}
			
			if (maskedAddr == 0x07) // CXPPMM
			{
				return (byte)((((_player0.Collisions & CXP1) != 0) ? 0x80 : 0x00) | (((_player0.Missile.Collisions & CXM1) != 0) ? 0x40 : 0x00));
			}
			
			if (maskedAddr == 0x08) // INPT0
			{
				// Changing the hard coded value will change the paddle position. The range seems to be roughly 0-56000 according to values from stella
				// 6105 roughly centers the paddle in Breakout
				if (_capCharging && _core.Cpu.TotalExecutedCycles - _capChargeStart >= 6105)
				{
					return 0x80;
				}

				return 0x00;
			}
			
			if (maskedAddr == 0x0C) // INPT4
			{
				return (byte)((_core.ReadControls1(peek) & 0x08) != 0 ? 0x80 : 0x00);
			}
			
			if (maskedAddr == 0x0D) // INPT5
			{
				return (byte)((_core.ReadControls2(peek) & 0x08) != 0 ? 0x80 : 0x00);
			}

			return 0x00;
		}

		public void WriteMemory(ushort addr, byte value)
		{
			var maskedAddr = (ushort)(addr & 0x3f);

			if (maskedAddr == 0x00) // VSYNC
			{
				if ((value & 0x02) != 0)
				{
					// Frame is complete, output to buffer
					_vsyncEnabled = true;
				}
				else if (_vsyncEnabled)
				{
					// When VSYNC is disabled, this will be the first line of the new frame

					// write to frame buffer
					OutputFrame();

					// Clear all from last frame
					_scanlinesBuffer.Clear();

					// Frame is done
					FrameComplete = true;

					_vsyncEnabled = false;

					// Do not reset hsync, since we're on the first line of the new frame
					// hsyncCnt = 0;
				}
			}
			else if (maskedAddr == 0x01) // VBLANK
			{
				_vblankEnabled = (value & 0x02) != 0;
				_capCharging = (value & 0x80) == 0;
				if ((value & 0x80) == 0)
				{
					_capChargeStart = _core.Cpu.TotalExecutedCycles;
				}
			}
			else if (maskedAddr == 0x02) // WSYNC
			{
				int count = 0;
				while (_hsyncCnt > 0)
				{
					count++;
					Execute(1);

					// Add a cycle to the cpu every 3 TIA clocks (corrects timer error in M6532)
					if (count % 3 == 0)
					{
						_core.M6532.Timer.Tick();
					}
				}
			}
			else if (maskedAddr == 0x04) // NUSIZ0
			{
				_player0.Nusiz = (byte)(value & 0x37);
				_player0.Missile.Size = (byte)((value & 0x30) >> 4);
				_player0.Missile.Number = (byte)(value & 0x07);
			}
			else if (maskedAddr == 0x05) // NUSIZ1
			{
				_player1.Nusiz = (byte)(value & 0x37);
				_player1.Missile.Size = (byte)((value & 0x30) >> 4);
				_player1.Missile.Number = (byte)(value & 0x07);
			}
			else if (maskedAddr == 0x06) // COLUP0
			{
				_player0.Color = (byte)(value & 0xFE);
			}
			else if (maskedAddr == 0x07) // COLUP1
			{
				_player1.Color = (byte)(value & 0xFE);
			}
			else if (maskedAddr == 0x08) // COLUPF
			{
				_playField.PfColor = (byte)(value & 0xFE);
			}
			else if (maskedAddr == 0x09) // COLUBK
			{
				_playField.BkColor = (byte)(value & 0xFE);
			}
			else if (maskedAddr == 0x0A) // CTRLPF
			{
				_playField.Reflect = (value & 0x01) != 0;
				_playField.Priority = (value & 0x04) != 0;

				_ball.Size = (byte)((value & 0x30) >> 4);
			}
			else if (maskedAddr == 0x0B) // REFP0
			{
				_player0.Reflect = (value & 0x08) != 0;
			}
			else if (maskedAddr == 0x0C) // REFP1
			{
				_player1.Reflect = (value & 0x08) != 0;
			}
			else if (maskedAddr == 0x0D) // PF0
			{
				_playField.Grp = (uint)((_playField.Grp & 0x0FFFF) + ((ReverseBits(value, 8) & 0x0F) << 16));
			}
			else if (maskedAddr == 0x0E) // PF1
			{
				_playField.Grp = (uint)((_playField.Grp & 0xF00FF) + (value << 8));
			}
			else if (maskedAddr == 0x0F) // PF2
			{
				_playField.Grp = (uint)((_playField.Grp & 0xFFF00) + ReverseBits(value, 8));
			}
			else if (maskedAddr == 0x10) // RESP0
			{
				// Borrowed from EMU7800. Apparently resetting between 68 and 76 has strange results. 
				if (_hsyncCnt < 69)
				{
					_player0.HPosCnt = 0;
					_player0.ResetCnt = 0;
					_player0.Reset = true;
				}
				else if (_hsyncCnt == 69)
				{
					_player0.ResetCnt = 3;
				}
				else if (_hsyncCnt == 72)
				{
					_player0.ResetCnt = 2;
				}
				else if (_hsyncCnt == 75)
				{
					_player0.ResetCnt = 1;
				}
				else
				{
					_player0.ResetCnt = 0;
				}
			}
			else if (maskedAddr == 0x11) // RESP1
			{
				// Borrowed from EMU7800. Apparently resetting between 68 and 76 has strange results. 
				// This fixes some graphic glitches with Frostbite
				if (_hsyncCnt < 69)
				{
					_player1.HPosCnt = 0;
					_player1.ResetCnt = 0;
					_player1.Reset = true;
				}
				else if (_hsyncCnt == 69)
				{
					_player1.ResetCnt = 3;
				}
				else if (_hsyncCnt == 72)
				{
					_player1.ResetCnt = 2;
				}
				else if (_hsyncCnt == 75)
				{
					_player1.ResetCnt = 1;
				}
				else
				{
					_player1.ResetCnt = 0;
				}
			}
			else if (maskedAddr == 0x12) // RESM0
			{
				_player0.Missile.HPosCnt = (byte)(_hsyncCnt < 68 ? 160 - 2 : 160 - 4);
			}
			else if (maskedAddr == 0x13) // RESM1
			{
				_player1.Missile.HPosCnt = (byte)(_hsyncCnt < 68 ? 160 - 2 : 160 - 4);
			}
			else if (maskedAddr == 0x14) // RESBL
			{
				_ball.HPosCnt = (byte)(_hsyncCnt < 68 ? 160 - 2 : 160 - 4);
			}
			else if (maskedAddr == 0x15) // AUDC0
			{
				WriteAudio(0, AudioRegister.AUDC, (byte)(value & 15));
			}
			else if (maskedAddr == 0x16) // AUDC1
			{
				WriteAudio(1, AudioRegister.AUDC, (byte)(value & 15));
			}
			else if (maskedAddr == 0x17) // AUDF0
			{
				WriteAudio(0, AudioRegister.AUDF, (byte)((value & 31) + 1));
			}
			else if (maskedAddr == 0x18) // AUDF1
			{
				WriteAudio(1, AudioRegister.AUDF, (byte)((value & 31) + 1));
			}
			else if (maskedAddr == 0x19) // AUDV0
			{
				WriteAudio(0, AudioRegister.AUDV, (byte)(value & 15));
			}
			else if (maskedAddr == 0x1A) // AUDV1
			{
				WriteAudio(1, AudioRegister.AUDV, (byte)(value & 15));
			}
			else if (maskedAddr == 0x1B) // GRP0
			{
				_player0.Grp = value;
				_player1.Dgrp = _player1.Grp;
			}
			else if (maskedAddr == 0x1C) // GRP1
			{
				_player1.Grp = value;
				_player0.Dgrp = _player0.Grp;

				// TODO: Find a game that uses this functionality and test it
				_ball.Denabled = _ball.Enabled;
			}
			else if (maskedAddr == 0x1D) // ENAM0
			{
				_player0.Missile.Enabled = (value & 0x02) != 0;
			}
			else if (maskedAddr == 0x1E) // ENAM1
			{
				_player1.Missile.Enabled = (value & 0x02) != 0;
			}
			else if (maskedAddr == 0x1F) // ENABL
			{
				_ball.Enabled = (value & 0x02) != 0;
			}
			else if (maskedAddr == 0x20) // HMP0
			{
				_player0.HM = (byte)((value & 0xF0) >> 4);
			}
			else if (maskedAddr == 0x21) // HMP1
			{
				_player1.HM = (byte)((value & 0xF0) >> 4);
			}
			else if (maskedAddr == 0x22) // HMM0
			{
				_player0.Missile.Hm = (byte)((value & 0xF0) >> 4);
			}
			else if (maskedAddr == 0x23) // HMM1
			{
				_player1.Missile.Hm = (byte)((value & 0xF0) >> 4);
			}
			else if (maskedAddr == 0x24) // HMBL
			{
				_ball.HM = (byte)((value & 0xF0) >> 4);
			}
			else if (maskedAddr == 0x25) // VDELP0
			{
				_player0.Delay = (value & 0x01) != 0;
			}
			else if (maskedAddr == 0x26) // VDELP1
			{
				_player1.Delay = (value & 0x01) != 0;
			}
			else if (maskedAddr == 0x27) // VDELBL
			{
				_ball.Delay = (value & 0x01) != 0;
			}
			else if (maskedAddr == 0x28) // RESMP0
			{
				_player0.Missile.ResetToPlayer = (value & 0x02) != 0;
			}
			else if (maskedAddr == 0x29) // RESMP1
			{
				_player1.Missile.ResetToPlayer = (value & 0x02) != 0;
			}
			else if (maskedAddr == 0x2A) // HMOVE
			{
				_hmove.HMoveEnabled = true;
				_hmove.HMoveJustStarted = true;
				_hmove.HMoveDelayCnt = 0;
			}
			else if (maskedAddr == 0x2B) // HMCLR
			{
				_player0.HM = 0;
				_player0.Missile.Hm = 0;
				_player1.HM = 0;
				_player1.Missile.Hm = 0;
				_ball.HM = 0;
			}
			else if (maskedAddr == 0x2C) // CXCLR
			{
				_player0.Collisions = 0;
				_player0.Missile.Collisions = 0;
				_player1.Collisions = 0;
				_player1.Missile.Collisions = 0;
				_ball.Collisions = 0;
			}
		}

		private static int ReverseBits(int value, int bits)
		{
			int result = 0;
			for (int i = 0; i < bits; i++)
			{
				result = (result << 1) | ((value >> i) & 0x01);
			}

			return result;
		}

		// =========================================================================
		// Audio bits
		// =========================================================================

		enum AudioRegister : byte { AUDC, AUDF, AUDV }
		struct QueuedCommand
		{
			public int Time;
			public byte Channel;
			public AudioRegister Register;
			public byte Value;
		}

		int frameStartCycles, frameEndCycles;
		Queue<QueuedCommand> commands = new Queue<QueuedCommand>(4096);
		
		public void BeginAudioFrame()
		{
			frameStartCycles = _core.Cpu.TotalExecutedCycles;
		}

		public void CompleteAudioFrame()
		{
			frameEndCycles = _core.Cpu.TotalExecutedCycles;
		}

		void WriteAudio(byte channel, AudioRegister register, byte value)
		{
			commands.Enqueue(new QueuedCommand { Channel = channel, Register = register, Value = value, Time = _core.Cpu.TotalExecutedCycles - frameStartCycles });
		}

		void ApplyAudioCommand(QueuedCommand cmd)
		{
			switch (cmd.Register)
			{
				case AudioRegister.AUDC: AUD[cmd.Channel].AUDC = cmd.Value; break;
				case AudioRegister.AUDF: AUD[cmd.Channel].AUDF = cmd.Value; break;
				case AudioRegister.AUDV: AUD[cmd.Channel].AUDV = cmd.Value; break;
			}
		}

		public void GetSamples(short[] samples)
		{
			var samples31khz = new short[((samples.Length / 2) * 31380) / 44100];

			int elapsedCycles = frameEndCycles - frameStartCycles;
			if (elapsedCycles == 0)
				elapsedCycles = 1; // better than diving by zero

			int start = 0;
			while (commands.Count > 0)
			{
				var cmd = commands.Dequeue();
				int pos = ((cmd.Time * samples31khz.Length) / elapsedCycles);
				pos = Math.Min(pos, samples31khz.Length); // sometimes the cpu timestamp of the write is > frameEndCycles
				GetSamplesImmediate(samples31khz, start, pos - start);
				start = pos;
				ApplyAudioCommand(cmd);
			}
			GetSamplesImmediate(samples31khz, start, samples31khz.Length - start);

			// convert from 31khz to 44khz
			for (int i = 0; i < samples.Length / 2; i++)
			{
				samples[i * 2] = samples31khz[(int)(((double)samples31khz.Length / (double)(samples.Length / 2)) * i)];
				samples[(i * 2) + 1] = samples[i * 2];
			}
		}

		public void GetSamplesImmediate(short[] samples, int start, int len)
		{
			for (int i = start; i < start + len; i++)
			{
				samples[i] += AUD[0].Cycle();
				samples[i] += AUD[1].Cycle();
			}
		}
	
		public void DiscardSamples() 
		{
			commands.Clear();
		}

		// =========================================================================
		
		public void SyncState(Serializer ser)
		{
			ser.BeginSection("TIA");
			_ball.SyncState(ser);
			_hmove.SyncState(ser);
			ser.Sync("hsyncCnt", ref _hsyncCnt);
			ser.BeginSection("Player0");
			_player0.SyncState(ser);
			ser.EndSection();
			ser.BeginSection("Player1");
			_player1.SyncState(ser);
			ser.EndSection();
			_playField.SyncState(ser);
			ser.EndSection();
		}
	}
}