using System;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

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
				config = default(T);
				//throw new InvalidOperationException("Config Error", ex);
				//Commented out because I'm not sure why you'd want an exception here. 
				//This happens when the application loads, so they'd never be able to run it if their config was corrupt.
			}

			if (config == null)
				return new T();

			return config;
		}

		public static void Save(string filepath, object config)
		{
			var file = new FileInfo(filepath);
			using (var writer = file.CreateText())
			{
				var w = new JsonTextWriter(writer) { Formatting = Formatting.Indented };
				Serializer.Serialize(w, config);
			}
		}
	}
}
