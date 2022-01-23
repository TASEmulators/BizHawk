using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Sony.PSX
{
	[PortedCore(CoreNames.Nymashock, "Mednafen Team", "1.27.1", "https://mednafen.github.io/releases/")]
	public class Nymashock : NymaCore, IRegionable
	{
		[CoreConstructor(VSystemID.Raw.PSX)]
		public Nymashock(CoreLoadParameters<NymaSettings, NymaSyncSettings> lp)
			: base(lp.Comm, VSystemID.Raw.PSX, "PSX Front Panel", lp.Settings, lp.SyncSettings)
		{
			if (lp.Roms.Count > 0)
				throw new InvalidOperationException("To load a PSX game, please load the CUE file and not the BIN file.");
			var firmwares = new Dictionary<string, FirmwareID>
			{
				{ "FIRMWARE:$J", new("PSX", "J") },
				{ "FIRMWARE:$U", new("PSX", "U") },
				{ "FIRMWARE:$E", new("PSX", "E") },
			};
			DoInit<LibNymaCore>(lp, "shock.wbx", firmwares);
		}

		protected override IDictionary<string, SettingOverride> SettingOverrides { get; } = new Dictionary<string, SettingOverride>
		{
			{ "psx.bios_jp", new() { Hide = true , Default = "$J" } }, // FIRMWARE:
			{ "psx.bios_na", new() { Hide = true , Default = "$U" } }, // FIRMWARE:
			{ "psx.bios_eu", new() { Hide = true , Default = "$E" } }, // FIRMWARE:

			{ "psx.input.port1.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port2.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port3.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port4.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port5.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port6.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port7.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port8.gun_chairs", new() { NonSync = true } },

			{ "psx.correct_aspect", new() { NonSync = true, Default = "0" } },

			/*{ "ss.affinity.vdp2", new() { Hide = true } },
			{ "ss.dbg_exe_cdpath", new() { Hide = true } },
			{ "ss.dbg_exe_cem", new() { Hide = true } },
			{ "ss.dbg_exe_hh", new() { Hide = true } },

			{ "ss.scsp.resamp_quality", new() { NonSync = true } }, // Don't set NoRestart = true for this
			{ "ss.input.mouse_sensitivity", new() { Hide = true } },

			{ "ss.input.port1.gun_chairs", new() { NonSync = true } },
			{ "ss.input.port2.gun_chairs", new() { NonSync = true } },
			{ "ss.input.port3.gun_chairs", new() { NonSync = true } },
			{ "ss.input.port4.gun_chairs", new() { NonSync = true } },
			{ "ss.input.port5.gun_chairs", new() { NonSync = true } },
			{ "ss.input.port6.gun_chairs", new() { NonSync = true } },
			{ "ss.input.port7.gun_chairs", new() { NonSync = true } },
			{ "ss.input.port8.gun_chairs", new() { NonSync = true } },
			{ "ss.input.port9.gun_chairs", new() { NonSync = true } },
			{ "ss.input.port10.gun_chairs", new() { NonSync = true } },
			{ "ss.input.port11.gun_chairs", new() { NonSync = true } },
			{ "ss.input.port12.gun_chairs", new() { NonSync = true } },

			{ "ss.slstart", new() { NonSync = true } },
			{ "ss.slend", new() { NonSync = true } },
			{ "ss.h_overscan", new() { NonSync = true } },
			{ "ss.h_blend", new() { NonSync = true } },
			{ "ss.correct_aspect", new() { NonSync = true, Default = "0" } },
			{ "ss.slstartp", new() { NonSync = true } },
			{ "ss.slendp", new() { NonSync = true } },*/
		};

		/*protected override HashSet<string> ComputeHiddenPorts()
		{
			var devCount = 12;
			if (SettingsQuery("ss.input.sport1.multitap") != "1")
				devCount -= 5;
			if (SettingsQuery("ss.input.sport2.multitap") != "1")
				devCount -= 5;
			var ret = new HashSet<string>();
			for (var i = 1; i <= 12; i++)
			{
				if (i > devCount)
					ret.Add($"port{i}");
			}
			return ret;
		}*/
	}
}
