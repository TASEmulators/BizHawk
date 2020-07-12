using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Sega.Saturn;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Emulation.DiscSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.IO;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.NEC.PCFX
{
	[Core("T. S. T.", "Mednafen Team", true, true, "1.24.3",
		"https://mednafen.github.io/releases/", false, "PC-FX")]
	public class Tst : NymaCore
	{
		[CoreConstructor("PCFX")]
		public Tst(CoreLoadParameters<NymaSettings, NymaSyncSettings> lp)
			: base(lp.Comm, "PCFX", "PC-FX Controller", lp.Settings, lp.SyncSettings)
		{
			if (lp.Roms.Count > 0)
				throw new InvalidOperationException("To load a PC-FX game, please load the CUE file and not the BIN file.");
			var firmwares = new Dictionary<string, (string, string)>
			{
				{ "FIRMWARE:pcfx.rom", ("PCFX", "BIOS") },
			};

			DoInit<LibNymaCore>(lp, "pcfx.wbx", firmwares);
		}

		protected override IDictionary<string, SettingOverride> SettingOverrides { get; } = new Dictionary<string, SettingOverride>
		{
			{ "pcfx.input.port1.multitap", new SettingOverride { Hide = true } },
			{ "pcfx.input.port2.multitap", new SettingOverride { Hide = true } },
			{ "pcfx.bios", new SettingOverride { Hide = true } },
			{ "pcfx.fxscsi", new SettingOverride { Hide = true } },
			{ "nyma.rtcinitialtime", new SettingOverride { Hide = true } },
			{ "nyma.rtcrealtime", new SettingOverride { Hide = true } },

			{ "pcfx.slstart", new SettingOverride { NonSync = true, NoRestart = true } },
			{ "pcfx.slend", new SettingOverride { NonSync = true, NoRestart = true } },

			{ "pcfx.mouse_sensitivity", new SettingOverride { Hide = true } },
			{ "pcfx.nospritelimit", new SettingOverride { NonSync = true } },
			{ "pcfx.high_dotclock_width", new SettingOverride { NonSync = true } },
			{ "pcfx.rainbow.chromaip", new SettingOverride { NonSync = true } },

			{ "pcfx.adpcm.suppress_channel_reset_clicks", new SettingOverride { NonSync = true } },
			{ "pcfx.adpcm.emulate_buggy_codec", new SettingOverride { NonSync = true } },

			{ "pcfx.resamp_quality", new SettingOverride { NonSync = true } },
			{ "pcfx.resamp_rate_error", new SettingOverride { Hide = true } },
		};

		protected override HashSet<string> ComputeHiddenPorts()
		{
			// NB: Since we're hiding these settings up above, this will always trim us down to 2 ports
			var devCount = 8;
			if (SettingsQuery("pcfx.input.port1.multitap") != "1")
				devCount -= 3;
			if (SettingsQuery("pcfx.input.port2.multitap") != "1")
				devCount -= 3;
			var ret = new HashSet<string>();
			for (var i = 1; i <= 8; i++)
			{
				if (i > devCount)
					ret.Add($"port{i}");
			}
			return ret;
		}
	}
}
