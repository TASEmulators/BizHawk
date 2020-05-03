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

			// Resp commands do not trigger start signals for main copies. We need to model this
			private int _drawTo;
			private byte _scanCnt;
			private bool _scanCntInit;
			private int _startSignal;
			private int _signalReached;
			public bool _draw_signaled;

			public bool Tick()
			{
				bool result = false;


				if (_scanCntInit)
				{
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
				}

				// Increment the counter
				HPosCnt++;

				// Counter loops at 160 
				HPosCnt %= 160;

				if (_startSignal == 160)
				{
					_scanCnt = 0;
					_startSignal++;
					_scanCntInit = true;
				}

				// our goal here is to send a start signal 4 clocks before drawing begins. The properly emulates
				// drawing on a real TIA
				if (HPosCnt == 156)
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
				ser.BeginSection("Ball");
				ser.Sync("enabled", ref Enabled);
				ser.Sync("denabled", ref Denabled);
				ser.Sync("delay", ref Delay);
				ser.Sync("size", ref Size);
				ser.Sync(nameof(HM), ref HM);
				ser.Sync("hPosCnt", ref HPosCnt);
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
