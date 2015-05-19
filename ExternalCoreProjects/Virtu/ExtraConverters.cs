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
		// Doing so while being able to fully preserve circular references would require storing the length of the array,
		// or reading ahead in the JSON to compute the length.  For arrays that could contain reference types, we choose the latter.
		// For arrays of primitive types, there is no issue. 

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
			if (reader.TokenType == JsonToken.Null)
				return null;
			else if (reader.TokenType != JsonToken.StartObject)
				throw new InvalidOperationException();

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
			else if (prop == "$id")
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
				else if (prop == "$values") // simple array
				{
					if (!reader.Read())
						throw new InvalidOperationException();
					object ret = bareserializer.Deserialize(reader, objectType);
					// OK to add this after deserializing, as arrays of primitive types can't contain backrefs
					serializer.ReferenceResolver.AddReference(serializer, id, ret);
					ReadExpectType(reader, JsonToken.EndObject);
					return ret;
				}
				else
				{
					throw new InvalidOperationException();
				}
			}
			else
			{
				throw new InvalidOperationException();
			}
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
				if (elementType.IsPrimitive)
				{
					writer.WritePropertyName("$values");
					bareserializer.Serialize(writer, value);
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
			assemblies = ass.ToList();
		}

		private List<Assembly> assemblies;
		private Dictionary<string, Type> readlookup = new Dictionary<string, Type>();

		public override bool CanConvert(Type objectType)
		{
			return typeof(Type).IsAssignableFrom(objectType);
		}

		public override bool CanRead { get { return true; } }
		public override bool CanWrite { get { return true; } }

		private Type GetType(string name)
		{
			Type ret;
			if (!readlookup.TryGetValue(name, out ret))
			{
				ret = assemblies.Select(ass => ass.GetType(name, false)).Where(t => t != null).Single();
				readlookup.Add(name, ret);
			}
			return ret;
		}

		private static string GetName(Type type)
		{
			return string.Format("{0}.{1}", type.Namespace, type.Name);
		}

		public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
		{
			if (reader.TokenType == JsonToken.Null)
			{
				return null;
			}
			else if (reader.TokenType == JsonToken.String)
			{
				return GetType(reader.Value.ToString());
			}
			else if (reader.TokenType == JsonToken.StartArray) // full generic
			{
				List<string> vals = serializer.Deserialize<List<string>>(reader);
				return GetType(vals[0]).MakeGenericType(vals.Skip(1).Select(GetType).ToArray());
			}
			else
			{
				throw new InvalidOperationException();
			}
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
