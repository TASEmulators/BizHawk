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

		public override bool SaveRamModified
		{
			get
			{
				_exe.AddTransientFile(Array.Empty<byte>(), "SAV:flash");
				if (!_neopop.GetSaveRam())
					throw new InvalidOperationException("Error divining saveram");
				return _exe.RemoveTransientFile("SAV:flash").Length > 0;
			}
		}

		public override byte[] CloneSaveRam(bool clearDirty)
		{
			_exe.AddTransientFile([ ], "SAV:flash");
			var success = _neopop.GetSaveRam();
			var ret = _exe.RemoveTransientFile("SAV:flash");
			if (!success)
			{
				throw new InvalidOperationException("Error returning saveram");
			}

			return ret;
		}

		public override void StoreSaveRam(byte[] data)
		{
			if (data.Length == 0)
			{
				// Empty SaveRAM is valid here, that means at the time of flushing SaveRAM, flash was not attempted to be used yet
				// The core will reject attempts to put this empty file however, so we must return here
				return;
			}

			_exe.AddReadonlyFile(data, "SAV:flash");
			var success = _neopop.PutSaveRam();
			_exe.RemoveReadonlyFile("SAV:flash");
			if (!success)
			{
				throw new InvalidOperationException("Core rejected the saveram");
			}
		}

		protected override IDictionary<string, SettingOverride> SettingOverrides { get; } = new Dictionary<string, SettingOverride>
		{
			{ "nyma.constantfb", new() { Hide = true } }, // TODO: Couldn't we just autodetect this whenever lcm == max == nominal?
		};
	}
}
