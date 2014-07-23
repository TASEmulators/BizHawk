using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64SyncSettings
	{
		public class N64GlidePluginSettings : IPluginSettings
		{
			public N64GlidePluginSettings()
			{
				wfmode = 1;
				wireframe = false;
				card_id = 0;
				flame_corona = false;
				ucode = 2;
				autodetect_ucode = true;
				motionblur = false;
				fb_read_always = false;
				unk_as_red = false;
				filter_cache = false;
				fast_crc = false;
				disable_auxbuf = false;
				fbo = false;
				noglsl = true;
				noditheredalpha = true;
				tex_filter = 0;
				fb_render = false;
				wrap_big_tex = false;
				use_sts1_only = false;
				soft_depth_compare = false;
				PPL = false;
				fb_optimize_write = false;
				fb_optimize_texrect = true;
				increase_texrect_edge = false;
				increase_primdepth = false;
				fb_ignore_previous = false;
				fb_ignore_aux_copy = false;
				fb_hires_buf_clear = true;
				force_microcheck = false;
				force_depth_compare = false;
				fog = true;
				fillcolor_fix = false;
				fb_smart = false;
				fb_read_alpha = false;
				fb_get_info = false;
				fb_hires = true;
				fb_clear = false;
				detect_cpu_write = false;
				decrease_fillrect_edge = false;
				buff_clear = true;
				alt_tex_size = false;
				UseDefaultHacks = true;
				enable_hacks_for_game = 0;
				swapmode = 1;
				stipple_pattern = 1041204192;
				stipple_mode = 2;
				scale_y = 100000;
				scale_x = 100000;
				offset_y = 0;
				offset_x = 0;
				lodmode = 0;
				fix_tex_coord = 0;
				filtering = 1;
				depth_bias = 20;
			}

			[DefaultValue(1)]
			[DisplayName("Wire Frame Mode")]
			[Description("0=Normal colors, 1=Vertex colors, 2=Red only")]
			public int wfmode { get; set; }

			[DefaultValue(false)]
			[DisplayName("Wire Frame Display")]
			public bool wireframe { get; set; }

			[DefaultValue(0)]
			[DisplayName("Card ID")]
			public int card_id { get; set; }

			[DefaultValue(false)]
			[DisplayName("Zelda corona fix")]
			public bool flame_corona { get; set; }

			[DefaultValue(2)]
			[DisplayName("Force microcode")]
			public int ucode { get; set; }

			[DefaultValue(true)]
			[DisplayName("Auto-detect microcode")]
			public bool autodetect_ucode { get; set; }

			[DefaultValue(false)]
			[DisplayName("Motion Blur")]
			public bool motionblur { get; set; }

			[DefaultValue(false)]
			[DisplayName("Framebuffer read every frame")]
			public bool fb_read_always { get; set; }

			[DefaultValue(false)]
			[DisplayName("Display unknown combines as red")]
			public bool unk_as_red { get; set; }

			[DefaultValue(false)]
			[DisplayName("Filter Cache")]
			public bool filter_cache { get; set; }

			[DefaultValue(false)]
			[DisplayName("Fast CRC")]
			public bool fast_crc { get; set; }

			[DefaultValue(false)]
			[DisplayName("Disable Aux Buffer")]
			public bool disable_auxbuf { get; set; }

			[DefaultValue(false)]
			[DisplayName("Use framebuffer objects")]
			public bool fbo { get; set; }

			[DefaultValue(true)]
			[DisplayName("Disable GLSL combiners")]
			public bool noglsl { get; set; }

			[DefaultValue(true)]
			[DisplayName("Disable dithered alpha")]
			public bool noditheredalpha { get; set; }

			[DefaultValue(0)]
			[DisplayName("Texture Filter")]
			[Description("0=None, 1=Blur edges, 2=Super 2xSai, 3=Hq2x, 4=Hq4x")]
			public int tex_filter { get; set; }

			[DefaultValue(false)]
			[DisplayName("Framebuffer Render")] 
			public bool fb_render { get; set; }

			[DefaultValue(false)]
			[DisplayName("Wrap textures too big for tmem")]
			public bool wrap_big_tex { get; set; }

			[DefaultValue(false)]
			[DisplayName("Use Sts1 Only")]
			public bool use_sts1_only { get; set; }

			[DefaultValue(false)]
			[DisplayName("Soft Depth Compare")]
			public bool soft_depth_compare { get; set; }

			[DefaultValue(false)]
			[DisplayName("PPL")]
			public bool PPL { get; set; }

			[DefaultValue(false)]
			[DisplayName("Framebuffer Optimize Write")]
			public bool fb_optimize_write { get; set; }

			[DefaultValue(true)]
			[DisplayName("Framebuffer Optimize Texture Rectangle")]
			public bool fb_optimize_texrect { get; set; }

			[DefaultValue(false)]
			[DisplayName("Increase Texture Rectangle Edge")]
			public bool increase_texrect_edge { get; set; }

			[DefaultValue(false)]
			[DisplayName("Increase Prim Depth")]
			public bool increase_primdepth { get; set; }

			[DefaultValue(false)]
			[DisplayName("Framebuffer Ignore Previous")]
			public bool fb_ignore_previous { get; set; }

			[DefaultValue(false)]
			[DisplayName("Framebuffer Ignore Aux Copy")]
			public bool fb_ignore_aux_copy { get; set; }

			[DefaultValue(true)]
			[DisplayName("Framebuffer High Resolution Buffer Clear")]
			public bool fb_hires_buf_clear { get; set; }

			[DefaultValue(false)]
			[DisplayName("Force Mirco Check")]
			public bool force_microcheck { get; set; }

			[DefaultValue(false)]
			[DisplayName("Force Depth Compare")]
			public bool force_depth_compare { get; set; }

			[DefaultValue(true)]
			[DisplayName("Fog Enabled")]
			public bool fog { get; set; }

			[DefaultValue(false)]
			[DisplayName("Fill Color Fix")]
			public bool fillcolor_fix { get; set; }

			[DefaultValue(false)]
			[DisplayName("Smart Framebuffer")]
			public bool fb_smart { get; set; }

			[DefaultValue(false)]
			[DisplayName("Framebuffer Read Alpha")]
			public bool fb_read_alpha { get; set; }

			[DefaultValue(false)]
			[DisplayName("Get Framebuffer Info")]
			public bool fb_get_info { get; set; }

			[DefaultValue(true)]
			[DisplayName("High Res Framebuffer")]
			public bool fb_hires { get; set; }

			[DefaultValue(false)]
			[DisplayName("Clear Framebuffer")]
			public bool fb_clear { get; set; }

			[DefaultValue(false)]
			[DisplayName("Detect CPU Writes")]
			public bool detect_cpu_write { get; set; }

			[DefaultValue(false)]
			[DisplayName("Decrease Fill Rect Edge")]
			public bool decrease_fillrect_edge { get; set; }

			[DefaultValue(true)]
			[DisplayName("Buffer Clear on Every Frame")]
			public bool buff_clear { get; set; }

			[DefaultValue(false)]
			[DisplayName("Alt Text Size")]
			public bool alt_tex_size { get; set; }

			[DefaultValue(true)]
			[DisplayName("Use Default Hacks")]
			public bool UseDefaultHacks { get; set; }

			[DefaultValue(0)]
			[DisplayName("Enable Hacks for Game")]
			public int enable_hacks_for_game { get; set; }

			[DefaultValue(1)]
			[DisplayName("Buffer swapping method")]
			[Description("0=Old, 1=New, 2=Hybrid")]
			public int swapmode { get; set; }

			[DefaultValue(1041204192)]
			[DisplayName("Stipple Pattern")]
			public int stipple_pattern { get; set; }

			[DefaultValue(2)]
			[DisplayName("Stipple Mode")]
			public int stipple_mode { get; set; }

			[DefaultValue(100000)]
			[DisplayName("Y Scale")]
			public int scale_y { get; set; }

			[DefaultValue(100000)]
			[DisplayName("X Scale")]
			public int scale_x { get; set; }

			[DefaultValue(0)]
			[DisplayName("Y Offset")]
			public int offset_y { get; set; }

			[DefaultValue(0)]
			[DisplayName("X Offset")]
			public int offset_x { get; set; }

			[DefaultValue(0)]
			[DisplayName("LOD calculation")]
			[Description("0=Off, 1=Fast, 2=Precise")]
			public int lodmode { get; set; }

			[DefaultValue(0)]
			[DisplayName("Fix Text Coordinates")]
			public int fix_tex_coord { get; set; }

			[DefaultValue(1)]
			[DisplayName("Filtering Mode")]
			[Description("0=None, 1=Force bilinear, 2=Force point-sampled")]
			public int filtering { get; set; }

			[DefaultValue(20)]
			[DisplayName("Depth bias level")]
			public int depth_bias { get; set; }

			public N64GlidePluginSettings Clone()
			{
				return (N64GlidePluginSettings)MemberwiseClone();
			}

			public void FillPerGameHacks(GameInfo game)
			{
				if (UseDefaultHacks)
				{
					alt_tex_size = game.GetBool("Glide_alt_tex_size", false);
					buff_clear = game.GetBool("Glide_buff_clear", true);
					decrease_fillrect_edge = game.GetBool("Glide_decrease_fillrect_edge", false);
					detect_cpu_write = game.GetBool("Glide_detect_cpu_write", false);
					fb_clear = game.GetBool("Glide_fb_clear", false);
					fb_hires = game.GetBool("Glide_fb_clear", true);
					fb_read_alpha = game.GetBool("Glide_fb_read_alpha", false);
					fb_smart = game.GetBool("Glide_fb_smart", false);
					fillcolor_fix = game.GetBool("Glide_fillcolor_fix", false);
					fog = game.GetBool("Glide_fog", true);
					force_depth_compare = game.GetBool("Glide_force_depth_compare", false);
					force_microcheck = game.GetBool("Glide_force_microcheck", false);
					fb_hires_buf_clear = game.GetBool("Glide_fb_hires_buf_clear", true);
					fb_ignore_aux_copy = game.GetBool("Glide_fb_ignore_aux_copy", false);
					fb_ignore_previous = game.GetBool("Glide_fb_ignore_previous", false);
					increase_primdepth = game.GetBool("Glide_increase_primdepth", false);
					increase_texrect_edge = game.GetBool("Glide_increase_texrect_edge", false);
					fb_optimize_texrect = game.GetBool("Glide_fb_optimize_texrect", true);
					fb_optimize_write = game.GetBool("Glide_fb_optimize_write", false);
					PPL = game.GetBool("Glide_PPL", false);
					soft_depth_compare = game.GetBool("Glide_soft_depth_compare", false);
					use_sts1_only = game.GetBool("Glide_use_sts1_only", false);
					wrap_big_tex = game.GetBool("Glide_wrap_big_tex", false);

					depth_bias = game.GetInt("Glide_depth_bias", 20);
					filtering = game.GetInt("Glide_filtering", 1);
					fix_tex_coord = game.GetInt("Glide_fix_tex_coord", 0);
					lodmode = game.GetInt("Glide_lodmode", 0);

					stipple_mode = game.GetInt("Glide_stipple_mode", 2);
					stipple_pattern = game.GetInt("Glide_stipple_pattern", 1041204192);
					swapmode = game.GetInt("Glide_swapmode", 1);
					enable_hacks_for_game = game.GetInt("Glide_enable_hacks_for_game", 0);
				}
			}

			public PluginType GetPluginType()
			{
				return PluginType.Glide;
			}
		}
	}
}