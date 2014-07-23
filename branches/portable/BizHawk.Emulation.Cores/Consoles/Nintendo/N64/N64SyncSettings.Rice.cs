using System.Collections.Generic;
using System.ComponentModel;

using BizHawk.Emulation.Common;
using System.Reflection;

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
			public int FrameBufferSetting { get; set; }

			[DefaultValue(0)]
			[DisplayName("Frame Buffer Write Back Control")]
			[Description("Frequency to write back the frame buffer: 0=every frame, 1=every other frame, etc")]
			public int FrameBufferWriteBackControl { get; set; }

			[DefaultValue(0)]
			[DisplayName("Render-to-texture emulation")]
			[Description("0=none, 1=ignore, 2=normal, 3=write back, 4=write back and reload")]
			public int RenderToTexture { get; set; }

			[DefaultValue(4)]
			[DisplayName("Screen Update Setting")]
			[Description("0=ROM default, 1=VI origin update, 2=VI origin change, 3=CI change, 4=first CI change, 5=first primitive draw, 6=before screen clear, 7=after screen drawn")]
			public int ScreenUpdateSetting { get; set; }

			[DefaultValue(2)]
			[DisplayName("Mip Mapping")]
			[Description("0=no, 1=nearest, 2=bilinear, 3=trilinear")]
			public int Mipmapping { get; set; }

			[DefaultValue(0)]
			[DisplayName("Fog Method")]
			[Description("0=Disable, 1=Enable n64 choose, 2=Force Fog")]
			public int FogMethod { get; set; }

			[DefaultValue(0)]
			[DisplayName("Force Texture Filter")]
			[Description("0=auto: n64 choose, 1=force no filtering, 2=force filtering")]
			public int ForceTextureFilter { get; set; }

			[DefaultValue(0)]
			[DisplayName("Primary texture enhancement filter")]
			[Description("0=None, 1=2X, 2=2XSAI, 3=HQ2X, 4=LQ2X, 5=HQ4X, 6=Sharpen, 7=Sharpen More, 8=External, 9=Mirrored")]
			public int TextureEnhancement { get; set; }

			[DefaultValue(0)]
			[DisplayName("Secondary texture enhancement filter")]
			[Description("0 = none, 1-4 = filtered")]
			public int TextureEnhancementControl { get; set; }

			[DefaultValue(0)]
			[DisplayName("Texture Quality")]
			[Description("Color bit depth to use for textures: 0=default, 1=32 bits, 2=16 bits")]
			public int TextureQuality { get; set; }

			[DefaultValue(16)]
			[DisplayName("OpenGL Depth Buffer Setting")]
			[Description("Z-buffer depth (only 16 or 32)")]
			public int OpenGLDepthBufferSetting { get; set; }

			[DefaultValue(0)]
			[DisplayName("Enable/Disable MultiSampling")]
			[Description("0=off, 2,4,8,16=quality")]
			public int MultiSampling { get; set; }

			[DefaultValue(0)]
			[DisplayName("Color Quality")]
			[Description("Color bit depth for rendering window: 0=32 bits, 1=16 bits")]
			public int ColorQuality { get; set; }

			[DefaultValue(0)]
			[DisplayName("OpenGL Render Setting")]
			[Description("0=auto, 1=OGL_1.1, 2=OGL_1.2, 3=OGL_1.3, 4=OGL_1.4, 5=OGL_1.4_V2, 6=OGL_TNT2, 7=NVIDIA_OGL, 8=OGL_FRAGMENT_PROGRAM")]
			public int OpenGLRenderSetting { get; set; }

			[DefaultValue(0)]
			[DisplayName("Anisotropic Filtering")]
			[Description("0=no filtering, 2-16=quality")]
			public int AnisotropicFiltering { get; set; }

			[DefaultValue(false)]
			[DisplayName("Normal Alpha Blender")]
			[Description("Force to use normal alpha blender")]
			public bool NormalAlphaBlender { get; set; }

			[DefaultValue(false)]
			[DisplayName("Fast Texture Loading")]
			[Description("Use a faster algorithm to speed up texture loading and CRC computation")]
			public bool FastTextureLoading { get; set; }

			[DefaultValue(true)]
			[DisplayName("Accurate Texture Mapping")]
			[Description("Use different texture coordinate clamping code")]
			public bool AccurateTextureMapping { get; set; }

			[DefaultValue(false)]
			[DisplayName("In N64 Resolution")]
			[Description("Force emulated frame buffers to be in N64 native resolution")]
			public bool InN64Resolution { get; set; }

			[DefaultValue(false)]
			[DisplayName("Save VRAM")]
			[Description("Try to reduce Video RAM usage (should never be used)")]
			public bool SaveVRAM { get; set; }

			[DefaultValue(false)]
			[DisplayName("Double Size for Small Texture Buffer")]
			[Description("Enable this option to have better render-to-texture quality")]
			public bool DoubleSizeForSmallTxtrBuf { get; set; }

			[DefaultValue(false)]
			[DisplayName("Default Combiner Disable")]
			[Description("Force to use normal color combiner")]
			public bool DefaultCombinerDisable { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Hacks")]
			[Description("Enable game-specific settings from INI file")]
			public bool EnableHacks { get; set; }

			[DefaultValue(false)]
			[DisplayName("WinFrame Mode")]
			[Description("If enabled, graphics will be drawn in WinFrame mode instead of solid and texture mode")]
			public bool WinFrameMode { get; set; }

			[DefaultValue(false)]
			[DisplayName("Full TMEM Emulation")]
			[Description("N64 Texture Memory Full Emulation (may fix some games, may break others)")]
			public bool FullTMEMEmulation { get; set; }

			[DefaultValue(false)]
			[DisplayName("OpenGL Vertex Clipper")]
			[Description("Enable vertex clipper for fog operations")]
			public bool OpenGLVertexClipper { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable SSE")]
			public bool EnableSSE { get; set; }

			[DefaultValue(false)]
			[DisplayName("Enable Vertex Shader")]
			public bool EnableVertexShader { get; set; }

			[DefaultValue(false)]
			[DisplayName("Skip Frame")]
			[Description("If this option is enabled, the plugin will skip every other frame")]
			public bool SkipFrame { get; set; }

			[DefaultValue(false)]
			[DisplayName("Text Rect Only")]
			[Description("If enabled, texture enhancement will be done only for TxtRect ucode")]
			public bool TexRectOnly { get; set; }

			[DefaultValue(false)]
			[DisplayName("Small Texture Only")]
			[Description("If enabled, texture enhancement will be done only for textures width+height<=128")]
			public bool SmallTextureOnly { get; set; }

			[DefaultValue(true)]
			[DisplayName("Load Hi Res CRC Only")]
			[Description("Select hi-resolution textures based only on the CRC and ignore format+size information (Glide64 compatibility)")]
			public bool LoadHiResCRCOnly { get; set; }

			[DefaultValue(false)]
			[DisplayName("Load Hi Res Textures")]
			public bool LoadHiResTextures { get; set; }

			[DefaultValue(false)]
			[DisplayName("Dump Textures to Files")]
			public bool DumpTexturesToFiles { get; set; }

			[DefaultValue(true)]
			[DisplayName("Use Default Hacks")]
			public bool UseDefaultHacks { get; set; }

			[DefaultValue(false)]
			[DisplayName("Disable Texture CRC")]
			public bool DisableTextureCRC { get; set; }

			[DefaultValue(false)]
			[DisplayName("Disable Culling")]
			public bool DisableCulling { get; set; }

			[DefaultValue(false)]
			[DisplayName("Include TexRect Edge")]
			public bool IncTexRectEdge { get; set; }

			[DefaultValue(false)]
			[DisplayName("ZHack")]
			public bool ZHack { get; set; }

			[DefaultValue(false)]
			[DisplayName("Texture Scale Hack")]
			public bool TextureScaleHack { get; set; }

			[DefaultValue(false)]
			[DisplayName("Primary Depth Hack")]
			public bool PrimaryDepthHack { get; set; }

			[DefaultValue(false)]
			[DisplayName("Texture 1 Hack")]
			public bool Texture1Hack { get; set; }

			[DefaultValue(false)]
			[DisplayName("Fast Load Tile")]
			public bool FastLoadTile { get; set; }

			[DefaultValue(false)]
			[DisplayName("Use Smaller Texture")]
			public bool UseSmallerTexture { get; set; }

			[DefaultValue(-1)]
			[DisplayName("VI Width")]
			public int VIWidth { get; set; }

			[DefaultValue(-1)]
			[DisplayName("VI Height")]
			public int VIHeight { get; set; }

			[DefaultValue(0)]
			[DisplayName("Use CI Width and Ratio")]
			public int UseCIWidthAndRatio { get; set; }

			[DefaultValue(0)]
			[DisplayName("Full THEM")]
			public int FullTMEM { get; set; }

			[DefaultValue(false)]
			[DisplayName("Text Size Method 2")]
			public bool TxtSizeMethod2 { get; set; }

			[DefaultValue(false)]
			[DisplayName("Enable Txt LOD")]
			public bool EnableTxtLOD { get; set; }

			[DefaultValue(0)]
			[DisplayName("Fast Texture CRC")]
			public int FastTextureCRC { get; set; }

			[DefaultValue(0)]
			[DisplayName("Emulate Clear")]
			public bool EmulateClear { get; set; }

			[DefaultValue(false)]
			[DisplayName("Force Screen Clear")]
			public bool ForceScreenClear { get; set; }

			[DefaultValue(0)]
			[DisplayName("Accurate Texture Mapping Hack")]
			public int AccurateTextureMappingHack { get; set; }

			[DefaultValue(0)]
			[DisplayName("Normal Blender")]
			public int NormalBlender { get; set; }

			[DefaultValue(false)]
			[DisplayName("Disable Blender")]
			public bool DisableBlender { get; set; }

			[DefaultValue(false)]
			[DisplayName("Force Depth Buffer")]
			public bool ForceDepthBuffer { get; set; }

			[DefaultValue(false)]
			[DisplayName("Disable Obj BG")]
			public bool DisableObjBG { get; set; }

			[DefaultValue(0)]
			[DisplayName("Frame Buffer Option")]
			public int FrameBufferOption { get; set; }

			[DefaultValue(0)]
			[DisplayName("Render to Texture Option")]
			public int RenderToTextureOption { get; set; }

			[DefaultValue(0)]
			[DisplayName("Screen Update Setting Hack")]
			public int ScreenUpdateSettingHack { get; set; }

			[DefaultValue(0)]
			[DisplayName("Enable Hacks for Game")]
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
