using System;

using BizHawk.Common;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Stella
{
	[Core(CoreNames.Stella, "The Stella Team")]
	[ServiceNotApplicable(new[] { typeof(IDriveLight), typeof(ISaveRam) })]
	public partial class Stella : IEmulator, IDebuggable, IInputPollable, IRomInfo,
		ICreateGameDBEntries, ISettable<Stella.A2600Settings, Stella.A2600SyncSettings>
	{
		internal static class RomChecksums
		{
			public const string CongoBongo = "SHA1:3A77DB43B6583E8689435F0F14AA04B9E57BDDED";

			public const string KangarooNotInGameDB = "SHA1:982B8016B393A9AA7DD110295A53C4612ECF2141";

			public const string Tapper = "SHA1:E986E1818E747BEB9B33CE4DFF1CDC6B55BDB620";
		}

		[CoreConstructor(VSystemID.Raw.A26)]
		public Stella(GameInfo game, byte[] rom, Stella.A2600Settings settings, Stella.A2600SyncSettings syncSettings)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			SyncSettings = syncSettings ?? new A2600SyncSettings();
			_controllerDeck = new Atari2600ControllerDeck(SyncSettings.Port1, SyncSettings.Port2);
		}

		public string RomDetails { get; private set; }

		private readonly Atari2600ControllerDeck _controllerDeck;

		private ITraceable Tracer { get; }

		// ICreateGameDBEntries
		public CompactGameInfo GenerateGameDbEntry()
		{
			return new CompactGameInfo
			{
			};
		}

		// IBoardInfo
		private static bool DetectPal(GameInfo game, byte[] rom)
		{
			return true;
		}
	}
}
