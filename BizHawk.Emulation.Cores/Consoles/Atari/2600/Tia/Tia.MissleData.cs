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

            // Resp commands do not trigger start signals for main copies. We need to model this
            public int Draw_To;
            public byte ScanCnt;
            public bool ScanCntInit;
            public int Start_Signal;
            public int Signal_Reached;

            public bool Tick()
			{
				var result = false;

                if (ScanCntInit==true)
                {
                    if (ScanCnt < (1 << Size) && Enabled && !ResetToPlayer)
                    {
                        result = true;
                        ScanCnt++;
                        
                    } else
                    {
                        ScanCntInit = false;
                    }

                }



                /*
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
				}*/

                if (Start_Signal == 160)
                {
                    ScanCnt = 0;
                    Start_Signal++;
                    ScanCntInit = true;
                }

                if (Start_Signal == 16 && ((Number & 0x07) == 0x01 || ((Number & 0x07) == 0x03)))
                {
                    ScanCnt = 0;
                    Start_Signal++;
                    ScanCntInit = true;
                }

                if (Start_Signal == 32 && ((Number & 0x07) == 0x02 || ((Number & 0x07) == 0x03) || ((Number & 0x07) == 0x06)))
                {
                    ScanCnt = 0;
                    Start_Signal++;
                    ScanCntInit = true;
                }

                if (Start_Signal == 64 && ((Number & 0x07) == 0x04 || ((Number & 0x07) == 0x06)))
                {
                    ScanCnt = 0;
                    Start_Signal++;
                    ScanCntInit = true;
                }

                // Increment the counter
                HPosCnt++;

				// Counter loops at 160 
				HPosCnt %= 160;

                //our goal here is to send a start signal 4 clocks before drawing begins. The properly emulates
                //drawing on a real TIA
                if (HPosCnt == 156 || HPosCnt == 12 || HPosCnt == 28 || HPosCnt == 60)
                {
                    Start_Signal = HPosCnt;
                    Signal_Reached = HPosCnt + 5;
                }

                if (Start_Signal < Signal_Reached)
                {
                    Start_Signal++;
                }

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
                ser.Sync("start_signal", ref Start_Signal);
                ser.Sync("signal_reached", ref Signal_Reached);
                ser.Sync("draw_to", ref Draw_To);
                ser.Sync("scanCnt", ref ScanCnt);
                ser.Sync("scanCntInit", ref ScanCntInit);
                ser.EndSection();
			}
		}
	}
}
