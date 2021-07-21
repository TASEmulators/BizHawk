using System.ComponentModel;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64SyncSettings
	{
		public class N64RicePluginSettings : IPluginSettings
		{
			public N64RicePluginSettings()
			{
				FrameBufferSetting = 0;
				FrameBufferWriteBackControl = 0;
				RenderToTexture = 0;
				ScreenUpdateSetting = 4;
				Mipmapping = 2;
				FogMethod = 0;
				ForceTextureFilter = 0;
				TextureEnhancement = 0;
				TextureEnhancementControl = 0;
				TextureQuality = 0;
				OpenGLDepthBufferSetting = 16;
				MultiSampling = 0;
				ColorQuality = 0;
				OpenGLRenderSetting = 0;
				AnisotropicFiltering = 0;

				NormalAlphaBlender = false;
				FastTextureLoading = false;
				AccurateTextureMapping = true;
				InN64Resolution = false;
				SaveVRAM = false;
				DoubleSizeForSmallTxtrBuf = false;
				DefaultCombinerDisable = false;
				EnableHacks = true;
				WinFrameMode = false;
				FullTMEMEmulation = false;
				OpenGLVertexClipper = false;
				EnableSSE = true;
				EnableVertexShader = false;
				SkipFrame = false;
				TexRectOnly = false;
				SmallTextureOnly = false;
				LoadHiResCRCOnly = true;
				LoadHiResTextures = false;
				DumpTexturesToFiles = false;

				UseDefaultHacks = true;
				DisableTextureCRC = false;
				DisableCulling = false;
				IncTexRectEdge = false;
				ZHack = false;
				TextureScaleHack = false;
				PrimaryDepthHack = false;
				Texture1Hack = false;
				FastLoadTile = false;
				UseSmallerTexture = false;
				VIWidth = -1;
				VIHeight = -1;
				UseCIWidthAndRatio = 0;
				FullTMEM = 0;
				TxtSizeMethod2 = false;
				EnableTxtLOD = false;
				FastTextureCRC = 0;
				EmulateClear = false;
				ForceScreenClear = false;
				AccurateTextureMappingHack = 0;
				NormalBlender = 0;
				DisableBlender = false;
				ForceDepthBuffer = false;
				DisableObjBG = false;
				FrameBufferOption = 0;
				RenderToTextureOption = 0;
				ScreenUpdateSettingHack = 0;
				EnableHacksForGame = 0;
			}

			[DefaultValue(0)]
			[DisplayName("Frame Buffer Emulation")]
			[Description("0=ROM default, 1=disable")]
			[Category("Game Default Options")]
			public int FrameBufferSetting { get; set; }

			[DefaultValue(0)]
			[DisplayName("Frame Buffer Write Back Control")]
			[Description("Frequency to write back the frame buffer: 0=every frame, 1=every other frame, etc")]
			[Category("Game Default Options")]
			public int FrameBufferWriteBackControl { get; set; }

			[DefaultValue(0)]
			[DisplayName("Render-to-texture emulation")]
			[Description("0=none, 1=ignore, 2=normal, 3=write back, 4=write back and reload")]
			[Category("Game Default Options")]
			public int RenderToTexture { get; set; }

			[DefaultValue(4)]
			[DisplayName("Screen Update Setting")]
			[Description("0=ROM default, 1=VI origin update, 2=VI origin change, 3=CI change, 4=first CI change, 5=first primitive draw, 6=before screen clear, 7=after screen drawn")]
			[Category("General")]
			public int ScreenUpdateSetting { get; set; }

			[DefaultValue(2)]
			[DisplayName("Mip Mapping")]
			[Description("0=no, 1=nearest, 2=bilinear, 3=trilinear")]
			[Category("General")]
			public int Mipmapping { get; set; }

			[DefaultValue(0)]
			[DisplayName("Fog Method")]
			[Description("0=Disable, 1=Enable n64 choose, 2=Force Fog")]
			[Category("General")]
			public int FogMethod { get; set; }

			[DefaultValue(0)]
			[DisplayName("Force Texture Filter")]
			[Description("0=auto: n64 choose, 1=force no filtering, 2=force filtering")]
			[Category("Texture Enhancement")]
			public int ForceTextureFilter { get; set; }

			[DefaultValue(0)]
			[DisplayName("Primary texture enhancement filter")]
			[Description("0=None, 1=2X, 2=2XSAI, 3=HQ2X, 4=LQ2X, 5=HQ4X, 6=Sharpen, 7=Sharpen More, 8=External, 9=Mirrored")]
			[Category("Texture Enhancement")]
			public int TextureEnhancement { get; set; }

			[DefaultValue(0)]
			[DisplayName("Secondary texture enhancement filter")]
			[Description("0 = none, 1-4 = filtered")]
			[Category("Texture Enhancement")]
			public int TextureEnhancementControl { get; set; }

			[DefaultValue(0)]
			[DisplayName("Texture Quality")]
			[Description("Color bit depth to use for textures: 0=default, 1=32 bits, 2=16 bits")]
			[Category("General")]
			public int TextureQuality { get; set; }

			[DefaultValue(16)]
			[DisplayName("OpenGL Depth Buffer Setting")]
			[Description("Z-buffer depth (only 16 or 32)")]
			[Category("General")]
			public int OpenGLDepthBufferSetting { get; set; }

			[DefaultValue(0)]
			[DisplayName("Enable/Disable MultiSampling")]
			[Description("0=off, 2,4,8,16=quality")]
			[Category("General")]
			public int MultiSampling { get; set; }

			[DefaultValue(0)]
			[DisplayName("Color Quality")]
			[Description("Color bit depth for rendering window: 0=32 bits, 1=16 bits")]
			[Category("General")]
			public int ColorQuality { get; set; }

			[DefaultValue(0)]
			[DisplayName("OpenGL Render Setting")]
			[Description("0=auto, 1=OGL_1.1, 2=OGL_1.2, 3=OGL_1.3, 4=OGL_1.4, 5=OGL_1.4_V2, 6=OGL_TNT2, 7=NVIDIA_OGL, 8=OGL_FRAGMENT_PROGRAM")]
			[Category("General")]
			public int OpenGLRenderSetting { get; set; }

			[DefaultValue(0)]
			[DisplayName("Anisotropic Filtering")]
			[Description("0=no filtering, 2-16=quality")]
			[Category("General")]
			public int AnisotropicFiltering { get; set; }

			[DefaultValue(false)]
			[DisplayName("Normal Alpha Blender")]
			[Description("Force to use normal alpha blender")]
			[Category("Game Default Options")]
			public bool NormalAlphaBlender { get; set; }

			[DefaultValue(false)]
			[DisplayName("Fast Texture Loading")]
			[Description("Use a faster algorithm to speed up texture loading and CRC computation")]
			[Category("General")]
			public bool FastTextureLoading { get; set; }

			[DefaultValue(true)]
			[DisplayName("Accurate Texture Mapping")]
			[Description("Use different texture coordinate clamping code")]
			[Category("General")]
			public bool AccurateTextureMapping { get; set; }

			[DefaultValue(false)]
			[DisplayName("In N64 Resolution")]
			[Description("Force emulated frame buffers to be in N64 native resolution")]
			[Category("General")]
			public bool InN64Resolution { get; set; }

			[DefaultValue(false)]
			[DisplayName("Save VRAM")]
			[Description("Try to reduce Video RAM usage (should never be used)")]
			[Category("General")]
			public bool SaveVRAM { get; set; }

			[DefaultValue(false)]
			[DisplayName("Double Size for Small Texture Buffer")]
			[Description("Enable this option to have better render-to-texture quality")]
			[Category("Game Default Options")]
			public bool DoubleSizeForSmallTxtrBuf { get; set; }

			[DefaultValue(false)]
			[DisplayName("Default Combiner Disable")]
			[Description("Force to use normal color combiner")]
			[Category("Game Default Options")]
			public bool DefaultCombinerDisable { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Hacks")]
			[Description("Enable game-specific settings from INI file")]
			[Category("General")]
			public bool EnableHacks { get; set; }

			[DefaultValue(false)]
			[DisplayName("Wireframe Mode")]
			[Description("If enabled, graphics will be drawn in Wireframe mode instead of solid and texture mode")]
			[Category("General")]
			public bool WinFrameMode { get; set; }

			[DefaultValue(false)]
			[DisplayName("Full TMEM Emulation")]
			[Description("N64 Texture Memory Full Emulation (may fix some games, may break others)")]
			[Category("General")]
			public bool FullTMEMEmulation { get; set; }

			[DefaultValue(false)]
			[DisplayName("OpenGL Vertex Clipper")]
			[Description("Enable vertex clipper for fog operations")]
			[Category("General")]
			public bool OpenGLVertexClipper { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable SSE")]
			[Category("General")]
			public bool EnableSSE { get; set; }

			[DefaultValue(false)]
			[DisplayName("Enable Vertex Shader")]
			[Category("General")]
			public bool EnableVertexShader { get; set; }

			[DefaultValue(false)]
			[DisplayName("Skip Frame")]
			[Description("If this option is enabled, the plugin will skip every other frame")]
			[Category("General")]
			public bool SkipFrame { get; set; }

			[DefaultValue(false)]
			[DisplayName("Text Rect Only")]
			[Description("If enabled, texture enhancement will be done only for TxtRect ucode")]
			[Category("Texture Enhancement")]
			public bool TexRectOnly { get; set; }

			[DefaultValue(false)]
			[DisplayName("Small Texture Only")]
			[Description("If enabled, texture enhancement will be done only for textures width+height<=128")]
			[Category("Texture Enhancement")]
			public bool SmallTextureOnly { get; set; }

			[DefaultValue(true)]
			[DisplayName("Load Hi Res CRC Only")]
			[Description("Select hi-resolution textures based only on the CRC and ignore format+size information (Glide64 compatibility)")]
			[Category("Texture Enhancement")]
			public bool LoadHiResCRCOnly { get; set; }

			[DefaultValue(false)]
			[DisplayName("Load Hi Res Textures")]
			[Category("Texture Enhancement")]
			public bool LoadHiResTextures { get; set; }

			[DefaultValue(false)]
			[DisplayName("Dump Textures to Files")]
			[Category("Texture Enhancement")]
			public bool DumpTexturesToFiles { get; set; }

			[DefaultValue(true)]
			[DisplayName("Use Default Hacks")]
			[Description("Use defaults for current game. This overrides all per game settings.")]
			[Category("Per-Game Hacks")]
			public bool UseDefaultHacks { get; set; }

			[DefaultValue(false)]
			[DisplayName("Disable Texture CRC")]
			[Category("Per-Game Hacks")]
			public bool DisableTextureCRC { get; set; }

			[DefaultValue(false)]
			[DisplayName("Disable Culling")]
			[Category("Per-Game Hacks")]
			public bool DisableCulling { get; set; }

			[DefaultValue(false)]
			[DisplayName("Include TexRect Edge")]
			[Category("Per-Game Hacks")]
			public bool IncTexRectEdge { get; set; }

			[DefaultValue(false)]
			[DisplayName("ZHack")]
			[Category("Per-Game Hacks")]
			public bool ZHack { get; set; }

			[DefaultValue(false)]
			[DisplayName("Texture Scale Hack")]
			[Category("Per-Game Hacks")]
			public bool TextureScaleHack { get; set; }

			[DefaultValue(false)]
			[DisplayName("Primary Depth Hack")]
			[Category("Per-Game Hacks")]
			public bool PrimaryDepthHack { get; set; }

			[DefaultValue(false)]
			[DisplayName("Texture 1 Hack")]
			[Category("Per-Game Hacks")]
			public bool Texture1Hack { get; set; }

			[DefaultValue(false)]
			[DisplayName("Fast Load Tile")]
			[Category("Per-Game Hacks")]
			public bool FastLoadTile { get; set; }

			[DefaultValue(false)]
			[DisplayName("Use Smaller Texture")]
			[Category("Per-Game Hacks")]
			public bool UseSmallerTexture { get; set; }

			[DefaultValue(-1)]
			[DisplayName("VI Width")]
			[Category("Per-Game Hacks")]
			public int VIWidth { get; set; }

			[DefaultValue(-1)]
			[DisplayName("VI Height")]
			[Category("Per-Game Hacks")]
			public int VIHeight { get; set; }

			[DefaultValue(0)]
			[DisplayName("Use CI Width and Ratio")]
			[Category("Per-Game Hacks")]
			public int UseCIWidthAndRatio { get; set; }

			[DefaultValue(0)]
			[DisplayName("Full THEM")]
			[Category("Per-Game Hacks")]
			public int FullTMEM { get; set; }

			[DefaultValue(false)]
			[DisplayName("Text Size Method 2")]
			[Category("Per-Game Hacks")]
			public bool TxtSizeMethod2 { get; set; }

			[DefaultValue(false)]
			[DisplayName("Enable Txt LOD")]
			[Category("Per-Game Hacks")]
			public bool EnableTxtLOD { get; set; }

			[DefaultValue(0)]
			[DisplayName("Fast Texture CRC")]
			[Category("Per-Game Hacks")]
			public int FastTextureCRC { get; set; }

			[DefaultValue(0)]
			[DisplayName("Emulate Clear")]
			[Category("Per-Game Hacks")]
			public bool EmulateClear { get; set; }

			[DefaultValue(false)]
			[DisplayName("Force Screen Clear")]
			[Category("Per-Game Hacks")]
			public bool ForceScreenClear { get; set; }

			[DefaultValue(0)]
			[DisplayName("Accurate Texture Mapping Hack")]
			[Category("Per-Game Hacks")]
			public int AccurateTextureMappingHack { get; set; }

			[DefaultValue(0)]
			[DisplayName("Normal Blender")]
			[Category("Per-Game Hacks")]
			public int NormalBlender { get; set; }

			[DefaultValue(false)]
			[DisplayName("Disable Blender")]
			[Category("Per-Game Hacks")]
			public bool DisableBlender { get; set; }

			[DefaultValue(false)]
			[DisplayName("Force Depth Buffer")]
			[Category("Per-Game Hacks")]
			public bool ForceDepthBuffer { get; set; }

			[DefaultValue(false)]
			[DisplayName("Disable Obj BG")]
			[Category("Per-Game Hacks")]
			public bool DisableObjBG { get; set; }

			[DefaultValue(0)]
			[DisplayName("Frame Buffer Option")]
			[Category("Per-Game Hacks")]
			public int FrameBufferOption { get; set; }

			[DefaultValue(0)]
			[DisplayName("Render to Texture Option")]
			[Category("Per-Game Hacks")]
			public int RenderToTextureOption { get; set; }

			[DefaultValue(0)]
			[DisplayName("Screen Update Setting Hack")]
			[Category("General")]
			public int ScreenUpdateSettingHack { get; set; }

			[DefaultValue(0)]
			[DisplayName("Enable Hacks for Game")]
			[Category("Per-Game Hacks")]
			public int EnableHacksForGame { get; set; }

			public N64RicePluginSettings Clone()
			{
				return (N64RicePluginSettings)MemberwiseClone();
			}

			public void FillPerGameHacks(GameInfo game)
			{
				if (UseDefaultHacks)
				{
					DisableTextureCRC = game.GetBool("RiceDisableTextureCRC", false);
					DisableCulling = game.GetBool("RiceDisableCulling", false);
					IncTexRectEdge = game.GetBool("RiceIncTexRectEdge", false);
					ZHack = game.GetBool("RiceZHack", false);
					TextureScaleHack = game.GetBool("RiceTextureScaleHack", false);
					PrimaryDepthHack = game.GetBool("RicePrimaryDepthHack", false);
					Texture1Hack = game.GetBool("RiceTexture1Hack", false);
					FastLoadTile = game.GetBool("RiceFastLoadTile", false);
					UseSmallerTexture = game.GetBool("RiceUseSmallerTexture", false);
					VIWidth = game.GetInt("RiceVIWidth", -1);
					VIHeight = game.GetInt("RiceVIHeight", -1);
					UseCIWidthAndRatio = game.GetInt("RiceUseCIWidthAndRatio", 0);
					FullTMEM = game.GetInt("RiceFullTMEM", 0);
					TxtSizeMethod2 = game.GetBool("RiceTxtSizeMethod2", false);
					EnableTxtLOD = game.GetBool("RiceEnableTxtLOD", false);
					FastTextureCRC = game.GetInt("RiceFastTextureCRC", 0);
					EmulateClear = game.GetBool("RiceEmulateClear", false);
					ForceScreenClear = game.GetBool("RiceForceScreenClear", false);
					AccurateTextureMappingHack = game.GetInt("RiceAccurateTextureMappingHack", 0);
					NormalBlender = game.GetInt("RiceNormalBlender", 0);
					DisableBlender = game.GetBool("RiceDisableBlender", false);
					ForceDepthBuffer = game.GetBool("RiceForceDepthBuffer", false);
					DisableObjBG = game.GetBool("RiceDisableObjBG", false);
					FrameBufferOption = game.GetInt("RiceFrameBufferOption", 0);
					RenderToTextureOption = game.GetInt("RiceRenderToTextureOption", 0);
					ScreenUpdateSettingHack = game.GetInt("RiceScreenUpdateSettingHack", 0);
					EnableHacksForGame = game.GetInt("RiceEnableHacksForGame", 0);
				}
			}

			public PluginType GetPluginType()
			{
				return PluginType.Rice;
			}
		}
	}
}
