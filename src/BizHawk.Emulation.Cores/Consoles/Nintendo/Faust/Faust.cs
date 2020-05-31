using System.Collections.Generic;
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

		protected override IDictionary<string, string> SettingsOverrides { get; } = new Dictionary<string, string>
		{
			{ "snes_faust.renderer", null },
			{ "snes_faust.affinity.ppu", null },
			{ "snes_faust.affinity.msu1.audio", null },
			{ "snes_faust.affinity.msu1.data", null },
			{ "snes_faust.frame_begin_vblank", null },
			{ "snes_faust.msu1.resamp_quality", null },
			{ "snes_faust.correct_aspect", null },
			{ "snes_faust.spex", null },
			{ "snes_faust.spex.sound", null },
			{ "nyma.rtcinitialtime", null },
			{ "nyma.rtcrealtime", null },
		};
	}
}
