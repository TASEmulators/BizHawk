using System.ComponentModel;
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
			[Category("General")]
			public int wfmode { get; set; }

			[DefaultValue(false)]
			[DisplayName("Wire Frame Display")]
			[Category("General")]
			public bool wireframe { get; set; }

			[DefaultValue(0)]
			[DisplayName("Card ID")]
			[Category("General")]
			public int card_id { get; set; }

			[DefaultValue(false)]
			[DisplayName("Zelda corona fix")]
			[Category("General")]
			public bool flame_corona { get; set; }

			[DefaultValue(2)]
			[DisplayName("Force microcode")]
			[Category("General")]
			public int ucode { get; set; }

			[DefaultValue(true)]
			[DisplayName("Auto-detect microcode")]
			[Category("General")]
			public bool autodetect_ucode { get; set; }

			[DefaultValue(false)]
			[DisplayName("Motion Blur")]
			[Category("General")]
			public bool motionblur { get; set; }

			[DefaultValue(false)]
			[DisplayName("Framebuffer read every frame")]
			[Category("General")]
			public bool fb_read_always { get; set; }

			[DefaultValue(false)]
			[DisplayName("Display unknown combines as red")]
			[Category("General")]
			public bool unk_as_red { get; set; }

			[DefaultValue(false)]
			[DisplayName("Filter Cache")]
			[Category("General")]
			public bool filter_cache { get; set; }

			[DefaultValue(false)]
			[DisplayName("Fast CRC")]
			[Category("General")]
			public bool fast_crc { get; set; }

			[DefaultValue(false)]
			[DisplayName("Disable Aux Buffer")]
			[Category("General")]
			public bool disable_auxbuf { get; set; }

			[DefaultValue(false)]
			[DisplayName("Use framebuffer objects")]
			[Category("General")]
			public bool fbo { get; set; }

			[DefaultValue(true)]
			[DisplayName("Disable GLSL combiners")]
			[Category("General")]
			public bool noglsl { get; set; }

			[DefaultValue(true)]
			[DisplayName("Disable dithered alpha")]
			[Category("General")]
			public bool noditheredalpha { get; set; }

			[DefaultValue(0)]
			[DisplayName("Texture Filter")]
			[Description("0=None, 1=Blur edges, 2=Super 2xSai, 3=Hq2x, 4=Hq4x")]
			[Category("General")]
			public int tex_filter { get; set; }

			[DefaultValue(false)]
			[DisplayName("Framebuffer Render")] 
			[Category("General")]
			public bool fb_render { get; set; }

			[DefaultValue(false)]
			[DisplayName("Wrap textures too big for tmem")]
			[Category("Per Game Settings")]
			public bool wrap_big_tex { get; set; }

			[DefaultValue(false)]
			[DisplayName("Use Sts1 Only")]
			[Category("Per Game Settings")]
			public bool use_sts1_only { get; set; }

			[DefaultValue(false)]
			[DisplayName("Soft Depth Compare")]
			[Category("Per Game Settings")]
			public bool soft_depth_compare { get; set; }

			[DefaultValue(false)]
			[DisplayName("PPL")]
			[Category("Per Game Settings")]
			public bool PPL { get; set; }

			[DefaultValue(false)]
			[DisplayName("Framebuffer Optimize Write")]
			[Category("Per Game Settings")]
			public bool fb_optimize_write { get; set; }

			[DefaultValue(true)]
			[DisplayName("Framebuffer Optimize Texture Rectangle")]
			[Category("Per Game Settings")]
			public bool fb_optimize_texrect { get; set; }

			[DefaultValue(false)]
			[DisplayName("Increase Texture Rectangle Edge")]
			[Category("Per Game Settings")]
			public bool increase_texrect_edge { get; set; }

			[DefaultValue(false)]
			[DisplayName("Increase Prim Depth")]
			[Category("Per Game Settings")]
			public bool increase_primdepth { get; set; }

			[DefaultValue(false)]
			[DisplayName("Framebuffer Ignore Previous")]
			[Category("Per Game Settings")]
			public bool fb_ignore_previous { get; set; }

			[DefaultValue(false)]
			[DisplayName("Framebuffer Ignore Aux Copy")]
			[Category("Per Game Settings")]
			public bool fb_ignore_aux_copy { get; set; }

			[DefaultValue(true)]
			[DisplayName("Framebuffer High Resolution Buffer Clear")]
			[Category("Per Game Settings")]
			public bool fb_hires_buf_clear { get; set; }

			[DefaultValue(false)]
			[DisplayName("Force Mirco Check")]
			[Category("Per Game Settings")]
			public bool force_microcheck { get; set; }

			[DefaultValue(false)]
			[DisplayName("Force Depth Compare")]
			[Category("Per Game Settings")]
			public bool force_depth_compare { get; set; }

			[DefaultValue(true)]
			[DisplayName("Fog Enabled")]
			[Category("Per Game Settings")]
			public bool fog { get; set; }

			[DefaultValue(false)]
			[DisplayName("Fill Color Fix")]
			[Category("Per Game Settings")]
			public bool fillcolor_fix { get; set; }

			[DefaultValue(false)]
			[DisplayName("Smart Framebuffer")]
			[Category("Per Game Settings")]
			public bool fb_smart { get; set; }

			[DefaultValue(false)]
			[DisplayName("Framebuffer Read Alpha")]
			[Category("Per Game Settings")]
			public bool fb_read_alpha { get; set; }

			[DefaultValue(false)]
			[DisplayName("Get Framebuffer Info")]
			[Category("General")]
			public bool fb_get_info { get; set; }

			[DefaultValue(true)]
			[DisplayName("High Res Framebuffer")]
			[Category("Per Game Settings")]
			public bool fb_hires { get; set; }

			[DefaultValue(false)]
			[DisplayName("Clear Framebuffer")]
			[Category("Per Game Settings")]
			public bool fb_clear { get; set; }

			[DefaultValue(false)]
			[DisplayName("Detect CPU Writes")]
			[Category("Per Game Settings")]
			public bool detect_cpu_write { get; set; }

			[DefaultValue(false)]
			[DisplayName("Decrease Fill Rect Edge")]
			[Category("Per Game Settings")]
			public bool decrease_fillrect_edge { get; set; }

			[DefaultValue(true)]
			[DisplayName("Buffer Clear on Every Frame")]
			[Category("Per Game Settings")]
			public bool buff_clear { get; set; }

			[DefaultValue(false)]
			[DisplayName("Alt Text Size")]
			[Category("Per Game Settings")]
			public bool alt_tex_size { get; set; }

			[DefaultValue(true)]
			[DisplayName("Use Default Hacks")]
			[Description("Use defaults for current game. This overrides all per game settings.")]
			[Category("Hacks")]
			public bool UseDefaultHacks { get; set; }

			[DefaultValue(0)]
			[DisplayName("Enable Hacks for Game")]
			[Category("More Per Game Settings")]
			public int enable_hacks_for_game { get; set; }

			[DefaultValue(1)]
			[DisplayName("Buffer swapping method")]
			[Description("0=Old, 1=New, 2=Hybrid")]
			[Category("More Per Game Settings")]
			public int swapmode { get; set; }

			[DefaultValue(1041204192)]
			[DisplayName("Stipple Pattern")]
			[Category("More Per Game Settings")]
			public int stipple_pattern { get; set; }

			[DefaultValue(2)]
			[DisplayName("Stipple Mode")]
			[Category("More Per Game Settings")]
			public int stipple_mode { get; set; }

			[DefaultValue(100000)]
			[DisplayName("Y Scale")]
			[Category("General")]
			public int scale_y { get; set; }

			[DefaultValue(100000)]
			[DisplayName("X Scale")]
			[Category("General")]
			public int scale_x { get; set; }

			[DefaultValue(0)]
			[DisplayName("Y Offset")]
			[Category("General")]
			public int offset_y { get; set; }

			[DefaultValue(0)]
			[DisplayName("X Offset")]
			[Category("General")]
			public int offset_x { get; set; }

			[DefaultValue(0)]
			[DisplayName("LOD calculation")]
			[Description("0=Off, 1=Fast, 2=Precise")]
			[Category("More Per Game Settings")]
			public int lodmode { get; set; }

			[DefaultValue(0)]
			[DisplayName("Fix Text Coordinates")]
			[Category("More Per Game Settings")]
			public int fix_tex_coord { get; set; }

			[DefaultValue(1)]
			[DisplayName("Filtering Mode")]
			[Description("0=None, 1=Force bilinear, 2=Force point-sampled")]
			[Category("More Per Game Settings")]
			public int filtering { get; set; }

			[DefaultValue(20)]
			[DisplayName("Depth bias level")]
			[Category("More Per Game Settings")]
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