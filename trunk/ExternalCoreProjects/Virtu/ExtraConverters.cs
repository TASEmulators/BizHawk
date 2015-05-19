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

		// TODO: on serialization, the type of the object is available, but is the expected type (ie, the one that we'll be fed during deserialization) available?
		// need this to at least detect covariance cases...

		private object ReadInternal(JsonReader reader, Type objectType, JsonSerializer serializer)
		{
			Type elementType = objectType.GetElementType();
			if (elementType.IsPrimitive)
			{
				if (!reader.Read())
					throw new InvalidOperationException();
				return bareserializer.Deserialize(reader, objectType);
			}
			else
			{
				int cap = 16;
				Array ret = Array.CreateInstance(elementType, cap);
				int used = 0;

				ReadExpectType(reader, JsonToken.StartArray);

				while (true)
				{
					if (!reader.Read())
						throw new InvalidOperationException();
					if (reader.TokenType == JsonToken.EndArray)
						break;
					ret.SetValue(serializer.Deserialize(reader, elementType), used++);
					if (used == cap)
					{
						cap *= 2;
						Array tmp = Array.CreateInstance(elementType, cap);
						Array.Copy(ret, tmp, used);
						ret = tmp;
					}
				}
				if (used != cap)
				{
					Array tmp = Array.CreateInstance(elementType, used);
					Array.Copy(ret, tmp, used);
					ret = tmp;
				}
				return ret;
			}
		}

		private void WriteInternal(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var elementType = value.GetType().GetElementType();
			if (elementType.IsPrimitive)
			{
				bareserializer.Serialize(writer, value);
			}
			else
			{
				writer.WriteStartArray();
				foreach (object o in (Array)value)
				{
					serializer.Serialize(writer, o, elementType);
				}
				writer.WriteEndArray();
			}
		}

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

		private JsonSerializer bareserializer = new JsonSerializer(); // full default settings, separate context

		private static void ReadExpectType(JsonReader reader, JsonToken expected)
		{
			if (!reader.Read())
				throw new InvalidOperationException();
			if (reader.TokenType != expected)
				throw new InvalidOperationException();
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			object ret;
			if (reader.TokenType == JsonToken.Null)
				return null;
			else if (reader.TokenType != JsonToken.StartObject)
				throw new InvalidOperationException();

			ReadExpectType(reader, JsonToken.PropertyName);
			string prop = reader.Value.ToString();
			ReadExpectType(reader, JsonToken.String);
			string val = reader.Value.ToString();
			if (prop == "$ref")
			{
				ret = serializer.ReferenceResolver.ResolveReference(serializer, val);
				ReadExpectType(reader, JsonToken.EndObject);
			}
			else if (prop == "$id")
			{
				ReadExpectType(reader, JsonToken.PropertyName);
				if (reader.Value.ToString() != "$values")
					throw new InvalidOperationException();
				ret = ReadInternal(reader, objectType, serializer);
				ReadExpectType(reader, JsonToken.EndObject);
				serializer.ReferenceResolver.AddReference(serializer, val, ret);
			}
			else
			{
				throw new InvalidOperationException();
			}
			return ret;
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			if (serializer.ReferenceResolver.IsReferenced(serializer, value))
			{
				writer.WriteStartObject();
				writer.WritePropertyName("$ref");
				writer.WriteValue(serializer.ReferenceResolver.GetReference(serializer, value));
				writer.WriteEndObject();
			}
			else
			{
				writer.WriteStartObject();
				writer.WritePropertyName("$id");
				writer.WriteValue(serializer.ReferenceResolver.GetReference(serializer, value));
				writer.WritePropertyName("$values");
				WriteInternal(writer, value, serializer);
				writer.WriteEndObject();
			}
		}
	}

	public class DelegateConverter : JsonConverter
	{
		// caveats:  if used on anonymous delegates and/or closures, brittle to name changes in the generated classes and methods
		// brittle to type name changes in general
		// must be serialized in tree with any real classes referred to by closures
		
		// CAN NOT preserve reference equality of the delegates themselves, because the delegate must be created with
		// target in one shot, with no possibility to change the target later.  We preserve references to targets,
		// and lose the ability to preserve references to delegates.
		

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
