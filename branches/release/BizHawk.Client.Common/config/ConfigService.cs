using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

#pragma warning disable 618

namespace BizHawk.Client.Common
{
	public static class ConfigService
	{
		static JsonSerializer Serializer;

		static ConfigService()
		{
			Serializer = new JsonSerializer
			{
				MissingMemberHandling = MissingMemberHandling.Ignore,
				MissingTypeHandling = MissingTypeHandling.Ignore,
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

		public static T Load<T>(string filepath) where T : new()
		{
			T config = default(T);

			try
			{
				var file = new FileInfo(filepath);
				if (file.Exists)
					using (var reader = file.OpenText())
					{
						var r = new JsonTextReader(reader);
						config = (T)Serializer.Deserialize(r, typeof(T));
					}
			}
			catch (Exception ex)
			{
				throw new InvalidOperationException("Config Error", ex);
			}

			if (config == null)
				return new T();

			return config;
		}

		public static void Save(string filepath, object config)
		{
			var file = new FileInfo(filepath);
			try
			{
				using (var writer = file.CreateText())
				{
					var w = new JsonTextWriter(writer) { Formatting = Formatting.Indented };
					Serializer.Serialize(w, config);
				}
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
			using (TextReader tr = new StringReader(serialized))
			using (JsonTextReader jr = new JsonTextReader(tr))
			{
				TypeNameEncapsulator tne = (TypeNameEncapsulator)Serializer.Deserialize(jr, typeof(TypeNameEncapsulator));
				// in the case of trying to deserialize nothing, tne will be nothing
				// we want to return nothing
				if (tne != null)
					return tne.o;
				else
					return null;
			}
		}

		public static string SaveWithType(object o)
		{
			using (StringWriter sw = new StringWriter())
			using (JsonTextWriter jw = new JsonTextWriter(sw) { Formatting = Formatting.None })
			{
				TypeNameEncapsulator tne = new TypeNameEncapsulator { o = o };
				Serializer.Serialize(jw, tne, typeof(TypeNameEncapsulator));
				sw.Flush();
				return sw.ToString();
			}
		}

	}
}
