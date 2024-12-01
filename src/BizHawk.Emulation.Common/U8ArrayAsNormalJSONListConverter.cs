using System.Collections.Generic;

using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace BizHawk.Emulation.Common
{
	/// <remarks>seems unnecessary, but suggested by <see href="https://www.newtonsoft.com/json/help/html/Performance.htm#JsonConverters">official docs</see> so sure why not</remarks>
	public sealed class U8ArrayAsNormalJSONListResolver : DefaultContractResolver
	{
		public static readonly U8ArrayAsNormalJSONListResolver INSTANCE = new();

		protected override JsonContract CreateContract(Type objectType)
		{
			var contract = base.CreateContract(objectType);
			if (objectType == typeof(byte[])) contract.Converter = U8ArrayAsNormalJSONListConverter.INSTANCE;
			return contract;
		}
	}

	/// <remarks>based on <see href="https://stackoverflow.com/a/15228384">this SO answer</see></remarks>
	public sealed class U8ArrayAsNormalJSONListConverter : JsonConverter<byte[]?>
	{
		public static readonly U8ArrayAsNormalJSONListConverter INSTANCE = new();

		public override byte[]? ReadJson(JsonReader reader, Type objectType, byte[]? existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			if (reader.TokenType is JsonToken.Null) return null;
			if (reader.TokenType is not JsonToken.StartArray) throw new Exception($"Unexpected token when reading bytes: expected {nameof(JsonToken.StartArray)}, got {reader.TokenType}");
			List<byte> list = new();
			while (reader.Read()) switch (reader.TokenType)
			{
				case JsonToken.Integer:
					list.Add(reader.Value switch
					{
						byte b => b,
						long l and >= byte.MinValue and <= byte.MaxValue => unchecked((byte) l),
						var o => throw new Exception($"Integer literal outside u8 range: {o}")
					});
					continue;
				case JsonToken.EndArray:
					return list.ToArray();
				case JsonToken.Comment:
					continue;
				default:
					throw new Exception($"Unexpected token when reading bytes: {reader.TokenType}");
			}
			throw new Exception("Unexpected end when reading bytes");
		}

		public override void WriteJson(JsonWriter writer, byte[]? value, JsonSerializer serializer)
		{
			if (value is null)
			{
				writer.WriteNull();
				return;
			}
			writer.WriteStartArray();
			for (var i = 0; i < value.Length; i++) writer.WriteValue(value[i]);
			writer.WriteEndArray();
		}
	}
}
