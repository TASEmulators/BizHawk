using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Sony.PSX
{
	[PortedCore(CoreNames.Nymashock, "Mednafen Team", "1.27.1", "https://mednafen.github.io/releases/")]
	public class Nymashock : NymaCore, IRegionable, ICycleTiming
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

			{ "psx.input.analog_mode_ct", new() { Hide = true } }, // probably don't want this
			{ "psx.input.analog_mode_ct.compare", new() { Hide = true } },

			{ "Virtual Port 1", new() { Default = "dualshock" } },
			{ "Virtual Port 2", new() { Default = "none" } },
			{ "Virtual Port 3", new() { Default = "none" } },
			{ "Virtual Port 4", new() { Default = "none" } },
			{ "Virtual Port 5", new() { Default = "none" } },
			{ "Virtual Port 6", new() { Default = "none" } },
			{ "Virtual Port 7", new() { Default = "none" } },
			{ "Virtual Port 8", new() { Default = "none" } },

			{ "psx.input.port2.memcard", new() { Default = "0" } },
			{ "psx.input.port3.memcard", new() { Default = "0" } },
			{ "psx.input.port4.memcard", new() { Default = "0" } },
			{ "psx.input.port5.memcard", new() { Default = "0" } },
			{ "psx.input.port6.memcard", new() { Default = "0" } },
			{ "psx.input.port7.memcard", new() { Default = "0" } },
			{ "psx.input.port8.memcard", new() { Default = "0" } },

			{ "psx.input.port1.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port2.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port3.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port4.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port5.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port6.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port7.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port8.gun_chairs", new() { NonSync = true } },

			{ "psx.dbg_exe_cdpath", new() { Hide = true } },

			{ "psx.spu.resamp_quality", new() { NonSync = true } },
			{ "psx.input.mouse_sensitivity", new() { Hide = true } },

			{ "psx.slstart", new() { NonSync = true } },
			{ "psx.slend", new() { NonSync = true } },
			{ "psx.h_overscan", new() { NonSync = true } },
			{ "psx.correct_aspect", new() { NonSync = true } },
			{ "psx.slstartp", new() { NonSync = true } },
			{ "psx.slendp", new() { NonSync = true } },

			{ "nyma.rtcinitialtime", new() { Hide = true } },
			{ "nyma.rtcrealtime", new() { Hide = true } },
		};
	}
}
