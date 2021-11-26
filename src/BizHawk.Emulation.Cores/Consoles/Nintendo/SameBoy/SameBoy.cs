using System;
using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Properties;

namespace BizHawk.Emulation.Cores.Nintendo.Sameboy
{
	/// <summary>
	/// a gameboy/gameboy color emulator wrapped around native C libsameboy
	/// </summary>
	[PortedCore(CoreNames.Sameboy, "LIJI32", "0.14.7", "https://github.com/LIJI32/SameBoy", isReleased: false)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public partial class Sameboy : ICycleTiming, IInputPollable
	{
		private readonly BasicServiceProvider _serviceProvider;

		public double ClockRate => 2097152;

		public long CycleCount { get; private set; }

		private IntPtr SameboyState { get; set; } = IntPtr.Zero;

		public bool IsCgb { get; set; }

		public bool IsCGBMode() => IsCgb;

		private readonly InputCallbackSystem _inputCallbacks = new InputCallbackSystem();

		private readonly LibSameboy.SampleCallback _sampleCallback;
		private readonly LibSameboy.InputCallback _inputCallback;

		[CoreConstructor(VSystemID.Raw.GB)]
		[CoreConstructor(VSystemID.Raw.GBC)]
		public Sameboy(CoreComm comm, GameInfo game, byte[] file, SameboySyncSettings syncSettings, bool deterministic)
		{
			_serviceProvider = new BasicServiceProvider(this);

			_syncSettings = syncSettings ?? new SameboySyncSettings();

			LibSameboy.LoadFlags flags = _syncSettings.ConsoleMode switch
			{
				SameboySyncSettings.ConsoleModeType.GB => LibSameboy.LoadFlags.IS_DMG,
				SameboySyncSettings.ConsoleModeType.GBC => LibSameboy.LoadFlags.IS_CGB,
				SameboySyncSettings.ConsoleModeType.GBA => LibSameboy.LoadFlags.IS_CGB | LibSameboy.LoadFlags.IS_AGB,
				_ => game.System == VSystemID.Raw.GBC ? LibSameboy.LoadFlags.IS_CGB : LibSameboy.LoadFlags.IS_DMG
			};

			IsCgb = (flags & LibSameboy.LoadFlags.IS_CGB) == LibSameboy.LoadFlags.IS_CGB;

			byte[] bios = null;
			if (_syncSettings.EnableBIOS)
			{
				FirmwareID fwid = new(
					IsCgb ? "GBC" : "GB",
					_syncSettings.ConsoleMode is SameboySyncSettings.ConsoleModeType.GBA
					? "AGB"
					: "World");
				bios = comm.CoreFileProvider.GetFirmwareOrThrow(fwid, "BIOS Not Found, Cannot Load.  Change SyncSettings to run without BIOS.");
			}
			else
			{
				bios = Util.DecompressGzipFile(new MemoryStream(IsCgb
					? _syncSettings.ConsoleMode is SameboySyncSettings.ConsoleModeType.GBA ? Resources.SameboyAgbBoot.Value : Resources.SameboyCgbBoot.Value
					: Resources.SameboyDmgBoot.Value));
			}

			SameboyState = LibSameboy.sameboy_create(file, file.Length, bios, bios.Length, flags);

			InitMemoryDomains();

			_sampleCallback = QueueSample;
			LibSameboy.sameboy_setsamplecallback(SameboyState, _sampleCallback);
			_inputCallback = InputCallback;
			LibSameboy.sameboy_setinputcallback(SameboyState, _inputCallback);

			_stateBuf = new byte[LibSameboy.sameboy_statelen(SameboyState)];
		}

		public int LagCount { get; set; } = 0;

		public bool IsLagFrame { get; set; } = false;

		public IInputCallbackSystem InputCallbacks => _inputCallbacks;

		private void InputCallback()
		{
			IsLagFrame = false;
			_inputCallbacks.Call();
		}
	}
}
