using System;
using BizHawk.Emulation.Common;
using Newtonsoft.Json.Linq;

namespace BizHawk.Client.Common
{
	public static class ConfigExtensions
	{
		private class TypeNameEncapsulator
		{
			public object o;
		}
		private static JToken Serialize(object o)
		{
			var tne = new TypeNameEncapsulator { o = o };
			return JToken.FromObject(tne, ConfigService.Serializer)["o"];
		}
		private static object Deserialize(JToken j)
		{
			var jne = new JObject(new JProperty("o", j));
			try
			{
				return jne.ToObject<TypeNameEncapsulator>(ConfigService.Serializer).o;
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
		public static object GetCoreSettings(this Config config, Type coreType)
		{
			config.CoreSettings.TryGetValue(coreType.ToString(), out var j);
			return Deserialize(j);
		}

		/// <summary>
		/// Returns the core settings for a core
		/// </summary>
		/// <param name="config"></param>
		/// <typeparam name="TCore"></typeparam>
		/// <returns>null if no settings were saved, or there was an error deserializing</returns>
		public static object GetCoreSettings<TCore>(this Config config)
			where TCore : IEmulator
		{
			return config.GetCoreSettings(typeof(TCore));
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
		public static object GetCoreSyncSettings(this Config config, Type coreType)
		{
			config.CoreSyncSettings.TryGetValue(coreType.ToString(), out var j);
			return Deserialize(j);
		}

		/// <summary>
		/// Returns the core syncsettings for a core
		/// </summary>
		/// <param name="config"></param>
		/// <typeparam name="TCore"></typeparam>
		/// <returns>null if no settings were saved, or there was an error deserializing</returns>
		public static object GetCoreSyncSettings<TCore>(this Config config)
			where TCore : IEmulator
		{
			return config.GetCoreSyncSettings(typeof(TCore));
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
