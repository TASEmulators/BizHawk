using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.NEC.PCFX
{
	[PortedCore(CoreNames.TST, "Mednafen Team", "1.32.1", "https://mednafen.github.io/releases/")]
	public class Tst : NymaCore
	{
		private Tst(CoreComm comm)
			: base(comm, VSystemID.Raw.NULL, null, null, null)
		{
		}

		private static NymaSettingsInfo _cachedSettingsInfo;

		public static NymaSettingsInfo CachedSettingsInfo(CoreComm comm)
		{
			if (_cachedSettingsInfo is null)
			{
				using var n = new Tst(comm);
				n.InitForSettingsInfo("pcfx.wbx");
				_cachedSettingsInfo = n.SettingsInfo.Clone();
			}

			return _cachedSettingsInfo;
		}

		[CoreConstructor(VSystemID.Raw.PCFX)]
		public Tst(CoreLoadParameters<NymaSettings, NymaSyncSettings> lp)
			: base(lp.Comm, VSystemID.Raw.PCFX, "PC-FX Controller", lp.Settings, lp.SyncSettings)
		{
			if (lp.Roms.Count > 0)
				throw new InvalidOperationException("To load a PC-FX game, please load the CUE file and not the BIN file.");
			var firmwareIDMap = new Dictionary<string, FirmwareID>
			{
				{ "FIRMWARE:pcfx.rom", new("PCFX", "BIOS") },
			};

			DoInit<LibNymaCore>(lp, "pcfx.wbx", firmwareIDMap);

			_cachedSettingsInfo ??= SettingsInfo.Clone();
		}

		protected override IDictionary<string, SettingOverride> SettingOverrides { get; } = new Dictionary<string, SettingOverride>
		{
			{ "pcfx.input.port1.multitap", new() { Hide = true } },
			{ "pcfx.input.port2.multitap", new() { Hide = true } },
			{ "pcfx.bios", new() { Hide = true } },
			{ "pcfx.fxscsi", new() { Hide = true } },
			{ "nyma.rtcinitialtime", new() { Hide = true } },
			{ "nyma.rtcrealtime", new() { Hide = true } },

			{ "pcfx.slstart", new() { NonSync = true, NoRestart = true } },
			{ "pcfx.slend", new() { NonSync = true, NoRestart = true } },

			{ "pcfx.mouse_sensitivity", new() { Hide = true } },
			{ "pcfx.nospritelimit", new() { NonSync = true } },
			{ "pcfx.high_dotclock_width", new() { NonSync = true } },
			{ "pcfx.rainbow.chromaip", new() { NonSync = true } },

			{ "pcfx.adpcm.suppress_channel_reset_clicks", new() { NonSync = true } },
			{ "pcfx.adpcm.emulate_buggy_codec", new() { NonSync = true } },

			{ "pcfx.resamp_quality", new() { NonSync = true } },
			{ "pcfx.resamp_rate_error", new() { Hide = true } },
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
