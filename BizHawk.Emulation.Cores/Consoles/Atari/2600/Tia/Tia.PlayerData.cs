using BizHawk.Common;

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
			public bool Reset;
			public byte ResetCnt;
			public byte Collisions;

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
					else
					{
						ScanCntInit = false;
						ScanCnt++;
					}
				}

				// At counter position 0 we should initalize the scan counter. 
				// Note that for double and quad sized players that the scan counter is not started immediately.
				if (HPosCnt == 0 && !Reset)
				{
					ScanCnt = 0;
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

				if (HPosCnt == 16 && ((Nusiz & 0x07) == 0x01 || ((Nusiz & 0x07) == 0x03)))
				{
					ScanCnt = 0;
				}

				if (HPosCnt == 32 && ((Nusiz & 0x07) == 0x02 || ((Nusiz & 0x07) == 0x03) || ((Nusiz & 0x07) == 0x06)))
				{
					ScanCnt = 0;
				}

				if (HPosCnt == 64 && ((Nusiz & 0x07) == 0x04 || ((Nusiz & 0x07) == 0x06)))
				{
					ScanCnt = 0;
				}

				// Reset is no longer in effect
				Reset = false;

				// Increment the counter
				HPosCnt++;

				// Counter loops at 160 
				HPosCnt %= 160;

				if (ResetCnt < 4)
				{
					ResetCnt++;
				}

				if (ResetCnt == 4)
				{
					HPosCnt = 0;
					Reset = true;
					ResetCnt++;
				}

				return result;
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
				ser.Sync("HM", ref HM);
				ser.Sync("reflect", ref Reflect);
				ser.Sync("delay", ref Delay);
				ser.Sync("nusiz", ref Nusiz);
				ser.Sync("reset", ref Reset);
				ser.Sync("resetCnt", ref ResetCnt);
				ser.Sync("collisions", ref Collisions);
			}
		}
	}
}
