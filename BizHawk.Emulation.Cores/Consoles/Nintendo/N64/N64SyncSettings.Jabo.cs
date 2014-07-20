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
				anisotropic_level = ANISOTROPIC_FILTERING_LEVEL.four_times;
				brightness = 100;
				super2xsal = false;
				texture_filter = false;
				adjust_aspect_ratio = false;
				legacy_pixel_pipeline = false;
				alpha_blending = false;
				wireframe = false;
				direct3d_transformation_pipeline = false;
				z_compare = false;
				copy_framebuffer = false;
				resolution_width = -1;
				resolution_height = -1;
				clear_mode = DIRECT3D_CLEAR_MODE.def;
			}

			public PluginType GetPluginType()
			{
				return PluginType.Jabo;
			}

			public void FillPerGameHacks(GameInfo game)
			{

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

			public enum ANISOTROPIC_FILTERING_LEVEL
			{
				[Description("Off")]
				off = 0,

				[Description("2X")]
				two_times = 1,

				[Description("4X")]
				four_times = 2,

				[Description("8X")]
				eight_times = 3,

				[Description("16X")]
				sixteen_times = 4
			}

			[DefaultValue(ANISOTROPIC_FILTERING_LEVEL.four_times)]
			[Description("Anisotropic filtering level")]
			//[DisplayName("Anisotropic filtering")]
			public ANISOTROPIC_FILTERING_LEVEL anisotropic_level { get; set; }

			[DefaultValue(100)]
			[Description("Brightness level, 100%-190%")]
			//[DisplayName("Brightness")]
			public int brightness { get; set; }

			[DefaultValue(false)]
			[Description("Enables Super2xSal textures")]
			//[DisplayName("Super2xSal textures")]
			public bool super2xsal { get; set; }

			[DefaultValue(false)]
			[Description("Always use texture filter")]
			//[DisplayName("Always use texture filter")]
			public bool texture_filter { get; set; }

			[DefaultValue(false)]
			[Description("Adjust game aspect ratio to match yours")]
			//[DisplayName("Adjust game aspect ratio to match yours")]
			public bool adjust_aspect_ratio { get; set; }

			[DefaultValue(false)]
			[Description("Use legacy pixel pipeline")]
			//[DisplayName("Use legacy pixel pipeline")]
			public bool legacy_pixel_pipeline { get; set; }

			[DefaultValue(false)]
			[Description("Force alpha blending")]
			//[DisplayName("Force alpha blending")]
			public bool alpha_blending { get; set; }

			[DefaultValue(false)]
			[Description("Wireframe rendering")]
			//[DisplayName("Wireframe rendering")]
			public bool wireframe { get; set; }

			[DefaultValue(false)]
			[Description("Use Direct3D transformation pipeline")]
			//[DisplayName("Use Direct3D transformation pipeline")]
			public bool direct3d_transformation_pipeline { get; set; }

			[DefaultValue(false)]
			[Description("Force Z Compare")]
			//[DisplayName("Force Z Compare")]
			public bool z_compare { get; set; }

			[DefaultValue(false)]
			[Description("Copy framebuffer to RDRAM")]
			//[DisplayName("Copy framebuffer to RDRAM")]
			public bool copy_framebuffer { get; set; }

			[DefaultValue(-1)]
			[Description("Emulated Width")]
			//[DisplayName("Emulated Width")]
			public int resolution_width { get; set; }

			[DefaultValue(-1)]
			[Description("Emulated Height")]
			//[DisplayName("Emulated Height")]
			public int resolution_height { get; set; }

			public enum DIRECT3D_CLEAR_MODE
			{
				[Description("Default")]
				def = 0,

				[Description("Only Per Frame")]
				per_frame = 1,

				[Description("Always")]
				always = 2
			}
			[DefaultValue(DIRECT3D_CLEAR_MODE.def)]
			[Description("Direct3D Clear Mode")]
			//[DisplayName("Direct3D Clear Mode")]
			public DIRECT3D_CLEAR_MODE clear_mode { get; set; }

			public N64JaboPluginSettings Clone()
			{
				return (N64JaboPluginSettings)MemberwiseClone();
			}
		}

	}
}