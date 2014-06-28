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
			}
			ips.FillPerGameHacks(game);
			ret.Parameters = ips.GetPluginSettings();
			return ret;
		}
	}

	public enum PLUGINTYPE { RICE, GLIDE, GLIDE64MK2 };

	public interface IPluginSettings
	{
		PLUGINTYPE PluginType { get; }
		Dictionary<string, object> GetPluginSettings();
		void FillPerGameHacks(GameInfo game);
	}

	public class N64RicePluginSettings : IPluginSettings
	{
		[JsonIgnore]
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

		public int FrameBufferSetting = 0;
		public int FrameBufferWriteBackControl = 0;
		public int RenderToTexture = 0;
		public int ScreenUpdateSetting = 4;
		public int Mipmapping = 2;
		public int FogMethod = 0;
		public int ForceTextureFilter = 0;
		public int TextureEnhancement = 0;
		public int TextureEnhancementControl = 0;
		public int TextureQuality = 0;
		public int OpenGLDepthBufferSetting = 16;
		public int MultiSampling = 0;
		public int ColorQuality = 0;
		public int OpenGLRenderSetting = 0;
		public int AnisotropicFiltering = 0;

		public bool NormalAlphaBlender = false;
		public bool FastTextureLoading = false;
		public bool AccurateTextureMapping = true;
		public bool InN64Resolution = false;
		public bool SaveVRAM = false;
		public bool DoubleSizeForSmallTxtrBuf = false;
		public bool DefaultCombinerDisable = false;
		public bool EnableHacks = true;
		public bool WinFrameMode = false;
		public bool FullTMEMEmulation = false;
		public bool OpenGLVertexClipper = false;
		public bool EnableSSE = true;
		public bool EnableVertexShader = false;
		public bool SkipFrame = false;
		public bool TexRectOnly = false;
		public bool SmallTextureOnly = false;
		public bool LoadHiResCRCOnly = true;
		public bool LoadHiResTextures = false;
		public bool DumpTexturesToFiles = false;

		public bool UseDefaultHacks = true;
		public bool DisableTextureCRC = false;
		public bool DisableCulling = false;
		public bool IncTexRectEdge = false;
		public bool ZHack = false;
		public bool TextureScaleHack = false;
		public bool PrimaryDepthHack = false;
		public bool Texture1Hack = false;
		public bool FastLoadTile = false;
		public bool UseSmallerTexture = false;
		public int VIWidth = -1;
		public int VIHeight = -1;
		public int UseCIWidthAndRatio = 0;
		public int FullTMEM = 0;
		public bool TxtSizeMethod2 = false;
		public bool EnableTxtLOD = false;
		public int FastTextureCRC = 0;
		public bool EmulateClear = false;
		public bool ForceScreenClear = false;
		public int AccurateTextureMappingHack = 0;
		public int NormalBlender = 0;
		public bool DisableBlender = false;
		public bool ForceDepthBuffer = false;
		public bool DisableObjBG = false;
		public int FrameBufferOption = 0;
		public int RenderToTextureOption = 0;
		public int ScreenUpdateSettingHack = 0;
		public int EnableHacksForGame = 0;

		public N64RicePluginSettings Clone()
		{
			return (N64RicePluginSettings)MemberwiseClone();
		}
	}

	public class N64GlidePluginSettings : IPluginSettings
	{
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

		public int wfmode = 1;
		public bool wireframe = false;
		public int card_id = 0;
		public bool flame_corona = false;
		public int ucode = 2;
		public bool autodetect_ucode = true;
		public bool motionblur = false;
		public bool fb_read_always = false;
		public bool unk_as_red = false;
		public bool filter_cache = false;
		public bool fast_crc = false;
		public bool disable_auxbuf = false;
		public bool fbo = false;
		public bool noglsl = true;
		public bool noditheredalpha = true;
		public int tex_filter = 0;
		public bool fb_render = false;
		public bool wrap_big_tex = false;
		public bool use_sts1_only = false;
		public bool soft_depth_compare = false;
		public bool PPL = false;
		public bool fb_optimize_write = false;
		public bool fb_optimize_texrect = true;
		public bool increase_texrect_edge = false;
		public bool increase_primdepth = false;
		public bool fb_ignore_previous = false;
		public bool fb_ignore_aux_copy = false;
		public bool fb_hires_buf_clear = true;
		public bool force_microcheck = false;
		public bool force_depth_compare = false;
		public bool fog = true;
		public bool fillcolor_fix = false;
		public bool fb_smart = false;
		public bool fb_read_alpha = false;
		public bool fb_get_info = false;
		public bool fb_hires = true;
		public bool fb_clear = false;
		public bool detect_cpu_write = false;
		public bool decrease_fillrect_edge = false;
		public bool buff_clear = true;
		public bool alt_tex_size = false;
		public bool UseDefaultHacks = true;
		public int enable_hacks_for_game = 0;
		public int swapmode = 1;
		public int stipple_pattern = 1041204192;
		public int stipple_mode = 2;
		public int scale_y = 100000;
		public int scale_x = 100000;
		public int offset_y = 0;
		public int offset_x = 0;
		public int lodmode = 0;
		public int fix_tex_coord = 0;
		public int filtering = 1;
		public int depth_bias = 20;

		public N64GlidePluginSettings Clone()
		{
			return (N64GlidePluginSettings)MemberwiseClone();
		}
	}

	public class N64Glide64mk2PluginSettings : IPluginSettings
	{
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

		public bool wrpFBO = true;
		public int card_id = 0;
		public bool use_sts1_only = false;
		public bool optimize_texrect = true;
		public bool increase_texrect_edge = false;
		public bool ignore_aux_copy = false;
		public bool hires_buf_clear = true;
		public bool force_microcheck = false;
		public bool fog = true;
		public bool fb_smart = false;
		public bool fb_read_alpha = false;
		public bool fb_hires = true;
		public bool detect_cpu_write = false;
		public bool decrease_fillrect_edge = false;
		public bool buff_clear = true;
		public bool alt_tex_size = false;
		public int swapmode = 1;
		public int stipple_pattern = 1041204192;
		public int stipple_mode = 2;
		public int lodmode = 0;
		public int filtering = 0;
		public bool wrpAnisotropic = false;
		public bool correct_viewport = false;
		public bool force_calc_sphere = false;
		public bool pal230 = false;
		public bool texture_correction = true;
		public bool n64_z_scale = false;
		public bool old_style_adither = false;
		public bool zmode_compare_less = false;
		public bool adjust_aspect = true;
		public bool clip_zmax = true;
		public bool clip_zmin = false;
		public bool force_quad3d = false;
		public bool useless_is_useless = false;
		public bool fb_read_always = false;
		public bool fb_get_info = false;
		public bool fb_render = true;
		public int aspectmode = 0;
		public int fb_crc_mode = 1;
		public bool fast_crc = true;
		public bool UseDefaultHacks = true;
		public int enable_hacks_for_game = 0;
		public int read_back_to_screen = 0;

		public N64Glide64mk2PluginSettings Clone()
		{
			return (N64Glide64mk2PluginSettings)MemberwiseClone();
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
