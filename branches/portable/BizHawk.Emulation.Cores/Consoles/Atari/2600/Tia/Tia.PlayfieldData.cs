using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class TIA
	{
		private struct PlayfieldData
		{
			public uint Grp;
			public byte PfColor;
			public byte BkColor;
			public bool Reflect;
			public bool Score;
			public bool Priority;

			public void SyncState(Serializer ser)
			{
				ser.BeginSection("PlayField");
				ser.Sync("grp", ref Grp);
				ser.Sync("pfColor", ref PfColor);
				ser.Sync("bkColor", ref BkColor);
				ser.Sync("reflect", ref Reflect);
				ser.Sync("score", ref Score);
				ser.Sync("priority", ref Priority);
				ser.EndSection();
			}
		}
	}
}
