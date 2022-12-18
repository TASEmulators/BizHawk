using System;
using System.Collections.Generic;
using NLua.Method;
using NLua.Extensions;

namespace NLua
{
	internal sealed class CheckType
	{
		internal readonly Dictionary<Type, ExtractValue> _extractValues = new Dictionary<Type, ExtractValue>();
		internal readonly ExtractValue _extractNetObject;
		internal readonly ObjectTranslator _translator;

		public CheckType(ObjectTranslator translator)
		{
			_translator = translator;
			_extractValues.Add(typeof(object), GetAsObject);
			_extractValues.Add(typeof(sbyte), GetAsSbyte);
			_extractValues.Add(typeof(byte), GetAsByte);
			_extractValues.Add(typeof(short), GetAsShort);
			_extractValues.Add(typeof(ushort), GetAsUshort);
			_extractValues.Add(typeof(int), GetAsInt);
			_extractValues.Add(typeof(uint), GetAsUint);
			_extractValues.Add(typeof(long), GetAsLong);
			_extractValues.Add(typeof(ulong), GetAsUlong);
			_extractValues.Add(typeof(double), GetAsDouble);
			_extractValues.Add(typeof(char), GetAsChar);
			_extractValues.Add(typeof(float), GetAsFloat);
			_extractValues.Add(typeof(decimal), GetAsDecimal);
			_extractValues.Add(typeof(bool), GetAsBoolean);
			_extractValues.Add(typeof(string), GetAsString);
			_extractValues.Add(typeof(char[]), GetAsCharArray);
			_extractValues.Add(typeof(byte[]), GetAsByteArray);
			_extractValues.Add(typeof(LuaFunction), GetAsFunction);
			_extractValues.Add(typeof(LuaTable), GetAsTable);
			_extractValues.Add(typeof(LuaThread), GetAsThread);
			_extractValues.Add(typeof(LuaUserData), GetAsUserdata);
			_extractNetObject = GetAsNetObject;
		}

		/// <summary>
		/// Checks if the value at Lua stack index stackPos matches paramType, 
		/// returning a conversion function if it does and null otherwise.
		/// </summary>
		internal ExtractValue GetExtractor(ProxyType paramType)
		{
			return GetExtractor(paramType.UnderlyingSystemType);
		}

		internal ExtractValue GetExtractor(Type paramType)
		{
			if (paramType.IsByRef)
				paramType = paramType.GetElementType();

			return _extractValues.ContainsKey(paramType) ? _extractValues[paramType] : _extractNetObject;
		}

