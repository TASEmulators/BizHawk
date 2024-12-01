using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Faust
{
	[PortedCore(CoreNames.Faust, "Mednafen Team", "1.32.1", "https://mednafen.github.io/releases/")]
	public class Faust : NymaCore, IRegionable
	{
		private Faust(CoreComm comm)
			: base(comm, VSystemID.Raw.NULL, null, null, null)
		{
		}

		private static NymaSettingsInfo _cachedSettingsInfo;

		public static NymaSettingsInfo CachedSettingsInfo(CoreComm comm)
		{
			if (_cachedSettingsInfo is null)
			{
				using var n = new Faust(comm);
				n.InitForSettingsInfo("faust.wbx");
				_cachedSettingsInfo = n.SettingsInfo.Clone();
			}

			return _cachedSettingsInfo;
		}

		[CoreConstructor(VSystemID.Raw.SNES)]
		public Faust(GameInfo game, byte[] rom, CoreComm comm, string extension,
			NymaSettings settings, NymaSyncSettings syncSettings, bool deterministic)
			: base(comm, VSystemID.Raw.SNES, "SNES Controller", settings, syncSettings)
		{
			if (deterministic)
				// force ST renderer
				SettingOverrides.Add("snes_faust.renderer", new() { Hide = true, Default = "0" });

			DoInit<LibNymaCore>(game, rom, null, "faust.wbx", extension, deterministic);

			_cachedSettingsInfo ??= SettingsInfo.Clone();
		}

		protected override HashSet<string> ComputeHiddenPorts()
		{
			var devCount = 8;
			if (SettingsQuery("snes_faust.input.sport1.multitap") != "1")
				devCount -= 3;
			if (SettingsQuery("snes_faust.input.sport2.multitap") != "1")
				devCount -= 3;
			var ret = new HashSet<string>();
			for (var i = 1; i <= 8; i++)
			{
				if (i > devCount)
					ret.Add($"port{i}");
			}
			return ret;
		}

		protected override IDictionary<string, SettingOverride> SettingOverrides { get; } = new Dictionary<string, SettingOverride>
		{
			{ "snes_faust.renderer", new() { Default = "mt" } },
			{ "snes_faust.affinity.ppu", new() { Hide = true } },
			{ "snes_faust.affinity.msu1.audio", new() { Hide = true } },
			{ "snes_faust.affinity.msu1.data", new() { Hide = true } },
			{ "snes_faust.frame_begin_vblank", new() { Hide = true } },
			{ "snes_faust.msu1.resamp_quality", new() { Hide = true } },
			{ "snes_faust.spex", new() { Hide = true } },
			{ "snes_faust.spex.sound", new() { Hide = true } },
			{ "nyma.rtcinitialtime", new() { Hide = true } },
			{ "nyma.rtcrealtime", new() { Hide = true } },

			{ "snes_faust.resamp_rate_error", new() { Hide = true } },
			{ "snes_faust.resamp_quality", new() { NonSync = true } },
			{ "snes_faust.correct_aspect", new() { NonSync = true } },
			{ "snes_faust.slstart", new() { NonSync = true } },
			{ "snes_faust.slend", new() { NonSync = true } },
			{ "snes_faust.slstartp", new() { NonSync = true } },
			{ "snes_faust.slendp", new() { NonSync = true } },
			{ "snes_faust.h_filter", new() { NonSync = true } },
		};
	}
}
