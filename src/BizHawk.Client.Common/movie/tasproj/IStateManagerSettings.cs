using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BizHawk.Client.Common
{
	[JsonConverter(typeof(StateManagerSettingsConverter))]
	public interface IStateManagerSettings
	{
		IStateManager CreateManager(Func<int, bool> reserveCallback);

		IStateManagerSettings Clone();
	}

	// Legacy deserialization support. It's ugly, but Newtonsoft is dumb like that.
	// All classes that implement IStateManager MUST use NoConverter as their converter or face infinite recursion.
	public class StateManagerSettingsConverter : JsonConverter<IStateManagerSettings>
	{
		public override IStateManagerSettings ReadJson(JsonReader reader, Type objectType, IStateManagerSettings existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			var jsonObject = Newtonsoft.Json.Linq.JObject.Load(reader);
			if (jsonObject.TryGetValue("$type", out JToken value))
			{
				string type = (string)value;
				if (type.Contains(nameof(PagedStateManager.PagedSettings)))
					return jsonObject.ToObject<PagedStateManager.PagedSettings>(serializer);
				else if (type.Contains(nameof(ZwinderStateManagerSettings)))
					return jsonObject.ToObject<ZwinderStateManagerSettings>(serializer);
				else
					throw new Exception("Invalid state manager type."); // This will get eaten.
			}
			else
			{
				if (reader.Path.Contains("DefaultTasStateManagerSettings"))
				{
					// We want to default to the new manager.
					// Users will only keep the zwinder manager as their default if they specify it after we've added the paged manager.
					return new PagedStateManager.PagedSettings();
				}
				else
				{
					// But inside a movie file, keep existing settings.
					return jsonObject.ToObject<ZwinderStateManagerSettings>(serializer);
				}
			}
		}

		public override bool CanWrite => false;

		public override void WriteJson(JsonWriter writer, IStateManagerSettings value, JsonSerializer serializer) => throw new NotImplementedException();
	}

	// Newtonsoft makes classes inherit converters from implemented interfaces. Using this converter undoes that.
	public class NoConverter : JsonConverter
	{
		public override bool CanConvert(Type objectType) => false;
		public override bool CanRead => false;
		public override bool CanWrite => false;

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer) => throw new NotImplementedException();

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer) => throw new NotImplementedException();
	}
}
