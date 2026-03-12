using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Emulation.DiscSystem;

namespace BizHawk.Emulation.Cores.Sony.PSX
{
	[PortedCore(CoreNames.Nymashock, "Mednafen Team", "1.32.1", "https://mednafen.github.io/releases/")]
	public class Nymashock : NymaCore, IRegionable, ICycleTiming, IRedumpDiscChecksumInfo
	{
		public string RomDetails { get; }

		private readonly IReadOnlyList<Disc> _discs;

		protected override void AddAxis(
			ControllerDefinition ret,
			string name,
			bool isReversed,
			ref ControllerThunk thunk,
			int thunkWriteOffset)
		{
			if (name.EndsWithOrdinal(" Left Stick Up / Down") || name.EndsWithOrdinal(" Left Stick Left / Right")
				|| name.EndsWithOrdinal(" Right Stick Up / Down") || name.EndsWithOrdinal(" Right Stick Left / Right"))
			{
				ret.AddAxis(name, 0.RangeTo(0xFF), 0x80, isReversed);
				thunk = (c, b) =>
				{
					b[thunkWriteOffset] = 0;
					b[thunkWriteOffset + 1] = (byte) c.AxisValue(name);
				};
			}
			else
			{
				base.AddAxis(ret, name, isReversed: isReversed, ref thunk, thunkWriteOffset);
			}
		}

		private Nymashock(CoreComm comm)
			: base(comm, VSystemID.Raw.NULL, null, null, null)
		{
		}

		private static NymaSettingsInfo _cachedSettingsInfo;

		public static NymaSettingsInfo CachedSettingsInfo(CoreComm comm)
		{
			if (_cachedSettingsInfo is null)
			{
				using var n = new Nymashock(comm);
				n.InitForSettingsInfo("shock.wbx");
				_cachedSettingsInfo = n.SettingsInfo.Clone();
			}

			return _cachedSettingsInfo;
		}

		[CoreConstructor(VSystemID.Raw.PSX)]
		public Nymashock(CoreLoadParameters<NymaSettings, NymaSyncSettings> lp)
			: base(lp.Comm, VSystemID.Raw.PSX, "PSX Front Panel", lp.Settings, lp.SyncSettings)
		{
			var firmwareIDMap = new Dictionary<string, FirmwareID>
			{
				{ "FIRMWARE:$J", new("PSX", "J") },
				{ "FIRMWARE:$U", new("PSX", "U") },
				{ "FIRMWARE:$E", new("PSX", "E") },
			};
			DoInit<LibNymaCore>(lp, "shock.wbx", firmwareIDMap);

			_cachedSettingsInfo ??= SettingsInfo.Clone();

			List<Disc> discs = new();
			foreach (var disc in lp.Discs) discs.Add(disc.DiscData);
			_discs = discs;
			RomDetails = DiscChecksumUtils.GenQuickRomDetails(lp.Discs);
		}

		public string CalculateDiscHashes()
			=> DiscChecksumUtils.CalculateDiscHashesImpl(_discs);

		protected override IDictionary<string, SettingOverride> SettingOverrides { get; } = new Dictionary<string, SettingOverride>
		{
			{ "psx.bios_jp", new() { Hide = true , Default = "$J" } }, // FIRMWARE:
			{ "psx.bios_na", new() { Hide = true , Default = "$U" } }, // FIRMWARE:
			{ "psx.bios_eu", new() { Hide = true , Default = "$E" } }, // FIRMWARE:

			{ "psx.input.analog_mode_ct", new() { Hide = true } }, // probably don't want this
			{ "psx.input.analog_mode_ct.compare", new() { Hide = true } },

			{ "Virtual Port 1", new() { Default = "dualshock" } },
			{ "Virtual Port 2", new() { Default = "none" } },
			{ "Virtual Port 3", new() { Default = "none" } },
			{ "Virtual Port 4", new() { Default = "none" } },
			{ "Virtual Port 5", new() { Default = "none" } },
			{ "Virtual Port 6", new() { Default = "none" } },
			{ "Virtual Port 7", new() { Default = "none" } },
			{ "Virtual Port 8", new() { Default = "none" } },

			{ "psx.input.port2.memcard", new() { Default = "0" } },
			{ "psx.input.port3.memcard", new() { Default = "0" } },
			{ "psx.input.port4.memcard", new() { Default = "0" } },
			{ "psx.input.port5.memcard", new() { Default = "0" } },
			{ "psx.input.port6.memcard", new() { Default = "0" } },
			{ "psx.input.port7.memcard", new() { Default = "0" } },
			{ "psx.input.port8.memcard", new() { Default = "0" } },

			{ "psx.input.port1.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port2.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port3.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port4.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port5.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port6.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port7.gun_chairs", new() { NonSync = true } },
			{ "psx.input.port8.gun_chairs", new() { NonSync = true } },

			{ "psx.dbg_exe_cdpath", new() { Hide = true, Default = string.Empty } },

			{ "psx.spu.resamp_quality", new() { NonSync = true } },
			{ "psx.input.mouse_sensitivity", new() { Hide = true } },

			{ "psx.slstart", new() { NonSync = true } },
			{ "psx.slend", new() { NonSync = true } },
			{ "psx.h_overscan", new() { NonSync = true } },
			{ "psx.correct_aspect", new() { NonSync = true } },
			{ "psx.slstartp", new() { NonSync = true } },
			{ "psx.slendp", new() { NonSync = true } },

			{ "nyma.rtcinitialtime", new() { Hide = true } },
			{ "nyma.rtcrealtime", new() { Hide = true } },
		};
	}
}
