using System.Numerics;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	// Emulates the TIA
	public partial class TIA : IVideoProvider, ISoundProvider
	{
		static TIA()
		{
			// add alpha to palette entries
			for (int i = 0; i < PALPalette.Length; i++)
			{
				PALPalette[i] |= unchecked((int)0xff000000);
			}

			for (int i = 0; i < NTSCPalette.Length; i++)
			{
				NTSCPalette[i] |= unchecked((int)0xff000000);
			}
		}

		public TIA(Atari2600 core, bool pal, bool secam)
		{
			_core = core;
			_player0.ScanCnt = 8;
			_player1.ScanCnt = 8;
			_pal = pal;

			SetSecam(secam);
			CalcFrameRate();

			_spf = _vsyncNum / (double)_vsyncDen > 55.0 ? 735 : 882;
		}

		// indicates to the core where a new frame is starting
		public bool New_Frame = false;

		private const int BackColor = unchecked((int)0xff000000);
		private const int ScreenWidth = 160;
		private const int MaxScreenHeight = 312;

		private const byte CXP0 = 0x01;
		private const byte CXP1 = 0x02;
		private const byte CXM0 = 0x04;
		private const byte CXM1 = 0x08;
		private const byte CXPF = 0x10;
		private const byte CXBL = 0x20;

		private readonly Atari2600 _core;

		// in all cases, the TIA has 228 clocks per scanline
		// the NTSC TIA has a clock rate of 3579575hz
		// the PAL/SECAM TIA has a clock rate of 3546894hz
		private readonly bool _pal;

		public int NominalNumScanlines => _pal ? 312 : 262;

		private readonly int[] _scanlinebuffer = new int[ScreenWidth * MaxScreenHeight];

		private int[] _palette;

		internal int BusState;

		private byte _pf0Update;
		private byte _pf1Update;
		private byte _pf2Update;
		private bool _pf0Updater;
		private bool _pf1Updater;
		private bool _pf2Updater;
		private byte _pf0DelayClock;
		private byte _pf1DelayClock;
		private byte _pf2DelayClock;
		private byte _pf0MaxDelay;
		private byte _pf1MaxDelay;
		private byte _pf2MaxDelay;

		private int _ctrlPFDelay;
		private byte _ctrlPFVal;

		private int _enam0Delay;
		private int _enam1Delay;
		private int _enambDelay;
		private bool _enam0Val;
		private bool _enam1Val;
		private bool _enambVal;

		private int _vblankDelay;
		private byte _vblankValue;

		private bool _p0Stuff;
		private bool _p1Stuff;
		private bool _m0Stuff;
		private bool _m1Stuff;
		private bool _bStuff;

		private int _hmp0Delay;
		private byte _hmp0Val;
		private int _hmp1Delay;
		private byte _hmp1Val;
		private int _hmm0Delay;
		private byte _hmm0Val;
		private int _hmm1Delay;
		private byte _hmm1Val;
		private int _hmbDelay;
		private byte _hmbVal;

		private bool _hmp0_no_tick;
		private bool _hmp1_no_tick;
		private bool _hmm0_no_tick;
		private bool _hmm1_no_tick;
		private bool _hmb_no_tick;

		private int _nusiz0Delay;
		private byte _nusiz0Val;
		private int _nusiz1Delay;
		private byte _nusiz1Val;

		private int _hmClrDelay;

		private int _prg0Delay;
		private int _prg1Delay;
		private byte _prg0Val;
		private byte _prg1Val;

		private bool _doTicks;
		private bool hmove_cnt_up;

		private byte _hsyncCnt;
		private long _capChargeStart;
		private bool _capCharging;
		private bool _vblankEnabled;
		private bool _vsyncEnabled;
		private int _currentScanLine;
		public int AudioClocks; // not savestated

		private PlayerData _player0;
		private PlayerData _player1;
		private PlayfieldData _playField;
		private HMoveData _hmove;
		private BallData _ball;

		private readonly Audio AUD =new Audio();

		// current audio register state used to sample correct positions in the scanline (clrclk 0 and 114)
		public readonly short[] LocalAudioCycles = new short[2000];

		private static int ReverseBits(int value, int bits)
		{
			int result = 0;
			for (int i = 0; i < bits; i++)
			{
				result = (result << 1) | ((value >> i) & 0x01);
			}

			return result;
		}

		private void CalcFrameRate()
		{
			// TODO when sound timing is made exact:
			// NTSC refclock is actually 315 / 88 mhz
			// 3546895
			int clockrate = _pal ? 3546895 : 3579545;
			int clocksperframe = 228 * NominalNumScanlines;
			int gcd = (int)BigInteger.GreatestCommonDivisor(clockrate, clocksperframe);
			_vsyncNum = clockrate / gcd;
			_vsyncDen = clocksperframe / gcd;
		}

		public void SetSecam(bool secam)
		{
			_palette = _pal ? secam ? SecamPalette : PALPalette : NTSCPalette;
		}

		public int CurrentScanLine => _currentScanLine;

		public bool IsVBlank => _vblankEnabled;

		public bool IsVSync => _vsyncEnabled;

		/// <summary>
		/// Gets or sets a count of lines emulated; incremented by the TIA but not used by it
		/// </summary>
		public int LineCount { get; set; }

		public void Reset()
		{
			_hsyncCnt = 0;
			_capChargeStart = 0;
			_capCharging = false;
			_vblankEnabled = false;
			_vblankDelay = 0;
			_vblankValue = 0;
			_vsyncEnabled = false;
			_currentScanLine = 0;
			AudioClocks = 0;

			BusState = 0;

			_pf0Update = 0;
			_pf1Update = 0;
			_pf2Update = 0;
			_pf0Updater = false;
			_pf1Updater = false;
			_pf2Updater = false;
			_pf0DelayClock = 0;
			_pf1DelayClock = 0;
			_pf2DelayClock = 0;
			_pf0MaxDelay = 0;
			_pf1MaxDelay = 0;
			_pf2MaxDelay = 0;

			_enam0Delay = 0;
			_enam1Delay = 0;
			_enambDelay = 0;
			_enam0Val = false;
			_enam1Val = false;
			_enambVal = false;

			_p0Stuff = false;
			_p1Stuff = false;
			_m0Stuff = false;
			_m1Stuff = false;
			_bStuff = false;

			_hmp0Delay = 0;
			_hmp0Val = 0;
			_hmp1Delay = 0;
			_hmp1Val = 0;
			_hmm0Delay = 0;
			_hmm0Val = 0;
			_hmm1Delay = 0;
			_hmm1Val = 0;
			_hmbDelay = 0;
			_hmbVal = 0;

			_nusiz0Delay = 0;
			_nusiz0Val = 0;
			_nusiz1Delay = 0;
			_nusiz1Val = 0;

			_prg0Delay = 0;
			_prg1Delay = 0;
			_prg0Val = 0;
			_prg1Val = 0;

			_doTicks = false;

			_player0 = new PlayerData();
			_player1 = new PlayerData();
			_playField = new PlayfieldData();
			_hmove = new HMoveData();
			_ball = new BallData();

			_player0.ScanCnt = 8;
			_player1.ScanCnt = 8;
		}

		// Execute TIA cycles
		public void Execute()
		{
			// Handle all of the Latch delays that occur in the TIA
			if (_vblankDelay > 0)
			{
				_vblankDelay++;
				if (_vblankDelay == 3)
				{
					_vblankEnabled = (_vblankValue & 0x02) != 0;
					_vblankDelay = 0;
				}
			}

			if (_ctrlPFDelay > 0)
			{
				_ctrlPFDelay++;
				if (_ctrlPFDelay == 2)
				{
					_playField.Reflect = (_ctrlPFVal & 0x01) != 0;
					_playField.Score = (_ctrlPFVal & 0x02) != 0;
					_playField.Priority = (_ctrlPFVal & 0x04) != 0;

					_ball.Size = (byte)((_ctrlPFVal & 0x30) >> 4);

					_ctrlPFDelay = 0;
				}
			}

			if (_pf0Updater)
			{
				_pf0DelayClock++;
				if (_pf0DelayClock > _pf0MaxDelay)
				{
					_playField.Grp = (uint)((_playField.Grp & 0x0FFFF) + ((ReverseBits(_pf0Update, 8) & 0x0F) << 16));
					_pf0Updater = false;
				}
			}

			if (_pf1Updater)
			{
				_pf1DelayClock++;
				if (_pf1DelayClock > _pf1MaxDelay)
				{
					_playField.Grp = (uint)((_playField.Grp & 0xF00FF) + (_pf1Update << 8));
					_pf1Updater = false;
				}
			}

			if (_pf2Updater)
			{
				_pf2DelayClock++;
				if (_pf2DelayClock > _pf2MaxDelay)
				{
					_playField.Grp = (uint)((_playField.Grp & 0xFFF00) + ReverseBits(_pf2Update, 8));
					_pf2Updater = false;
				}
			}

			if (_enam0Delay > 0)
			{
				_enam0Delay++;
				if (_enam0Delay == 3)
				{
					_enam0Delay = 0;
					_player0.Missile.Enabled = _enam0Val;
				}
			}

			if (_enam1Delay > 0)
			{
				_enam1Delay++;
				if (_enam1Delay == 3)
				{
					_enam1Delay = 0;
					_player1.Missile.Enabled = _enam1Val;
				}
			}

			if (_enambDelay > 0)
			{
				_enambDelay++;
				if (_enambDelay == 3)
				{
					_enambDelay = 0;
					_ball.Enabled = _enambVal;
				}
			}

			if (_prg0Delay > 0)
			{
				_prg0Delay++;
				if (_prg0Delay == 3)
				{
					_prg0Delay = 0;
					_player0.Grp = _prg0Val;
					_player1.Dgrp = _player1.Grp;
				}
			}

			if (_prg1Delay > 0)
			{
				_prg1Delay++;
				if (_prg1Delay == 3)
				{
					_prg1Delay = 0;
					_player1.Grp = _prg1Val;
					_player0.Dgrp = _player0.Grp;

					// TODO: Find a game that uses this functionality and test it
					_ball.Denabled = _ball.Enabled;
				}
			}

			if (_hmp0Delay > 0)
			{
				_hmp0Delay++;
				if (_hmp0Delay == 5)
				{
					_hmp0Delay = 0;
					_player0.HM = _hmp0Val;
				}
			}

			if (_hmp1Delay > 0)
			{
				_hmp1Delay++;
				if (_hmp1Delay == 5)
				{
					_hmp1Delay = 0;
					_player1.HM = _hmp1Val;
				}
			}

			if (_hmm0Delay > 0)
			{
				_hmm0Delay++;
				if (_hmm0Delay == 5)
				{
					_hmm0Delay = 0;
					_player0.Missile.Hm = _hmm0Val;
				}
			}

			if (_hmm1Delay > 0)
			{
				_hmm1Delay++;
				if (_hmm1Delay == 5)
				{
					_hmm1Delay = 0;
					_player1.Missile.Hm = _hmm1Val;
				}
			}

			if (_hmbDelay > 0)
			{
				_hmbDelay++;
				if (_hmbDelay == 5)
				{
					_hmbDelay = 0;
					_ball.HM = _hmbVal;
				}
			}

			if (_hmClrDelay > 0)
			{
				_hmClrDelay++;
				if (_hmClrDelay == 5)
				{
					_hmClrDelay = 0;

					_player0.HM = 0;
					_player0.Missile.Hm = 0;
					_player1.HM = 0;
					_player1.Missile.Hm = 0;
					_ball.HM = 0;
				}
			}

			if (_nusiz0Delay > 0)
			{
				_nusiz0Delay++;
				if (_nusiz0Delay == 4)
				{
					_nusiz0Delay = 0;

					_player0.Nusiz = (byte)(_nusiz0Val & 0x37);
					_player0.Missile.Size = (byte)((_nusiz0Val & 0x30) >> 4);
					_player0.Missile.Number = (byte)(_nusiz0Val & 0x07);
				}
			}

			if (_nusiz1Delay > 0)
			{
				_nusiz1Delay++;
				if (_nusiz1Delay == 4)
				{
					_nusiz1Delay = 0;

					_player1.Nusiz = (byte)(_nusiz1Val & 0x37);
					_player1.Missile.Size = (byte)((_nusiz1Val & 0x30) >> 4);
					_player1.Missile.Number = (byte)(_nusiz1Val & 0x07);
				}
			}

			// Reset the RDY flag when we reach hblank
			if (_hsyncCnt <= 0)
			{
				_core.Cpu.RDY = true;
			}

			// Assume we're on the left side of the screen for now
			var rightSide = false;

			// ---- Things that happen only in the drawing section ----
			// TODO: Remove this magic number (17). It depends on the HMOVE
			if (_hsyncCnt >= (_hmove.LateHBlankReset ? 76 : 68))
			{
				_doTicks = false;
				
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
				int pixelColor = BackColor;
				if (_core.Settings.ShowBG)
				{
					pixelColor = _palette[_playField.BkColor];
				}

				if ((collisions & CXPF) != 0 && _core.Settings.ShowPlayfield)
				{
					if (_playField.Score)
					{
						pixelColor = !rightSide
							? _palette[_player0.Color]
							: _palette[_player1.Color];
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

				if (_playField.Score && !_playField.Priority && ((collisions & CXPF) != 0) && _core.Settings.ShowPlayfield)
				{
					pixelColor = !rightSide ? _palette[_player0.Color] : _palette[_player1.Color];
				}

				if (_playField.Priority && (collisions & CXPF) != 0 && _core.Settings.ShowPlayfield)
				{
					pixelColor = _palette[_playField.PfColor];
				}

				// Handle vblank
				if (_vblankEnabled)
				{
					pixelColor = BackColor;
				}

				// Add the pixel to the scanline
				// TODO: Remove this magic number (68)
				int y = _currentScanLine;

				// y >= max screen height means lag frame or game crashed, but is a legal situation.
				// either way, there's nothing to display
				if (y < MaxScreenHeight)
				{
					int x = _hsyncCnt - 68;
					if (x < 0 || x > 159) // this can't happen, right?
					{
						throw new Exception(); // TODO
					}

					_scanlinebuffer[(_currentScanLine * ScreenWidth) + x] = pixelColor;
				}
			}
			else
			{
				_doTicks = true;
			}

			// if extended HBLank is active, the screen area still needs a color
			if (_hmove.LateHBlankReset && _hsyncCnt >= 68 && _hsyncCnt < 76)
			{
				int pixelColor = 0;

				// Add the pixel to the scanline
				// TODO: Remove this magic number (68)
				int y = _currentScanLine;

				// y >= max screen height means lag frame or game crashed, but is a legal situation.
				// either way, there's nothing to display
				if (y < MaxScreenHeight)
				{
					int x = _hsyncCnt - 68;
					if (x < 0 || x > 159) // this can't happen, right?
					{
						throw new Exception(); // TODO
					}

					_scanlinebuffer[(_currentScanLine * ScreenWidth) + x] = pixelColor;
				}
			}

			// Handle HMOVE
			if (_hmove.HMoveEnabled)
			{
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
								_p0Stuff = true;
							}
							else
							{
								_hmove.Player0Latch = false;
							}
						}

						if (_hmove.Missile0Latch)
						{
							// If the move counter still has a bit in common with the HM register
							if (((15 - _hmove.Missile0Cnt) ^ ((_player0.Missile.Hm & 0x07) | ((~(_player0.Missile.Hm & 0x08)) & 0x08))) != 0x0F)
							{
								_m0Stuff = true;
							}
							else
							{
								_hmove.Missile0Latch = false;
							}
						}

						if (_hmove.Player1Latch)
						{
							// If the move counter still has a bit in common with the HM register
							if (((15 - _hmove.Player1Cnt) ^ ((_player1.HM & 0x07) | ((~(_player1.HM & 0x08)) & 0x08))) != 0x0F)
							{
								_p1Stuff = true;
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
								_m1Stuff = true;
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
								_bStuff = true;
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

					if (_p0Stuff)
					{
						_p0Stuff = false;

						// "Clock-Stuffing"
						if (_doTicks && !_hmp0_no_tick)
						{
							_player0.Tick();
						}

						// Increase by 1, max of 15
						_hmove.test_count_p0++;
						if (_hmove.test_count_p0 < 16)
						{
							_hmove.Player0Cnt++;
						}
						else
						{
							_hmove.Player0Cnt = 0;
						}
					}

					if (_p1Stuff)
					{
						_p1Stuff = false;

						// "Clock-Stuffing"
						if (_doTicks && !_hmp1_no_tick)
						{
							_player1.Tick();
						}

						// Increase by 1, max of 15
						_hmove.test_count_p1++;
						if (_hmove.test_count_p1 < 16)
						{
							_hmove.Player1Cnt++;
						}
						else
						{
							_hmove.Player1Cnt = 0;
						}
					}

					if (_m0Stuff)
					{
						_m0Stuff = false;

						// "Clock-Stuffing"
						if (_doTicks && !_hmm0_no_tick)
						{
							_player0.Missile.Tick();
						}

						// Increase by 1, max of 15
						_hmove.test_count_m0++;
						if (_hmove.test_count_m0 < 16)
						{
							_hmove.Missile0Cnt++;
						}
						else
						{
							_hmove.Missile0Cnt = 0;
						}
					}

					if (_m1Stuff)
					{
						_m1Stuff = false;

						// "Clock-Stuffing"
						if (_doTicks && !_hmm1_no_tick)
						{
							_player1.Missile.Tick();
						}

						// Increase by 1, max of 15
						_hmove.test_count_m1++;
						if (_hmove.test_count_m1 < 16)
						{
							_hmove.Missile1Cnt++;
						}
						else
						{
							_hmove.Missile1Cnt = 0;
						}
					}

					if (_bStuff)
					{
						_bStuff = false;

						// "Clock-Stuffing"
						if (_doTicks && !_hmb_no_tick)
						{
							_ball.Tick();
						}

						// Increase by 1, max of 15
						_hmove.test_count_b++;
						if (_hmove.test_count_b < 16)
						{
							_hmove.BallCnt++;
						}
						else
						{
							_hmove.BallCnt = 0;
						}
					}
				}

				if (hmove_cnt_up)
				{
					_hmove.HMoveDelayCnt++;
				}

				if ((_hmove.HMoveDelayCnt >= 5) && hmove_cnt_up)
				{
					hmove_cnt_up = false;
					_hmove.HMoveCnt = 4;
					_hmove.DecCntEnabled = true;

					_hmove.test_count_p0 = 0;
					_hmove.test_count_p1 = 0;
					_hmove.test_count_m0 = 0;
					_hmove.test_count_m1 = 0;
					_hmove.test_count_b = 0;

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

					if (_hsyncCnt < 67) { _hmove.LateHBlankReset = true; }			
				}
			}

			// do the audio sampling
			if (_hsyncCnt == 36 || _hsyncCnt == 148)
			{
				if (AudioClocks < 2000)
				{
					LocalAudioCycles[AudioClocks] += (short)(AUD.Cycle_L() / 2);
					LocalAudioCycles[AudioClocks] += (short)(AUD.Cycle_R() / 2);
					AudioClocks++;
				}
			}

			// Increment the hsync counter
			_hsyncCnt++;
			_hsyncCnt %= 228;

			// End of the line? Add it to the buffer!
			if (_hsyncCnt == 0)
			{
				_hmove.LateHBlankReset = false;
				_currentScanLine++;
				LineCount++;
			}

			_hmp0_no_tick = false;
			_hmp1_no_tick = false;
			_hmm0_no_tick = false;
			_hmm1_no_tick = false;
			_hmb_no_tick = false;
		}

		private void OutputFrame(int validlines)
		{
			int topLine = _pal ? _core.Settings.PALTopLine : _core.Settings.NTSCTopLine;
			int bottomLine = _pal ? _core.Settings.PALBottomLine : _core.Settings.NTSCBottomLine;

			// if vsync occured unexpectedly early, black out the remainder
			for (; validlines < bottomLine; validlines++)
			{
				for (int i = 0; i < 160; i++)
				{
					_scanlinebuffer[(validlines * 160) + i] = BackColor;
				}
			}

			int srcbytes = sizeof(int) * ScreenWidth * topLine;
			int count = bottomLine - topLine; // no +1, as the bottom line number is not inclusive
			count *= sizeof(int) * ScreenWidth;

			Buffer.BlockCopy(_scanlinebuffer, srcbytes, _frameBuffer, 0, count);
		}

		public byte ReadMemory(ushort addr, bool peek)
		{
			var maskedAddr = (ushort)(addr & 0x000F);
			byte coll = 0;
			int mask = 0xFF;

			if (maskedAddr == 0x00) // CXM0P
			{
				coll = (byte)((((_player0.Missile.Collisions & CXP1) != 0) ? 0x80 : 0x00) | (((_player0.Missile.Collisions & CXP0) != 0) ? 0x40 : 0x00));
				mask = 0x3f;
			}

			if (maskedAddr == 0x01) // CXM1P
			{
				coll = (byte)((((_player1.Missile.Collisions & CXP0) != 0) ? 0x80 : 0x00) | (((_player1.Missile.Collisions & CXP1) != 0) ? 0x40 : 0x00));
				mask = 0x3f;
			}

			if (maskedAddr == 0x02) // CXP0FB
			{
				coll = (byte)((((_player0.Collisions & CXPF) != 0) ? 0x80 : 0x00) | (((_player0.Collisions & CXBL) != 0) ? 0x40 : 0x00));
				mask = 0x3f;
			}

			if (maskedAddr == 0x03) // CXP1FB
			{
				coll = (byte)((((_player1.Collisions & CXPF) != 0) ? 0x80 : 0x00) | (((_player1.Collisions & CXBL) != 0) ? 0x40 : 0x00));
				mask = 0x3f;
			}

			if (maskedAddr == 0x04) // CXM0FB
			{
				coll = (byte)((((_player0.Missile.Collisions & CXPF) != 0) ? 0x80 : 0x00) | (((_player0.Missile.Collisions & CXBL) != 0) ? 0x40 : 0x00));
				mask = 0x3f;
			}

			if (maskedAddr == 0x05) // CXM1FB
			{
				coll = (byte)((((_player1.Missile.Collisions & CXPF) != 0) ? 0x80 : 0x00) | (((_player1.Missile.Collisions & CXBL) != 0) ? 0x40 : 0x00));
				mask = 0x3f;
			}

			if (maskedAddr == 0x06) // CXBLPF
			{
				coll = (byte)(((_ball.Collisions & CXPF) != 0) ? 0x80 : 0x00);
				mask = 0x7f;
			}

			if (maskedAddr == 0x07) // CXPPMM
			{
				coll = (byte)((((_player0.Collisions & CXP1) != 0) ? 0x80 : 0x00) | (((_player0.Missile.Collisions & CXM1) != 0) ? 0x40 : 0x00));
				mask = 0x3f;
			}

			// inputs 0-3 are measured by a charging capacitor, these inputs are used with the paddles and the keyboard
			// Changing the hard coded value will change the paddle position. The range seems to be roughly 0-56000 according to values from stella
			// 6105 roughly centers the paddle in Breakout
			if (maskedAddr == 0x08) // INPT0
			{
				if (_core.ReadPot1(0) > 0 && _capCharging && _core.Cpu.TotalExecutedCycles - _capChargeStart >= _core.ReadPot1(0))
				{
					coll = 0x80;
				}
				else if (_core.ReadPot1(0) == -2)
				{
					coll |= 0x80;
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x10) == 0x00)
					{
						if (_core.ReadControls1(peek) == 0x1) { coll &= 0x7F; }
					}
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x20) == 0x00)
					{
						if (_core.ReadControls1(peek) == 0x4) { coll &= 0x7F; }
					}
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x40) == 0x00)
					{
						if (_core.ReadControls1(peek) == 0x7) { coll &= 0x7F; }
					}
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x80) == 0x00)
					{
						if (_core.ReadControls1(peek) == 0xA) { coll &= 0x7F; }
					}
				}
				else
				{
					coll = 0x00;
				}

				mask = 0x7f;
			}

			if (maskedAddr == 0x09) // INPT1
			{
				if (_core.ReadPot1(1) > 0 && _capCharging && _core.Cpu.TotalExecutedCycles - _capChargeStart >= _core.ReadPot1(1))
				{
					coll = 0x80;
				}
				else if (_core.ReadPot1(1) == -2)
				{
					coll |= 0x80;
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x10) == 0x00)
					{
						if (_core.ReadControls1(peek) == 0x2) { coll &= 0x7F; }
					}
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x20) == 0x00)
					{
						if (_core.ReadControls1(peek) == 0x5) { coll &= 0x7F; }
					}
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x40) == 0x00)
					{
						if (_core.ReadControls1(peek) == 0x8) { coll &= 0x7F; }
					}
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x80) == 0x00)
					{
						if (_core.ReadControls1(peek) == 0x0) { coll &= 0x7F; }
					}
				}
				else
				{
					coll = 0x00;
				}

				mask = 0x7f;
			}

			if (maskedAddr == 0x0A) // INPT2
			{
				if (_core.ReadPot2(0) > 0 && _capCharging && _core.Cpu.TotalExecutedCycles - _capChargeStart >= _core.ReadPot2(0))
				{
					coll = 0x80;
				}
				else if (_core.ReadPot2(0) == -2)
				{
					coll |= 0x80;
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x01) == 0x00)
					{
						if (_core.ReadControls2(peek) == 0x1) { coll &= 0x7F; }
					}
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x02) == 0x00)
					{
						if (_core.ReadControls2(peek) == 0x4) { coll &= 0x7F; }
					}
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x04) == 0x00)
					{
						if (_core.ReadControls2(peek) == 0x7) { coll &= 0x7F; }
					}
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x08) == 0x00)
					{
						if (_core.ReadControls2(peek) == 0xA) { coll &= 0x7F; }
					}
				}
				else
				{
					coll = 0x00;
				}

				mask = 0x7f;
			}

			if (maskedAddr == 0x0B) // INPT3
			{
				if (_core.ReadPot2(1) > 0 && _capCharging && _core.Cpu.TotalExecutedCycles - _capChargeStart >= _core.ReadPot2(1))
				{
					coll = 0x80;
				}
				else if (_core.ReadPot2(1) == -2)
				{
					coll |= 0x80;
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x01) == 0x00)
					{
						if (_core.ReadControls2(peek) == 0x2) { coll &= 0x7F; }
					}
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x02) == 0x00)
					{
						if (_core.ReadControls2(peek) == 0x5) { coll &= 0x7F; }
					}
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x04) == 0x00)
					{
						if (_core.ReadControls2(peek) == 0x8) { coll &= 0x7F; }
					}
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x08) == 0x00)
					{
						if (_core.ReadControls2(peek) == 0x0) { coll &= 0x7F; }
					}
				}
				else
				{
					coll = 0x00;
				}

				mask = 0x7f;
			}

			if (maskedAddr == 0x0C) // INPT4
			{
				if (_core.ReadPot1(0) != -2)
				{
					coll = (byte)((_core.ReadControls1(peek) & 0x08) != 0 ? 0x80 : 0x00);
				}
				else
				{
					coll |= 0x80;
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x10) == 0x00)
					{
						if (_core.ReadControls1(peek) == 0x3) { coll &= 0x7F; }
					}
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x20) == 0x00)
					{
						if (_core.ReadControls1(peek) == 0x6) { coll &= 0x7F; }
					}
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x40) == 0x00)
					{
						if (_core.ReadControls1(peek) == 0x9) { coll &= 0x7F; }
					}
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x80) == 0x00)
					{
						if (_core.ReadControls1(peek) == 0xB) { coll &= 0x7F; }
					}
				}

				mask = 0x7f;
			}

			if (maskedAddr == 0x0D) // INPT5
			{
				if (_core.ReadPot2(0) != -2)
				{
					coll = (byte)((_core.ReadControls2(peek) & 0x08) != 0 ? 0x80 : 0x00);
				}
				else
				{
					coll |= 0x80;
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x01) == 0x00)
					{
						if (_core.ReadControls2(peek) == 0x3) { coll &= 0x7F; }
					}
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x02) == 0x00)
					{
						if (_core.ReadControls2(peek) == 0x6) { coll &= 0x7F; }
					}
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x04) == 0x00)
					{
						if (_core.ReadControls2(peek) == 0x9) { coll &= 0x7F; }
					}
					if (((_core._m6532._ddRa & _core._m6532._outputA) & 0x08) == 0x00)
					{
						if (_core.ReadControls2(peek) == 0xB) { coll &= 0x7F; }
					}
				}

				mask = 0x7f;
			}

			// some bits of the databus will be undriven when a read call is made. Our goal here is to sort out what
			// happens to the undriven pins. Most of the time, they will be in whatever state they were when previously
			// assigned in some other bus access, so let's go with that. 
			coll += (byte)(mask & BusState);

			if (!peek)
			{
				BusState = coll;
			}

			return coll;
		}

		public void WriteMemory(ushort addr, byte value, bool poke)
		{
			var maskedAddr = (ushort)(addr & 0x3f);
			if (!poke)
			{
				BusState = value;
			}

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
					OutputFrame(_currentScanLine);

					New_Frame = true;

					// Clear all from last frame
					_currentScanLine = 0;

					// Frame is done
					_vsyncEnabled = false;

					// Do not reset hsync, since we're on the first line of the new frame
					// hsyncCnt = 0;
				}
			}
			else if (maskedAddr == 0x01) // VBLANK
			{
				_vblankDelay = 1;
				_vblankValue = value;
				_capCharging = (value & 0x80) == 0;
				if ((value & 0x80) == 0)
				{
					_capChargeStart = _core.Cpu.TotalExecutedCycles;
				}
			}
			else if (maskedAddr == 0x02) // WSYNC
			{
				// Halt the CPU until we reach hblank
				_core.Cpu.RDY = false;
			}
			else if (maskedAddr == 0x04) // NUSIZ0
			{
				_nusiz0Delay = 1;
				_nusiz0Val = value;
			}
			else if (maskedAddr == 0x05) // NUSIZ1
			{
				_nusiz1Delay = 1;
				_nusiz1Val = value;
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
				_ctrlPFDelay = 1;
				_ctrlPFVal = value;
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
				_pf0Update = value;
				_pf0Updater = true;
				_pf0DelayClock = 0;
				if (((_hsyncCnt / 3) & 3) == 0)
				{
					_pf0MaxDelay = 4;
				}

				if (((_hsyncCnt / 3) & 3) == 1)
				{
					_pf0MaxDelay = 5;
				}

				if (((_hsyncCnt / 3) & 3) == 2)
				{
					_pf0MaxDelay = 2;
				}

				if (((_hsyncCnt / 3) & 3) == 3)
				{
					_pf0MaxDelay = 3;
				}
			}
			else if (maskedAddr == 0x0E) // PF1
			{
				_pf1Update = value;
				_pf1Updater = true;
				_pf1DelayClock = 0;
				if (((_hsyncCnt / 3) & 3) == 0)
				{
					_pf1MaxDelay = 4;
				}

				if (((_hsyncCnt / 3) & 3) == 1)
				{
					_pf1MaxDelay = 5;
				}

				if (((_hsyncCnt / 3) & 3) == 2)
				{
					_pf1MaxDelay = 2;
				}

				if (((_hsyncCnt / 3) & 3) == 3)
				{
					_pf1MaxDelay = 3;
				}
			}
			else if (maskedAddr == 0x0F) // PF2
			{
				_pf2Update = value;
				_pf2Updater = true;
				_pf2DelayClock = 0;
				if (((_hsyncCnt / 3) & 3) == 0)
				{
					_pf2MaxDelay = 4;
				}

				if (((_hsyncCnt / 3) & 3) == 1)
				{
					_pf2MaxDelay = 5;
				}

				if (((_hsyncCnt / 3) & 3) == 2)
				{
					_pf2MaxDelay = 2;
				}

				if (((_hsyncCnt / 3) & 3) == 3)
				{
					_pf2MaxDelay = 3;
				}
			}
			else if (maskedAddr == 0x10) // RESP0
			{
				// RESP delays draw signal clocking
				_player0.Resp_check();
				//_hmp0_no_tick = true;
				// Resp depends on HMOVE
				if (!_hmove.LateHBlankReset)
				{
					_player0.HPosCnt = (byte)(_hsyncCnt < 68 ? 160 - 2 : 160 - 4);
					if (_hsyncCnt == 67)
					{
						_player0.HPosCnt = 160 - 3;
					}
				}
				else
				{
					_player0.HPosCnt = (byte)(_hsyncCnt < 76 ? 160 - 2 : 160 - 4);
					if (_hsyncCnt == 75)
					{
						_player0.HPosCnt = 160 - 3;
					}
				}
			}
			else if (maskedAddr == 0x11) // RESP1
			{
				// RESP delays draw signal clocking
				_player1.Resp_check();
				//_hmp1_no_tick = true;
				// RESP depends on HMOVE
				if (!_hmove.LateHBlankReset)
				{
					_player1.HPosCnt = (byte)(_hsyncCnt < 68 ? 160 - 2 : 160 - 4);
					if (_hsyncCnt == 67)
					{
						_player1.HPosCnt = 160 - 3;
					}
				}
				else
				{
					_player1.HPosCnt = (byte)(_hsyncCnt < 76 ? 160 - 2 : 160 - 4);
					if (_hsyncCnt == 75)
					{
						_player1.HPosCnt = 160 - 3;
					}
				}
			}
			else if (maskedAddr == 0x12) // RESM0
			{
				// RESP delays draw signal clocking
				// but only for players? Needs investigation
				//_player0.Missile.Resp_check();
				_hmm0_no_tick = true;
				if (!_hmove.LateHBlankReset)
				{
					_player0.Missile.HPosCnt = (byte)(_hsyncCnt < 68 ? 160 - 2 : 160 - 4);
					if (_hsyncCnt == 67)
					{
						_player0.Missile.HPosCnt = 160 - 3;
					}
				}
				else
				{
					_player0.Missile.HPosCnt = (byte)(_hsyncCnt < 76 ? 160 - 2 : 160 - 4);
					if (_hsyncCnt == 75)
					{
						_player0.Missile.HPosCnt = 160 - 3;
					}
				}
			}
			else if (maskedAddr == 0x13) // RESM1
			{
				// RESP delays draw signal clocking
				// but only for players? Needs investigation
				//_player1.Missile.Resp_check();
				_hmm1_no_tick = true;
				if (!_hmove.LateHBlankReset)
				{
					_player1.Missile.HPosCnt = (byte)(_hsyncCnt < 68 ? 160 - 2 : 160 - 4);
					if (_hsyncCnt == 67)
					{
						_player1.Missile.HPosCnt = 160 - 3;
					}
				}
				else
				{
					_player1.Missile.HPosCnt = (byte)(_hsyncCnt < 76 ? 160 - 2 : 160 - 4);
					if (_hsyncCnt == 75)
					{
						_player1.Missile.HPosCnt = 160 - 3;
					}
				}
			}
			else if (maskedAddr == 0x14) // RESBL
			{
				// RESP delays draw signal clocking
				// but only for players? Needs investigation
				//_ball.Resp_check();
				_hmb_no_tick = true;
				if (!_hmove.LateHBlankReset)
				{
					_ball.HPosCnt = (byte)(_hsyncCnt < 68 ? 160 - 2 : 160 - 4);
					if (_hsyncCnt == 67)
					{
						_ball.HPosCnt = 160 - 3;
					}
				}
				else
				{
					_ball.HPosCnt = (byte)(_hsyncCnt < 76 ? 160 - 2 : 160 - 4);
					if (_hsyncCnt == 75)
					{
						_ball.HPosCnt = 160 - 3;
					}
				}
			}
			else if (maskedAddr == 0x15) // AUDC0
			{
				AUD.AUDC_L = (byte)(value & 15);
			}
			else if (maskedAddr == 0x16) // AUDC1
			{
				AUD.AUDC_R = (byte)(value & 15);
			}
			else if (maskedAddr == 0x17) // AUDF0
			{
				AUD.AUDF_L = (byte)((value & 31) + 1);
			}
			else if (maskedAddr == 0x18) // AUDF1
			{
				AUD.AUDF_R = (byte)((value & 31) + 1);
			}
			else if (maskedAddr == 0x19) // AUDV0
			{
				AUD.AUDV_L = (byte)(value & 15);
			}
			else if (maskedAddr == 0x1A) // AUDV1
			{
				AUD.AUDV_R = (byte)(value & 15);
			}
			else if (maskedAddr == 0x1B) // GRP0
			{
				_prg0Val = value;
				_prg0Delay = 1;
			}
			else if (maskedAddr == 0x1C) // GRP1
			{
				_prg1Val = value;
				_prg1Delay = 1;
			}
			else if (maskedAddr == 0x1D) // ENAM0
			{
				_enam0Val = (value & 0x02) != 0;
				_enam0Delay = 1;
			}
			else if (maskedAddr == 0x1E) // ENAM1
			{
				_enam1Val = (value & 0x02) != 0;
				_enam1Delay = 1;
			}
			else if (maskedAddr == 0x1F) // ENABL
			{
				_enambVal = (value & 0x02) != 0;
				_enambDelay = 1;
			}
			else if (maskedAddr == 0x20) // HMP0
			{
				_hmp0Val = (byte)((value & 0xF0) >> 4);
				_hmp0Delay = 1;
			}
			else if (maskedAddr == 0x21) // HMP1
			{
				_hmp1Val = (byte)((value & 0xF0) >> 4);
				_hmp1Delay = 1;
			}
			else if (maskedAddr == 0x22) // HMM0
			{
				_hmm0Val = (byte)((value & 0xF0) >> 4);
				_hmm0Delay = 1;
			}
			else if (maskedAddr == 0x23) // HMM1
			{
				_hmm1Val = (byte)((value & 0xF0) >> 4);
				_hmm1Delay = 1;
			}
			else if (maskedAddr == 0x24) // HMBL
			{
				_hmbVal = (byte)((value & 0xF0) >> 4);
				_hmbDelay = 1;
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
				hmove_cnt_up = true;
				_hmove.HMoveDelayCnt = 0;
			}
			else if (maskedAddr == 0x2B) // HMCLR
			{
				_hmClrDelay = 1;			
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

		private enum AudioRegister : byte
		{
			AUDC, AUDF, AUDV
		}
	}
}