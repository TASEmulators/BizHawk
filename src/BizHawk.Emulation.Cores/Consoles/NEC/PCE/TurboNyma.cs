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
	[Core(CoreNames.TurboNyma, "Mednafen Team", true, true, "1.24.3", "https://mednafen.github.io/releases/", false, "PCE")]
	public class TurboNyma : NymaCore, IRegionable, IPceGpuView
	{
		private readonly LibTurboNyma _turboNyma;

		[CoreConstructor("PCE")]
		[CoreConstructor("SGX")]
		public TurboNyma(GameInfo game, byte[] rom, CoreComm comm, string extension,
			NymaSettings settings, NymaSyncSettings syncSettings, bool deterministic)
			: base(comm, "PCE", "PC Engine Controller", settings, syncSettings)
		{
			if (game["BRAM"])
				SettingOverrides["pce.disable_bram_hucard"].Default = "0";
			_turboNyma = DoInit<LibTurboNyma>(game, rom, null, "turbo.wbx", extension, deterministic);
		}
		[CoreConstructor("PCECD")]
		public TurboNyma(CoreLoadParameters<NymaSettings, NymaSyncSettings> lp)
			: base(lp.Comm, "PCE", "PC Engine Controller", lp.Settings, lp.SyncSettings)
		{
			var ids = lp.Discs.Select(dg => dg.DiscType).ToList();
			var firmwares = new Dictionary<string, (string, string)>();
			if (ids.Contains(DiscType.TurboCD))
				firmwares.Add("FIRMWARE:syscard3.pce", ("PCECD", "Bios"));
			if (ids.Contains(DiscType.TurboGECD))
				firmwares.Add("FIRMWARE:gecard.pce", ("PCECD", "GE-Bios"));
			_turboNyma = DoInit<LibTurboNyma>(lp, "turbo.wbx", firmwares);
		}

		public override string SystemId => IsSgx ? "SGX" : "PCE";

		protected override IDictionary<string, SettingOverride> SettingOverrides { get; } = new Dictionary<string, SettingOverride>
		{
			// handled by hawk
			{ "pce.cdbios", new SettingOverride { Hide = true } },
			{ "pce.gecdbios", new SettingOverride { Hide = true } },
			// so fringe i don't want people bothering me about it
			{ "pce.resamp_rate_error", new SettingOverride { Hide = true } },
			{ "pce.vramsize", new SettingOverride { Hide = true } },
			// match hawk behavior on BRAM, instead of giving every game BRAM
			{ "pce.disable_bram_hucard", new SettingOverride { Hide = true, Default = "1" } },
			// nyma settings that don't apply here
			// TODO: not quite happy with how this works out
			{ "nyma.rtcinitialtime", new SettingOverride { Hide = true } },
			{ "nyma.rtcrealtime", new SettingOverride { Hide = true } },
			// these can be changed dynamically
			{ "pce.slstart", new SettingOverride { NonSync = true, NoRestart = true } },
			{ "pce.slend", new SettingOverride { NonSync = true, NoRestart = true } },

			{ "pce.h_overscan", new SettingOverride { NonSync = true } },
			{ "pce.mouse_sensitivity", new SettingOverride { Hide = true } },
			{ "pce.nospritelimit", new SettingOverride { NonSync = true } },
			{ "pce.resamp_quality", new SettingOverride { NonSync = true } },

			{ "pce.cdpsgvolume", new SettingOverride { NonSync = true, NoRestart = true } },
			{ "pce.cddavolume", new SettingOverride { NonSync = true, NoRestart = true } },
			{ "pce.adpcmvolume", new SettingOverride { NonSync = true, NoRestart = true } },
		};

		protected override HashSet<string> ComputeHiddenPorts()
		{
			if (SettingsQuery("pce.input.multitap") == "1")
			{
				return new HashSet<string>();
			}

			return new HashSet<string>
			{
				"port2", "port3", "port4", "port5"
			};
		}

		// pce always has two layers, sgx always has 4, and mednafen knows this
		public bool IsSgx => SettingsInfo.LayerNames.Count == 4;

		public unsafe void GetGpuData(int vdc, Action<PceGpuData> callback)
		{
			using(_exe.EnterExit())
			{
				var palScratch = new int[512];
				var v = new PceGpuData();
				_turboNyma.GetVramInfo(v, vdc);
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

	public abstract class LibTurboNyma : LibNymaCore
	{
		[BizImport(CallingConvention.Cdecl, Compatibility = true)]
		public abstract void GetVramInfo([Out]PceGpuData v, int vdcIndex);
	}
}
