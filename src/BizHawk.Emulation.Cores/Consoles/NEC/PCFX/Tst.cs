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
		"https://mednafen.github.io/releases/", false)]
	public class Tst : NymaCore
	{
		[CoreConstructor("PCFX")]
		public Tst(CoreComm comm, NymaSettings settings, NymaSyncSettings syncSettings)
			: base(comm, "PCFX", "PCFX Controller Deck", settings, syncSettings)
		{
			throw new InvalidOperationException("To load a PC-FX game, please load the CUE file and not the BIN file.");
		}

		public Tst(CoreComm comm, GameInfo game,
			IEnumerable<Disc> disks, NymaSettings settings, NymaSyncSettings syncSettings, bool deterministic)
			: base(comm, "PCFX", "PCFX Controller Deck", settings, syncSettings)
		{
			var firmwares = new Dictionary<string, ValueTuple<string, string>>
			{
				{ "FIRMWARE:pcfx.rom", ("PCFX", "BIOS") },
			};

			DoInit<LibNymaCore>(game, null, disks.ToArray(), "pcfx.wbx", null, deterministic, firmwares);
		}

		protected override IDictionary<string, string> SettingsOverrides { get; } = new Dictionary<string, string>
		{
			{ "pcfx.input.port1.multitap", null },
			{ "pcfx.input.port2.multitap", null },
			{ "pcfx.bios", null },
			{ "pcfx.fxscsi", null },
			{ "nyma.rtcinitialtime", null },
			{ "nyma.rtcrealtime", null },
		};
		protected override ISet<string> NonSyncSettingNames { get; } = new HashSet<string>
		{
			"pcfx.slstart", "pcfx.slend",
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
