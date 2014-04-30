using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class TIA
	{
		private struct BallData
		{
			public bool Enabled;
			public bool Denabled;
			public bool Delay;
			public byte Size;
			public byte HM;
			public byte HPosCnt;
			public byte Collisions;

			public bool Tick()
			{
				bool result = false;
				if (HPosCnt < (1 << Size))
				{
					if (!Delay && Enabled)
					{
						// Draw the ball!
						result = true;
					}
					else if (Delay && Denabled)
					{
						// Draw the ball!
						result = true;
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
				ser.BeginSection("Ball");
				ser.Sync("enabled", ref Enabled);
				ser.Sync("denabled", ref Denabled);
				ser.Sync("delay", ref Delay);
				ser.Sync("size", ref Size);
				ser.Sync("HM", ref HM);
				ser.Sync("hPosCnt", ref HPosCnt);
				ser.Sync("collisions", ref Collisions);
				ser.EndSection();
			}
		}
	}
}
