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
			private int _drawTo;
			private byte _scanCnt;
			private bool _scanCntInit;
			private int _startSignal;
			private int _signalReached;
			public bool _draw_signaled;

			public bool Tick()
			{
				var result = false;

				if (_scanCntInit)
				{
					if (_scanCnt < 1 << Size && Enabled && !ResetToPlayer)
					{
						result = true;
						_scanCnt++;
					}
					else
					{
						_scanCntInit = false;
					}
				}

				if (_startSignal == 160)
				{
					_scanCnt = 0;
					_startSignal++;
					_scanCntInit = true;
				}

				if (_startSignal == 16 && ((Number & 0x07) == 0x01 || ((Number & 0x07) == 0x03)))
				{
					_scanCnt = 0;
					_startSignal++;
					_scanCntInit = true;
					_draw_signaled = false;
				}

				if (_startSignal == 32 && ((Number & 0x07) == 0x02 || ((Number & 0x07) == 0x03) || ((Number & 0x07) == 0x06)))
				{
					_scanCnt = 0;
					_startSignal++;
					_scanCntInit = true;
					_draw_signaled = false;
				}

				if (_startSignal == 64 && ((Number & 0x07) == 0x04 || ((Number & 0x07) == 0x06)))
				{
					_scanCnt = 0;
					_startSignal++;
					_scanCntInit = true;
					_draw_signaled = false;
				}

				// Increment the counter
				HPosCnt++;

				// Counter loops at 160 
				HPosCnt %= 160;

				// our goal here is to send a start signal 4 clocks before drawing begins. The properly emulates
				// drawing on a real TIA
				if (HPosCnt == 156 || HPosCnt == 12 || HPosCnt == 28 || HPosCnt == 60)
				{
					_startSignal = HPosCnt;
					_signalReached = HPosCnt + 5;
					_draw_signaled = true;
				}

				if (_startSignal < _signalReached)
				{
					_startSignal++;
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
				ser.Sync("start_signal", ref _startSignal);
				ser.Sync("signal_reached", ref _signalReached);
				ser.Sync("draw_to", ref _drawTo);
				ser.Sync("scanCnt", ref _scanCnt);
				ser.Sync("scanCntInit", ref _scanCntInit);
				ser.Sync("_draw_signaled", ref _draw_signaled);
				ser.EndSection();
			}
		}
	}
}
