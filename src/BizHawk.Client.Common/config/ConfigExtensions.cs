using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public static class ConfigExtensions
	{
		public static object GetCoreSettings(this Config config, Type t)
		{
			config.CoreSettings.TryGetValue(t.ToString(), out var ret);
			return ret;
		}

		public static object GetCoreSettings<T>(this Config config)
			where T : IEmulator
		{
			return config.GetCoreSettings(typeof(T));
		}

		public static void PutCoreSettings(this Config config, object o, Type t)
		{
			if (o != null)
			{
				config.CoreSettings[t.ToString()] = o;
			}
			else
			{
				config.CoreSettings.Remove(t.ToString());
			}
		}

		public static void PutCoreSettings<T>(this Config config, object o)
			where T : IEmulator
		{
			config.PutCoreSettings(o, typeof(T));
		}

		public static object GetCoreSyncSettings<T>(this Config config)
			where T : IEmulator
		{
			return config.GetCoreSyncSettings(typeof(T));
		}

		public static object GetCoreSyncSettings(this Config config, Type t)
		{
			config.CoreSyncSettings.TryGetValue(t.ToString(), out var ret);
			return ret;
		}

		public static void PutCoreSyncSettings(this Config config, object o, Type t)
		{
			if (o != null)
			{
				config.CoreSyncSettings[t.ToString()] = o;
			}
			else
			{
				config.CoreSyncSettings.Remove(t.ToString());
			}
		}

		public static void PutCoreSyncSettings<T>(this Config config, object o)
			where T : IEmulator
		{
			config.PutCoreSyncSettings(o, typeof(T));
		}
	}
}
