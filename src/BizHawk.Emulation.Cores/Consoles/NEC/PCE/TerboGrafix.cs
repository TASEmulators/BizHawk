using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.NEC.PCE
{
	[Core(CoreNames.TerboGrafix, "Mednafen Team", true, false, "", "", false)]
	public class TerboGrafix : NymaCore, IRegionable
	{
		[CoreConstructor("PCE")]
		public TerboGrafix(GameInfo game, byte[] rom, CoreComm comm, string extension)
			: base(game, rom, comm, new Configuration
			{
				SystemId = "PCE" // whatever
				// TODO: This stuff isn't used so much
			})
		{
			DoInit<LibNymaCore>(game, rom, "pce.wbx", extension);
		}
	}
}
