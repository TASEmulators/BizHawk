using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Emulation.Cores.Consoles.NEC.PCE
{
	[Core(CoreNames.TurboTurboNyma, "Mednafen Team", true, true, "1.24.3", "https://mednafen.github.io/releases/", false)]
	public class TerboGrafixSanic : NymaCore, IRegionable, IPceGpuView
	{
		private readonly LibTerboGrafixSanic _terboGrafix;

		[CoreConstructor(new[] { "PCE", "SGX" })]
		public TerboGrafixSanic(GameInfo game, byte[] rom, CoreComm comm, string extension,
			NymaSettings settings, NymaSyncSettings syncSettings, bool deterministic)
			: base(comm, "PCE", "PC Engine Controller", settings, syncSettings)
		{
			_terboGrafix = DoInit<LibTerboGrafixSanic>(game, rom, null, "pce-fast.wbx", extension, deterministic);
		}
		public TerboGrafixSanic(GameInfo game, Disc[] discs, CoreComm comm,
			NymaSettings settings, NymaSyncSettings syncSettings, bool deterministic)
			: base(comm, "PCE", "PC Engine Controller", settings, syncSettings)
		{
			var firmwares = new Dictionary<string, byte[]>
			{
				{ "FIRMWARE:syscard3.pce", comm.CoreFileProvider.GetFirmware("PCECD", "Bios", true) },
			};
			_terboGrafix = DoInit<LibTerboGrafixSanic>(game, null, discs, "pce-fast.wbx", null, deterministic, firmwares);
		}

		public override string SystemId => IsSgx ? "SGX" : "PCE";

		protected override IDictionary<string, string> SettingsOverrides { get; } = new Dictionary<string, string>
		{
			{ "pce_fast.correct_aspect", null },
			{ "pce_fast.mouse_sensitivity", null },
			{ "pce_fast.disable_softreset", null },
			{ "pce_fast.cdbios", null },
			{ "nyma.rtcinitialtime", null },
			{ "nyma.rtcrealtime", null },
		};
		protected override ISet<string> NonSyncSettingNames { get; } = new HashSet<string>
		{
			"pce_fast.slstart", "pce_fast.slend",
		};

		protected override IDictionary<string, string> ButtonNameOverrides { get; } = new Dictionary<string, string>
		{
			{ "RIGHT →", "Right up my arse" },
		};

		// pce always has two layers, sgx always has 4, and mednafen knows this
		public bool IsSgx => SettingsInfo.LayerNames.Count == 4;

		public unsafe void GetGpuData(int vdc, Action<PceGpuData> callback)
		{
			using(_exe.EnterExit())
			{
				var palScratch = new int[512];
				var v = new PceGpuData();
				_terboGrafix.GetVramInfo(v, vdc);
				fixed(int* p = palScratch)
				{
					for (var i = 0; i < 512; i++)
						p[i] = v.PaletteCache[i] | unchecked((int)0xff000000);
					v.PaletteCache = p;
					callback(v);
				}
				
			}
		}
	}

	public abstract class LibTerboGrafixSanic : LibNymaCore
	{
		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract void GetVramInfo([Out]PceGpuData v, int vdcIndex);
	}
}
