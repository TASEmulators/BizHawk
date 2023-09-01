using BizHawk.Common;
using BizHawk.Emulation.Common;

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.N3DS
{
	public partial class Citra : ISettable<Citra.CitraSettings, Citra.CitraSyncSettings>
	{
		private CitraSettings _settings;
		private CitraSyncSettings _syncSettings;
		private readonly string _userPath;

		private bool GetBooleanSettingCallback(string label) => label switch
		{
			"use_cpu_jit" => _syncSettings.UseCpuJit,
			"use_hw_shader" => _syncSettings.UseHwShader,
			"shaders_accurate_mul" => _syncSettings.ShadersAccurateMul,
			"use_shader_jit" => _syncSettings.UseShaderJit,
			"use_virtual_sd" => _syncSettings.UseVirtualSd,
			"is_new_3ds" => _syncSettings.IsNew3ds,
			"filter_mode" => _settings.FilterMode,
			"swap_screen" => _settings.SwapScreen,
			"upright_screen" => _settings.UprightScreen,
			"custom_layout" => _settings.UseCustomLayout,
			_ => throw new InvalidOperationException()
		};

		private static readonly DateTime _epoch = new(1970, 1, 1, 0, 0, 0);

		private ulong GetIntegerSettingCallback(string label) => label switch
		{
			"cpu_clock_percentage" => (ulong)_syncSettings.CpuClockPercentage,
			"graphics_api" => (ulong)CitraSyncSettings.EGraphicsApi.OpenGL,//_supportsOpenGL43 ? (ulong)_syncSettings.GraphicsApi : (ulong)CitraSyncSettings.EGraphicsApi.Software,
			"region_value" => (ulong)_syncSettings.RegionValue,
			"init_clock" => _syncSettings.UseRealTime && !DeterministicEmulation ? 0UL : 1UL,
			"init_time" => (ulong)(_syncSettings.InitialTime - _epoch).TotalSeconds,
			"birthmonth" => (ulong)_syncSettings.CFGBirthdayMonth,
			"birthday" => (ulong)_syncSettings.CFGBirthdayDay,
			"language" => (ulong)_syncSettings.CFGSystemLanguage,
			"sound_mode" => (ulong)_syncSettings.CFGSoundOutputMode,
			"playcoins" => _syncSettings.PTMPlayCoins,
			"resolution_factor" => (ulong)_settings.ResolutionFactor,
			"texture_filter" => (ulong)_settings.TextureFilter,
			"mono_render_option" => (ulong)_settings.MonoRenderOption,
			"render_3d" => (ulong)_settings.StereoRenderOption,
			"factor_3d" => (ulong)_settings.Factor3D,
			"layout_option" => (ulong)_settings.LayoutOption,
			"custom_top_left" => (ulong)_settings.CustomLayoutTopScreenRectangle.Left,
			"custom_top_top" => (ulong)_settings.CustomLayoutTopScreenRectangle.Top,
			"custom_top_right" => (ulong)_settings.CustomLayoutTopScreenRectangle.Right,
			"custom_top_bottom" => (ulong)_settings.CustomLayoutTopScreenRectangle.Bottom,
			"custom_bottom_left" => (ulong)_settings.CustomLayoutBottomScreenRectangle.Left,
			"custom_bottom_top" => (ulong)_settings.CustomLayoutBottomScreenRectangle.Top,
			"custom_bottom_right" => (ulong)_settings.CustomLayoutBottomScreenRectangle.Right,
			"custom_bottom_bottom" => (ulong)_settings.CustomLayoutBottomScreenRectangle.Bottom,
			"custom_second_layer_opacity" => (ulong)_settings.CustomLayoutSecondLayerOpacity,
			"window_scale_factor" => (ulong)_settings.WindowFactor,
			_ => throw new InvalidOperationException()
		};

		private double GetFloatSettingCallback(string label) => label switch
		{
			"volume" => _syncSettings.Volume / 100.0,
			"bg_red" => _settings.BackgroundColor.R / 255.0,
			"bg_green" => _settings.BackgroundColor.G / 255.0,
			"bg_blue" => _settings.BackgroundColor.B / 255.0,
			"large_screen_proportion" => _settings.LargeScreenProportion,
			_ => throw new InvalidOperationException()
		};

		private void GetStringSettingCallback(string label, IntPtr buffer, int bufferSize)
		{
			string ret = label switch
			{
				"user_directory" => _userPath,
				"username" => _syncSettings.CFGUsername,
				_ => throw new InvalidOperationException()
			};

			byte[] bytes = Encoding.UTF8.GetBytes(ret);
			int numToCopy = Math.Min(bytes.Length, bufferSize - 1);
			Marshal.Copy(bytes, 0, buffer, numToCopy);
			Marshal.WriteByte(buffer, numToCopy, 0);
		}

		public CitraSettings GetSettings()
			=> _settings.Clone();

		public CitraSyncSettings GetSyncSettings()
			=> _syncSettings.Clone();

		public PutSettingsDirtyBits PutSettings(CitraSettings o)
		{
			_settings = o;
			_core.Citra_ReloadConfig(_context);
			return PutSettingsDirtyBits.None;
		}

		public PutSettingsDirtyBits PutSyncSettings(CitraSyncSettings o)
		{
			bool ret = CitraSyncSettings.NeedsReboot(_syncSettings, o);
			_syncSettings = o;
			return ret ? PutSettingsDirtyBits.RebootCore : PutSettingsDirtyBits.None;
		}

		public class CitraSettings
		{
			[DisplayName("Resolution Scale Factor")]
			[Description("Scale factor for the 3DS resolution.")]
			[DefaultValue(1)]
			[Range(1, 10)]
			[TypeConverter(typeof(ConstrainedIntConverter))]
			public int ResolutionFactor { get; set; }
			
			[DisplayName("Window Scale Factor")]
			[Description("Scale factor for the window.")]
			[DefaultValue(1)]
			[Range(1, 10)]
			[TypeConverter(typeof(ConstrainedIntConverter))]
			public int WindowFactor { get; set; }

			public enum ETextureFilter
			{
				None = 0,
				Anime4K = 1,
				Bicubic = 2,
				NearestNeighbor = 3,
				ScaleForce = 4,
				xBRZ = 5
			}

			[DisplayName("Texture Filter")]
			[Description("")]
			[DefaultValue(ETextureFilter.None)]
			public ETextureFilter TextureFilter { get; set; }

			public enum EMonoRenderOption
			{
				LeftEye,
				RightEye
			}

			[DisplayName("Mono Render Option")]
			[Description("Change Default Eye to Render When in Monoscopic Mode")]
			[DefaultValue(EMonoRenderOption.LeftEye)]
			public EMonoRenderOption MonoRenderOption { get; set; }

			public enum EStereoRenderOption
			{
				Off,
				SideBySide,
				Anaglyph,
				Interlaced,
				ReverseInterlaced
			}

			[DisplayName("Stereo Render Option")]
			[Description(" Whether and how Stereoscopic 3D should be rendered")]
			[DefaultValue(EStereoRenderOption.Off)]
			public EStereoRenderOption StereoRenderOption { get; set; }

			[DisplayName("3D Intensity")]
			[Description("Change 3D Intensity")]
			[DefaultValue(0)]
			[Range(0, 100)]
			[TypeConverter(typeof(ConstrainedIntConverter))]
			public int Factor3D { get; set; }

			[DisplayName("Enable Linear Filtering")]
			[Description("Whether to enable linear filtering or not")]
			[DefaultValue(true)]
			public bool FilterMode { get; set; }

			[DisplayName("Background Color")]
			[Description("The clear color for the renderer. What shows up on the sides of the bottom screen.")]
			[DefaultValue(typeof(Color), "0xFF000000")]
			public Color BackgroundColor { get; set; }

			public enum ELayoutOption
			{
				TopBottomScreen,
				SingleScreen,
				LargeScreen,
				SideScreen,
				//HybridScreen = 5,
			}

			[DisplayName("Layout Option")]
			[Description("Layout for the screen inside the render window.")]
			[DefaultValue(ELayoutOption.TopBottomScreen)]
			public ELayoutOption LayoutOption { get; set; }
			
			[DisplayName("Swap Screen")]
			[Description("Swaps the prominent screen with the other screen.")]
			[DefaultValue(false)]
			public bool SwapScreen { get; set; }

			[DisplayName("Upright Screen")]
			[Description("Toggle upright orientation, for book style games.")]
			[DefaultValue(false)]
			public bool UprightScreen { get; set; }
			
			[DisplayName("Large Screen Proportion")]
			[Description("The proportion between the large and small screens when playing in Large Screen Small Screen layout.")]
			[DefaultValue(4.0f)]
			[Range(1.0f, 16.0f)]
			[TypeConverter(typeof(ConstrainedFloatConverter))]
			public float LargeScreenProportion { get; set; }

			[DisplayName("Use Custom Layout")]
			[Description("Toggle custom layout on or off")]
			[DefaultValue(false)]
			public bool UseCustomLayout { get; set; }

			[DisplayName("Custom Layout Top Screen Rectangle")]
			[Description("")]
			public Rectangle CustomLayoutTopScreenRectangle { get; set; }

			[DisplayName("Custom Layout Bottom Screen Rectangle")]
			[Description("")]
			public Rectangle CustomLayoutBottomScreenRectangle { get; set; }

			[DisplayName("Custom Layout Second Layer Opacity")]
			[Description("Opacity of second layer when using custom layout option (bottom screen unless swapped)")]
			[DefaultValue(100)]
			[Range(0, 100)]
			[TypeConverter(typeof(ConstrainedIntConverter))]
			public int CustomLayoutSecondLayerOpacity { get; set; }

			public CitraSettings Clone()
				=> (CitraSettings)MemberwiseClone();

			public CitraSettings()
				=> SettingsUtil.SetDefaultValues(this);
		}

		public class CitraSyncSettings
		{
			[DisplayName("Use CPU JIT")]
			[Description("Whether to use the Just-In-Time (JIT) compiler for CPU emulation")]
			[DefaultValue(true)]
			public bool UseCpuJit { get; set; }

			[DisplayName("CPU Clock Percentage")]
			[Description("Change the Clock Frequency of the emulated 3DS CPU.\n" +
				"Underclocking can increase the performance of the game at the risk of freezing.\n" +
				"Overclocking may fix lag that happens on console, but also comes with the risk of freezing.")]
			[DefaultValue(100)]
			[Range(25, 400)]
			[TypeConverter(typeof(ConstrainedIntConverter))]
			public int CpuClockPercentage { get; set; }

			public enum EGraphicsApi
			{
				Software,
				OpenGL
			}

			[DisplayName("Graphics API")]
			[Description("Whether to render using OpenGL or Software. Forced to software if OpenGL 4.3+ is not available.")]
			[DefaultValue(EGraphicsApi.OpenGL)]
			public EGraphicsApi GraphicsApi { get; set; }

			[DisplayName("Use HW Shader")]
			[Description("Whether to use hardware shaders to emulate 3DS shaders")]
			[DefaultValue(true)]
			public bool UseHwShader { get; set; }
			
			[DisplayName("Shaders Accurate Mul")]
			[Description("Whether to use accurate multiplication in hardware shaders")]
			[DefaultValue(true)]
			public bool ShadersAccurateMul { get; set; }
			
			[DisplayName("Use Shader JIT")]
			[Description("Whether to use the Just-In-Time (JIT) compiler for shader emulation")]
			[DefaultValue(true)]
			public bool UseShaderJit { get; set; }

			[DisplayName("Volume Percentage")]
			[Description("Output volume")]
			[DefaultValue(100)]
			[Range(0, 100)]
			[TypeConverter(typeof(ConstrainedIntConverter))]
			public int Volume { get; set; }

			[DisplayName("Use Virtual SD")]
			[Description("Whether to create a virtual SD card.")]
			[DefaultValue(true)]
			public bool UseVirtualSd { get; set; }
			
			[DisplayName("Is New 3DS")]
			[Description("The system model that Citra will try to emulate.")]
			[DefaultValue(true)]
			public bool IsNew3ds { get; set; }

			public enum ERegionValue
			{
				Autodetect = -1,
				Japan,
				USA,
				Europe,
				Australia,
				China,
				Korea,
				Taiwan,
			}
			
			[DisplayName("Region Value")]
			[Description("The system region that Citra will use during emulation")]
			[DefaultValue(ERegionValue.Autodetect)]
			public ERegionValue RegionValue { get; set; }

			[DisplayName("Use Real Time")]
			[Description("If true, RTC clock will be based off of real time instead of emulated time. Ignored (set to false) when recording a movie.")]
			[DefaultValue(true)]
			public bool UseRealTime { get; set; }

			[DisplayName("Initial Time")]
			[Description("Initial time of emulation.")]
			[DefaultValue(typeof(DateTime), "2010-01-01")]
			[TypeConverter(typeof(BizDateTimeConverter))]
			public DateTime InitialTime { get; set; }
			
			[DisplayName("CFG Username")]
			[Description("The system username that Citra will use during emulation")]
			[DefaultValue("CITRA")]
			[MaxLength(10)]
			[TypeConverter(typeof(ConstrainedStringConverter))]
			public string CFGUsername { get; set; }

			public enum ECFGBirthdayMonth
			{
				January = 1,
				February,
				March,
				April,
				May,
				June,
				July,
				August,
				September,
				October,
				November,
				December,
			}

			[DisplayName("CFG Birthday Month")]
			[Description("The system birthday month that Citra will use during emulation")]
			[DefaultValue(ECFGBirthdayMonth.March)]
			public ECFGBirthdayMonth CFGBirthdayMonth { get; set; }

			[DisplayName("CFG Birthday Day")]
			[Description("The system birthday day that Citra will use during emulation")]
			[DefaultValue(25)]
			[Range(1, 31)]
			[TypeConverter(typeof(ConstrainedIntConverter))]
			public int CFGBirthdayDay { get; set; }

			public enum ECFGSystemLanguage
			{
				Japan,
				English,
				French,
				German,
				Italian,
				Spanish,
				Simplified_Chinese,
				Korean,
				Dutch,
				Portuguese,
				Russian,
				Traditional_Chinese
			}

			[DisplayName("CFG System Language")]
			[Description("The system language that Citra will use during emulation")]
			[DefaultValue(ECFGSystemLanguage.English)]
			public ECFGSystemLanguage CFGSystemLanguage { get; set; }
			
			public enum ECFGSoundOutputMode
			{
				Mono,
				Stereo,
				Surround
			}
			
			[DisplayName("CFG Sound Output Mode")]
			[Description("The system sound output mode that Citra will use during emulation")]
			[DefaultValue(ECFGSoundOutputMode.Stereo)]
			public ECFGSoundOutputMode CFGSoundOutputMode { get; set; }
			
			[DisplayName("CFG Sound Output Mode")]
			[Description("The system sound output mode that Citra will use during emulation")]
			[DefaultValue(typeof(ushort), "42")]
			public ushort PTMPlayCoins { get; set; }

			public CitraSyncSettings Clone()
				=> (CitraSyncSettings)MemberwiseClone();

			public static bool NeedsReboot(CitraSyncSettings x, CitraSyncSettings y)
				=> !DeepEquality.DeepEquals(x, y);

			public CitraSyncSettings()
				=> SettingsUtil.SetDefaultValues(this);
		}
	}
}
