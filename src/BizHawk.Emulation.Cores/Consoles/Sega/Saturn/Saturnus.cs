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
		"https://mednafen.github.io/releases/", false)]
	public class Saturnus : NymaCore, IRegionable
	{
		[CoreConstructor("SAT")]
		public Saturnus(CoreComm comm, NymaSettings settings, NymaSyncSettings syncSettings)
			: base(comm, "SAT", "Saturn Controller Deck", settings, syncSettings)
		{
			throw new InvalidOperationException("To load a Saturn game, please load the CUE file and not the BIN file.");
		}

		public Saturnus(CoreComm comm, GameInfo game,
			IEnumerable<Disc> disks, NymaSettings settings, NymaSyncSettings syncSettings, bool deterministic)
			: base(comm, "SAT", "Saturn Controller Deck", settings, syncSettings)
		{
			var firmwares = new Dictionary<string, ValueTuple<string, string>>
			{
				{ "FIRMWARE:$J", ("SAT", "J") },
				{ "FIRMWARE:$U", ("SAT", "U") },
				{ "FIRMWARE:$KOF", ("SAT", "KOF95") },
				{ "FIRMWARE:$ULTRA", ("SAT", "ULTRAMAN") },
				// { "FIRMWARE:$SATAR", ("SAT", "AR") }, // action replay garbage
			};

			DoInit<LibNymaCore>(game, null, disks.ToArray(), "ss.wbx", null, deterministic, firmwares);
		}

		protected override IDictionary<string, string> SettingsOverrides { get; } = new Dictionary<string, string>
		{
			{ "ss.bios_jp", "$J" }, // FIRMWARE:
			{ "ss.bios_na_eu", "$U" }, // FIRMWARE:
			{ "ss.cart.kof95_path", "$KOF" }, // FIRMWARE:
			{ "ss.cart.ultraman_path", "$ULTRA" }, // FIRMWARE:
			{ "ss.cart.satar4mp_path", "$SATAR" }, // FIRMWARE:
			{ "ss.affinity.vdp2", null },
			{ "ss.dbg_exe_cdpath", null },
			{ "ss.dbg_exe_cem", null },
			{ "ss.dbg_exe_hh", null },
		};
		protected override ISet<string> NonSyncSettingNames { get; } = new HashSet<string>
		{
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
