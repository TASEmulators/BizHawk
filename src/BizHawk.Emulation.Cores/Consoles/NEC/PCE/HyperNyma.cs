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
	[Core(CoreNames.HyperNyma, "Mednafen Team", true, true, "1.24.3", "https://mednafen.github.io/releases/", false)]
	public class HyperNyma : NymaCore, IRegionable, IPceGpuView
	{
		private readonly LibHyperNyma _terboGrafix;

		[CoreConstructor(new[] { "PCE", "SGX" })]
		public HyperNyma(GameInfo game, byte[] rom, CoreComm comm, string extension,
			NymaSettings settings, NymaSyncSettings syncSettings, bool deterministic)
			: base(comm, "PCE", "PC Engine Controller", settings, syncSettings)
		{
			_terboGrafix = DoInit<LibHyperNyma>(game, rom, null, "pce-fast.wbx", extension, deterministic);
		}
		public HyperNyma(GameInfo game, Disc[] discs, CoreComm comm,
			NymaSettings settings, NymaSyncSettings syncSettings, bool deterministic)
			: base(comm, "PCE", "PC Engine Controller", settings, syncSettings)
		{
			var firmwares = new Dictionary<string, ValueTuple<string, string>>
			{
				{ "FIRMWARE:syscard3.pce", ("PCECD", "Bios") },
				// { "FIRMWARE:gecard.pce", ("PCECD", "GE-Bios") },
			};
			_terboGrafix = DoInit<LibHyperNyma>(game, null, discs, "pce-fast.wbx", null, deterministic, firmwares);
		}

		public override string SystemId => IsSgx ? "SGX" : "PCE";

		protected override IDictionary<string, SettingOverride> SettingOverrides { get; } = new Dictionary<string, SettingOverride>
		{
			{ "pce_fast.mouse_sensitivity", new SettingOverride { Hide = true } },
			{ "pce_fast.disable_softreset", new SettingOverride { Hide = true } },
			{ "pce_fast.cdbios", new SettingOverride { Hide = true } },
			{ "nyma.rtcinitialtime", new SettingOverride { Hide = true } },
			{ "nyma.rtcrealtime", new SettingOverride { Hide = true } },
			{ "pce_fast.slstart", new SettingOverride { NonSync = true, NoRestart = true } },
			{ "pce_fast.slend", new SettingOverride { NonSync = true, NoRestart = true } },

			{ "pce_fast.correct_aspect", new SettingOverride { NonSync = true } },
			{ "pce_fast.nospritelimit", new SettingOverride { NonSync = true } },
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

	public abstract class LibHyperNyma : LibNymaCore
	{
		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract void GetVramInfo([Out]PceGpuData v, int vdcIndex);
	}
}
