using BizHawk.Common;

// TODO: Some of the values that are being latched (like Nusiz) are being latched based on the internal player ticks, not the external hsync ticks.
// This can be seen for example in the player32_hblank test ROM. 
// Which values these are exactly needs to be more carefully studied.

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class TIA
	{
		private struct PlayerData
		{
			public MissileData Missile;

			public byte Grp;
			public byte Dgrp;
			public byte Color;
			public byte HPosCnt;
			public byte ScanCnt;
			public bool ScanCntInit;
			public byte HM;
			public bool Reflect;
			public bool Delay;
			public byte Nusiz;
			public byte Collisions;

			// Resp commands do not trigger start signals for main copies. We need to model this
			public int _startSignal;
			private int _signalReached;
			public bool _draw_signaled;

			public bool Tick()
			{
				var result = false;
				if (ScanCnt < 8)
				{
					if (!ScanCntInit)
					{
						// Make the mask to check the graphic
						byte playerMask = (byte)(1 << (8 - 1 - ScanCnt));

						// Reflect it if needed
						if (Reflect)
						{
							playerMask = (byte)ReverseBits(playerMask, 8);
						}

						// Check the graphic (depending on delay)
						if (!Delay)
						{
							if ((Grp & playerMask) != 0)
							{
								result = true;
							}
						}
						else
						{
							if ((Dgrp & playerMask) != 0)
							{
								result = true;
							}
						}

						// Reset missile, if desired
						if (ScanCnt == 0x04 && HPosCnt <= 16 && Missile.ResetToPlayer)
						{
							Missile.HPosCnt = 0;
						}
					}

					// Increment the Player Graphics Scan Counter

					// This counter advances once per clock for single sized players,
					// once every 2 clocks for double sized players (Nusiz == 0x05),
					// and once every 4 clocks for quad sizes players (Nusize == 0x07)

					// The ticks for starting and advancing this counter are tied to the div4 clocking phase.
					// The first tick for single sized players happens immediately.
					// The first tick for double and quad sized players is delayed one clock cycle,
					// and then happen every 2 or 4 clocks
					if ((Nusiz & 0x07) == 0x05)
					{
						if ((HPosCnt + 3) % 2 == 0)
						{
							if (ScanCntInit)
							{
								ScanCntInit = false;
								ScanCnt = 0;
							}
							else
							{
								ScanCnt++;
							}
						}
					}
					else if ((Nusiz & 0x07) == 0x07)
					{
						if ((HPosCnt + 3) % 4 == 0)
						{
							if (ScanCntInit)
							{
								ScanCntInit = false;
								ScanCnt = 0;
							}
							else
							{
								ScanCnt++;
							}
						}
					}
					else if (ScanCntInit)
					{
						ScanCntInit = false;
						//ScanCnt++;
					}
					else
					{
						ScanCnt++;
					}
				}

				// At counter position 0 we should initalize the scan counter. 
				// Note that for double and quad sized players that the scan counter is not started immediately.
				if (_startSignal == 160)
				{
					ScanCnt = 0;
					_startSignal++;
					if ((Nusiz & 0x07) == 0x05)
					{
						ScanCntInit = true;
					}
					else if ((Nusiz & 0x07) == 0x07)
					{
						ScanCntInit = true;
					}
					else
					{
						ScanCntInit = false;
					}
				}

				if (_startSignal == 16 && ((Nusiz & 0x07) == 0x01 || ((Nusiz & 0x07) == 0x03)))
				{
					ScanCnt = 0;
					_startSignal++;
					_draw_signaled = false;
				}

				if (_startSignal == 32 && ((Nusiz & 0x07) == 0x02 || ((Nusiz & 0x07) == 0x03) || ((Nusiz & 0x07) == 0x06)))
				{
					ScanCnt = 0;
					_startSignal++;
					_draw_signaled = false;
				}

				if (_startSignal == 64 && ((Nusiz & 0x07) == 0x04 || ((Nusiz & 0x07) == 0x06)))
				{
					ScanCnt = 0;
					_startSignal++;
					_draw_signaled = false;
				}

				// Increment the counter
				HPosCnt++;

				// Counter loops at 160 
				HPosCnt %= 160;

				// our goal here is to send a start signal 4 clocks before drawing begins. This properly emulates
				// drawing on a real TIA
				if (HPosCnt == 156 || HPosCnt == 12 || HPosCnt == 28 || HPosCnt == 60)
				{
					_startSignal = HPosCnt - 1;
					_signalReached = HPosCnt + 5;
					_draw_signaled = true;
				}

				if (_startSignal < _signalReached)
				{
					_startSignal++;
				}

				return result;
			}

			public void Resp_check()
			{
				if (_draw_signaled)
				{
					if (_startSignal < 17)
					{
						_startSignal -= _startSignal - 12;
					}

					else if (_startSignal < 33)
					{
						_startSignal -= _startSignal - 28;
					}

					else if (_startSignal < 65)
					{
						_startSignal -= _startSignal - 60;
					}

					else if (_startSignal < 161)
					{
						_startSignal -= _startSignal - 156;
					}
				}
			}

			public void SyncState(Serializer ser)
			{
				Missile.SyncState(ser);
				ser.Sync("grp", ref Grp);
				ser.Sync("dgrp", ref Dgrp);
				ser.Sync("color", ref Color);
				ser.Sync("hPosCnt", ref HPosCnt);
				ser.Sync("scanCnt", ref ScanCnt);
				ser.Sync("scanCntInit", ref ScanCntInit);
				ser.Sync(nameof(HM), ref HM);
				ser.Sync("reflect", ref Reflect);
				ser.Sync("delay", ref Delay);
				ser.Sync("nusiz", ref Nusiz);
				ser.Sync("collisions", ref Collisions);
				ser.Sync("start_signal", ref _startSignal);
				ser.Sync("signal_reached", ref _signalReached);
				ser.Sync("_draw_signaled", ref _draw_signaled);
			}
		}
	}
}
