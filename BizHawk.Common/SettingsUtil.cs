using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Collections.Concurrent;
using System.ComponentModel;

namespace BizHawk.Common
{
	public class SettingsUtil
	{
		private sealed class DefaultValueSetter
		{
			public Action<object, object[]> SetDefaultValues;
			public object[] DefaultValues;
		}


		private static IDictionary<Type, DefaultValueSetter> DefaultValueSetters = new ConcurrentDictionary<Type, DefaultValueSetter>();

		/// <summary>
		/// set all properties (not fields!) of obj with a DefaultValueAttribute to that value
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="obj">the obj to act on</param>
		public static void SetDefaultValues<T>(T obj)
		{
			DefaultValueSetter f;
			if (!DefaultValueSetters.TryGetValue(typeof(T), out f))
			{
				f = CreateSetter(typeof(T));
				DefaultValueSetters[typeof(T)] = f;
			}
			f.SetDefaultValues(obj, f.DefaultValues);
		}

		private static Dictionary<Type, OpCode> IntTypes = new Dictionary<Type,OpCode>
		{
			{ typeof(byte), OpCodes.Conv_U1 },
			{ typeof(sbyte), OpCodes.Conv_I1 },
			{ typeof(ushort), OpCodes.Conv_U2 },
			{ typeof(short), OpCodes.Conv_I2 },
			{ typeof(uint), OpCodes.Conv_U4 },
			{ typeof(int), OpCodes.Conv_I4 },
			{ typeof(ulong), OpCodes.Conv_U8 },
			{ typeof(long), OpCodes.Conv_I8 },
			{ typeof(UIntPtr), OpCodes.Conv_U },
			{ typeof(IntPtr), OpCodes.Conv_I },
		};

		private static DefaultValueSetter CreateSetter(Type t)
		{
			var dyn = new DynamicMethod("SetDefaultValues_" + t.Name, null, new[] { typeof(object), typeof(object[]) }, false);
			var il = dyn.GetILGenerator();
			List<object> DefaultValues = new List<object>();

			il.Emit(OpCodes.Ldarg_0); // arg0: object to set properties of
			il.Emit(OpCodes.Castclass, t); // cast to appropriate type

			foreach (var prop in t.GetProperties())
			{
				if (!prop.CanWrite)
					continue;
				MethodInfo method = prop.GetSetMethod(true);
				foreach (object attr in prop.GetCustomAttributes(true))
				{
					if (attr is DefaultValueAttribute)
					{
						object value = (attr as DefaultValueAttribute).Value;
						Type desiredType = method.GetParameters()[0].ParameterType;
						Type sourceType = value.GetType();

						int idx = DefaultValues.Count;
						DefaultValues.Add(value);

						il.Emit(OpCodes.Dup); // object to act on
						il.Emit(OpCodes.Ldarg_1); // arg1: array of default values
						il.Emit(OpCodes.Ldc_I4, idx); // load index
						il.Emit(OpCodes.Ldelem, typeof(object)); // get default value at appropriate index

						// cast to the expected type of the set method
						if (desiredType.IsAssignableFrom(sourceType))
						{
							il.Emit(OpCodes.Unbox_Any, desiredType);
						}
						else if (IntTypes.ContainsKey(sourceType) && IntTypes.ContainsKey(desiredType))
						{
							il.Emit(OpCodes.Unbox_Any, sourceType);
							il.Emit(IntTypes[desiredType]);
						}
						else
						{
							throw new InvalidOperationException(string.Format("Default value assignment will fail for {0}.{1}", t.Name, prop.Name));
						}
						il.Emit(OpCodes.Callvirt, method);
					}
				}
			}
			il.Emit(OpCodes.Pop);
			il.Emit(OpCodes.Ret);
			return new DefaultValueSetter
			{
				SetDefaultValues = (Action<object, object[]>)dyn.CreateDelegate(typeof(Action<object, object[]>)),
				DefaultValues = DefaultValues.ToArray()
			};
		}
	}
}
