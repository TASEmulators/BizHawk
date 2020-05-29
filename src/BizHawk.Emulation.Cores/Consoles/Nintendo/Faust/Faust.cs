using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Faust
{
	[Core("Faust", "Mednafen Team", true, false, "1.24.3", "https://mednafen.github.io/releases/", false)]
	public class Faust : NymaCore, IRegionable
	{
		[CoreConstructor("SNES")]
		public Faust(GameInfo game, byte[] rom, CoreComm comm, string extension,
			NymaSettings settings, NymaSyncSettings syncSettings, bool deterministic)
			: base(comm, "SNES", "I don't think anything uses this parameter", settings, syncSettings)
		{
			DoInit<LibNymaCore>(game, rom, null, "faust.wbx", extension, deterministic);
		}
	}
}
