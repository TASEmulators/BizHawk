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
	[Core("T. S. T.", "Mednafen Team", true, true, "0.9.44.1",
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
			var bios = comm.CoreFileProvider.GetFirmware("PCFX", "BIOS", true);
			if (bios.Length != 1024 * 1024)
				throw new InvalidOperationException("Wrong size BIOS file!");
			
			var firmwares = new Dictionary<string, byte[]>
			{
				{ "FIRMWARE:pcfx.rom", bios },
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
	}
}
