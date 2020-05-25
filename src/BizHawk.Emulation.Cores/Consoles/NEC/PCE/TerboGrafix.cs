using System.Collections.Generic;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Emulation.Cores.Consoles.NEC.PCE
{
	[Core(CoreNames.TurboNyma, "Mednafen Team", true, false, "1.24.3", "", false)]
	public class TerboGrafix : NymaCore, IRegionable
	{
		[CoreConstructor(new[] { "PCE", "SGX" })]
		public TerboGrafix(GameInfo game, byte[] rom, CoreComm comm, string extension,
			NymaSettings settings, NymaSyncSettings syncSettings)
			: base(comm, "PCE", "PC Engine Controller", settings, syncSettings)
		{
			DoInit<LibNymaCore>(game, rom, null, "pce.wbx", extension);
		}
		public TerboGrafix(GameInfo game, Disc[] discs, CoreComm comm,
			NymaSettings settings, NymaSyncSettings syncSettings)
			: base(comm, "PCE", "PC Engine Controller", settings, syncSettings)
		{
			// TODO: detect GECD and only ask for the firmware we need
			var firmwares = new Dictionary<string, byte[]>
			{
				{ "FIRMWARE:syscard3.pce", comm.CoreFileProvider.GetFirmware("PCECD", "Bios", true) },
				{ "FIRMWARE:gecard.pce", comm.CoreFileProvider.GetFirmware("PCECD", "GE-Bios", true) },				
			};
			DoInit<LibNymaCore>(game, null, discs, "pce.wbx", null, firmwares);
		}
	}
}
