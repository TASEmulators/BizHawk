﻿using System.ComponentModel;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64SyncSettings
	{
		public class N64Glide64mk2PluginSettings : IPluginSettings
		{
			public N64Glide64mk2PluginSettings()
			{
				wrpFBO = true;
				card_id = 0;
				use_sts1_only = false;
				optimize_texrect = true;
				increase_texrect_edge = false;
				ignore_aux_copy = false;
				hires_buf_clear = true;
				force_microcheck = false;
				fog = true;
				fb_smart = false;
				fb_read_alpha = false;
				fb_hires = true;
				detect_cpu_write = false;
				decrease_fillrect_edge = false;
				buff_clear = true;
				alt_tex_size = false;
				swapmode = 1;
				stipple_pattern = 1041204192;
				stipple_mode = 2;
				lodmode = 0;
				filtering = 0;
				wrpAnisotropic = false;
				correct_viewport = false;
				force_calc_sphere = false;
				pal230 = false;
				texture_correction = true;
				n64_z_scale = false;
				old_style_adither = false;
				zmode_compare_less = false;
				adjust_aspect = true;
				clip_zmax = true;
				clip_zmin = false;
				force_quad3d = false;
				useless_is_useless = false;
				fb_read_always = false;
				fb_get_info = false;
				fb_render = true;
				aspectmode = 0;
				fb_crc_mode = 1;
				fast_crc = true;
				UseDefaultHacks = true;
				enable_hacks_for_game = 0;
				read_back_to_screen = 0;
			}

			[DefaultValue(true)]
			[DisplayName("Wrapper FBO")]
			[Category("General")]
			public bool wrpFBO { get; set; }

			[DefaultValue(0)]
			[DisplayName("Card ID")]
			[Category("General")]
			public int card_id { get; set; }

			[DefaultValue(false)]
			[DisplayName("Use Sts1 Only")]
			[Category("Per Game Settings")]
			public bool use_sts1_only { get; set; }

			[DefaultValue(true)]
			[DisplayName("Optimize Texture Rectangle")]
			[Category("Per Game Settings")]
			public bool optimize_texrect { get; set; }

			[DefaultValue(false)]
			[DisplayName("Increase Texture Rectangle Edge")]
			[Category("Per Game Settings")]
			public bool increase_texrect_edge { get; set; }

			[DefaultValue(false)]
			[DisplayName("Ignore Aux Copy")]
			[Category("Per Game Settings")]
			public bool ignore_aux_copy { get; set; }

			[DefaultValue(true)]
			[DisplayName("High Res Buffer Clear")]
			[Category("Per Game Settings")]
			public bool hires_buf_clear { get; set; }

			[DefaultValue(false)]
			[DisplayName("Force Micro Check")]
			[Category("Per Game Settings")]
			public bool force_microcheck { get; set; }

			[DefaultValue(true)]
			[DisplayName("Fog Enabled")]
			[Category("Per Game Settings")]
			public bool fog { get; set; }

			[DefaultValue(false)]
			[DisplayName("Smart Framebuffer")]
			[Category("Per Game Settings")]
			public bool fb_smart { get; set; }

			[DefaultValue(false)]
			[DisplayName("Framebuffer Read Alpha")]
			[Category("Per Game Settings")]
			public bool fb_read_alpha { get; set; }

			[DefaultValue(true)]
			[DisplayName("High Res Framebuffer")]
			[Category("Per Game Settings")]
			public bool fb_hires { get; set; }

			[DefaultValue(false)]
			[DisplayName("Detect CPU Writes")]
			[Category("Per Game Settings")]
			public bool detect_cpu_write { get; set; }

			[DefaultValue(false)]
			[DisplayName("Decrease Fill Rectangle Edge")]
			[Category("Per Game Settings")]
			public bool decrease_fillrect_edge { get; set; }

			[DefaultValue(true)]
			[DisplayName("Buffer Clear")]
			[Category("Per Game Settings")]
			public bool buff_clear { get; set; }

			[DefaultValue(false)]
			[DisplayName("Alt Texture Size")]
			[Category("Per Game Settings")]
			public bool alt_tex_size { get; set; }

			[DefaultValue(1)]
			[DisplayName("Swap Mode")]
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

			[DefaultValue(0)]
			[DisplayName("LOD Calcuation")]
			[Category("More Per Game Settings")]
			public int lodmode { get; set; }

			[DefaultValue(0)]
			[DisplayName("Filtering")]
			[Category("More Per Game Settings")]
			public int filtering { get; set; }

			[DefaultValue(false)]
			[DisplayName("Wrapper Anisotropic Filtering")]
			[Category("General")]
			public bool wrpAnisotropic { get; set; }

			[DefaultValue(false)]
			[DisplayName("Correct Viewport")]
			[Category("Per Game Settings")]
			public bool correct_viewport { get; set; }

			[DefaultValue(false)]
			[DisplayName("Force Calc Sphere")]
			[Category("Per Game Settings")]
			public bool force_calc_sphere { get; set; }

			[DefaultValue(false)]
			[DisplayName("Pal230")]
			[Category("Per Game Settings")]
			public bool pal230 { get; set; }

			[DefaultValue(true)]
			[DisplayName("Texture Correction")]
			[Category("Per Game Settings")]
			public bool texture_correction { get; set; }

			[DefaultValue(false)]
			[DisplayName("N64 Z Scale")]
			[Category("Per Game Settings")]
			public bool n64_z_scale { get; set; }

			[DefaultValue(false)]
			[DisplayName("Old Style Adither")]
			[Category("Per Game Settings")]
			public bool old_style_adither { get; set; }

			[DefaultValue(false)]
			[DisplayName("Z Mode Compare Less")]
			[Category("Per Game Settings")]
			public bool zmode_compare_less { get; set; }

			[DefaultValue(true)]
			[DisplayName("Adjust Aspect")]
			[Category("Per Game Settings")]
			public bool adjust_aspect { get; set; }

			[DefaultValue(true)]
			[DisplayName("Clip Z Max")]
			[Category("Per Game Settings")]
			public bool clip_zmax { get; set; }

			[DefaultValue(false)]
			[DisplayName("Clip Z Min")]
			[Category("Per Game Settings")]
			public bool clip_zmin { get; set; }

			[DefaultValue(false)]
			[DisplayName("Force Quad 3D")]
			[Category("Per Game Settings")]
			public bool force_quad3d { get; set; }

			[DefaultValue(false)]
			[DisplayName("Useless is Useless")]
			[Category("Per Game Settings")]
			public bool useless_is_useless { get; set; }

			[DefaultValue(false)]
			[DisplayName("Framebuffer read every frame")]
			[Category("Per Game Settings")]
			public bool fb_read_always { get; set; }

			[DefaultValue(false)]
			[DisplayName("Get Framebuffer Info")]
			[Category("General")]
			public bool fb_get_info { get; set; }

			[DefaultValue(true)]
			[DisplayName("Framebuffer Render")]
			[Category("General")]
			public bool fb_render { get; set; }

			[DefaultValue(0)]
			[DisplayName("Aspect Mode")]
			[Category("More Per Game Settings")]
			public int aspectmode { get; set; }

			[DefaultValue(1)]
			[DisplayName("Framebuffer CRC Mode")]
			[Category("More Per Game Settings")]
			public int fb_crc_mode { get; set; }

			[DefaultValue(true)]
			[DisplayName("Fast CRC")]
			[Category("Per Game Settings")]
			public bool fast_crc { get; set; }

			[DefaultValue(0)]
			[DisplayName("Use Default Hacks")]
			[Description("Use defaults for current game. This overrides all per game settings.")]
			[Category("Hacks")]
			public bool UseDefaultHacks { get; set; }

			[DefaultValue(0)]
			[DisplayName("Enable Hacks for Game")]
			[Category("More Per Game Settings")]
			public int enable_hacks_for_game { get; set; }

			[DefaultValue(0)]
			[DisplayName("Read Back to Screen")]
			[Category("More Per Game Settings")]
			public int read_back_to_screen { get; set; }

			public N64Glide64mk2PluginSettings Clone()
			{
				return (N64Glide64mk2PluginSettings)MemberwiseClone();
			}

			public void FillPerGameHacks(GameInfo game)
			{
				if (UseDefaultHacks)
				{
					use_sts1_only = game.GetBool("Glide64mk2_use_sts1_only", false);
					optimize_texrect = game.GetBool("Glide64mk2_optimize_texrect", true);
					increase_texrect_edge = game.GetBool("Glide64mk2_increase_texrect_edge", false);
					ignore_aux_copy = game.GetBool("Glide64mk2_ignore_aux_copy", false);
					hires_buf_clear = game.GetBool("Glide64mk2_hires_buf_clear", true);
					force_microcheck = game.GetBool("Glide64mk2_force_microcheck", false);
					fog = game.GetBool("Glide64mk2_fog", true);
					fb_smart = game.GetBool("Glide64mk2_fb_smart", false);
					fb_read_alpha = game.GetBool("Glide64mk2_fb_read_alpha", false);
					fb_hires = game.GetBool("Glide64mk2_fb_hires", true);
					detect_cpu_write = game.GetBool("Glide64mk2_detect_cpu_write", false);
					decrease_fillrect_edge = game.GetBool("Glide64mk2_decrease_fillrect_edge", false);
					buff_clear = game.GetBool("Glide64mk2_buff_clear", true);
					alt_tex_size = game.GetBool("Glide64mk2_alt_tex_size", true);
					swapmode = game.GetInt("Glide64mk2_swapmode", 1);
					stipple_pattern = game.GetInt("Glide64mk2_stipple_pattern", 1041204192);
					stipple_mode = game.GetInt("Glide64mk2_stipple_mode", 2);
					lodmode = game.GetInt("Glide64mk2_lodmode", 0);
					filtering = game.GetInt("Glide64mk2_filtering", 0);
					correct_viewport = game.GetBool("Glide64mk2_correct_viewport", false);
					force_calc_sphere = game.GetBool("Glide64mk2_force_calc_sphere", false);
					pal230 = game.GetBool("Glide64mk2_pal230", false);
					texture_correction = game.GetBool("Glide64mk2_texture_correction", true);
					n64_z_scale = game.GetBool("Glide64mk2_n64_z_scale", false);
					old_style_adither = game.GetBool("Glide64mk2_old_style_adither", false);
					zmode_compare_less = game.GetBool("Glide64mk2_zmode_compare_less", false);
					adjust_aspect = game.GetBool("Glide64mk2_adjust_aspect", true);
					clip_zmax = game.GetBool("Glide64mk2_clip_zmax", true);
					clip_zmin = game.GetBool("Glide64mk2_clip_zmin", false);
					force_quad3d = game.GetBool("Glide64mk2_force_quad3d", false);
					useless_is_useless = game.GetBool("Glide64mk2_useless_is_useless", false);
					fb_read_always = game.GetBool("Glide64mk2_fb_read_always", false);
					aspectmode = game.GetInt("Glide64mk2_aspectmode", 0);
					fb_crc_mode = game.GetInt("Glide64mk2_fb_crc_mode", 1);
					enable_hacks_for_game = game.GetInt("Glide64mk2_enable_hacks_for_game", 0);
					read_back_to_screen = game.GetInt("Glide64mk2_read_back_to_screen", 0);
					fast_crc = game.GetBool("Glide64mk2_fast_crc", true);
				}
			}

			public PluginType GetPluginType()
			{
				return PluginType.GlideMk2;
			}
		}
	}
}