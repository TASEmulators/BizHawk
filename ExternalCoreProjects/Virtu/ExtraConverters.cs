using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json.Linq;
using System.Reflection;

namespace Jellyfish.Virtu
{
	public class ArrayConverter : JsonConverter
	{
		// JSON.NET cannot, when reading, use PreserveReferencesHandling on arrays, although it fully supports it on writing.
		// stupid decision, but there you have it.  we need that to work here.

		// as an added "bonus", this disables base64ing of byte[] arrays.
		// TODO: optimize the byte/short/int cases.

		// TODO: on serialization, the type of the object is available, but is the expected type (ie, the one that we'll be fed during deserialization) available?
		// need this to at least detect covariance cases...

		public override bool CanConvert(Type objectType)
		{
			if (!typeof(Array).IsAssignableFrom(objectType))
				return false;

			if (objectType.GetArrayRank() > 1)
				throw new NotImplementedException();

			return true;
		}

		public override bool CanRead { get { return true; } }
		public override bool CanWrite { get { return true; } }

		int nextRef = 1;
		Dictionary<object, int> refs = new Dictionary<object, int>();
		Dictionary<int, Array> readrefs = new Dictionary<int, Array>();

		private static void ReadExpectType(JsonReader reader, JsonToken expected)
		{
			if (!reader.Read())
				throw new InvalidOperationException();
			if (reader.TokenType != expected)
				throw new InvalidOperationException();
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
				return null;
			if (reader.TokenType != JsonToken.StartObject)
				throw new InvalidOperationException();

			ReadExpectType(reader, JsonToken.PropertyName);
			if (reader.Value.ToString() != "$myRef")
				throw new InvalidOperationException();
			ReadExpectType(reader, JsonToken.Integer);
			int myRef = Convert.ToInt32(reader.Value);

			if (!reader.Read())
				throw new InvalidOperationException();
			if (reader.TokenType == JsonToken.EndObject)
				return readrefs[myRef];
			else if (reader.TokenType != JsonToken.PropertyName || reader.Value.ToString() != "$myCount")
				throw new InvalidOperationException();
			ReadExpectType(reader, JsonToken.Integer);
			int myCount = Convert.ToInt32(reader.Value);

			ReadExpectType(reader, JsonToken.PropertyName);
			if (reader.Value.ToString() != "$myVals")
				throw new InvalidOperationException();
			ReadExpectType(reader, JsonToken.StartArray);
			var elementType = objectType.GetElementType();
			Array ret = Array.CreateInstance(elementType, myCount);
			for (int i = 0; i < myCount; i++)
			{
				if (!reader.Read())
					throw new InvalidOperationException();
				ret.SetValue(serializer.Deserialize(reader, elementType), i);
			}
			ReadExpectType(reader, JsonToken.EndArray);

			ReadExpectType(reader, JsonToken.EndObject);
			readrefs[myRef] = ret;
			return ret;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			int myRef;
			if (refs.TryGetValue(value, out myRef))
			{
				writer.WriteStartObject();
				writer.WritePropertyName("$myRef");
				writer.WriteValue(myRef);
				writer.WriteEndObject();
			}
			else
			{
				myRef = nextRef++;
				refs.Add(value, myRef);
				writer.WriteStartObject();
				writer.WritePropertyName("$myRef");
				writer.WriteValue(myRef);
				writer.WritePropertyName("$myCount"); // not needed, but avoids us having to make some sort of temp structure on deserialization
				writer.WriteValue(((Array)value).Length);
				writer.WritePropertyName("$myVals");
				writer.WriteStartArray();
				var elementType = value.GetType().GetElementType();
				foreach (object o in (Array)value)
				{
					serializer.Serialize(writer, o, elementType);
				}
				writer.WriteEndArray();
				writer.WriteEndObject();
			}
		}
	}

	public class DelegateConverter : JsonConverter
	{
		// caveats:  if used on anonymous delegates and/or closures, brittle to name changes in the generated classes and methods
		// brittle to type name changes in general
		// must be serialized in tree with any real classes referred to by closures

		// TODO: much of this could be made somewhat smarter and more resilient

		public override bool CanConvert(Type objectType)
		{
			return typeof(Delegate).IsAssignableFrom(objectType);
		}

		public override bool CanRead { get { return true; } }
		public override bool CanWrite { get { return true; } }

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var slug = serializer.Deserialize<Slug>(reader);
			if (slug == null)
				return null;
			return slug.GetDelegate();
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var slug = new Slug((Delegate)value);
			serializer.Serialize(writer, slug);
		}

		private class Slug
		{
			public Type DelegateType;
			public Type MethodDeclaringType;
			public string MethodName;
			public List<Type> MethodParameters;
			public object Target;

			public Delegate GetDelegate()
			{
				var mi = MethodDeclaringType.GetMethod(
					MethodName,
					BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
					null,
					MethodParameters.ToArray(),
					null);

				return Delegate.CreateDelegate(DelegateType, Target, mi);
			}

			public Slug() { }

			public Slug(Delegate d)
			{
				DelegateType = d.GetType();
				MethodDeclaringType = d.Method.DeclaringType;
				MethodName = d.Method.Name;
				MethodParameters = d.Method.GetParameters().Select(p => p.ParameterType).ToList();
				Target = d.Target;
			}
		}
	}
}
