using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64SyncSettings
	{
		public class N64GlidePluginSettings : IPluginSettings
		{
			public N64GlidePluginSettings()
			{
				WFMode = 1;
				WireFrame = false;
				CardId = 0;
				FlameCorona = false;
				UCode = 2;
				AutodetectUCode = true;
				MotionBlur = false;
				FbReadAlways = false;
				UnkAsRed = false;
				FilterCache = false;
				FastCRC = false;
				DisableAuxBuf = false;
				Fbo = false;
				NoGlsl = true;
				NoDitheredAlpha = true;
				TexFilter = 0;
				FbRender = false;
				WrapBigTex = false;
				UseSts1Only = false;
				SoftDepthCompare = false;
				PPL = false;
				FbOptimizeWrite = false;
				FbOptimizeTexRect = true;
				IncreaseTexRectEdge = false;
				IncreasePrimDepth = false;
				FbIgnorePrevious = false;
				FbIgnoreAuxCopy = false;
				FbHiResBufClear = true;
				ForceMicroCheck = false;
				ForceDepthCompare = false;
				Fog = true;
				FillColorFix = false;
				FbSmart = false;
				FbReadAlpha = false;
				FbGetInfo = false;
				FbHiRes = true;
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

			[JsonIgnore]
			public PluginType PluginType
			{
				get { return PluginType.GLIDE; }
			}

			public int WFMode { get; set; }
			public bool WireFrame { get; set; }
			public int CardId { get; set; }
			public bool FlameCorona { get; set; }
			public int UCode { get; set; }
			public bool AutodetectUCode { get; set; }
			public bool MotionBlur { get; set; }
			public bool FbReadAlways { get; set; }
			public bool UnkAsRed { get; set; }
			public bool FilterCache { get; set; }
			public bool FastCRC { get; set; }
			public bool DisableAuxBuf { get; set; }
			public bool Fbo { get; set; }
			public bool NoGlsl { get; set; }
			public bool NoDitheredAlpha { get; set; }
			public int TexFilter { get; set; }
			public bool FbRender { get; set; }
			public bool WrapBigTex { get; set; }
			public bool UseSts1Only { get; set; }
			public bool SoftDepthCompare { get; set; }
			public bool PPL { get; set; }
			public bool FbOptimizeWrite { get; set; }
			public bool FbOptimizeTexRect { get; set; }
			public bool IncreaseTexRectEdge { get; set; }
			public bool IncreasePrimDepth { get; set; }
			public bool FbIgnorePrevious { get; set; }
			public bool FbIgnoreAuxCopy { get; set; }
			public bool FbHiResBufClear { get; set; }
			public bool ForceMicroCheck { get; set; }
			public bool ForceDepthCompare { get; set; }
			public bool Fog { get; set; }
			public bool FillColorFix { get; set; }
			public bool FbSmart { get; set; }
			public bool FbReadAlpha { get; set; }
			public bool FbGetInfo { get; set; }
			public bool FbHiRes { get; set; }
			public bool fb_clear { get; set; }
			public bool detect_cpu_write { get; set; }
			public bool decrease_fillrect_edge { get; set; }
			public bool buff_clear { get; set; }
			public bool alt_tex_size { get; set; }
			public bool UseDefaultHacks { get; set; }
			public int enable_hacks_for_game { get; set; }
			public int swapmode { get; set; }
			public int stipple_pattern { get; set; }
			public int stipple_mode { get; set; }
			public int scale_y { get; set; }
			public int scale_x { get; set; }
			public int offset_y { get; set; }
			public int offset_x { get; set; }
			public int lodmode { get; set; }
			public int fix_tex_coord { get; set; }
			public int filtering { get; set; }
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
					FbHiRes = game.GetBool("Glide_fb_clear", true);
					FbReadAlpha = game.GetBool("Glide_fb_read_alpha", false);
					FbSmart = game.GetBool("Glide_fb_smart", false);
					FillColorFix = game.GetBool("Glide_fillcolor_fix", false);
					Fog = game.GetBool("Glide_fog", true);
					ForceDepthCompare = game.GetBool("Glide_force_depth_compare", false);
					ForceMicroCheck = game.GetBool("Glide_force_microcheck", false);
					FbHiResBufClear = game.GetBool("Glide_fb_hires_buf_clear", true);
					FbIgnoreAuxCopy = game.GetBool("Glide_fb_ignore_aux_copy", false);
					FbIgnorePrevious = game.GetBool("Glide_fb_ignore_previous", false);
					IncreasePrimDepth = game.GetBool("Glide_increase_primdepth", false);
					IncreaseTexRectEdge = game.GetBool("Glide_increase_texrect_edge", false);
					FbOptimizeTexRect = game.GetBool("Glide_fb_optimize_texrect", true);
					FbOptimizeWrite = game.GetBool("Glide_fb_optimize_write", false);
					PPL = game.GetBool("Glide_PPL", false);
					SoftDepthCompare = game.GetBool("Glide_soft_depth_compare", false);
					UseSts1Only = game.GetBool("Glide_use_sts1_only", false);
					WrapBigTex = game.GetBool("Glide_wrap_big_tex", false);

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