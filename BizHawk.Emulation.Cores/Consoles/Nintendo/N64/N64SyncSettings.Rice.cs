using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

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

			[JsonIgnore]
			[Description("Plugin Type")]
			public PluginType PluginType
			{
				get { return PluginType.RICE; }
			}

			[DefaultValue(0)]
			[DisplayName("Frame Buffer Setting")]
			public int FrameBufferSetting { get; set; }

			[DefaultValue(0)]
			[DisplayName("Frame Buffer Write Back Control")]
			public int FrameBufferWriteBackControl { get; set; }

			[DefaultValue(0)]
			[DisplayName("Frame Buffer Write Back Control")]
			public int RenderToTexture { get; set; }

			[DefaultValue(4)]
			[DisplayName("Screen Update Setting")]
			public int ScreenUpdateSetting { get; set; }

			[DefaultValue(2)]
			[DisplayName("Mip Mapping")]
			public int Mipmapping { get; set; }

			[DefaultValue(0)]
			[DisplayName("Fog Method")]
			public int FogMethod { get; set; }

			[DefaultValue(0)]
			[DisplayName("Force Texture Filter")]
			public int ForceTextureFilter { get; set; }

			[DefaultValue(0)]
			[DisplayName("Texture Enhancement")]
			public int TextureEnhancement { get; set; }

			[DefaultValue(0)]
			[DisplayName("Texture Enhancement Control")]
			public int TextureEnhancementControl { get; set; }

			[DefaultValue(0)]
			[DisplayName("Texture Quality")]
			public int TextureQuality { get; set; }

			[DefaultValue(16)]
			[DisplayName("OpenGL Depth Buffer Setting")]
			public int OpenGLDepthBufferSetting { get; set; }

			[DefaultValue(0)]
			[DisplayName("Multi-sampling")]
			public int MultiSampling { get; set; }

			[DefaultValue(0)]
			[DisplayName("Color Quality")]
			public int ColorQuality { get; set; }

			[DefaultValue(0)]
			[DisplayName("OpenGL Render Setting")]
			public int OpenGLRenderSetting { get; set; }

			[DefaultValue(0)]
			[DisplayName("Anisotropic Filter")]
			public int AnisotropicFiltering { get; set; }

			[DefaultValue(false)]
			[DisplayName("Normal Alpha Blender")]
			public bool NormalAlphaBlender { get; set; }

			[DefaultValue(false)]
			[DisplayName("Fast Texture Loading")]
			public bool FastTextureLoading { get; set; }

			[DefaultValue(true)]
			[DisplayName("Accurate Texture Mapping")]
			public bool AccurateTextureMapping { get; set; }

			[DefaultValue(false)]
			[DisplayName("In N64 Resolution")]
			public bool InN64Resolution { get; set; }

			[DefaultValue(false)]
			[DisplayName("Save VRAM")]
			public bool SaveVRAM { get; set; }

			[DefaultValue(false)]
			[DisplayName("Double Size for Small Texture Buffer")]
			public bool DoubleSizeForSmallTxtrBuf { get; set; }

			[DefaultValue(false)]
			[DisplayName("Default Combiner Disable")]
			public bool DefaultCombinerDisable { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable Hacks")]
			public bool EnableHacks { get; set; }

			[DefaultValue(false)]
			[DisplayName("WinFrame Mode")]
			public bool WinFrameMode { get; set; }

			[DefaultValue(false)]
			[DisplayName("Full TMEM Emulation")]
			public bool FullTMEMEmulation { get; set; }

			[DefaultValue(false)]
			[DisplayName("OpenGL Vertex Clipper")]
			public bool OpenGLVertexClipper { get; set; }

			[DefaultValue(true)]
			[DisplayName("Enable SSE")]
			public bool EnableSSE { get; set; }

			[DefaultValue(false)]
			[DisplayName("Enable Vertex Shader")]
			public bool EnableVertexShader { get; set; }

			[DefaultValue(false)]
			[DisplayName("Skip Frame")]
			public bool SkipFrame { get; set; }

			[DefaultValue(false)]
			[DisplayName("Text Rect Only")]
			public bool TexRectOnly { get; set; }

			[DefaultValue(false)]
			[DisplayName("Small Texture Only")]
			public bool SmallTextureOnly { get; set; }

			[DefaultValue(true)]
			[DisplayName("Load Hi Res CRC Only")]
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

			public Dictionary<string, object> GetPluginSettings()
			{
				//TODO: deal witn the game depedent settings
				var dictionary = new Dictionary<string, object>();
				var members = this.GetType().GetMembers();
				foreach (var member in members)
				{
					if (member.MemberType == MemberTypes.Property)
					{
						var field = this.GetType().GetProperty(member.Name).GetValue(this, null);
						dictionary.Add(member.Name, field);
					}
				}

				return dictionary;
			}
		}
	}
}
