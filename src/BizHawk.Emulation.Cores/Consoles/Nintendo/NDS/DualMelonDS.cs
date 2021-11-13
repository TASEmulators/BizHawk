using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	[PortedCore(CoreNames.DualMelonDS, "Arisotura", "0.9.3", "http://melonds.kuribo64.net/", isReleased: false)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight), typeof(IRegionable) })]
	public partial class DualNDS
	{
		private NDS L;
		private NDS R;

		private readonly SaveController LCont = new SaveController(NDS.NDSController);
		private readonly SaveController RCont = new SaveController(NDS.NDSController);

		private readonly BasicServiceProvider _serviceProvider;
		private readonly NDSDisassembler _disassembler;
		private SpeexResampler _resampler;
		private bool _disposed = false;

		[CoreConstructor("DualNDS")]
		public DualNDS(CoreLoadParameters<DualNDSSettings, DualNDSSyncSettings> lp)
		{
			if (lp.Roms.Count != 2)
			{
				throw new InvalidOperationException("Wrong number of ROMs!");
			}

			_serviceProvider = new BasicServiceProvider(this);
			DualNDSSettings dualSettings = lp.Settings ?? new DualNDSSettings();
			DualNDSSyncSettings dualSyncSettings = lp.SyncSettings ?? new DualNDSSyncSettings();

			L = new NDS(ExtractLoadParameters(lp, dualSettings, dualSyncSettings, false), false);
			R = new NDS(ExtractLoadParameters(lp, dualSettings, dualSyncSettings, true), true);

			_disassembler = new NDSDisassembler();
			_serviceProvider.Register<IDisassemblable>(_disassembler);

			_resampler = new SpeexResampler(SpeexResampler.Quality.QUALITY_DEFAULT, 32768, 44100, 32768, 44100, null, this);
			_serviceProvider.Register<ISoundProvider>(_resampler);

			SetMemoryDomains();
		}

		private static CoreLoadParameters<NDS.NDSSettings, NDS.NDSSyncSettings> ExtractLoadParameters(
			CoreLoadParameters<DualNDSSettings, DualNDSSyncSettings> lp,
			DualNDSSettings dualSettings,
			DualNDSSyncSettings dualSyncSettings,
			bool right)
		{
			var ret = new CoreLoadParameters<NDS.NDSSettings, NDS.NDSSyncSettings>
			{
				Comm = lp.Comm,
				Game = lp.Roms[right ? 1 : 0].Game,
				Settings = right ? dualSettings.R : dualSettings.L,
				SyncSettings = right ? dualSyncSettings.R : dualSyncSettings.L,
				DeterministicEmulationRequested = lp.DeterministicEmulationRequested,
			};
			ret.Roms.Add(lp.Roms[right ? 1 : 0]);
			return ret;
		}
	}
}
