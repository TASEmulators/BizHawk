using System;
using BizHawk.Emulation.Common;
using Newtonsoft.Json.Linq;

namespace BizHawk.Client.Common
{
	public static class ConfigExtensions
	{
		private static JToken Serialize(object o)
		{
			return JToken.FromObject(o, ConfigService.Serializer);
		}
		private static object Deserialize(JToken j, Type type)
		{
			try
			{
				return j.ToObject(type, ConfigService.Serializer);
			}
			catch
			{
				// presumably some sort of config mismatch.  Anywhere we can expose this usefully?
				return null;
			}
		}

		/// <summary>
		/// Returns the core settings for a core
		/// </summary>
		/// <param name="config"></param>
		/// <param name="coreType"></param>
		/// <returns>null if no settings were saved, or there was an error deserializing</returns>
		public static object GetCoreSettings(this Config config, Type coreType, Type settingsType)
		{
			config.CoreSettings.TryGetValue(coreType.ToString(), out var j);
			return Deserialize(j, settingsType);
		}

		/// <summary>
		/// Returns the core settings for a core
		/// </summary>
		/// <returns>null if no settings were saved, or there was an error deserializing</returns>
		public static TSetting GetCoreSettings<TCore, TSetting>(this Config config)
			where TCore : IEmulator
		{
			return (TSetting)config.GetCoreSettings(typeof(TCore), typeof(TSetting));
		}

		/// <summary>
		/// saves the core settings for a core
		/// </summary>
		/// <param name="config"></param>
		/// <param name="o">null to remove settings for that core instead</param>
		/// <param name="coreType"></param>
		public static void PutCoreSettings(this Config config, object o, Type coreType)
		{
			if (o != null)
			{
				config.CoreSettings[coreType.ToString()] = Serialize(o);
			}
			else
			{
				config.CoreSettings.Remove(coreType.ToString());
			}
		}

		/// <summary>
		/// saves the core settings for a core
		/// </summary>
		/// <param name="config"></param>
		/// <param name="o">null to remove settings for that core instead</param>
		/// <typeparam name="TCore"></typeparam>
		public static void PutCoreSettings<TCore>(this Config config, object o)
			where TCore : IEmulator
		{
			config.PutCoreSettings(o, typeof(TCore));
		}

		/// <summary>
		/// Returns the core syncsettings for a core
		/// </summary>
		/// <param name="config"></param>
		/// <param name="coreType"></param>
		/// <returns>null if no settings were saved, or there was an error deserializing</returns>
		public static object GetCoreSyncSettings(this Config config, Type coreType, Type syncSettingsType)
		{
			config.CoreSyncSettings.TryGetValue(coreType.ToString(), out var j);
			return Deserialize(j, syncSettingsType);
		}

		/// <summary>
		/// Returns the core syncsettings for a core
		/// </summary>
		/// <returns>null if no settings were saved, or there was an error deserializing</returns>
		public static TSync GetCoreSyncSettings<TCore, TSync>(this Config config)
			where TCore : IEmulator
		{
			return (TSync)config.GetCoreSyncSettings(typeof(TCore), typeof(TSync));
		}

		/// <summary>
		/// saves the core syncsettings for a core
		/// </summary>
		/// <param name="config"></param>
		/// <param name="o">null to remove settings for that core instead</param>
		/// <param name="coreType"></param>
		public static void PutCoreSyncSettings(this Config config, object o, Type coreType)
		{
			if (o != null)
			{
				config.CoreSyncSettings[coreType.ToString()] = Serialize(o);
			}
			else
			{
				config.CoreSyncSettings.Remove(coreType.ToString());
			}
		}

		/// <summary>
		/// saves the core syncsettings for a core
		/// </summary>
		/// <param name="config"></param>
		/// <param name="o">null to remove settings for that core instead</param>
		/// <typeparam name="TCore"></typeparam>
		public static void PutCoreSyncSettings<TCore>(this Config config, object o)
			where TCore : IEmulator
		{
			config.PutCoreSyncSettings(o, typeof(TCore));
		}
	}
}
