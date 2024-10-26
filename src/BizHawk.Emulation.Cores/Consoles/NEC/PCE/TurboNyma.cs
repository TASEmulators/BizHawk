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
	[PortedCore(CoreNames.TurboNyma, "Mednafen Team", "1.32.1", "https://mednafen.github.io/releases/")]
	public class TurboNyma : NymaCore, IRegionable, IPceGpuView
	{
		private TurboNyma(CoreComm comm)
			: base(comm, VSystemID.Raw.NULL, null, null, null)
		{
		}

		private static NymaSettingsInfo _cachedSettingsInfo;

		public static NymaSettingsInfo CachedSettingsInfo(CoreComm comm)
		{
			if (_cachedSettingsInfo is null)
			{
				using var n = new TurboNyma(comm);
				n.InitForSettingsInfo("turbo.wbx");
				_cachedSettingsInfo = n.SettingsInfo.Clone();
			}

			return _cachedSettingsInfo;
		}

		private readonly LibTurboNyma _turboNyma;
		private readonly bool _hasCds;

		[CoreConstructor(VSystemID.Raw.PCE)]
		[CoreConstructor(VSystemID.Raw.SGX)]
		[CoreConstructor(VSystemID.Raw.PCECD)]
		[CoreConstructor(VSystemID.Raw.SGXCD)]
		public TurboNyma(CoreLoadParameters<NymaSettings, NymaSyncSettings> lp)
			: base(lp.Comm, VSystemID.Raw.PCE, "PC Engine Controller", lp.Settings, lp.SyncSettings)
		{
			var firmwareIDMap = new Dictionary<string, FirmwareID>();
			if (lp.Discs.Count > 0)
			{
				_hasCds = true;
				var ids = lp.Discs.Select(dg => dg.DiscType).ToList();
				if (ids.Contains(DiscType.TurboCD))
					firmwareIDMap.Add("FIRMWARE:syscard3.pce", new("PCECD", "Bios"));
				if (ids.Contains(DiscType.TurboGECD))
					firmwareIDMap.Add("FIRMWARE:gecard.pce", new("PCECD", "GE-Bios"));
			}
			else if (lp.Roms.Count == 1)
			{
				if (lp.Game["BRAM"])
					SettingOverrides["pce.disable_bram_hucard"].Default = "0";
			}

			_turboNyma = DoInit<LibTurboNyma>(lp, "turbo.wbx", firmwareIDMap);

			_cachedSettingsInfo ??= SettingsInfo.Clone();
		}

		public override string SystemId => IsSgx
			? _hasCds ? VSystemID.Raw.SGXCD : VSystemID.Raw.SGX
			: _hasCds ? VSystemID.Raw.PCECD : VSystemID.Raw.PCE;

		protected override IDictionary<string, SettingOverride> SettingOverrides { get; } = new Dictionary<string, SettingOverride>
		{
			// handled by hawk
			{ "pce.cdbios", new() { Hide = true } },
			{ "pce.gecdbios", new() { Hide = true } },
			// so fringe i don't want people bothering me about it
			{ "pce.resamp_rate_error", new() { Hide = true } },
			{ "pce.vramsize", new() { Hide = true } },
			// match hawk behavior on BRAM, instead of giving every game BRAM
			{ "pce.disable_bram_hucard", new() { Hide = true, Default = "1" } },
			// nyma settings that don't apply here
			// TODO: not quite happy with how this works out
			{ "nyma.rtcinitialtime", new() { Hide = true } },
			{ "nyma.rtcrealtime", new() { Hide = true } },
			// these can be changed dynamically
			{ "pce.slstart", new() { NonSync = true, NoRestart = true } },
			{ "pce.slend", new() { NonSync = true, NoRestart = true } },

			{ "pce.h_overscan", new() { NonSync = true } },
			{ "pce.mouse_sensitivity", new() { Hide = true } },
			{ "pce.nospritelimit", new() { NonSync = true } },
			{ "pce.resamp_quality", new() { NonSync = true } },

			{ "pce.cdpsgvolume", new() { NonSync = true, NoRestart = true } },
			{ "pce.cddavolume", new() { NonSync = true, NoRestart = true } },
			{ "pce.adpcmvolume", new() { NonSync = true, NoRestart = true } },
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
