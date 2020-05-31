using System.Collections.Generic;
using BizHawk.Emulation.Common;

// ReSharper disable StyleCop.SA1401
namespace BizHawk.Client.Common
{
	public static class Global
	{
		public static Config Config { get; set; }
		public static GameInfo Game { get; set; }
		public static FirmwareManager FirmwareManager { get; set; }
		public static IMovieSession MovieSession { get; set; }
		public static InputManager InputManager { get; } = new InputManager();
		public static Dictionary<string, object> UserBag { get; set; } = new Dictionary<string, object>();
	}
}
