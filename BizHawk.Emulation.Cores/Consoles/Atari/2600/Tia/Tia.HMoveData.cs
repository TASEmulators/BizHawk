using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class TIA
	{
		private struct HMoveData
		{
			public bool HMoveEnabled;
			public bool LateHBlankReset;
			public bool DecCntEnabled;

			public bool Player0Latch;
			public bool Player1Latch;
			public bool Missile0Latch;
			public bool Missile1Latch;
			public bool BallLatch;

			public byte HMoveDelayCnt;
			public byte HMoveCnt;

            public int test_count_p0;
            public int test_count_p1;
            public int test_count_m0;
            public int test_count_m1;
            public int test_count_b;

            public byte Player0Cnt;
			public byte Player1Cnt;
			public byte Missile0Cnt;
			public byte Missile1Cnt;
			public byte BallCnt;

			public void SyncState(Serializer ser)
			{
				ser.BeginSection("HMove");
				ser.Sync("hmoveEnabled", ref HMoveEnabled);
				ser.Sync("lateHBlankReset", ref LateHBlankReset);
				ser.Sync("decCntEnabled", ref DecCntEnabled);
				ser.Sync("player0Latch", ref Player0Latch);
				ser.Sync("player1Latch", ref Player1Latch);
				ser.Sync("missile0Latch", ref Missile0Latch);
				ser.Sync("missile1Latch", ref Missile1Latch);
				ser.Sync("ballLatch", ref BallLatch);
				ser.Sync("hmoveDelayCnt", ref HMoveDelayCnt);
				ser.Sync("hmoveCnt", ref HMoveCnt);
				ser.Sync("player0Cnt", ref Player0Cnt);
				ser.Sync("player1Cnt", ref Player1Cnt);
				ser.Sync("missile0Cnt", ref Missile0Cnt);
				ser.Sync("missile1Cnt", ref Missile1Cnt);
				ser.Sync("Test_count_p0", ref test_count_p0);
                ser.Sync("Test_count_p1", ref test_count_p1);
                ser.Sync("Test_count_m0", ref test_count_m0);
                ser.Sync("Test_count_m1", ref test_count_m1);
                ser.Sync("Test_count_b", ref test_count_b);
                ser.Sync("ballCnt", ref BallCnt);
                ser.EndSection();
			}
		}
	}
}