		internal ExtractValue CheckLuaType(LuaState luaState, int stackPos, Type paramType)
		{
			LuaType luatype = luaState.Type(stackPos);

			if (paramType.IsByRef)
				paramType = paramType.GetElementType();

			var underlyingType = Nullable.GetUnderlyingType(paramType);

			if (underlyingType != null)
			{
				paramType = underlyingType;  // Silently convert nullable types to their non null requics
			}


			bool netParamIsNumeric = paramType == typeof(int) ||
										paramType == typeof(uint) ||
										paramType == typeof(long) ||
										paramType == typeof(ulong) ||
										paramType == typeof(short) ||
										paramType == typeof(ushort) ||
										paramType == typeof(float) ||
										paramType == typeof(double) ||
										paramType == typeof(decimal) ||
										paramType == typeof(byte) ||
										paramType == typeof(sbyte) ||
										paramType == typeof(char);

			// If it is a nullable
			if (underlyingType != null)
			{
				// null can always be assigned to nullable
				if (luatype == LuaType.Nil)
				{
					// Return the correct extractor anyways
					if (netParamIsNumeric || paramType == typeof(bool))
						return _extractValues[paramType];
					return _extractNetObject;
				}
			}

			if (paramType == typeof(object))
				return _extractValues[paramType];

			//CP: Added support for generic parameters
			if (paramType.IsGenericParameter)
			{
				if (luatype == LuaType.Boolean)
					return _extractValues[typeof(bool)];
				if (luatype == LuaType.String)
					return _extractValues[typeof(string)];
				if (luatype == LuaType.Table)
					return _extractValues[typeof(LuaTable)];
				if (luatype == LuaType.Thread)
					return _extractValues[typeof(LuaThread)];
				if (luatype == LuaType.UserData)
					return _extractValues[typeof(object)];
				if (luatype == LuaType.Function)
					return _extractValues[typeof(LuaFunction)];
				if (luatype == LuaType.Number)
					return _extractValues[typeof(double)];
			}
			bool netParamIsString = paramType == typeof(string) || paramType == typeof(char[]) || paramType == typeof(byte[]);

			if (netParamIsNumeric)
			{
				if (luaState.IsNumericType(stackPos) && !netParamIsString)
					return _extractValues[paramType];
			}
			else if (paramType == typeof(bool))
			{
				if (luaState.IsBoolean(stackPos))
					return _extractValues[paramType];
			}
			else if (netParamIsString)
			{
				if (luaState.IsStringOrNumber(stackPos) || luatype == LuaType.Nil)
					return _extractValues[paramType];
			}
			else if (paramType == typeof(LuaTable))
			{
				if (luatype == LuaType.Table || luatype == LuaType.Nil)
					return _extractValues[paramType];
			}
			else if (paramType == typeof(LuaThread))
			{
				if (luatype == LuaType.Thread || luatype == LuaType.Nil)
					return _extractValues[paramType];
			}
			else if (paramType == typeof(LuaUserData))
			{
				if (luatype == LuaType.UserData || luatype == LuaType.Nil)
					return _extractValues[paramType];
			}
			else if (paramType == typeof(LuaFunction))
			{
				if (luatype == LuaType.Function || luatype == LuaType.Nil)
					return _extractValues[paramType];
			}
			else if (typeof(Delegate).IsAssignableFrom(paramType) && luatype == LuaType.Function && paramType.GetMethod("Invoke") != null)
				return new DelegateGenerator(_translator, paramType).ExtractGenerated;
			else if (paramType.IsInterface && luatype == LuaType.Table)
				return new ClassGenerator(_translator, paramType).ExtractGenerated;
			else if ((paramType.IsInterface || paramType.IsClass) && luatype == LuaType.Nil)
			{
				// kevinh - allow nil to be silently converted to null - extractNetObject will return null when the item ain't found
				return _extractNetObject;
			}
			else if (luaState.Type(stackPos) == LuaType.Table)
			{
				if (luaState.GetMetaField(stackPos, "__index") != LuaType.Nil)
				{
					object obj = _translator.GetNetObject(luaState, -1);
					luaState.SetTop(-2);
					if (obj != null && paramType.IsInstanceOfType(obj))
						return _extractNetObject;
				}
				else
					return null;
			}

			object netObj = _translator.GetNetObject(luaState, stackPos);
			if (netObj != null && paramType.IsInstanceOfType(netObj))
				return _extractNetObject;

			return null;
		}

		/// <summary>
		/// The following functions return the value in the Lua stack
		/// index stackPos as the desired type if it can, or null
		/// otherwise.
		/// </summary>
		private object GetAsSbyte(LuaState luaState, int stackPos)
		{
			if (!luaState.IsNumericType(stackPos))
				return null;

			if (luaState.IsInteger(stackPos))
				return (sbyte)luaState.ToInteger(stackPos);

			return (sbyte)luaState.ToNumber(stackPos);
		}

		private object GetAsByte(LuaState luaState, int stackPos)
		{
			if (!luaState.IsNumericType(stackPos))
				return null;

			if (luaState.IsInteger(stackPos))
				return (byte)luaState.ToInteger(stackPos);

			return (byte)luaState.ToNumber(stackPos);
		}

		private object GetAsShort(LuaState luaState, int stackPos)
		{
			if (!luaState.IsNumericType(stackPos))
				return null;

			if (luaState.IsInteger(stackPos))
				return (short)luaState.ToInteger(stackPos);

			return (short)luaState.ToNumber(stackPos);
		}

		private object GetAsUshort(LuaState luaState, int stackPos)
		{
			if (!luaState.IsNumericType(stackPos))
				return null;

			if (luaState.IsInteger(stackPos))
				return (ushort)luaState.ToInteger(stackPos);

			return (ushort)luaState.ToNumber(stackPos);
		}

		private object GetAsInt(LuaState luaState, int stackPos)
		{
			if (!luaState.IsNumericType(stackPos))
				return null;

			if (luaState.IsInteger(stackPos))
				return (int)luaState.ToInteger(stackPos);

			return (int)luaState.ToNumber(stackPos);
		}

		private object GetAsUint(LuaState luaState, int stackPos)
		{
			if (!luaState.IsNumericType(stackPos))
				return null;

			if (luaState.IsInteger(stackPos))
				return (uint)luaState.ToInteger(stackPos);

