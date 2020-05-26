using System.Collections.Generic;
using System.Linq;
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
			var firmwares = new Dictionary<string, byte[]>();
			var types = discs.Select(d => new DiscIdentifier(d).DetectDiscType())
				.ToList();
			if (types.Contains(DiscType.TurboCD))
				firmwares.Add("FIRMWARE:syscard3.pce", comm.CoreFileProvider.GetFirmware("PCECD", "Bios", true));
			if (types.Contains(DiscType.TurboGECD))
				firmwares.Add("FIRMWARE:gecard.pce", comm.CoreFileProvider.GetFirmware("PCECD", "GE-Bios", true));
			DoInit<LibNymaCore>(game, null, discs, "pce.wbx", null, firmwares);
		}

		// pce always has two layers, sgx always has 4, and mednafen knows this
		public override string SystemId => SettingsInfo.LayerNames.Count == 4 ? "SGX" : "PCE";

		protected override ICollection<string> HiddenSettings { get; } = new[]
		{
			// handled by hawk
			"pce.cdbios",
			"pce.gecdbios",
			// so fringe i don't want people bothering me about it
			"pce.resamp_rate_error",
			"pce.vramsize",
		};
	}
}
