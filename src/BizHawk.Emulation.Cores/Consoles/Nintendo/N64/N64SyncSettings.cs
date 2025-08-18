using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64.NativeApi;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64SyncSettings
	{
		public N64SyncSettings()
		{
			VideoPlugin = PluginType.GLideN64;
			Core = CoreType.Interpret;
			Rsp = RspType.Rsp_Hle;
			DisableExpansionSlot = true;

			Controllers = new[]
			{
				new N64ControllerSettings(),
				new N64ControllerSettings { IsConnected = false },
				new N64ControllerSettings { IsConnected = false },
				new N64ControllerSettings { IsConnected = false },
			};

			RicePlugin = new N64RicePluginSettings();
			GlidePlugin = new N64GlidePluginSettings();
			Glide64mk2Plugin = new N64Glide64mk2PluginSettings();
			GLideN64Plugin = new N64GLideN64PluginSettings();
			AngrylionPlugin = new N64AngrylionPluginSettings();
		}

		public CoreType Core { get; set; }
		public RspType Rsp { get; set; }
		public PluginType VideoPlugin  { get; set; }

		public bool DisableExpansionSlot { get; set; }

		public N64ControllerSettings[] Controllers { get; private set; }

		public N64RicePluginSettings RicePlugin { get; private set; }
		public N64GlidePluginSettings GlidePlugin { get; private set; }
		public N64Glide64mk2PluginSettings Glide64mk2Plugin { get; private set; }
		public N64GLideN64PluginSettings GLideN64Plugin { get; private set; }
		public N64AngrylionPluginSettings AngrylionPlugin { get; private set; }

		public N64SyncSettings Clone()
		{
			return new N64SyncSettings
			{
				Core = Core,
				Rsp = Rsp,
				VideoPlugin = VideoPlugin,
				DisableExpansionSlot = DisableExpansionSlot,
				RicePlugin = RicePlugin.Clone(),
				GlidePlugin = GlidePlugin.Clone(),
				Glide64mk2Plugin = Glide64mk2Plugin.Clone(),
				GLideN64Plugin = GLideN64Plugin.Clone(),
				AngrylionPlugin = AngrylionPlugin.Clone(),
				Controllers = System.Array.ConvertAll(Controllers, a => a.Clone())
			};
		}

		// get mupenapi internal object
		public VideoPluginSettings GetVPS(GameInfo game, int videoSizeX, int videoSizeY)
		{
			var ret = new VideoPluginSettings(VideoPlugin, videoSizeX, videoSizeY);
			IPluginSettings ips = null;
			switch (VideoPlugin)
			{
				// clone so per game hacks don't overwrite our settings object
				case PluginType.Glide: ips = GlidePlugin.Clone(); break;
				case PluginType.GlideMk2: ips = Glide64mk2Plugin.Clone(); break;
				case PluginType.Rice: ips = RicePlugin.Clone(); break;
				case PluginType.GLideN64: ips = GLideN64Plugin.Clone(); break;
				case PluginType.Angrylion: ips = AngrylionPlugin.Clone(); break;
			}

			ips.FillPerGameHacks(game);
			ret.Parameters = ips.GetPluginSettings();
			return ret;
		}

		public enum CoreType
		{
			[Description("Pure Interpreter")]
			Pure_Interpret = 0,

			[Description("Interpreter")]
			Interpret = 1,

			//[Description("DynaRec")]
			//Dynarec = 2,
		}

		public enum RspType
		{
			[Description("Hle")]
			Rsp_Hle = 0,

			[Description("cxd4 LLE")]
			Rsp_cxd4 = 2
		}
	}

	public enum PluginType
	{
		[Description("Rice")]
		Rice,

		[Description("Glide64")]
		Glide,

		[Description("Glide64 mk2")]
		GlideMk2,

		//this is left to keep some enums from getting mixed up
		[Description("FormerlyJabo")]
		FormerlyJabo,

		[Description("GLideN64")]
		GLideN64,

		[Description("Angrylion")]
		Angrylion,
	}

	public interface IPluginSettings
	{
		PluginType GetPluginType();
		void FillPerGameHacks(GameInfo game);
	}

	public static class PluginExtensions
	{
		public static Dictionary<string, object> GetPluginSettings(this IPluginSettings plugin)
		{
			// TODO: deal witn the game depedent settings
			var dictionary = new Dictionary<string, object>();
			var members = plugin.GetType().GetMembers();
			foreach (var member in members)
			{
				if (member.MemberType == MemberTypes.Property)
				{
					var field = plugin.GetType().GetProperty(member.Name).GetValue(plugin, null);
					dictionary.Add(member.Name, field);
				}
			}

			return dictionary;
		}
	}
}
