using BizHawk.Common;
using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Emulation.DiscSystem;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace BizHawk.Emulation.Cores.Consoles.Sega.Saturn
{
	[Core("Saturnus", "Mednafen Team", true, true, "1.24.3",
		"https://mednafen.github.io/releases/", false, "Saturn")]
	public class Saturnus : NymaCore, IRegionable
	{
		[CoreConstructor("SAT")]
		public Saturnus(CoreLoadParameters<NymaSettings, NymaSyncSettings> lp)
			: base(lp.Comm, "SAT", "Saturn Controller", lp.Settings, lp.SyncSettings)
		{
			if (lp.Roms.Count > 0)
				throw new InvalidOperationException("To load a Saturn game, please load the CUE file and not the BIN file.");
			var firmwares = new Dictionary<string, (string, string)>
			{
				{ "FIRMWARE:$J", ("SAT", "J") },
				{ "FIRMWARE:$U", ("SAT", "U") },
				{ "FIRMWARE:$KOF", ("SAT", "KOF95") },
				{ "FIRMWARE:$ULTRA", ("SAT", "ULTRAMAN") },
				// { "FIRMWARE:$SATAR", ("SAT", "AR") }, // action replay garbage
			};
			DoInit<LibNymaCore>(lp, "ss.wbx", firmwares);
		}

		protected override IDictionary<string, SettingOverride> SettingOverrides { get; } = new Dictionary<string, SettingOverride>
		{
			{ "ss.bios_jp", new SettingOverride { Hide = true , Default = "$J" } }, // FIRMWARE:
			{ "ss.bios_na_eu", new SettingOverride { Hide = true , Default = "$U" } }, // FIRMWARE:
			{ "ss.cart.kof95_path", new SettingOverride { Hide = true , Default = "$KOF" } }, // FIRMWARE:
			{ "ss.cart.ultraman_path", new SettingOverride { Hide = true , Default = "$ULTRA" } }, // FIRMWARE:
			{ "ss.cart.satar4mp_path", new SettingOverride { Hide = true , Default = "$SATAR" } }, // FIRMWARE:
			{ "ss.affinity.vdp2", new SettingOverride { Hide = true } },
			{ "ss.dbg_exe_cdpath", new SettingOverride { Hide = true } },
			{ "ss.dbg_exe_cem", new SettingOverride { Hide = true } },
			{ "ss.dbg_exe_hh", new SettingOverride { Hide = true } },

			{ "ss.scsp.resamp_quality", new SettingOverride { NonSync = true } }, // Don't set NoRestart = true for this
			{ "ss.input.mouse_sensitivity", new SettingOverride { Hide = true } },

			{ "ss.input.port1.gun_chairs", new SettingOverride { NonSync = true } },
			{ "ss.input.port2.gun_chairs", new SettingOverride { NonSync = true } },
			{ "ss.input.port3.gun_chairs", new SettingOverride { NonSync = true } },
			{ "ss.input.port4.gun_chairs", new SettingOverride { NonSync = true } },
			{ "ss.input.port5.gun_chairs", new SettingOverride { NonSync = true } },
			{ "ss.input.port6.gun_chairs", new SettingOverride { NonSync = true } },
			{ "ss.input.port7.gun_chairs", new SettingOverride { NonSync = true } },
			{ "ss.input.port8.gun_chairs", new SettingOverride { NonSync = true } },
			{ "ss.input.port9.gun_chairs", new SettingOverride { NonSync = true } },
			{ "ss.input.port10.gun_chairs", new SettingOverride { NonSync = true } },
			{ "ss.input.port11.gun_chairs", new SettingOverride { NonSync = true } },
			{ "ss.input.port12.gun_chairs", new SettingOverride { NonSync = true } },

			{ "ss.slstart", new SettingOverride { NonSync = true } },
			{ "ss.slend", new SettingOverride { NonSync = true } },
			{ "ss.h_overscan", new SettingOverride { NonSync = true } },
			{ "ss.h_blend", new SettingOverride { NonSync = true } },
			{ "ss.correct_aspect", new SettingOverride { NonSync = true } },
			{ "ss.slstartp", new SettingOverride { NonSync = true } },
			{ "ss.slendp", new SettingOverride { NonSync = true } },
		};

		protected override HashSet<string> ComputeHiddenPorts()
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
		}
	}
}
