using System.IO;
using System.Reflection;

using BizHawk.Common;

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

#pragma warning disable 618

namespace BizHawk.Client.Common
{
	public static class ConfigService
	{
		internal static readonly JsonSerializer Serializer;

		static ConfigService()
		{
			Serializer = new JsonSerializer
			{
				MissingMemberHandling = MissingMemberHandling.Ignore,
				TypeNameHandling = TypeNameHandling.Auto,
				ConstructorHandling = ConstructorHandling.Default,

				// because of the peculiar setup of Binding.cs and PathEntry.cs
				ObjectCreationHandling = ObjectCreationHandling.Replace,
				
				ContractResolver = new DefaultContractResolver
				{
					DefaultMembersSearchFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic
				},
			};
		}

		public static bool IsFromSameVersion(string filepath, out string msg)
		{
			const string MSGFMT_NEWER = "Your config file ({0}) is from a newer version of EmuHawk, {2} (this is {1}). It may fail to load.";
			const string MSGFMT_OLDER = "Your config file ({0}) is from an older version of EmuHawk, {2} (this is {1}). It may fail to load.";
			const string MSGFMT_PRE_2_3_3 = "Your config file ({0}) is corrupted, or is from an older version of EmuHawk, predating 2.3.3 (this is {1}). It may fail to load.";
			const string MSGFMT_PRE_2_5 = "Your config file ({0}) is corrupted, or is from an older version of EmuHawk, predating 2.5 (this is {1}). It may fail to load.";

			if (!File.Exists(filepath))
			{
				msg = null;
				return true;
			}
			string cfgVersionStr = null;
			try
			{
				cfgVersionStr = JObject.Parse(File.ReadAllText(filepath))["LastWrittenFrom"]?.Value<string>();
			}
			catch (Exception)
			{
				// ignored
			}
			if (cfgVersionStr == VersionInfo.MainVersion)
			{
				msg = null;
				return true;
			}
			string fmt;
			if (cfgVersionStr == null)
			{
				fmt = MSGFMT_PRE_2_3_3;
			}
			else
			{
				var cfgVersion = VersionInfo.VersionStrToInt(cfgVersionStr);
				if (cfgVersion < 0x02050000U)
				{
					fmt = MSGFMT_PRE_2_5;
				}
				else
				{
					var thisVersion = VersionInfo.VersionStrToInt(VersionInfo.MainVersion);
					fmt = cfgVersion < thisVersion ? MSGFMT_OLDER : MSGFMT_NEWER;
				}
			}
			msg = string.Format(fmt, Path.GetFileName(filepath), VersionInfo.MainVersion, cfgVersionStr);
			return false;
		}

		/// <exception cref="InvalidOperationException">internal error</exception>
		public static T Load<T>(string filepath) where T : new()
		{
			T config = default(T);

			try
			{
				var file = new FileInfo(filepath);
				if (file.Exists)
				{
					using var reader = file.OpenText();
					var r = new JsonTextReader(reader);
					config = (T)Serializer.Deserialize(r, typeof(T));
				}
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Config Error", ex);
			}

			return config ?? new T();
		}

		public static void Save(string filepath, object config)
		{
			var file = new FileInfo(filepath);
			try
			{
				using var writer = file.CreateText();
				var w = new JsonTextWriter(writer) { Formatting = Formatting.Indented };
				Serializer.Serialize(w, config);
			}
			catch
			{
				/* Eat it */
			}
		}

		// movie 1.0 header stuff
		private class TypeNameEncapsulator
		{
			public object o;
		}

		public static object LoadWithType(string serialized)
		{
			using var tr = new StringReader(serialized);
			using var jr = new JsonTextReader(tr);
			var tne = (TypeNameEncapsulator)Serializer.Deserialize(jr, typeof(TypeNameEncapsulator));

			// in the case of trying to deserialize nothing, tne will be nothing
			// we want to return nothing
			return tne?.o;
		}

		public static string SaveWithType(object o)
		{
			using var sw = new StringWriter();
			using var jw = new JsonTextWriter(sw) { Formatting = Formatting.None };
			var tne = new TypeNameEncapsulator { o = o };
			Serializer.Serialize(jw, tne, typeof(TypeNameEncapsulator));
			sw.Flush();
			return sw.ToString();
		}
	}
}
