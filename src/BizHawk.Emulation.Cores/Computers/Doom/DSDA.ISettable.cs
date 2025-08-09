using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

using BizHawk.Common;
using BizHawk.Common.ReflectionExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Doom
{
	public partial class DSDA : ISettable<DSDA.DoomSettings, DSDA.DoomSyncSettings>
	{
		public enum ControllerType
		{
			Doom,
			Heretic,
			Hexen
		}

		public enum CompatibilityLevel
		{
			[Display(Name = "0 - Doom v1.2")]
			Doom_12 = 0,
			[Display(Name = "1 - Doom v1.666")]
			Doom_1666 = 1,
			[Display(Name = "2 - Doom & Doom 2 v1.9")]
			Doom2_19 = 2,
			[Display(Name = "3 - Ultimate Doom & Doom95")]
			UltimateDoom95 = 3,
			[Display(Name = "4 - Final Doom")]
			FinalDoom = 4,
			[Display(Name = "5 - DOSDoom")]
			DosDoom = 5,
			[Display(Name = "6 - TASDoom")]
			TasDoom = 6,
			[Display(Name = "7 - Boom's Vanilla Compatibility Mode")]
			Boom_Compatibility = 7,
			[Display(Name = "8 - Boom v2.01")]
			Boom_201 = 8,
			[Display(Name = "9 - Boom v2.02")]
			Boom_202 = 9,
			[Display(Name = "10 - LxDoom")]
			LxDoom = 10,
			[Display(Name = "11 - MBF")]
			MBF = 11,
			[Display(Name = "12 - PrBoom v2.03beta")]
			PrBoom_1 = 12,
			[Display(Name = "13 - PrBoom v2.1.0")]
			PrBoom_2 = 13,
			[Display(Name = "14 - PrBoom v2.1.1 - 2.2.6")]
			PrBoom_3 = 14,
			[Display(Name = "15 - PrBoom v2.3.x")]
			PrBoom_4 = 15,
			[Display(Name = "16 - PrBoom v2.4.0")]
			PrBoom_5 = 16,
			[Display(Name = "17 - PrBoom Latest")]
			PrBoom_6 = 17,
			[Display(Name = "21 - MBF21")]
			MBF21 = 21
		}

		public enum SkillLevel
		{
			[Display(Name = "1 - I'm too young to die")]
			ITYTD = 1,
			[Display(Name = "2 - Hey, not too rough")]
			HNTR = 2,
			[Display(Name = "3 - Hurt me plenty")]
			HMP = 3,
			[Display(Name = "4 - Ultra-Violence")]
			UV = 4,
			[Display(Name = "5 - Nightmare!")]
			NM = 5
		}

		public enum HudMode
		{
			Vanilla = 0,
			DSDA = 1,
			None = 2
		}

		public enum MapDetail
		{
			Normal = 0,
			Linedefs = 1,
			[Display(Name = "Linedefs and things")]
			Everything = 2
		}

		public enum MapOverlays
		{
			Disabled = 0,
			Enabled = 1,
			Dark = 2
		}

		public enum AspectRatio
		{
			Native = 0,
			[Display(Name = "16:9")]
			_16by9 = 1,
			[Display(Name = "16:10")]
			_16by10 = 2,
			[Display(Name = "4:3")]
			_4by3 = 3,
		}

		public enum TurningResolution
		{
			[Display(Name = "16 bits (longtics)")]
			Longtics = 1,
			[Display(Name = "8 bits (shorttics)")]
			Shorttics = 2,
		}

		public enum Strafe50Turning
		{
			Ignore = 0,
			Allow = 1,
		}

		public enum MultiplayerMode
		{
			[Display(Name = "Single Player / Cooperative")]
			Single_Coop = 0,
			[Display(Name = "Deathmatch")]
			Deathmatch = 1,
			[Display(Name = "Alternate Deathmatch (v2.0)")]
			Altdeath = 2
		}

		public enum HexenClass
		{
			Fighter = 1,
			Cleric = 2,
			Mage = 3
		}

		public const int TURBO_AUTO = -1;

		private DoomSettings _settings;
		private readonly DoomSyncSettings _syncSettings;
		private DoomSyncSettings _finalSyncSettings;

		public DoomSettings GetSettings()
			=> _settings.Clone();

		public DoomSyncSettings GetSyncSettings()
			=> _finalSyncSettings.Clone();

		public PutSettingsDirtyBits PutSyncSettings(DoomSyncSettings o)
		{
			var ret = DoomSyncSettings.NeedsReboot(_finalSyncSettings, o);
			_finalSyncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		[CoreSettings]
		public class DoomSettings
		{
			[DisplayName("Internal Resolution Scale Factor")]
			[Description("Which factor to increase internal resolution by [1 - 12]. Improves \"quality\" of the rendered image at the cost of accuracy.\n\nVanilla resolution is 320x200 resized to 4:3 DAR on a CRT monitor.\n\nRequires restart.")]
			[Range(1, 12)]
			[DefaultValue(1)]
			[TypeConverter(typeof(ConstrainedIntConverter))]
			public int ScaleFactor { get; set; }

			[DisplayName("Internal Aspect Ratio")]
			[Description("Sets aspect ratio of the rendered screen. 'Native' is multiples of 320x200 with aspect correction (to 4:3) applied by the frontend, similar to vanilla. Other modes produce pre-corrected image, useful for viewing Automap on higher resolutions (to avoid pixel distortion caused by external aspect correcton).\n\nRequires restart.")]
			[DefaultValue(AspectRatio.Native)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public AspectRatio InternalAspect { get; set; }

			[DisplayName("Sfx Volume")]
			[Description("Sound effects volume [0 - 15].")]
			[Range(0, 15)]
			[DefaultValue(8)]
			public int SfxVolume { get; set; }

			[DisplayName("Music Volume")]
			[Description("[0 - 15]")]
			[Range(0, 15)]
			[DefaultValue(8)]
			public int MusicVolume { get; set; }

			[DisplayName("Gamma Correction Level")]
			[Description("Increases brightness [0 - 4].\n\nDefault value in vanilla is \"OFF\" (0).")]
			[Range(0, 4)]
			[DefaultValue(0)]
			[TypeConverter(typeof(ConstrainedIntConverter))]
			public int Gamma { get; set; }

			[DisplayName("Show Messages")]
			[Description("Displays messages about items you pick up.\n\nDefault value in vanilla is \"ON\".")]
			[DefaultValue(true)]
			public bool ShowMessages { get; set; }

			[DisplayName("Report Revealed Secrets")]
			[Description("Shows an on-screen notification when revealing a secret. Not available in vanilla.")]
			[DefaultValue(false)]
			public bool ReportSecrets { get; set; }

			[DisplayName("HUD Mode")]
			[Description("Sets heads-up display mode.")]
			[DefaultValue(HudMode.Vanilla)]
			public HudMode HeadsUpMode { get; set; }

			[DisplayName("Extended HUD")]
			[Description("Shows DSDA-Doom-specific information above vanilla heads-up-display. Not available in vanilla.")]
			[DefaultValue(false)]
			public bool DsdaExHud { get; set; }

			[DisplayName("Display Coordinates")]
			[Description("Shows player position, angle, velocity, and distance travelled per frame. Color indicates movement tiers: green - SR40, blue - SR50, red - turbo/wallrun.\n\nAvailable in vanilla via the IDMYPOS cheat code, however vanilla only shows angle, X, and Y.")]
			[DefaultValue(false)]
			public bool DisplayCoordinates { get; set; }

			[DisplayName("Display Commands")]
			[Description("Shows input history on the screen. History size is 10, empty commands are excluded. Not available in vanilla.")]
			[DefaultValue(false)]
			public bool DisplayCommands { get; set; }

			[DisplayName("Automap Totals")]
			[Description("Shows counts for kills, items, and secrets on Automap. Not available in vanilla.")]
			[DefaultValue(false)]
			public bool MapTotals { get; set; }

			[DisplayName("Automap Time")]
			[Description("Shows elapsed time on Automap. Not available in vanilla.")]
			[DefaultValue(false)]
			public bool MapTime { get; set; }

			[DisplayName("Automap Coordinates")]
			[Description("Shows in-level coordinates on Automap. Not available in vanilla.")]
			[DefaultValue(false)]
			public bool MapCoordinates { get; set; }

			[DisplayName("Automap Overlay")]
			[Description("Shows Automap on top of gameplay. Not available in vanilla.")]
			[DefaultValue(MapOverlays.Disabled)]
			public MapOverlays MapOverlay { get; set; }

			[DisplayName("Automap Details")]
			[Description("Exposes all linedefs and things.\n\nAvailable in vanilla via the IDDT cheat code.")]
			[DefaultValue(MapDetail.Normal)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public MapDetail MapDetails { get; set; }

			[DisplayName("Full Vision")]
			[Description("Disables all darkness.\n\nAvailable in vanilla via the IDBEHOLDL cheat code.\n\nRequires restart.")]
			[DefaultValue(false)]
			public bool FullVision { get; set; }

			[DisplayName("Player Point of View")]
			[Description("Which of the players' point of view to use during rendering.")]
			[Range(1, 4)]
			[DefaultValue(1)]
			[TypeConverter(typeof(ConstrainedIntConverter))]
			public int DisplayPlayer { get; set; }

			public DoomSettings()
				=> SettingsUtil.SetDefaultValues(this);

			public DoomSettings Clone()
				=> (DoomSettings)MemberwiseClone();
		}

		private readonly string FMT_STR_PLAYER_NOT_ACTIVE = $"Trying to set '{typeof(DoomSettings).GetProperty(nameof(DoomSettings.DisplayPlayer)).DisplayName()}' to '{{0}}' but that player is not active.";

		public PutSettingsDirtyBits PutSettings(DoomSettings o)
		{
			if (!PlayerPresent(_syncSettings, o.DisplayPlayer))
			{
				throw new ArgumentException(
					message: string.Format(FMT_STR_PLAYER_NOT_ACTIVE, o.DisplayPlayer),
					paramName: nameof(o));
			}
			var ret = (_settings.ScaleFactor == o.ScaleFactor
				&& _settings.InternalAspect == o.InternalAspect
				&& _settings.FullVision == o.FullVision)
				? PutSettingsDirtyBits.None
				: PutSettingsDirtyBits.RebootCore;
			_settings = o;
			return ret;
		}

		[CoreSettings]
		public class DoomSyncSettings
		{
			[DefaultValue(ControllerType.Doom)]
			[DisplayName("Input Format")]
			[Description("The format provided for the players' input.")]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public ControllerType InputFormat { get; set; }

			[DisplayName("Compatibility Level")]
			[Description("The version of Doom or its ports that this movie is meant to emulate. Highest vanilla-compatible level is 'Final Doom'. Newer WADs may require higher levels. Standalone DSDA-Doom defaults to MBF21, which supports features of all of the lower levels, but is the farthest from vanilla.")]
			[DefaultValue(CompatibilityLevel.Doom2_19)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public CompatibilityLevel CompatibilityLevel { get; set; }

			[DisplayName("Skill Level")]
			[Description("Difficulty setting. Vanilla defaults to 'Hurt me plenty', but the de-facto current stanadard is 'Ultra-Violence'.")]
			[DefaultValue(SkillLevel.UV)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public SkillLevel SkillLevel { get; set; }

			[DisplayName("Multiplayer Mode")]
			[Description("Indicates the multiplayer mode.")]
			[DefaultValue(MultiplayerMode.Single_Coop)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public MultiplayerMode MultiplayerMode { get; set; }

			[DisplayName("Initial Episode")]
			[Description("Selects the initial episode. Ignored for non-episodic IWADs (e.g., DOOM2) and Shareware.")]
			[DefaultValue(1)]
			public int InitialEpisode { get; set; }

			[DisplayName("Initial Map")]
			[Description("Selects the initial map.")]
			[DefaultValue(1)]
			public int InitialMap { get; set; }

			[DisplayName("Fast Monsters")]
			[Description("Makes monsters move and attack much faster. Forced to 'true' when playing Nightmare! difficulty.")]
			[DefaultValue(false)]
			public bool FastMonsters { get; set; }

			[DisplayName("Monsters Respawn")]
			[Description("Makes monsters respawn shortly after dying. Forced to 'true' when playing Nightmare! difficulty.")]
			[DefaultValue(false)]
			public bool MonstersRespawn { get; set; }

			[DisplayName("No Monsters")]
			[Description("Removes all monsters from the level.")]
			[DefaultValue(false)]
			public bool NoMonsters { get; set; }

			[DisplayName("Pistol Start")]
			[Description("Starts every level with a clean slate, with nothing carried over from previus levels. Health is reset to 100% as well.")]
			[DefaultValue(false)]
			public bool PistolStart { get; set; }

			[DisplayName("Coop Mode Spawns")]
			[Description("Play single-player mode with cooperative mode thing spawns.")]
			[DefaultValue(false)]
			public bool CoopSpawns { get; set; }

			[DisplayName("Chain Episodes")]
			[Description("Completing one episode leads to the next without interruption. Not available in vanilla.")]
			[DefaultValue(false)]
			public bool ChainEpisodes { get; set; }

			[DisplayName("Always Run")]
			[Description("Toggles whether the player is permanently in the running state, without the slower walking speed available. This emulates a bug in vanilla Doom: setting the joystick run button to an invalid high number causes the game to always have it enabled.")]
			[DefaultValue(true)]
			public bool AlwaysRun { get; set; }

			[DisplayName("Render Wipescreen")]
			[Description("Enables screen melt - an effect seen when Doom changes scene, for example, when starting or exiting a level. Can't be disabled in vanilla.")]
			[DefaultValue(true)]
			public bool RenderWipescreen { get; set; }

			[DisplayName("Turning Resolution")]
			[Description("'Shorttics' refers to decreased turning resolution normally used for demos. 'Longtics' refers to the regular turning resolution outside of a demo-recording environment. Newer demo formats support both.")]
			[DefaultValue(TurningResolution.Longtics)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public TurningResolution TurningResolution { get; set; }

			[DisplayName("Horizontal Mouse Sensitivity")]
			[Description("How fast the Doom player will turn when using the mouse.")]
			[DefaultValue(10)]
			public int MouseTurnSensitivity { get; set; }

			[DisplayName("Vertical Mouse Sensitivity")]
			[Description("How fast the Doom player will run when using the mouse.")]
			[DefaultValue(1)]
			public int MouseRunSensitivity { get; set; }

			[DisplayName("Turning During Strafe50")]
			[Description("\"Strafe\" key is required to convert angular movement into strafe50, without it maximum strafe value is 40. So using keyboard and mouse, it's impossible to turn during strafe50. But if strafe50+turning appears in a demo, the game will process it fine, which makes it a TAS-only feature. This setting allows disabling it for maximum authenticity.")]
			[DefaultValue(Strafe50Turning.Allow)]
			public Strafe50Turning Strafe50Turns { get; set; }

			[DisplayName("Prevent Level Exit")]
			[Description("Level exit triggers won't have an effect. This is useful for debugging / optimizing / botting purposes. Not available in vanilla.")]
			[DefaultValue(false)]
			public bool PreventLevelExit { get; set; }

			[DisplayName("Prevent Game End")]
			[Description("Game end triggers won't have an effect. This is useful for debugging / optimizing / botting purposes. Not available in vanilla.")]
			[DefaultValue(false)]
			public bool PreventGameEnd { get; set; }

			[DisplayName("Turbo")]
			[Description("Modifies the player running / strafing speed [0-255]. '-1' means Disabled.")]
			[Range(TURBO_AUTO, 255)]
			[DefaultValue(TURBO_AUTO)]
			[TypeConverter(typeof(ConstrainedIntConverter))]
			public int Turbo { get; set; }
			/*
			[DisplayName("Initial RNG Seed")]
			[Description("Boom demos.")]
			[DefaultValue(1993)]
			public uint RNGSeed { get; set; }
			*/
			[DisplayName("Player 1 Present")]
			[Description("Specifies if player 1 is present")]
			[DefaultValue(true)]
			public bool Player1Present { get; set; }

			[DisplayName("Player 2 Present")]
			[Description("Specifies if player 2 is present")]
			[DefaultValue(false)]
			public bool Player2Present { get; set; }

			[DisplayName("Player 3 Present")]
			[Description("Specifies if player 3 is present")]
			[DefaultValue(false)]
			public bool Player3Present { get; set; }

			[DisplayName("Player 4 Present")]
			[Description("Specifies if player 4 is present")]
			[DefaultValue(false)]
			public bool Player4Present { get; set; }

			[DisplayName("[Hexen] Player 1 Class")]
			[Description("The Hexen class to use for player 1. Has no effect for Doom / Heretic")]
			[DefaultValue(HexenClass.Fighter)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public HexenClass Player1Class { get; set; }

			[DisplayName("[Hexen] Player 2 Class")]
			[Description("The Hexen class to use for player 2. Has no effect for Doom / Heretic")]
			[DefaultValue(HexenClass.Fighter)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public HexenClass Player2Class { get; set; }

			[DisplayName("[Hexen] Player 3 Class")]
			[Description("The Hexen class to use for player 3. Has no effect for Doom / Heretic")]
			[DefaultValue(HexenClass.Fighter)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public HexenClass Player3Class { get; set; }

			[DisplayName("[Hexen] Player 4 Class")]
			[Description("The Hexen class to use for player 4. Has no effect for Doom / Heretic")]
			[DefaultValue(HexenClass.Fighter)]
			[TypeConverter(typeof(DescribableEnumConverter))]
			public HexenClass Player4Class { get; set; }

			public LibDSDA.InitSettings GetNativeSettings()
			{
				return new LibDSDA.InitSettings
				{
					Player1Present = Player1Present ? 1 : 0,
					Player2Present = Player2Present ? 1 : 0,
					Player3Present = Player3Present ? 1 : 0,
					Player4Present = Player4Present ? 1 : 0,
					Player1Class = (int)Player1Class,
					Player2Class = (int)Player2Class,
					Player3Class = (int)Player3Class,
					Player4Class = (int)Player4Class,
					PreventLevelExit = PreventLevelExit ? 1 : 0,
					PreventGameEnd = PreventGameEnd ? 1 : 0,
					//RNGSeed = RNGSeed,
				};
			}

			public DoomSyncSettings Clone()
				=> (DoomSyncSettings)MemberwiseClone();

			public DoomSyncSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public static bool NeedsReboot(DoomSyncSettings x, DoomSyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);
		}
	}
}
