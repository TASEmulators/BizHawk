using System.Collections.Generic;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Faust
{
	[Core("Faust", "Mednafen Team", true, true, "1.26.1", "https://mednafen.github.io/releases/", false)]
	public class Faust : NymaCore, IRegionable
	{
		[CoreConstructor("SNES")]
		public Faust(GameInfo game, byte[] rom, CoreComm comm, string extension,
			NymaSettings settings, NymaSyncSettings syncSettings, bool deterministic)
			: base(comm, "SNES", "SNES Controller", settings, syncSettings)
		{
			if (deterministic)
				// force ST renderer
				SettingOverrides.Add("snes_faust.renderer", new SettingOverride { Hide = true, Default = "0" });

			DoInit<LibNymaCore>(game, rom, null, "faust.wbx", extension, deterministic);
		}

		protected override HashSet<string> ComputeHiddenPorts()
		{
			var devCount = 8;
			if (SettingsQuery("snes_faust.input.sport1.multitap") != "1")
				devCount -= 3;
			if (SettingsQuery("snes_faust.input.sport2.multitap") != "1")
				devCount -= 3;
			var ret = new HashSet<string>();
			for (var i = 1; i <= 8; i++)
			{
				if (i > devCount)
					ret.Add($"port{i}");
			}
			return ret;
		}

		protected override IDictionary<string, SettingOverride> SettingOverrides { get; } = new Dictionary<string, SettingOverride>
		{
			{ "snes_faust.affinity.ppu", new SettingOverride { Hide = true } },
			{ "snes_faust.affinity.msu1.audio", new SettingOverride { Hide = true } },
			{ "snes_faust.affinity.msu1.data", new SettingOverride { Hide = true } },
			{ "snes_faust.frame_begin_vblank", new SettingOverride { Hide = true } },
			{ "snes_faust.msu1.resamp_quality", new SettingOverride { Hide = true } },
			{ "snes_faust.spex", new SettingOverride { Hide = true } },
			{ "snes_faust.spex.sound", new SettingOverride { Hide = true } },
			{ "nyma.rtcinitialtime", new SettingOverride { Hide = true } },
			{ "nyma.rtcrealtime", new SettingOverride { Hide = true } },

			{ "snes_faust.resamp_rate_error", new SettingOverride { Hide = true } },
			{ "snes_faust.resamp_quality", new SettingOverride { NonSync = true } },
			{ "snes_faust.correct_aspect", new SettingOverride { NonSync = true } },
			{ "snes_faust.slstart", new SettingOverride { NonSync = true } },
			{ "snes_faust.slend", new SettingOverride { NonSync = true } },
			{ "snes_faust.slstartp", new SettingOverride { NonSync = true } },
			{ "snes_faust.slendp", new SettingOverride { NonSync = true } },
			{ "snes_faust.h_filter", new SettingOverride { NonSync = true } },
		};
	}
}
