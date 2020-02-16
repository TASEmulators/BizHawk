using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	public partial class TIA
	{
		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(TIA));

			ser.Sync("hsyncCnt", ref _hsyncCnt);

			// add everything to the state 
			ser.Sync("Bus_State", ref BusState);

			ser.Sync("Ticks", ref _doTicks);

			// some of these things weren't in the state because they weren't needed if
			// states were always taken at frame boundaries
			ser.Sync("capChargeStart", ref _capChargeStart);
			ser.Sync("capCharging", ref _capCharging);
			ser.Sync(nameof(AudioClocks), ref AudioClocks);
			ser.Sync("FrameStartCycles", ref _frameStartCycles);
			ser.Sync("FrameEndCycles", ref _frameEndCycles);

			ser.BeginSection("AUD0");
			AUD[0].SyncState(ser);
			ser.EndSection();
			ser.BeginSection("AUD1");
			AUD[1].SyncState(ser);
			ser.EndSection();

			ser.EndSection();
		}
	}
}
