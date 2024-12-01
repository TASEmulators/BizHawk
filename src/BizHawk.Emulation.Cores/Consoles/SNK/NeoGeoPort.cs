using System.Collections.Generic;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.SNK
{
	[PortedCore(CoreNames.NeoPop, "Thomas Klausner, Mednafen Team", "1.32.1", "https://mednafen.github.io/releases/")]
	public class NeoGeoPort : NymaCore,
		ISaveRam // NGP provides its own saveram interface
	{
		private NeoGeoPort(CoreComm comm)
			: base(comm, VSystemID.Raw.NULL, null, null, null)
		{
		}

		private static NymaSettingsInfo _cachedSettingsInfo;

		public static NymaSettingsInfo CachedSettingsInfo(CoreComm comm)
		{
			if (_cachedSettingsInfo is null)
			{
				using var n = new NeoGeoPort(comm);
				n.InitForSettingsInfo("ngp.wbx");
				_cachedSettingsInfo = n.SettingsInfo.Clone();
			}

			return _cachedSettingsInfo;
		}

		private readonly LibNeoGeoPort _neopop;

		[CoreConstructor(VSystemID.Raw.NGP)]
		public NeoGeoPort(CoreComm comm, byte[] rom, GameInfo game,
			NymaSettings settings, NymaSyncSettings syncSettings, bool deterministic, string extension)
			: base(comm, VSystemID.Raw.NGP, "NeoGeo Portable Controller", settings, syncSettings)
		{
			_neopop = DoInit<LibNeoGeoPort>(game, rom, null, "ngp.wbx", extension, deterministic);

			_cachedSettingsInfo ??= SettingsInfo.Clone();
		}

		public new bool SaveRamModified
		{
			get
			{
				_exe.AddTransientFile(new byte[0], "SAV:flash");
				if (!_neopop.GetSaveRam())
					throw new InvalidOperationException("Error divining saveram");
				return _exe.RemoveTransientFile("SAV:flash").Length > 0;
			}
		}

		public new byte[] CloneSaveRam()
		{
			_exe.AddTransientFile(new byte[0], "SAV:flash");

			if (!_neopop.GetSaveRam())
				throw new InvalidOperationException("Error returning saveram");
			return _exe.RemoveTransientFile("SAV:flash");
		}

		public new void StoreSaveRam(byte[] data)
		{
			_exe.AddTransientFile(data, "SAV:flash");
			if (!_neopop.PutSaveRam())
				throw new InvalidOperationException("Core rejected the saveram");
			_exe.RemoveTransientFile("SAV:flash");
		}

		protected override IDictionary<string, SettingOverride> SettingOverrides { get; } = new Dictionary<string, SettingOverride>
		{
			{ "nyma.constantfb", new() { Hide = true } }, // TODO: Couldn't we just autodetect this whenever lcm == max == nominal?
		};
	}
}
