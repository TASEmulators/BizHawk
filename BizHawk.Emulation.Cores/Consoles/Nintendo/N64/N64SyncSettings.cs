using System.Collections.Generic;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.N64.NativeApi;
using Newtonsoft.Json;
using System.ComponentModel;

namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public partial class N64SyncSettings
	{
		public CoreType Core = CoreType.Dynarec;

		public enum CoreType
		{
			[Description("Pure Interpreter")]
			Pure_Interpret = 0,

			[Description("Interpreter")]
			Interpret = 1,

			[Description("DynaRec")]
			Dynarec = 2,
		}

		public enum RspType
		{
			[Description("Hle")]
			Rsp_Hle = 0,

			[Description("Z64 Hle Video")]
			Rsp_Z64_hlevideo = 1
		}

		public PluginType VidPlugin = PluginType.Rice;
		public RspType Rsp = RspType.Rsp_Hle;

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
				Core = Core,
				Rsp = Rsp,
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
				case PluginType.Glide: ips = GlidePlugin.Clone(); break;
				case PluginType.GlideMk2: ips = Glide64mk2Plugin.Clone(); break;
				case PluginType.Rice: ips = RicePlugin.Clone(); break;
				case PluginType.Jabo: ips = JaboPlugin.Clone(); break;
			}
			ips.FillPerGameHacks(game);
			ret.Parameters = ips.GetPluginSettings();
			return ret;
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

		[Description("Jabo")]
		Jabo
	}

	public interface IPluginSettings
	{
		PluginType GetPluginType();
		Dictionary<string, object> GetPluginSettings();
		void FillPerGameHacks(GameInfo game);
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
