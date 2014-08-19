using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

using BizHawk.Emulation.Common;
using System.Reflection;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64SyncSettings
	{
		public class N64JaboPluginSettings : IPluginSettings
		{
			public N64JaboPluginSettings()
			{
				UseDefaultHacks = true;

				anisotropic_level = ANISOTROPIC_FILTERING_LEVEL.FourTimes;
				antialiasing_level = ANTIALIASING_LEVEL.Off;
				brightness = 100;
				super2xsal = false;
				texture_filter = false;

				adjust_aspect_ratio = false;
				legacy_pixel_pipeline = false;
				alpha_blending = false;
				//wireframe = false;
				direct3d_transformation_pipeline = false;
				z_compare = false;
				copy_framebuffer = false;

				// Per game
				resolution_width = -1;
				resolution_height = -1;
				clear_mode = Direct3DClearMode.Default;
			}

			public PluginType GetPluginType()
			{
				return PluginType.Jabo;
			}

			public void FillPerGameHacks(GameInfo game)
			{
				if (UseDefaultHacks)
				{
					resolution_width = game.GetInt("Jabo_Resolution_Width", -1);
					resolution_height = game.GetInt("Jabo_Resolution_Height", -1);
					clear_mode = (Direct3DClearMode)game.GetInt("Jabo_Clear_Frame", (int)Direct3DClearMode.Default);
				}
			}

			public bool UseDefaultHacks { get; set; }

			[DefaultValue(ANISOTROPIC_FILTERING_LEVEL.FourTimes)]
			[DisplayName("Anisotropic filtering")]
			[Description("Anisotropic filtering level")]
			public ANISOTROPIC_FILTERING_LEVEL anisotropic_level { get; set; }

			[DefaultValue(ANTIALIASING_LEVEL.Off)]
			[DisplayName("Full-Scene Antialiasing")]
			[Description("Full-Scene Antialiasing level")]
			public ANTIALIASING_LEVEL antialiasing_level { get; set; }

			// Range: 100-190 in increments of 3, TODO: would be nice to put this in the metadata
			[DefaultValue(100)]
			[DisplayName("Brightness")]
			[Description("Brightness level, 100%-190%")]
			public int brightness { get; set; }

			[DefaultValue(false)]
			[DisplayName("Super2xSal textures")]
			[Description("Enables Super2xSal textures")]
			public bool super2xsal { get; set; }

			[DefaultValue(false)]
			[DisplayName("Always use texture filter")]
			[Description("Always use texture filter")]
			public bool texture_filter { get; set; }

			[DefaultValue(false)]
			[DisplayName("Adjust game aspect ratio to match yours")]
			[Description("Adjust game aspect ratio to match yours")]
			public bool adjust_aspect_ratio { get; set; }

			[DefaultValue(false)]
			[DisplayName("Use legacy pixel pipeline")]
			[Description("Use legacy pixel pipeline")]
			public bool legacy_pixel_pipeline { get; set; }

			[DefaultValue(false)]
			[DisplayName("Force alpha blending")]
			[Description("Force alpha blending")]
			public bool alpha_blending { get; set; }

			// As far as I can tell there is no way to apply this setting without opening the dll config window
			//[DefaultValue(false)]
			//[DisplayName("Wireframe rendering")]
			//[Description("Wireframe rendering")]
			//public bool wireframe { get; set; }

			[DefaultValue(false)]
			[DisplayName("Use Direct3D trans pipeline")]
			[Description("Use Direct3D transformation pipeline")]
			public bool direct3d_transformation_pipeline { get; set; }

			[DefaultValue(false)]
			[DisplayName("Force Z Compare")]
			[Description("Force Z Compare")]
			public bool z_compare { get; set; }

			[DefaultValue(false)]
			[DisplayName("Copy framebuffer")]
			[Description("Copy framebuffer to RDRAM")]
			public bool copy_framebuffer { get; set; }

			[DefaultValue(-1)]
			[DisplayName("Emulated Width")]
			[Description("Emulated Width")]
			public int resolution_width { get; set; }

			[DefaultValue(-1)]
			[DisplayName("Emulated Height")]
			[Description("Emulated Height")]
			public int resolution_height { get; set; }

			[DefaultValue(Direct3DClearMode.Default)]
			[DisplayName("Direct3D Clear Mode")]
			[Description("Direct3D Clear Mode")]
			public Direct3DClearMode clear_mode { get; set; }

			public enum ANISOTROPIC_FILTERING_LEVEL
			{
				[Description("Off")]
				Off = 0,

				[Description("2X")]
				TwoTimes = 1,

				[Description("4X")]
				FourTimes = 2,

				[Description("8X")]
				EightTimes = 3,

				[Description("16X")]
				SixteenTimes = 4
			}

			public enum ANTIALIASING_LEVEL
			{
				[Description("Off")]
				Off = 0,

				[Description("2X")]
				TwoTimes = 1,

				[Description("4X")]
				FourTimes = 2,

				[Description("8X")]
				EightTimes = 3
			}

			public enum Direct3DClearMode
			{
				[Description("Default")]
				Default = 0,

				[Description("Only Per Frame")]
				PerFrame = 1,

				[Description("Always")]
				Always = 2
			}

			public N64JaboPluginSettings Clone()
			{
				return (N64JaboPluginSettings)MemberwiseClone();
			}
		}

	}
}