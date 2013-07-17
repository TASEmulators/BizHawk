using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace BizHawk.MultiClient
{
	public static class ConfigService
	{
		public static T Load<T>(string filepath, T currentConfig) where T : new()
		{
			T config = new T();

			try
			{
				var file = new FileInfo(filepath);
				if (file.Exists)
					using (var reader = file.OpenText())
					{
						var s = new JsonSerializer {SuppressMissingMemberException = true};
						var r = new JsonReader(reader);
						config = (T)s.Deserialize(r, typeof(T));
					}
			}
			catch (Exception e) { MessageBox.Show(e.ToString(), "Config Error"); }
			if (config == null) return new T();

			//patch up arrays in the config with the minimum number of things
			foreach(var fi in typeof(T).GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
				if (fi.FieldType.IsArray)
				{
					Array aold = fi.GetValue(currentConfig) as Array;
					Array anew = fi.GetValue(config) as Array;
					if (aold.Length == anew.Length) continue;

					//create an array of the right size
					Array acreate = Array.CreateInstance(fi.FieldType.GetElementType(), Math.Max(aold.Length,anew.Length));
					
					//copy the old values in, (presumably the defaults), and then copy the new ones on top
					Array.Copy(aold, acreate, Math.Min(aold.Length,acreate.Length));
					Array.Copy(anew, acreate, Math.Min(anew.Length, acreate.Length));
					
					//stash it into the config struct
					fi.SetValue(config, acreate);
				}
					
			return config;
		}

		public static void Save(string filepath, object config)
		{
			var file = new FileInfo(filepath);
			using (var writer = file.CreateText())
			{
				var s = new JsonSerializer();
				var w = new JsonWriter(writer) { Formatting = Formatting.Indented };
				s.Serialize(w, config);
			}
		}
	}
}