			return (uint)luaState.ToNumber(stackPos);
		}

		private object GetAsLong(LuaState luaState, int stackPos)
		{
			if (!luaState.IsNumericType(stackPos))
				return null;

			if (luaState.IsInteger(stackPos))
				return luaState.ToInteger(stackPos);

			return (long)luaState.ToNumber(stackPos);
		}

		private object GetAsUlong(LuaState luaState, int stackPos)
		{
			if (!luaState.IsNumericType(stackPos))
				return null;

			if (luaState.IsInteger(stackPos))
				return (ulong)luaState.ToInteger(stackPos);

			return (ulong)luaState.ToNumber(stackPos);
		}

		private object GetAsDouble(LuaState luaState, int stackPos)
		{
			if (!luaState.IsNumericType(stackPos))
				return null;

			if (luaState.IsInteger(stackPos))
				return (double)luaState.ToInteger(stackPos);

			return luaState.ToNumber(stackPos);
		}

		private object GetAsChar(LuaState luaState, int stackPos)
		{
			if (!luaState.IsNumericType(stackPos))
				return null;

			if (luaState.IsInteger(stackPos))
				return (char)luaState.ToInteger(stackPos);

			return (char)luaState.ToNumber(stackPos);
		}

		private object GetAsFloat(LuaState luaState, int stackPos)
		{
			if (!luaState.IsNumericType(stackPos))
				return null;

			if (luaState.IsInteger(stackPos))
				return (float)luaState.ToInteger(stackPos);

			return (float)luaState.ToNumber(stackPos);
		}

		private object GetAsDecimal(LuaState luaState, int stackPos)
		{
			if (!luaState.IsNumericType(stackPos))
				return null;

			if (luaState.IsInteger(stackPos))
				return (decimal)luaState.ToInteger(stackPos);

			return (decimal)luaState.ToNumber(stackPos);
		}

		private object GetAsBoolean(LuaState luaState, int stackPos)
		{
			return luaState.ToBoolean(stackPos);
		}

		private object GetAsCharArray(LuaState luaState, int stackPos)
		{
			if (!luaState.IsStringOrNumber(stackPos))
				return null;
			string retVal = luaState.ToString(stackPos, false);
			return retVal.ToCharArray();
		}

		private object GetAsByteArray(LuaState luaState, int stackPos)
		{
			if (!luaState.IsStringOrNumber(stackPos))
				return null;

			byte [] retVal = luaState.ToBuffer(stackPos, false);
			return retVal;
		}

		private object GetAsString(LuaState luaState, int stackPos)
		{
			if (!luaState.IsStringOrNumber(stackPos))
				return null;
			return luaState.ToString(stackPos, false);
		}
		
		private object GetAsTable(LuaState luaState, int stackPos)
		{
			return _translator.GetTable(luaState, stackPos);
		}

		private object GetAsThread(LuaState luaState, int stackPos)
		{
			return _translator.GetThread(luaState, stackPos);
		}

		private object GetAsFunction(LuaState luaState, int stackPos)
		{
			return _translator.GetFunction(luaState, stackPos);
		}

		private object GetAsUserdata(LuaState luaState, int stackPos)
		{
			return _translator.GetUserData(luaState, stackPos);
		}

		public object GetAsObject(LuaState luaState, int stackPos)
		{
			if (luaState.Type(stackPos) == LuaType.Table)
			{
				if (luaState.GetMetaField(stackPos, "__index") != LuaType.Nil)
				{
					if (luaState.CheckMetaTable(-1, _translator.Tag))
					{
						luaState.Insert(stackPos);
						luaState.Remove(stackPos + 1);
					}
					else
						luaState.SetTop(-2);
				}
			}

			object obj = _translator.GetObject(luaState, stackPos);
			return obj;
		}

		public object GetAsNetObject(LuaState luaState, int stackPos)
		{
			object obj = _translator.GetNetObject(luaState, stackPos);

			if (obj != null || luaState.Type(stackPos) != LuaType.Table)
				return obj;

			if (luaState.GetMetaField(stackPos, "__index") == LuaType.Nil)
				return null;

			if (luaState.CheckMetaTable(-1, _translator.Tag))
			{
				luaState.Insert(stackPos);
				luaState.Remove(stackPos + 1);
				obj = _translator.GetNetObject(luaState, stackPos);
			}
			else
				luaState.SetTop(-2);

			return obj;
		}
	}
}
