using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

using BizHawk.Emulation.Common;
using System.Reflection;

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
			[Description("Plugin Type")]
			public PluginType PluginType
			{
				get { return PluginType.GLIDE; }
			}

			[DefaultValue(1)]
			[DisplayName("Wire Frame Mode")]
			[Description("0=Normal colors, 1=Vertex colors, 2=Red only")]
			public int WFMode { get; set; }

			[DefaultValue(false)]
			[DisplayName("Wire Frame Display")]
			public bool WireFrame { get; set; }

			[DefaultValue(0)]
			[DisplayName("Card ID")]
			public int CardId { get; set; }

			[DefaultValue(false)]
			[DisplayName("Zelda corona fix")]
			public bool FlameCorona { get; set; }

			[DefaultValue(2)]
			[DisplayName("Force microcode")]
			public int UCode { get; set; }

			[DefaultValue(true)]
			[DisplayName("Auto-detect microcode")]
			public bool AutodetectUCode { get; set; }

			[DefaultValue(false)]
			[DisplayName("Motion Blur")]
			public bool MotionBlur { get; set; }

			[DefaultValue(false)]
			[DisplayName("Framebuffer read every frame")]
			public bool FbReadAlways { get; set; }

			[DefaultValue(false)]
			[DisplayName("Display unknown combines as red")]
			public bool UnkAsRed { get; set; }

			[DefaultValue(false)]
			[DisplayName("Filter Cache")]
			public bool FilterCache { get; set; }

			[DefaultValue(false)]
			[DisplayName("Fast CRC")]
			public bool FastCRC { get; set; }

			[DefaultValue(false)]
			[DisplayName("Disable Aux Buffer")]
			public bool DisableAuxBuf { get; set; }

			[DefaultValue(false)]
			[DisplayName("Use framebuffer objects")]
			public bool Fbo { get; set; }

			[DefaultValue(true)]
			[DisplayName("Disable GLSL combiners")]
			public bool NoGlsl { get; set; }

			[DefaultValue(true)]
			[DisplayName("Disable dithered alpha")]
			public bool NoDitheredAlpha { get; set; }

			[DefaultValue(0)]
			[DisplayName("Texture Filter")]
			[Description("0=None, 1=Blur edges, 2=Super 2xSai, 3=Hq2x, 4=Hq4x")]
			public int TexFilter { get; set; }

			[DefaultValue(false)]
			[DisplayName("Framebuffer Render")] 
			public bool FbRender { get; set; }

			[DefaultValue(false)]
			[DisplayName("Wrap textures too big for tmem")]
			public bool WrapBigTex { get; set; }

			[DefaultValue(false)]
			[DisplayName("Use Sts1 Only")]
			public bool UseSts1Only { get; set; }

			[DefaultValue(false)]
			[DisplayName("Soft Depth Compare")]
			public bool SoftDepthCompare { get; set; }

			[DefaultValue(false)]
			[DisplayName("PPL")]
			public bool PPL { get; set; }

			[DefaultValue(false)]
			[DisplayName("Framebuffer Optimize Write")]
			public bool FbOptimizeWrite { get; set; }

			[DefaultValue(true)]
			[DisplayName("Framebuffer Optimize Texture Rectangle")]
			public bool FbOptimizeTexRect { get; set; }

			[DefaultValue(false)]
			[DisplayName("Increase Texture Rectangle Edge")]
			public bool IncreaseTexRectEdge { get; set; }

			[DefaultValue(false)]
			[DisplayName("Increase Prim Depth")]
			public bool IncreasePrimDepth { get; set; }

			[DefaultValue(false)]
			[DisplayName("Framebuffer Ignore Previous")]
			public bool FbIgnorePrevious { get; set; }

			[DefaultValue(false)]
			[DisplayName("Framebuffer Ignore Aux Copy")]
			public bool FbIgnoreAuxCopy { get; set; }

			[DefaultValue(true)]
			[DisplayName("Framebuffer High Resolution Buffer Clear")]
			public bool FbHiResBufClear { get; set; }

			[DefaultValue(false)]
			[DisplayName("Force Mirco Check")]
			public bool ForceMicroCheck { get; set; }

			[DefaultValue(false)]
			[DisplayName("Force Depth Compare")]
			public bool ForceDepthCompare { get; set; }

			[DefaultValue(true)]
			[DisplayName("Fog Enabled")]
			public bool Fog { get; set; }

			[DefaultValue(false)]
			[DisplayName("Fill Color Fix")]
			public bool FillColorFix { get; set; }

			[DefaultValue(false)]
			[DisplayName("Smart Framebuffer")]
			public bool FbSmart { get; set; }

			[DefaultValue(false)]
			[DisplayName("Framebuffer Read Alpha")]
			public bool FbReadAlpha { get; set; }

			[DefaultValue(false)]
			[DisplayName("Get Framebuffer Info")]
			public bool FbGetInfo { get; set; }

			[DefaultValue(true)]
			[DisplayName("High Res Framebuffer")]
			public bool FbHiRes { get; set; }

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