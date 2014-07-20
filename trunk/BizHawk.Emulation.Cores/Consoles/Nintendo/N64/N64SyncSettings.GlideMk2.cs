using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

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

			[JsonIgnore]
			public PluginType PluginType
			{
				get { return PluginType.GLIDE64MK2; }
			}

			public bool wrpFBO { get; set; }
			public int card_id { get; set; }
			public bool use_sts1_only { get; set; }
			public bool optimize_texrect { get; set; }
			public bool increase_texrect_edge { get; set; }
			public bool ignore_aux_copy { get; set; }
			public bool hires_buf_clear { get; set; }
			public bool force_microcheck { get; set; }
			public bool fog { get; set; }
			public bool fb_smart { get; set; }
			public bool fb_read_alpha { get; set; }
			public bool fb_hires { get; set; }
			public bool detect_cpu_write { get; set; }
			public bool decrease_fillrect_edge { get; set; }
			public bool buff_clear { get; set; }
			public bool alt_tex_size { get; set; }
			public int swapmode { get; set; }
			public int stipple_pattern { get; set; }
			public int stipple_mode { get; set; }
			public int lodmode { get; set; }
			public int filtering { get; set; }
			public bool wrpAnisotropic { get; set; }
			public bool correct_viewport { get; set; }
			public bool force_calc_sphere { get; set; }
			public bool pal230 { get; set; }
			public bool texture_correction { get; set; }
			public bool n64_z_scale { get; set; }
			public bool old_style_adither { get; set; }
			public bool zmode_compare_less { get; set; }
			public bool adjust_aspect { get; set; }
			public bool clip_zmax { get; set; }
			public bool clip_zmin { get; set; }
			public bool force_quad3d { get; set; }
			public bool useless_is_useless { get; set; }
			public bool fb_read_always { get; set; }
			public bool fb_get_info { get; set; }
			public bool fb_render { get; set; }
			public int aspectmode { get; set; }
			public int fb_crc_mode { get; set; }
			public bool fast_crc { get; set; }
			public bool UseDefaultHacks { get; set; }
			public int enable_hacks_for_game { get; set; }
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

			public Dictionary<string, object> GetPluginSettings()
			{
				//TODO: deal witn the game depedent settings
				var dictionary = new Dictionary<string, object>();
				var members = this.GetType().GetFields();
				foreach (var member in members)
				{
					var field = this.GetType().GetField(member.Name).GetValue(this);
					dictionary.Add(member.Name, field);
				}

				return dictionary;
			}
		}

	}
}