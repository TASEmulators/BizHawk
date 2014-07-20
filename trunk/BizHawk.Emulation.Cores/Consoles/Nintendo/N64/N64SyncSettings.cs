using System.Collections.Generic;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64.NativeApi;
using Newtonsoft.Json;
using System.ComponentModel;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public class N64Settings
	{
		public int VideoSizeX = 320;
		public int VideoSizeY = 240;

		public N64Settings Clone()
		{
			return new N64Settings
			{
				VideoSizeX = VideoSizeX,
				VideoSizeY = VideoSizeY,
			};
		}
	}

	public class N64SyncSettings
	{
		public CORETYPE CoreType = CORETYPE.Dynarec;

		public enum CORETYPE
		{
			[Description("Pure Interpreter")]
			Pure_Interpret = 0,

			[Description("Interpreter")]
			Interpret = 1,

			[Description("DynaRec")]
			Dynarec = 2,
		}

		public RSPTYPE RspType = RSPTYPE.Rsp_Hle;

		public enum RSPTYPE
		{
			[Description("Hle")]
			Rsp_Hle = 0,

			[Description("Z64 Hle Video")]
			Rsp_Z64_hlevideo = 1
		}

		public PLUGINTYPE VidPlugin = PLUGINTYPE.RICE;

		public N64ControllerSettings[] Controllers = 
		{
			new N64ControllerSettings(),
			new N64ControllerSettings { IsConnected = false },
			new N64ControllerSettings { IsConnected = false },
			new N64ControllerSettings { IsConnected = false },
		};

		public N64RicePluginSettings RicePlugin = new N64RicePluginSettings();
		public N64GlidePluginSettings GlidePlugin = new N64GlidePluginSettings();
		public N64Glide64mk2PluginSettings Glide64mk2Plugin = new N64Glide64mk2PluginSettings();
		public N64JaboPluginSettings JaboPlugin = new N64JaboPluginSettings();

		public N64SyncSettings Clone()
		{
			return new N64SyncSettings
			{
				CoreType = CoreType,
				RspType = RspType,
				VidPlugin = VidPlugin,
				RicePlugin = RicePlugin.Clone(),
				GlidePlugin = GlidePlugin.Clone(),
				Glide64mk2Plugin = Glide64mk2Plugin.Clone(),
				JaboPlugin = JaboPlugin.Clone(),
				Controllers = System.Array.ConvertAll(Controllers, a => a.Clone())
			};
		}

		// get mupenapi internal object
		public VideoPluginSettings GetVPS(GameInfo game, int videoSizeX, int videoSizeY)
		{
			var ret = new VideoPluginSettings(VidPlugin, videoSizeX, videoSizeY);
			IPluginSettings ips = null;
			switch (VidPlugin)
			{
				// clone so per game hacks don't overwrite our settings object
				case PLUGINTYPE.GLIDE: ips = GlidePlugin.Clone(); break;
				case PLUGINTYPE.GLIDE64MK2: ips = Glide64mk2Plugin.Clone(); break;
				case PLUGINTYPE.RICE: ips = RicePlugin.Clone(); break;
				case PLUGINTYPE.JABO: ips = JaboPlugin.Clone(); break;
			}
			ips.FillPerGameHacks(game);
			ret.Parameters = ips.GetPluginSettings();
			return ret;
		}
	}

	public enum PLUGINTYPE
	{
		[Description("Rice")]
		RICE,

		[Description("Glide64")]
		GLIDE,

		[Description("Glide64 mk2")]
		GLIDE64MK2,

		[Description("Jabo")]
		JABO
	}

	public interface IPluginSettings
	{
		PLUGINTYPE PluginType { get; }
		Dictionary<string, object> GetPluginSettings();
		void FillPerGameHacks(GameInfo game);
	}

	public class N64RicePluginSettings : IPluginSettings
	{
		public N64RicePluginSettings()
		{
			FrameBufferSetting = 0;
			FrameBufferWriteBackControl = 0;
			RenderToTexture = 0;
			ScreenUpdateSetting = 4;
			Mipmapping = 2;
			FogMethod = 0;
			ForceTextureFilter = 0;
			TextureEnhancement = 0;
			TextureEnhancementControl = 0;
			TextureQuality = 0;
			OpenGLDepthBufferSetting = 16;
			MultiSampling = 0;
			ColorQuality = 0;
			OpenGLRenderSetting = 0;
			AnisotropicFiltering = 0;

			NormalAlphaBlender = false;
			FastTextureLoading = false;
			AccurateTextureMapping = true;
			InN64Resolution = false;
			SaveVRAM = false;
			DoubleSizeForSmallTxtrBuf = false;
			DefaultCombinerDisable = false;
			EnableHacks = true;
			WinFrameMode = false;
			FullTMEMEmulation = false;
			OpenGLVertexClipper = false;
			EnableSSE = true;
			EnableVertexShader = false;
			SkipFrame = false;
			TexRectOnly = false;
			SmallTextureOnly = false;
			LoadHiResCRCOnly = true;
			LoadHiResTextures = false;
			DumpTexturesToFiles = false;

			UseDefaultHacks = true;
			DisableTextureCRC = false;
			DisableCulling = false;
			IncTexRectEdge = false;
			ZHack = false;
			TextureScaleHack = false;
			PrimaryDepthHack = false;
			Texture1Hack = false;
			FastLoadTile = false;
			UseSmallerTexture = false;
			VIWidth = -1;
			VIHeight = -1;
			UseCIWidthAndRatio = 0;
			FullTMEM = 0;
			TxtSizeMethod2 = false;
			EnableTxtLOD = false;
			FastTextureCRC = 0;
			EmulateClear = false;
			ForceScreenClear = false;
			AccurateTextureMappingHack = 0;
			NormalBlender = 0;
			DisableBlender = false;
			ForceDepthBuffer = false;
			DisableObjBG = false;
			FrameBufferOption = 0;
			RenderToTextureOption = 0;
			ScreenUpdateSettingHack = 0;
			EnableHacksForGame = 0;
		}

		[JsonIgnore]
		[Description("Plugin Type")]
		public PLUGINTYPE PluginType
		{
			get { return PLUGINTYPE.RICE; }
		}

		public void FillPerGameHacks(GameInfo game)
		{
			if (UseDefaultHacks)
			{
				DisableTextureCRC = game.GetBool("RiceDisableTextureCRC", false);
				DisableCulling = game.GetBool("RiceDisableCulling", false);
				IncTexRectEdge = game.GetBool("RiceIncTexRectEdge", false);
				ZHack = game.GetBool("RiceZHack", false);
				TextureScaleHack = game.GetBool("RiceTextureScaleHack", false);
				PrimaryDepthHack = game.GetBool("RicePrimaryDepthHack", false);
				Texture1Hack = game.GetBool("RiceTexture1Hack", false);
				FastLoadTile = game.GetBool("RiceFastLoadTile", false);
				UseSmallerTexture = game.GetBool("RiceUseSmallerTexture", false);
				VIWidth = game.GetInt("RiceVIWidth", -1);
				VIHeight = game.GetInt("RiceVIHeight", -1);
				UseCIWidthAndRatio = game.GetInt("RiceUseCIWidthAndRatio", 0);
				FullTMEM = game.GetInt("RiceFullTMEM", 0);
				TxtSizeMethod2 = game.GetBool("RiceTxtSizeMethod2", false);
				EnableTxtLOD = game.GetBool("RiceEnableTxtLOD", false);
				FastTextureCRC = game.GetInt("RiceFastTextureCRC", 0);
				EmulateClear = game.GetBool("RiceEmulateClear", false);
				ForceScreenClear = game.GetBool("RiceForceScreenClear", false);
				AccurateTextureMappingHack = game.GetInt("RiceAccurateTextureMappingHack", 0);
				NormalBlender = game.GetInt("RiceNormalBlender", 0);
				DisableBlender = game.GetBool("RiceDisableBlender", false);
				ForceDepthBuffer = game.GetBool("RiceForceDepthBuffer", false);
				DisableObjBG = game.GetBool("RiceDisableObjBG", false);
				FrameBufferOption = game.GetInt("RiceFrameBufferOption", 0);
				RenderToTextureOption = game.GetInt("RiceRenderToTextureOption", 0);
				ScreenUpdateSettingHack = game.GetInt("RiceScreenUpdateSettingHack", 0);
				EnableHacksForGame = game.GetInt("RiceEnableHacksForGame", 0);
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

		public int FrameBufferSetting { get; set; }
		public int FrameBufferWriteBackControl { get; set; }
		public int RenderToTexture { get; set; }
		public int ScreenUpdateSetting { get; set; }
		public int Mipmapping { get; set; }
		public int FogMethod { get; set; }
		public int ForceTextureFilter { get; set; }
		public int TextureEnhancement { get; set; }
		public int TextureEnhancementControl { get; set; }
		public int TextureQuality { get; set; }
		public int OpenGLDepthBufferSetting { get; set; }
		public int MultiSampling { get; set; }
		public int ColorQuality { get; set; }
		public int OpenGLRenderSetting { get; set; }
		public int AnisotropicFiltering { get; set; }

		public bool NormalAlphaBlender { get; set; }
		public bool FastTextureLoading { get; set; }
		public bool AccurateTextureMapping { get; set; }
		public bool InN64Resolution { get; set; }
		public bool SaveVRAM { get; set; }
		public bool DoubleSizeForSmallTxtrBuf { get; set; }
		public bool DefaultCombinerDisable { get; set; }
		public bool EnableHacks { get; set; }
		public bool WinFrameMode { get; set; }
		public bool FullTMEMEmulation { get; set; }
		public bool OpenGLVertexClipper { get; set; }
		public bool EnableSSE { get; set; }
		public bool EnableVertexShader { get; set; }
		public bool SkipFrame { get; set; }
		public bool TexRectOnly { get; set; }
		public bool SmallTextureOnly { get; set; }
		public bool LoadHiResCRCOnly { get; set; }
		public bool LoadHiResTextures { get; set; }
		public bool DumpTexturesToFiles { get; set; }

		public bool UseDefaultHacks { get; set; }
		public bool DisableTextureCRC { get; set; }
		public bool DisableCulling { get; set; }
		public bool IncTexRectEdge { get; set; }
		public bool ZHack { get; set; }
		public bool TextureScaleHack { get; set; }
		public bool PrimaryDepthHack { get; set; }
		public bool Texture1Hack { get; set; }
		public bool FastLoadTile { get; set; }
		public bool UseSmallerTexture { get; set; }
		public int VIWidth { get; set; }
		public int VIHeight { get; set; }
		public int UseCIWidthAndRatio { get; set; }
		public int FullTMEM { get; set; }
		public bool TxtSizeMethod2 { get; set; }
		public bool EnableTxtLOD { get; set; }
		public int FastTextureCRC { get; set; }
		public bool EmulateClear { get; set; }
		public bool ForceScreenClear { get; set; }
		public int AccurateTextureMappingHack { get; set; }
		public int NormalBlender { get; set; }
		public bool DisableBlender { get; set; }
		public bool ForceDepthBuffer { get; set; }
		public bool DisableObjBG { get; set; }
		public int FrameBufferOption { get; set; }
		public int RenderToTextureOption { get; set; }
		public int ScreenUpdateSettingHack { get; set; }
		public int EnableHacksForGame { get; set; }

		public N64RicePluginSettings Clone()
		{
			return (N64RicePluginSettings)MemberwiseClone();
		}
	}

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

		[JsonIgnore]
		public PLUGINTYPE PluginType
		{
			get { return PLUGINTYPE.GLIDE; }
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

		public Dictionary<string, object> GetPluginSettings()
		{
			//TODO: deal witn the game depedent settings
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			System.Reflection.FieldInfo[] members = this.GetType().GetFields();
			foreach (System.Reflection.FieldInfo member in members)
			{
				object field = this.GetType().GetField(member.Name).GetValue(this);
				dictionary.Add(member.Name, field);
			}

			return dictionary;
		}

		public int wfmode { get; set; }
		public bool wireframe { get; set; }
		public int card_id { get; set; }
		public bool flame_corona { get; set; }
		public int ucode { get; set; }
		public bool autodetect_ucode { get; set; }
		public bool motionblur { get; set; }
		public bool fb_read_always { get; set; }
		public bool unk_as_red { get; set; }
		public bool filter_cache { get; set; }
		public bool fast_crc { get; set; }
		public bool disable_auxbuf { get; set; }
		public bool fbo { get; set; }
		public bool noglsl { get; set; }
		public bool noditheredalpha { get; set; }
		public int tex_filter { get; set; }
		public bool fb_render { get; set; }
		public bool wrap_big_tex { get; set; }
		public bool use_sts1_only { get; set; }
		public bool soft_depth_compare { get; set; }
		public bool PPL { get; set; }
		public bool fb_optimize_write { get; set; }
		public bool fb_optimize_texrect { get; set; }
		public bool increase_texrect_edge { get; set; }
		public bool increase_primdepth { get; set; }
		public bool fb_ignore_previous { get; set; }
		public bool fb_ignore_aux_copy { get; set; }
		public bool fb_hires_buf_clear { get; set; }
		public bool force_microcheck { get; set; }
		public bool force_depth_compare { get; set; }
		public bool fog { get; set; }
		public bool fillcolor_fix { get; set; }
		public bool fb_smart { get; set; }
		public bool fb_read_alpha { get; set; }
		public bool fb_get_info { get; set; }
		public bool fb_hires { get; set; }
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
	}

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
		public PLUGINTYPE PluginType
		{
			get { return PLUGINTYPE.GLIDE64MK2; }
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
			Dictionary<string, object> dictionary = new Dictionary<string, object>();
			System.Reflection.FieldInfo[] members = this.GetType().GetFields();
			foreach (System.Reflection.FieldInfo member in members)
			{
				object field = this.GetType().GetField(member.Name).GetValue(this);
				dictionary.Add(member.Name, field);
			}
			return dictionary;
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
	}

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

		[JsonIgnore]
		public PLUGINTYPE PluginType
		{
			get { return PLUGINTYPE.JABO; }
		}

		public void FillPerGameHacks(GameInfo game)
		{

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

	public class N64ControllerSettings
	{
		/// <summary>
		/// Enumeration defining the different controller pak types
		/// for N64
		/// </summary>
		public enum N64ControllerPakType
		{
			[Description("None")]
			NO_PAK = 1,

			[Description("Memory Card")]
			MEMORY_CARD = 2,

			[Description("Rumble Pak")]
			RUMBLE_PAK = 3,

			[Description("Transfer Pak")]
			TRANSFER_PAK = 4
		}

		[JsonIgnore]
		private N64ControllerPakType _type = N64ControllerPakType.NO_PAK;

		/// <summary>
		/// Type of the pak inserted in the controller
		/// Currently only NO_PAK and MEMORY_CARD are
		/// supported. Other values may be set and
		/// are recognized but they have no function
		/// yet. e.g. TRANSFER_PAK makes the N64
		/// recognize a transfer pak inserted in
		/// the controller but there is no
		/// communication to the transfer pak.
		/// </summary>
		public N64ControllerPakType PakType
		{
			get { return _type; }
			set { _type = value; }
		}

		[JsonIgnore]
		private bool _isConnected = true;

		/// <summary>
		/// Connection status of the controller i.e.:
		/// Is the controller plugged into the N64?
		/// </summary>
		public bool IsConnected
		{
			get { return _isConnected; }
			set { _isConnected = value; }
		}

		/// <summary>
		/// Clones this object
		/// </summary>
		/// <returns>New object with the same values</returns>
		public N64ControllerSettings Clone()
		{
			return new N64ControllerSettings
			{
				PakType = PakType,
				IsConnected = IsConnected
			};
		}
	}
}
