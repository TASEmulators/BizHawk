using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.NEC.PCE
{
	[Core(CoreNames.TurboNyma, "Mednafen Team", true, false, "1.24.3", "", false)]
	public class TerboGrafix : NymaCore, IRegionable
	{
		[CoreConstructor("PCE")]
		public TerboGrafix(GameInfo game, byte[] rom, CoreComm comm, string extension,
			NymaSettings settings, NymaSyncSettings syncSettings)
			: base(game, rom, comm, "PCE", "PC Engine Controller", settings, syncSettings)
		{
			DoInit<LibNymaCore>(game, rom, "pce.wbx", extension);
		}
	}
}
