using System.Collections.Generic;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Sega.MasterSystem;

// ReSharper disable StyleCop.SA1401
namespace BizHawk.Client.Common
{
	public static class Global
	{
		public static IEmulator Emulator;
		public static Config Config;
		public static GameInfo Game;
		public static CheatCollection CheatList;
		public static FirmwareManager FirmwareManager;
		public static Rewinder Rewinder;

		public static IMovieSession MovieSession = new MovieSession();

		/// <summary>
		/// Used to disable secondary throttling (e.g. vsync, audio) for unthrottled modes or when the primary (clock) throttle is taking over (e.g. during fast forward/rewind).
		/// </summary>
		public static bool DisableSecondaryThrottling;

		/// <summary>
		/// The maximum number of milliseconds the sound output buffer can go below full before causing a noticeable sound interruption.
		/// </summary>
		public static int SoundMaxBufferDeficitMs;

		// the movie will be spliced in between these if it is present
		public static readonly CopyControllerAdapter MovieInputSourceAdapter = new CopyControllerAdapter();
		public static readonly CopyControllerAdapter MovieOutputHardpoint = new CopyControllerAdapter();
		public static readonly MultitrackRewiringControllerAdapter MultitrackRewiringAdapter = new MultitrackRewiringControllerAdapter();

		// dont take my word for it, since the final word is actually in RewireInputChain, but here is a guide...
		// user -> Input -> ActiveController -> UDLR -> StickyXORPlayerInputAdapter -> TurboAdapter(TBD) -> Lua(?TBD?) -> ..
		// .. -> MultitrackRewiringControllerAdapter -> MovieInputSourceAdapter -> (MovieSession) -> MovieOutputAdapter -> ControllerOutput(1) -> Game
		// (1)->Input Display

		// the original source controller, bound to the user, sort of the "input" port for the chain, i think
		public static Controller ActiveController;

		// rapid fire version on the user controller, has its own key bindings and is OR'ed against ActiveController
		public static AutofireController AutoFireController;

		// the "output" port for the controller chain.
		public static readonly CopyControllerAdapter ControllerOutput = new CopyControllerAdapter();

		public static readonly UD_LR_ControllerAdapter UD_LR_ControllerAdapter = new UD_LR_ControllerAdapter();

		public static readonly AutoFireStickyXorAdapter AutofireStickyXORAdapter = new AutoFireStickyXorAdapter();

		/// <summary>
		/// provides an opportunity to mutate the player's input in an autohold style
		/// </summary>
		public static readonly StickyXorAdapter StickyXORAdapter = new StickyXorAdapter();

		/// <summary>
		/// Used to AND to another controller, used for <see cref="JoypadApi.Set(System.Collections.Generic.Dictionary{string,bool},System.Nullable{int})">JoypadApi.Set</see>
		/// </summary>
		public static readonly OverrideAdaptor ButtonOverrideAdaptor = new OverrideAdaptor();

		/// <summary>
		/// fire off one-frame logical button clicks here. useful for things like ti-83 virtual pad and reset buttons
		/// </summary>
		public static readonly ClickyVirtualPadController ClickyVirtualPadController = new ClickyVirtualPadController();

		public static Controller ClientControls;

		// Input state which has been estine for game controller inputs are coalesce here
		// This relies on a client specific implementation!
		public static SimpleController ControllerInputCoalescer;

		public static SystemInfo SystemInfo
		{
			get
			{
				switch (Emulator.SystemId)
				{ 
					default:
					case "NULL":
						return SystemInfo.Null;
					case "NES":
						return SystemInfo.Nes;
					case "INTV":
						return SystemInfo.Intellivision;
					case "SG":
						return SystemInfo.SG;
					case "SMS":
						if (Emulator is SMS gg && gg.IsGameGear)
						{
							return SystemInfo.GG;
						}

						if (Emulator is SMS sg && sg.IsSG1000)
						{
							return SystemInfo.SG;
						}

						return SystemInfo.SMS;
					case "PCECD":
						return SystemInfo.PCECD;
					case "PCE":
						return SystemInfo.PCE;
					case "SGX":
						return SystemInfo.SGX;
					case "GEN":
						return SystemInfo.Genesis;
					case "TI83":
						return SystemInfo.TI83;
					case "SNES":
						return SystemInfo.SNES;
					case "GB":
						/*
						if ((Emulator as IGameboyCommon).IsCGBMode())
						{
							return SystemInfo.GBC;
						}
						*/
						return SystemInfo.GB;
					case "A26":
						return SystemInfo.Atari2600;
					case "A78":
						return SystemInfo.Atari7800;
					case "C64":
						return SystemInfo.C64;
					case "Coleco":
						return SystemInfo.Coleco;
					case "GBA":
						return SystemInfo.GBA;
					case "N64":
						return SystemInfo.N64;
					case "SAT":
						return SystemInfo.Saturn;
					case "DGB":
						return SystemInfo.DualGB;
					case "GB3x":
						return SystemInfo.GB3x;
					case "GB4x":
						return SystemInfo.GB4x;
					case "WSWAN":
						return SystemInfo.WonderSwan;
					case "Lynx":
						return SystemInfo.Lynx;
					case "PSX":
						return SystemInfo.PSX;
					case "AppleII":
						return SystemInfo.AppleII;
					case "Libretro":
						return SystemInfo.Libretro;
					case "VB":
						return SystemInfo.VirtualBoy;
					case "VEC":
						return SystemInfo.Vectrex;
					case "NGP":
						return SystemInfo.NeoGeoPocket;
					case "ZXSpectrum":
						return SystemInfo.ZxSpectrum;
					case "AmstradCPC":
						return SystemInfo.AmstradCpc;
					case "ChannelF":
						return SystemInfo.ChannelF;
					case "O2":
						return SystemInfo.O2;
					case "MAME":
						return SystemInfo.Mame;
				}
			}
		}

		public static Dictionary<string, object> UserBag = new Dictionary<string, object>();
	}
}
