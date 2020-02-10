using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using System.Reflection;

namespace Virtu.Library
{
	public class ArrayConverter : JsonConverter
	{
		// JSON.NET cannot, when reading, use PreserveReferencesHandling on arrays, although it fully supports it on writing.
		// Doing so while being able to fully preserve circular references would require storing the length of the array,
		// or reading ahead in the JSON to compute the length.  For arrays that could contain reference types, we choose the latter.
		// For arrays of primitive types, there is no issue. 

		// TODO: on serialization, the type of the object is available, but is the expected type (ie, the one that we'll be fed during deserialization) available?
		// need this to at least detect covariance cases...

		public override bool CanConvert(Type objectType)
		{
			if (!typeof(Array).IsAssignableFrom(objectType))
			{
				return false;
			}

			if (objectType.GetArrayRank() > 1)
			{
				throw new NotImplementedException();
			}

			return true;
		}

		public override bool CanRead => true;
		public override bool CanWrite => true;

		private readonly JsonSerializer _bareSerializer = new JsonSerializer(); // full default settings, separate context

		// ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
		private static void ReadExpectType(JsonReader reader, JsonToken expected)
		{
			if (!reader.Read())
			{
				throw new InvalidOperationException();
			}

			if (reader.TokenType != expected)
			{
				throw new InvalidOperationException();
			}
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
			{
				return null;
			}
			
			if (reader.TokenType != JsonToken.StartObject)
			{
				throw new InvalidOperationException();
			}

			ReadExpectType(reader, JsonToken.PropertyName);
			string prop = reader.Value.ToString();
			ReadExpectType(reader, JsonToken.String);
			string id = reader.Value.ToString();
			if (prop == "$ref")
			{
				object ret = serializer.ReferenceResolver.ResolveReference(serializer, id);
				ReadExpectType(reader, JsonToken.EndObject);
				return ret;
			}

			if (prop == "$id")
			{
				ReadExpectType(reader, JsonToken.PropertyName);
				prop = reader.Value.ToString();
				if (prop == "$length") // complex array
				{
					ReadExpectType(reader, JsonToken.Integer);
					int length = Convert.ToInt32(reader.Value);
					ReadExpectType(reader, JsonToken.PropertyName);
					if (reader.Value.ToString() != "$values")
						throw new InvalidOperationException();

					Type elementType = objectType.GetElementType();

					// ReSharper disable once AssignNullToNotNullAttribute
					Array ret = Array.CreateInstance(elementType, length);

					// must register reference before deserializing elements to handle possible circular references
					serializer.ReferenceResolver.AddReference(serializer, id, ret);
					int index = 0;

					ReadExpectType(reader, JsonToken.StartArray);
					while (true)
					{
						if (!reader.Read())
							throw new InvalidOperationException();
						if (reader.TokenType == JsonToken.EndArray)
							break;
						ret.SetValue(serializer.Deserialize(reader, elementType), index++);
					}
					ReadExpectType(reader, JsonToken.EndObject);
					return ret;
				}

				if (prop == "$values") // simple array
				{
					if (!reader.Read())
					{
						throw new InvalidOperationException();
					}

					object ret = _bareSerializer.Deserialize(reader, objectType);

					// OK to add this after deserializing, as arrays of primitive types can't contain backrefs
					serializer.ReferenceResolver.AddReference(serializer, id, ret);
					ReadExpectType(reader, JsonToken.EndObject);
					return ret;
				}

				throw new InvalidOperationException();
			}

			throw new InvalidOperationException();
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

				var elementType = value.GetType().GetElementType();
				if (elementType?.IsPrimitive ?? false)
				{
					writer.WritePropertyName("$values");
					_bareSerializer.Serialize(writer, value);
				}
				else
				{
					var array = (Array)value;
					writer.WritePropertyName("$length");
					writer.WriteValue(array.Length);

					writer.WritePropertyName("$values");
					writer.WriteStartArray();
					foreach (object o in array)
					{
						serializer.Serialize(writer, o, elementType);
					}

					writer.WriteEndArray();
				}

				writer.WriteEndObject();
			}
		}
	}

	public class TypeTypeConverter : JsonConverter
	{
		// serialize and deserialize types, ignoring assembly entirely and only using namespace+typename
		// all types, including generic type arguments to supplied types, must be in one of the declared assemblies (only checked on read!)
		// the main goal here is to have something with a slight chance of working across versions
		public TypeTypeConverter(IEnumerable<Assembly> ass)
		{
			_assemblies = ass.ToList();
		}

		private readonly List<Assembly> _assemblies;
		private readonly Dictionary<string, Type> _readLookup = new Dictionary<string, Type>();

		public override bool CanConvert(Type objectType)
		{
			return typeof(Type).IsAssignableFrom(objectType);
		}

		public override bool CanRead => true;
		public override bool CanWrite => true;

		private Type GetType(string name)
		{
			if (!_readLookup.TryGetValue(name, out var ret))
			{
				ret = _assemblies.Select(ass => ass.GetType(name, false)).Single(t => t != null);
				_readLookup.Add(name, ret);
			}

			return ret;
		}

		private static string GetName(Type type)
		{
			return $"{type.Namespace}.{type.Name}";
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
			{
				return null;
			}

			if (reader.TokenType == JsonToken.String)
			{
				return GetType(reader.Value.ToString());
			}

			if (reader.TokenType == JsonToken.StartArray) // full generic
			{
				List<string> values = serializer.Deserialize<List<string>>(reader);
				return GetType(values[0]).MakeGenericType(values.Skip(1).Select(GetType).ToArray());
			}

			throw new InvalidOperationException();
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var type = (Type)value;
			if (type.IsGenericType && !type.IsGenericTypeDefinition)
			{
				writer.WriteStartArray();
				writer.WriteValue(GetName(type));
				foreach (var t in type.GetGenericArguments())
				{
					writer.WriteValue(GetName(t));
				}
				writer.WriteEndArray();
			}
			else
			{
				writer.WriteValue(GetName(type));
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

		public override bool CanRead => true;
		public override bool CanWrite => true;

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			var slug = serializer.Deserialize<Slug>(reader);
			return slug?.GetDelegate();
		}

		public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
		{
			var slug = new Slug((Delegate)value);
			serializer.Serialize(writer, slug);
		}

		private class Slug
		{
			// ReSharper disable once MemberCanBePrivate.Local
			// ReSharper disable once FieldCanBeMadeReadOnly.Local
			public Type DelegateType;

			// ReSharper disable once MemberCanBePrivate.Local
			// ReSharper disable once FieldCanBeMadeReadOnly.Local
			public Type MethodDeclaringType;

			// ReSharper disable once MemberCanBePrivate.Local
			// ReSharper disable once FieldCanBeMadeReadOnly.Local
			public string MethodName;

			// ReSharper disable once MemberCanBePrivate.Local
			// ReSharper disable once FieldCanBeMadeReadOnly.Local
			public List<Type> MethodParameters;

			// ReSharper disable once MemberCanBePrivate.Local
			// ReSharper disable once FieldCanBeMadeReadOnly.Local
			public object Target;

			public Delegate GetDelegate()
			{
				var mi = MethodDeclaringType.GetMethod(
					MethodName,
					BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
					null,
					MethodParameters.ToArray(),
					null);

				// ReSharper disable once AssignNullToNotNullAttribute
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
