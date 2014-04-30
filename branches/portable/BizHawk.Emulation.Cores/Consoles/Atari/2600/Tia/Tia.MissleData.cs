using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class TIA
	{
		private struct MissileData
		{
			public bool Enabled;
			public bool ResetToPlayer;
			public byte HPosCnt;
			public byte Size;
			public byte Number;
			public byte Hm;
			public byte Collisions;

			public bool Tick()
			{
				var result = false;

				// At hPosCnt == 0, start drawing the missile, if enabled
				if (HPosCnt < (1 << Size))
				{
					if (Enabled && !ResetToPlayer)
					{
						// Draw the missile
						result = true;
					}
				}

				if ((Number & 0x07) == 0x01 || ((Number & 0x07) == 0x03))
				{
					if (HPosCnt >= 16 && HPosCnt <= (16 + (1 << Size) - 1))
					{
						if (Enabled && !ResetToPlayer)
						{
							// Draw the missile
							result = true;
						}
					}
				}

				if ((Number & 0x07) == 0x02 || ((Number & 0x07) == 0x03) || ((Number & 0x07) == 0x06))
				{
					if (HPosCnt >= 32 && HPosCnt <= (32 + (1 << Size) - 1))
					{
						if (Enabled && !ResetToPlayer)
						{
							// Draw the missile
							result = true;
						}
					}
				}

				if ((Number & 0x07) == 0x04 || (Number & 0x07) == 0x06)
				{
					if (HPosCnt >= 64 && HPosCnt <= (64 + (1 << Size) - 1))
					{
						if (Enabled && !ResetToPlayer)
						{
							// Draw the missile
							result = true;
						}
					}
				}

				// Increment the counter
				HPosCnt++;

				// Counter loops at 160 
				HPosCnt %= 160;

				return result;
			}

			public void SyncState(Serializer ser)
			{
				ser.BeginSection("Missile");
				ser.Sync("enabled", ref Enabled);
				ser.Sync("resetToPlayer", ref ResetToPlayer);
				ser.Sync("hPosCnt", ref HPosCnt);
				ser.Sync("size", ref Size);
				ser.Sync("number", ref Number);
				ser.Sync("HM", ref Hm);
				ser.Sync("collisions", ref Collisions);
				ser.EndSection();
			}
		}
	}
}
