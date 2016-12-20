using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64SyncSettings
	{
		public class N64GLideN64PluginSettings : IPluginSettings
		{
			public N64GLideN64PluginSettings()
			{
				UseDefaultHacks = true;

				MultiSampling = 0;
				AspectRatio = AspectRatioMode.FourThree;
				BufferSwapMode = SwapMode.OnVIUpdateCall;
				UseNativeResolutionFactor = 0;
				bilinearMode = bilinearFilteringMode.Standard;
				MaxAnisotropy = false;
				CacheSize = 500;
				EnableNoise = true;
				EnableLOD = true;
				EnableHWLighting = false;
				EnableShadersStorage = true;
				CorrectTexrectCoords = TexrectCoordsMode.Off;
				EnableNativeResTexrects = false;
				EnableLegacyBlending = false;
				EnableFragmentDepthWrite = true;
				EnableFBEmulation = true;
				EnableCopyAuxiliaryToRDRAM = false;
				EnableN64DepthCompare = false;
				DisableFBInfo = true;
				FBInfoReadColorChunk = false;
				FBInfoReadDepthChunk = true;
				EnableCopyColorToRDRAM = CopyColorToRDRAMMode.AsyncMode;
				EnableCopyDepthToRDRAM = CopyDepthToRDRAMMode.DoNotCopy;
				EnableCopyColorFromRDRAM = false;
				txFilterMode = TextureFilterMode.None;
				txEnhancementMode = TextureEnhancementMode.None;
				txDeposterize = false;
				txFilterIgnoreBG = false;
				txCacheSize = 100;
				txHiresEnable = false;
				txHiresFullAlphaChannel = false;
				txHresAltCRC = false;
				txDump = false;
				txCacheCompression = true;
				txForce16bpp = false;
				txSaveCache = true;
				txPath = "";
				EnableBloom = false;
				bloomThresholdLevel = 4;
				bloomBlendMode = BlendMode.Strong;
				blurAmount = 10;
				blurStrength = 20;
				ForceGammaCorrection = false;
				GammaCorrectionLevel = 2.0f;
			}

			public bool UseDefaultHacks { get; set; }

			[DefaultValue(0)]
			[DisplayName("Multi-sampling")]
			[Description("Enable/Disable MultiSampling (0=off, 2,4,8,16=quality)")]
			public int MultiSampling { get; set; }

			public enum AspectRatioMode
			{
				[Description("Stretch")]
				Stretch = 0,

				[Description("Force 4:3")]
				FourThree = 1,

				[Description("Force 16:9")]
				SixteenNine = 2,

				[Description("Adjust")]
				Adjust = 3
			}

			[DefaultValue(AspectRatioMode.FourThree)]
			[DisplayName("Aspect Ratio")]
			[Description("Screen aspect ratio (0=stretch, 1=force 4:3, 2=force 16:9, 3=adjust)")]
			public AspectRatioMode AspectRatio { get; set; }

			public enum SwapMode
			{
				[Description("On VI update call")]
				OnVIUpdateCall = 0,

				[Description("On VI origin change")]
				OnVIOriginChange = 1,

				[Description("On buffer update")]
				OnBufferUpdate = 2
			}

			[DefaultValue(SwapMode.OnVIUpdateCall)]
			[DisplayName("Buffer swap mode")]
			[Description("Swap frame buffers (0=On VI update call, 1=On VI origin change, 2=On buffer update)")]
			public SwapMode BufferSwapMode { get; set; }

			[DefaultValue(0)]
			[DisplayName("Use native resolution factor")]
			[Description("Frame buffer size is the factor of N64 native resolution.")]
			public int UseNativeResolutionFactor { get; set; }

			public enum bilinearFilteringMode
			{
				[Description("N64 3point")]
				ThreePoint = 0,

				[Description("Standard")]
				Standard = 1
			}

			[DefaultValue(bilinearFilteringMode.Standard)]
			[DisplayName("Bilinear filtering mode")]
			[Description("Bilinear filtering mode (0=N64 3point, 1=standard)")]
			public bilinearFilteringMode bilinearMode { get; set; }

			[DefaultValue(false)]
			[DisplayName("Max level of Anisotropic Filtering")]
			[Description("Max level of Anisotropic Filtering, 0 for off")]
			public bool MaxAnisotropy { get; set; }

			[DefaultValue(500)]
			[DisplayName("Cache Size")]
			[Description("Size of texture cache in megabytes. Good value is VRAM*3/4")]
			public int CacheSize { get; set; }

			[DefaultValue(true)]
			[DisplayName("Color noise emulation")]
			[Description("Enable color noise emulation.")]
			public bool EnableNoise { get; set; }

			[DefaultValue(true)]
			[DisplayName("LOD emulation")]
			[Description("Enable LOD emulation.")]
			public bool EnableLOD { get; set; }

			[DefaultValue(false)]
			[DisplayName("HW Lighting")]
			[Description("Enable hardware per-pixel lighting.")]
			public bool EnableHWLighting { get; set; }

			[DefaultValue(true)]
			[DisplayName("Shaders storage")]
			[Description("Use persistent storage for compiled shaders.")]
			public bool EnableShadersStorage { get; set; }

			public enum TexrectCoordsMode
			{
				[Description("Off")]
				Off = 0,

				[Description("Auto")]
				Auto = 1,

				[Description("Force")]
				Force = 2
			}

			[DefaultValue(TexrectCoordsMode.Off)]
			[DisplayName("Bilinear filtering mode")]
			[Description("Make texrect coordinates continuous to avoid black lines between them. (0=Off, 1=Auto, 2=Force)")]
			public TexrectCoordsMode CorrectTexrectCoords { get; set; }

			[DefaultValue(false)]
			[DisplayName("Native Res Texrects")]
			[Description("Render 2D texrects in native resolution to fix misalignment between parts of 2D image.")]
			public bool EnableNativeResTexrects { get; set; }

			[DefaultValue(false)]
			[DisplayName("Legacy Blending")]
			[Description("Do not use shaders to emulate N64 blending modes. Works faster on slow GPU. Can cause glitches.")]
			public bool EnableLegacyBlending { get; set; }

			[DefaultValue(true)]
			[DisplayName("Fragment Depth Write")]
			[Description("Enable writing of fragment depth. Some mobile GPUs do not support it, thus it made optional. Leave enabled.")]
			public bool EnableFragmentDepthWrite { get; set; }

			[DefaultValue(true)]
			[DisplayName("FB Emulation")]
			[Description("Enable frame and|or depth buffer emulation.")]
			public bool EnableFBEmulation { get; set; }

			[DefaultValue(false)]
			[DisplayName("Copy auxiliary to RDRAM")]
			[Description("Copy auxiliary buffers to RDRAM")]
			public bool EnableCopyAuxiliaryToRDRAM { get; set; }

			[DefaultValue(false)]
			[DisplayName("N64 Depth Compare")]
			[Description("Enable N64 depth compare instead of OpenGL standard one. Experimental.")]
			public bool EnableN64DepthCompare { get; set; }

			[DefaultValue(true)]
			[DisplayName("FB Info")]
			[Description("Disable buffers read/write with FBInfo. Use for games, which do not work with FBInfo.")]
			public bool DisableFBInfo { get; set; }

			[DefaultValue(false)]
			[DisplayName("FB Info Read Color Chunk")]
			[Description("Read color buffer by 4kb chunks (strict follow to FBRead specification)")]
			public bool FBInfoReadColorChunk { get; set; }

			[DefaultValue(true)]
			[DisplayName("FB Info Read Depth Chunk")]
			[Description("Read depth buffer by 4kb chunks (strict follow to FBRead specification)")]
			public bool FBInfoReadDepthChunk { get; set; }

			public enum CopyColorToRDRAMMode
			{
				[Description("Do not copy")]
				DoNotCopy = 0,

				[Description("Copy in sync mode")]
				SyncMode = 1,

				[Description("Copy in async mode")]
				AsyncMode = 2
			}

			[DefaultValue(CopyColorToRDRAMMode.AsyncMode)]
			[DisplayName("Copy Color To RDRAM")]
			[Description("Enable color buffer copy to RDRAM (0=do not copy, 1=copy in sync mode, 2=copy in async mode)")]
			public CopyColorToRDRAMMode EnableCopyColorToRDRAM { get; set; }

			public enum CopyDepthToRDRAMMode
			{
				[Description("Do not copy")]
				DoNotCopy = 0,

				[Description("Copy from video memory")]
				VideoMemory = 1,

				[Description("Use software render")]
				SoftwareRender = 2
			}

			[DefaultValue(CopyDepthToRDRAMMode.DoNotCopy)]
			[DisplayName("Copy Depth To RDRAM")]
			[Description("Enable depth buffer copy to RDRAM  (0=do not copy, 1=copy from video memory, 2=use software render)")]
			public CopyDepthToRDRAMMode EnableCopyDepthToRDRAM { get; set; }

			[DefaultValue(false)]
			[DisplayName("Copy Color From RDRAM")]
			[Description("Enable color buffer copy from RDRAM.")]
			public bool EnableCopyColorFromRDRAM { get; set; }

			public enum TextureFilterMode
			{
				[Description("None")]
				None = 0,

				[Description("Smooth filtering 1")]
				Smooth1 = 1,

				[Description("Smooth filtering 2")]
				Smooth2 = 2,

				[Description("Smooth filtering 3")]
				Smooth3 = 3,

				[Description("Smooth filtering 4")]
				Smooth4 = 4,

				[Description("Sharp filtering 1")]
				Sharp1 = 5,

				[Description("Sharp filtering 2")]
				Sharp2 = 6
			}

			[DefaultValue(TextureFilterMode.None)]
			[DisplayName("Texture filter")]
			[Description("Texture filter (0=none, 1=Smooth filtering 1, 2=Smooth filtering 2, 3=Smooth filtering 3, 4=Smooth filtering 4, 5=Sharp filtering 1, 6=Sharp filtering 2)")]
			public TextureFilterMode txFilterMode { get; set; }

			public enum TextureEnhancementMode
			{
				[Description("None")]
				None = 0,

				[Description("Store as is")]
				StoreAsIs = 1,

				[Description("X2")]
				X2 = 2,

				[Description("X2SAI")]
				X2SAI = 3,

				[Description("HQ2X")]
				HQ2X = 4,

				[Description("HQ2XS")]
				HQ2XS = 5,

				[Description("LQ2X")]
				LQ2X = 6,

				[Description("LQ2XS")]
				LQ2XS = 7,

				[Description("HQ4X")]
				HQ4X = 8,

				[Description("2xBRZ")]
				TwoxBRZ = 9,

				[Description("3xBRZ")]
				ThreexBRZ = 10,

				[Description("4xBRZ")]
				FourxBRZ = 11,

				[Description("5xBRZ")]
				FivexBRZ = 12,

				[Description("6xBRZ")]
				SizxBRZ = 13
			}

			[DefaultValue(TextureEnhancementMode.None)]
			[DisplayName("Texture Enhancement Mode")]
			[Description("Texture Enhancement (0=none, 1=store as is, 2=X2, 3=X2SAI, 4=HQ2X, 5=HQ2XS, 6=LQ2X, 7=LQ2XS, 8=HQ4X, 9=2xBRZ, 10=3xBRZ, 11=4xBRZ, 12=5xBRZ), 13=6xBRZ)")]
			public TextureEnhancementMode txEnhancementMode { get; set; }

			[DefaultValue(false)]
			[DisplayName("Texture Deposterize")]
			[Description("Deposterize texture before enhancement.")]
			public bool txDeposterize { get; set; }

			[DefaultValue(false)]
			[DisplayName("Texture Filter Ignore BG")]
			[Description("Don't filter background textures.")]
			public bool txFilterIgnoreBG { get; set; }

			[DefaultValue(100)]
			[DisplayName("Texture Cache Size")]
			[Description("Size of filtered textures cache in megabytes.")]
			public int txCacheSize { get; set; }

			[DefaultValue(false)]
			[DisplayName("Texture Hires Enable")]
			[Description("Use high-resolution texture packs if available.")]
			public bool txHiresEnable { get; set; }

			[DefaultValue(false)]
			[DisplayName("Texture Hires Full Alpha Channel")]
			[Description("Allow to use alpha channel of high-res texture fully.")]
			public bool txHiresFullAlphaChannel { get; set; }

			[DefaultValue(false)]
			[DisplayName("Texture Hres Alt CRC")]
			[Description("Use alternative method of paletted textures CRC calculation.")]
			public bool txHresAltCRC { get; set; }

			[DefaultValue(false)]
			[DisplayName("Texture Dump")]
			[Description("Enable dump of loaded N64 textures.")]
			public bool txDump { get; set; }

			[DefaultValue(true)]
			[DisplayName("Texture Cache Compression")]
			[Description("Zip textures cache.")]
			public bool txCacheCompression { get; set; }

			[DefaultValue(false)]
			[DisplayName("Texture Force 16bpp")]
			[Description("Force use 16bit texture formats for HD textures.")]
			public bool txForce16bpp { get; set; }

			[DefaultValue(true)]
			[DisplayName("Texture Save Cache")]
			[Description("Save texture cache to hard disk.")]
			public bool txSaveCache { get; set; }

			[DefaultValue("")]
			[DisplayName("Texture Path")]
			[Description("Path to folder with hi-res texture packs.")]
			public string txPath { get; set; }

			[DefaultValue(false)]
			[DisplayName("Enable Bloom")]
			[Description("Enable bloom filter")]
			public bool EnableBloom { get; set; }

			[DefaultValue(4)]
			[DisplayName("Bloom Threshold Level")]
			[Description("Brightness threshold level for bloom. Values [2, 6]")]
			public int bloomThresholdLevel { get; set; }

			public enum BlendMode
			{
				[Description("Strong")]
				Strong = 0,

				[Description("Mild")]
				Mild = 1,

				[Description("Light")]
				Light = 2
			}

			[DefaultValue(BlendMode.Strong)]
			[DisplayName("Bloom Blend Mode")]
			[Description("Bloom blend mode (0=Strong, 1=Mild, 2=Light)")]
			public BlendMode bloomBlendMode { get; set; }

			[DefaultValue(10)]
			[DisplayName("Blur Amount")]
			[Description("Blur radius. Values [2, 10]")]
			public int blurAmount { get; set; }

			[DefaultValue(20)]
			[DisplayName("Blur Strength")]
			[Description("Blur strength. Values [10, 100]")]
			public int blurStrength { get; set; }

			[DefaultValue(false)]
			[DisplayName("Force Gamma Correction")]
			[Description("Force gamma correction.")]
			public bool ForceGammaCorrection { get; set; }

			[DefaultValue(2.0)]
			[DisplayName("Gamma Correction Level")]
			[Description("Gamma correction level.")]
			public float GammaCorrectionLevel { get; set; }

			public N64GLideN64PluginSettings Clone()
			{
				return (N64GLideN64PluginSettings)MemberwiseClone();
			}

			public void FillPerGameHacks(GameInfo game)
			{
				if (UseDefaultHacks)
				{
					EnableN64DepthCompare = game.GetBool("GLideN64_N64DepthCompare", false);
					EnableCopyColorToRDRAM = (CopyColorToRDRAMMode)game.GetInt("GLideN64_CopyColorToRDRAM", (int)CopyColorToRDRAMMode.AsyncMode);
					EnableCopyDepthToRDRAM = (CopyDepthToRDRAMMode)game.GetInt("GLideN64_CopyDepthToRDRAM", (int)CopyDepthToRDRAMMode.DoNotCopy);
					EnableCopyColorFromRDRAM = game.GetBool("GLideN64_CopyColorFromRDRAM", false);
					EnableCopyAuxiliaryToRDRAM = game.GetBool("GLideN64_CopyAuxiliaryToRDRAM", false);
				}
			}

			public PluginType GetPluginType()
			{
				return PluginType.GLideN64;
			}
		}
	}
}