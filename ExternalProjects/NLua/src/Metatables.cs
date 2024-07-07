using System;
using System.Linq;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;

using NLua.Exceptions;
using NLua.Extensions;
using NLua.Method;
using NLua.Native;

namespace NLua
{
	internal class MetaFunctions
	{
		public static readonly LuaNativeFunction GcFunction = CollectObject;
		public static readonly LuaNativeFunction IndexFunction = GetMethod;
		public static readonly LuaNativeFunction NewIndexFunction = SetFieldOrProperty;
		public static readonly LuaNativeFunction BaseIndexFunction = GetBaseMethod;
		public static readonly LuaNativeFunction ClassIndexFunction = GetClassMethod;
		public static readonly LuaNativeFunction ClassNewIndexFunction = SetClassFieldOrProperty;
		public static readonly LuaNativeFunction ExecuteDelegateFunction = RunFunctionDelegate;
		public static readonly LuaNativeFunction CallConstructorFunction = CallConstructor;
		public static readonly LuaNativeFunction ToStringFunction = ToStringLua;
		public static readonly LuaNativeFunction CallDelegateFunction = CallDelegate;

		public static readonly LuaNativeFunction AddFunction = AddLua;
		public static readonly LuaNativeFunction SubtractFunction = SubtractLua;
		public static readonly LuaNativeFunction MultiplyFunction = MultiplyLua;
		public static readonly LuaNativeFunction DivisionFunction = DivideLua;
		public static readonly LuaNativeFunction ModulosFunction = ModLua;
		public static readonly LuaNativeFunction UnaryNegationFunction = UnaryNegationLua;
		public static readonly LuaNativeFunction EqualFunction = EqualLua;
		public static readonly LuaNativeFunction LessThanFunction = LessThanLua;
		public static readonly LuaNativeFunction LessThanOrEqualFunction = LessThanOrEqualLua;

		internal readonly Dictionary<object, Dictionary<object, object>> _memberCache = new();
		internal readonly ObjectTranslator _translator;

		/// <summary>
		/// __index metafunction for CLR objects. Implemented in Lua.
		/// </summary>
		public const string LuaIndexFunction = @"local a={}local function b(c,d)local e=getmetatable(c)local f=e.cache[d]if f~=nil then if f==a then return nil end;return f else local g,h=get_object_member(c,d)if h then if g==nil then e.cache[d]=a else e.cache[d]=g end end;return g end end;return b";
		//@"local fakenil = {}
		//  local function index(obj, name)
		//      local meta = getmetatable(obj)
		//      local cached = meta.cache[name]
			  
		//      if cached ~= nil then
		//          if cached == fakenil then
		//              return nil
		//          end
		//          return cached
			  
		//      else
		//          local value, isCached = get_object_member(obj, name)
		//          if isCached then
		//              if value == nil then
		//                  meta.cache[name] = fakenil
		//              else
		//                  meta.cache[name] = value
		//              end
		//          end
		//          return value
		//      end
		//  end
			  
		//  return index";

		public MetaFunctions(ObjectTranslator translator)
		{
			_translator = translator;
		}

		/// <summary>
		/// __call metafunction of CLR delegates, retrieves and calls the delegate.
		/// </summary>
		private static int RunFunctionDelegate(IntPtr luaState)
		{
			var state = LuaState.FromIntPtr(luaState);
			var translator = ObjectTranslatorPool.Instance.Find(state);
			var func = (LuaNativeFunction)translator.GetRawNetObject(state, 1);
			if (func == null)
			{
				return state.Error();
			}

			state.Remove(1);
			var result = func(luaState);

			if (translator.GetObject(state, -1) is LuaScriptException)
			{
				return state.Error();
			}

			if (state != state.MainThread)
			{
				if (translator.GetObject(state.MainThread, -1) is LuaScriptException)
				{
					return state.Error();
				}
			}

			return result;
		}

		/// <summary>
		/// __gc metafunction of CLR objects.
		/// </summary>
		private static int CollectObject(IntPtr state)
		{
			var luaState = LuaState.FromIntPtr(state);
			var translator = ObjectTranslatorPool.Instance.Find(luaState);
			return CollectObject(luaState, translator);
		}

		private static int CollectObject(LuaState luaState, ObjectTranslator translator)
		{
			var udata = luaState.RawNetObj(1);

			if (udata != -1)
			{
				translator.CollectObject(udata);
			}

			return 0;
		}

		/// <summary>
		/// __tostring metafunction of CLR objects.
		/// </summary>
		private static int ToStringLua(IntPtr state)
		{
			var luaState = LuaState.FromIntPtr(state);
			var translator = ObjectTranslatorPool.Instance.Find(luaState);
			return ToStringLua(luaState, translator);
		}

		private static int ToStringLua(LuaState luaState, ObjectTranslator translator)
		{
			var obj = translator.GetRawNetObject(luaState, 1);

			if (obj != null)
			{
				translator.Push(luaState, obj + ": " + obj.GetHashCode());
			}
			else
			{
				luaState.PushNil();
			}

			return 1;
		}

		/// <summary>
		/// __add metafunction of CLR objects.
		/// </summary>
		internal static int AddLua(IntPtr luaState)
		{
			var state = LuaState.FromIntPtr(luaState);
			var translator = ObjectTranslatorPool.Instance.Find(state);
			var result = MatchOperator(state, "op_Addition", translator);
			return translator.GetObject(state, -1) is LuaScriptException ? state.Error() : result;
		}

		/// <summary>
		/// __sub metafunction of CLR objects.
		/// </summary>
		internal static int SubtractLua(IntPtr luaState)
		{
			var state = LuaState.FromIntPtr(luaState);
			var translator = ObjectTranslatorPool.Instance.Find(state);
			var result = MatchOperator(state, "op_Subtraction", translator);
			return translator.GetObject(state, -1) is LuaScriptException ? state.Error() : result;
		}

		/// <summary>
		/// __mul metafunction of CLR objects.
		/// </summary>
		internal static int MultiplyLua(IntPtr luaState)
		{
			var state = LuaState.FromIntPtr(luaState);
			var translator = ObjectTranslatorPool.Instance.Find(state);
			var result = MatchOperator(state, "op_Multiply", translator);
			return translator.GetObject(state, -1) is LuaScriptException ? state.Error() : result;
		}

		/// <summary>
		/// __div metafunction of CLR objects.
		/// </summary>
		internal static int DivideLua(IntPtr luaState)
		{
			var state = LuaState.FromIntPtr(luaState);
			var translator = ObjectTranslatorPool.Instance.Find(state);
			var result = MatchOperator(state, "op_Division", translator);
			return translator.GetObject(state, -1) is LuaScriptException ? state.Error() : result;
		}

		/// <summary>
		/// __mod metafunction of CLR objects.
		/// </summary>
		internal static int ModLua(IntPtr luaState)
		{
			var state = LuaState.FromIntPtr(luaState);
			var translator = ObjectTranslatorPool.Instance.Find(state);
			var result = MatchOperator(state, "op_Modulus", translator);
			return translator.GetObject(state, -1) is LuaScriptException ? state.Error() : result;
		}

		/// <summary>
		/// __unm metafunction of CLR objects.
		/// </summary>
		internal static int UnaryNegationLua(IntPtr luaState)
		{
			var state = LuaState.FromIntPtr(luaState);
			var translator = ObjectTranslatorPool.Instance.Find(state);
			var result = UnaryNegationLua(state, translator);
			return translator.GetObject(state, -1) is LuaScriptException ? state.Error() : result;
		}

		internal static int UnaryNegationLua(LuaState luaState, ObjectTranslator translator) //-V3009
		{
			var obj1 = translator.GetRawNetObject(luaState, 1);

			if (obj1 == null)
			{
				translator.ThrowError(luaState, "Cannot negate a nil object");
				return 1;
			}

			var type = obj1.GetType();
			var opUnaryNegation = type.GetMethod("op_UnaryNegation");

			if (opUnaryNegation == null)
			{
				translator.ThrowError(luaState, "Cannot negate object (" + type.Name + " does not overload the operator -)");
				return 1;
			}

			obj1 = opUnaryNegation.Invoke(obj1, new[] { obj1 });
			translator.Push(luaState, obj1);
			return 1;
		}

		/// <summary>
		/// __eq metafunction of CLR objects.
		/// </summary>
		internal static int EqualLua(IntPtr luaState)
		{
			var state = LuaState.FromIntPtr(luaState);
			var translator = ObjectTranslatorPool.Instance.Find(state);
			var result = MatchOperator(state, "op_Equality", translator);
			return translator.GetObject(state, -1) is LuaScriptException ? state.Error() : result;
		}

		/// <summary>
		/// __lt metafunction of CLR objects.
		/// </summary>
		internal static int LessThanLua(IntPtr luaState)
		{
			var state = LuaState.FromIntPtr(luaState);
			var translator = ObjectTranslatorPool.Instance.Find(state);
			var result = MatchOperator(state, "op_LessThan", translator);
			return translator.GetObject(state, -1) is LuaScriptException ? state.Error() : result;
		}

		/// <summary>
		/// __le metafunction of CLR objects.
		/// </summary>
		internal static int LessThanOrEqualLua(IntPtr luaState)
		{
			var state = LuaState.FromIntPtr(luaState);
			var translator = ObjectTranslatorPool.Instance.Find(state);
			var result = MatchOperator(state, "op_LessThanOrEqual", translator);
			return translator.GetObject(state, -1) is LuaScriptException ? state.Error() : result;
		}

		/// <summary>
		/// Called by the __index metafunction of CLR objects in case the
		/// method is not cached or it is a field/property/event.
		/// Receives the object and the member name as arguments and returns
		/// either the value of the member or a delegate to call it.
		/// If the member does not exist returns nil.
		/// </summary>
		private static int GetMethod(IntPtr state)
		{
			var luaState = LuaState.FromIntPtr(state);
			var translator = ObjectTranslatorPool.Instance.Find(luaState);
			var instance = translator.MetaFunctionsInstance;
			var result = instance.GetMethodInternal(luaState);
			return translator.GetObject(luaState, -1) is LuaScriptException ? luaState.Error() : result;
		}

		private int GetMethodInternal(LuaState luaState)
		{
			var obj = _translator.GetRawNetObject(luaState, 1);

			if (obj == null)
			{
				_translator.ThrowError(luaState, "Trying to index an invalid object reference");
				return 1;
			}

			var index = _translator.GetObject(luaState, 2);
			var methodName = index as string; // will be null if not a string arg
			var objType = obj.GetType();
			var proxyType = new ProxyType(objType);

			// Handle the most common case, looking up the method by name. 
			// CP: This will fail when using indexers and attempting to get a value with the same name as a property of the object, 
			// ie: xmlelement['item'] <- item is a property of xmlelement

			if (!string.IsNullOrEmpty(methodName) && IsMemberPresent(proxyType, methodName))
			{
				return GetMember(luaState, proxyType, obj, methodName, BindingFlags.Instance);
			}

			// Try to access by array if the type is right and index is an int (lua numbers always come across as double)
			if (TryAccessByArray(luaState, objType, obj, index))
			{
				return 1;
			}

			var fallback = GetMethodFallback(luaState, objType, obj, methodName);
			if (fallback != 0)
			{
				return fallback;
			}

			if (!string.IsNullOrEmpty(methodName) || index != null)
			{
				if (string.IsNullOrEmpty(methodName))
				{
					methodName = index.ToString();
				}

				return PushInvalidMethodCall(luaState, objType, methodName);
			}

			luaState.PushBoolean(false);
			return 2;
		}

		private int PushInvalidMethodCall(LuaState luaState, Type type, string name)
		{
			SetMemberCache(type, name, null);

			_translator.Push(luaState, null);
			_translator.Push(luaState, false);
			return 2;
		}

		private bool TryAccessByArray(LuaState luaState, Type objType, object obj, object index)
		{
			if (!objType.IsArray)
			{
				return false;
			}

			var intIndex = index switch
			{
				long l => (int)l,
				double d => (int)d,
				_ => -1
			};

			if (intIndex == -1)
			{
				return false;
			}

			var type = objType.UnderlyingSystemType;

			if (type == typeof(long[]))
			{
				var arr = (long[])obj;
				_translator.Push(luaState, arr[intIndex]);
				return true;
			}

			if (type == typeof(float[]))
			{
				var arr = (float[])obj;
				_translator.Push(luaState, arr[intIndex]);
				return true;
			}

			if (type == typeof(double[]))
			{
				var arr = (double[])obj;
				_translator.Push(luaState, arr[intIndex]);
				return true;
			}

			if (type == typeof(int[]))
			{
				var arr = (int[])obj;
				_translator.Push(luaState, arr[intIndex]);
				return true;
			}

			if (type == typeof(byte[]))
			{
				var arr = (byte[])obj;
				_translator.Push(luaState, arr[intIndex]);
				return true;
			}

			if (type == typeof(short[]))
			{
				var arr = (short[])obj;
				_translator.Push(luaState, arr[intIndex]);
				return true;
			}

			if (type == typeof(ushort[]))
			{
				var arr = (ushort[])obj;
				_translator.Push(luaState, arr[intIndex]);
				return true;
			}

			if (type == typeof(ulong[]))
			{
				var arr = (ulong[])obj;
				_translator.Push(luaState, arr[intIndex]);
				return true;
			}

			if (type == typeof(uint[]))
			{
				var arr = (uint[])obj;
				_translator.Push(luaState, arr[intIndex]);
				return true;
			}

			if (type == typeof(sbyte[]))
			{
				var arr = (sbyte[])obj;
				_translator.Push(luaState, arr[intIndex]);
				return true;
			}

			var array = (Array)obj;
			var element = array.GetValue(intIndex);
			_translator.Push(luaState, element);
			return true;
		}

		private int GetMethodFallback(LuaState luaState, Type objType, object obj, string methodName)
		{
			if (!string.IsNullOrEmpty(methodName) && TryGetExtensionMethod(objType, methodName, out var method))
			{
				return PushExtensionMethod(luaState, objType, obj, methodName, method);
			}

			// Try to use get_Item to index into this .net object
			var methods = objType.GetMethods();

			var res = TryIndexMethods(luaState, methods, obj);
			if (res != 0)
			{
				return res;
			}

			// Fallback to GetRuntimeMethods
			methods = objType.GetRuntimeMethods().ToArray();

			res = TryIndexMethods(luaState, methods, obj);
			if (res != 0)
			{
				return res;
			}

			res = TryGetValueForKeyMethods(luaState, methods, obj);
			if (res != 0)
			{
				return res;
			}

			// Try find explicity interface implementation
			var explicitInterfaceMethod = objType.GetMethods(BindingFlags.NonPublic | BindingFlags.Instance)
				.FirstOrDefault(m => m.Name == methodName && m.IsPrivate && m.IsVirtual && m.IsFinal);

			if (explicitInterfaceMethod != null)
			{
				var proxyType = new ProxyType(objType);
				var methodWrapper = new LuaMethodWrapper(_translator, obj, proxyType, explicitInterfaceMethod);
				var invokeDelegate = new LuaNativeFunction(methodWrapper.InvokeFunction);

				SetMemberCache(proxyType, methodName, invokeDelegate);

				_translator.PushFunction(luaState, invokeDelegate);
				_translator.Push(luaState, true);
				return 2;
			}

			return 0;
		}

		private int TryGetValueForKeyMethods(LuaState luaState, IEnumerable<MethodInfo> methods, object obj)
		{
			foreach (var methodInfo in methods)
			{
				if (methodInfo.Name != "TryGetValueForKey")
				{
					continue;
				}

				// Check if the signature matches the input
				if (methodInfo.GetParameters().Length != 2)
				{
					continue;
				}

				var actualParams = methodInfo.GetParameters();

				// Get the index in a form acceptable to the getter
				var index = _translator.GetAsType(luaState, 2, actualParams[0].ParameterType);

				// If the index type and the parameter doesn't match, just skip it
				if (index == null)
				{
					break;
				}

				// Just call the indexer - if out of bounds an exception will happen
				var args = new[] { index, null };
				try
				{
					var found = (bool)methodInfo.Invoke(obj, args);
					if (!found)
					{
						_translator.ThrowError(luaState, "key not found: " + index);
						return 1;
					}

					_translator.Push(luaState, args[1]);
					return 1;
				}
				catch (TargetInvocationException e)
				{
					// Provide a more readable description for the common case of key not found
					if (e.InnerException is KeyNotFoundException)
					{
						_translator.ThrowError(luaState, "key '" + index + "' not found ");
					}
					else
					{
						_translator.ThrowError(luaState, "exception indexing '" + index + "' " + e.Message);
					}

					return 1;
				}
			}

			return 0;
		}

		private int TryIndexMethods(LuaState luaState, IEnumerable<MethodInfo> methods, object obj)
		{
			foreach (var methodInfo in methods)
			{
				if (methodInfo.Name != "get_Item")
				{
					continue;
				}

				// Check if the signature matches the input
				if (methodInfo.GetParameters().Length != 1)
				{
					continue;
				}

				var actualParams = methodInfo.GetParameters();

				// Get the index in a form acceptable to the getter
				var index = _translator.GetAsType(luaState, 2, actualParams[0].ParameterType);

				// If the index type and the parameter doesn't match, just skip it
				if (index == null)
				{
					continue;
				}

				// Just call the indexer - if out of bounds an exception will happen
				var args = new[] { index };
				try
				{
					var result = methodInfo.Invoke(obj, args);
					_translator.Push(luaState, result);
					return 1;
				}
				catch (TargetInvocationException e)
				{
					// Provide a more readable description for the common case of key not found
					if (e.InnerException is KeyNotFoundException)
					{
						_translator.ThrowError(luaState, "key '" + index + "' not found ");
					}
					else
					{
						_translator.ThrowError(luaState, "exception indexing '" + index + "' " + e.Message);
					}

					return 1;
				}
			}

			return 0;
		}

		/// <summary>
		/// __index metafunction of base classes (the base field of Lua tables).
		/// Adds a prefix to the method name to call the base version of the method.
		/// </summary>
		private static int GetBaseMethod(IntPtr state)
		{
			var luaState = LuaState.FromIntPtr(state);
			var translator = ObjectTranslatorPool.Instance.Find(luaState);
			var instance = translator.MetaFunctionsInstance;
			var result = instance.GetBaseMethodInternal(luaState);
			return translator.GetObject(luaState, -1) is LuaScriptException ? luaState.Error() : result;
		}

		private int GetBaseMethodInternal(LuaState luaState)
		{
			var obj = _translator.GetRawNetObject(luaState, 1);
			if (obj == null)
			{
				_translator.ThrowError(luaState, "Trying to index an invalid object reference");
				return 1;
			}

			var methodName = luaState.ToString(2, false);
			if (string.IsNullOrEmpty(methodName))
			{
				luaState.PushNil();
				luaState.PushBoolean(false);
				return 2;
			}

			GetMember(luaState, new(obj.GetType()), obj, "__luaInterface_base_" + methodName, BindingFlags.Instance);
			luaState.SetTop(-2);

			if (luaState.Type(-1) == LuaType.Nil)
			{
				luaState.SetTop(-2);
				return GetMember(luaState, new(obj.GetType()), obj, methodName, BindingFlags.Instance);
			}

			luaState.PushBoolean(false);
			return 2;
		}

		/// <summary>
		/// Does this method exist as either an instance or static?
		/// </summary>
		/// <param name="objType"></param>
		/// <param name="methodName"></param>
		/// <returns></returns>
		internal bool IsMemberPresent(ProxyType objType, string methodName)
		{
			var cachedMember = CheckMemberCache(objType, methodName);
			if (cachedMember != null)
			{
				return true;
			}

			var members = objType.GetMember(methodName, BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public);
			return members.Length > 0;
		}

		internal bool TryGetExtensionMethod(Type type, string name, out object method)
		{
			var cachedMember = CheckMemberCache(type, name);
			if (cachedMember != null)
			{
				method = cachedMember;
				return true;
			}

			var found = _translator.TryGetExtensionMethod(type, name, out var methodInfo);
			method = methodInfo;
			return found;
		}

		internal int PushExtensionMethod(LuaState luaState, Type type, object obj, string name, object method)
		{
			if (method is LuaNativeFunction cachedMember)
			{
				_translator.PushFunction(luaState, cachedMember);
				_translator.Push(luaState, true);
				return 2;
			}

			var methodInfo = (MethodInfo)method;
			var methodWrapper = new LuaMethodWrapper(_translator, obj, new(type), methodInfo);
			var invokeDelegate = new LuaNativeFunction(methodWrapper.InvokeFunction);

			SetMemberCache(type, name, invokeDelegate);

			_translator.PushFunction(luaState, invokeDelegate);
			_translator.Push(luaState, true);
			return 2;
		}

		/// <summary>
		/// Pushes the value of a member or a delegate to call it, depending on the type of
		/// the member. Works with static or instance members.
		/// Uses reflection to find members, and stores the reflected MemberInfo object in
		/// a cache (indexed by the type of the object and the name of the member).
		/// </summary>
		internal int GetMember(LuaState luaState, ProxyType objType, object obj, string methodName, BindingFlags bindingType)
		{
			while (true)
			{
				var implicitStatic = false;
				MemberInfo member = null;
				var cachedMember = CheckMemberCache(objType, methodName);

				if (cachedMember is LuaNativeFunction function)
				{
					_translator.PushFunction(luaState, function);
					_translator.Push(luaState, true);
					return 2;
				}

				if (cachedMember != null)
				{
					member = (MemberInfo)cachedMember;
				}
				else
				{
					var members = objType.GetMember(methodName, bindingType | BindingFlags.Public);

					if (members.Length > 0)
					{
						member = members[0];
					}
					else
					{
						// If we can't find any suitable instance members, try to find them as statics - but we only want to allow implicit static
						members = objType.GetMember(methodName, bindingType | BindingFlags.Static | BindingFlags.Public);

						if (members.Length > 0)
						{
							member = members[0];
							implicitStatic = true;
						}
					}
				}

				if (member != null)
				{
					switch (member.MemberType)
					{
						case MemberTypes.Field:
						{
							var field = (FieldInfo)member;

							if (cachedMember == null)
							{
								SetMemberCache(objType, methodName, member);
							}

							try
							{
								var value = field.GetValue(obj);
								_translator.Push(luaState, value);
							}
							catch
							{
								Debug.WriteLine("[Exception] Fail to get field value");
								luaState.PushNil();
							}

							break;
						}
						case MemberTypes.Property:
						{
							var property = (PropertyInfo)member;
							if (cachedMember == null)
							{
								SetMemberCache(objType, methodName, member);
							}

							try
							{
								var value = property.GetValue(obj, null);
								_translator.Push(luaState, value);
							}
							catch (ArgumentException)
							{
								// If we can't find the getter in our class, recurse up to the base class and see
								// if they can help.
								if (objType.UnderlyingSystemType != typeof(object))
								{
									return GetMember(luaState, new ProxyType(objType.UnderlyingSystemType.BaseType), obj, methodName, bindingType);
								}

								luaState.PushNil();
							}
							catch (TargetInvocationException e)
							{
								// Convert this exception into a Lua error
								ThrowError(luaState, e);
								luaState.PushNil();
							}

							break;
						}
						case MemberTypes.Event:
						{
							var eventInfo = (EventInfo)member;
							if (cachedMember == null)
							{
								SetMemberCache(objType, methodName, member);
							}

							_translator.Push(luaState, new RegisterEventHandler(_translator.PendingEvents, obj, eventInfo));
							break;
						}
						case MemberTypes.All:
						case MemberTypes.Constructor:
						case MemberTypes.Custom:
						case MemberTypes.Method:
						case MemberTypes.NestedType:
						case MemberTypes.TypeInfo:
						default:
						{
							if (!implicitStatic)
							{
								if (member.MemberType == MemberTypes.NestedType && member.DeclaringType != null)
								{
									if (cachedMember == null)
									{
										SetMemberCache(objType, methodName, member);
									}

									// Find the name of our class
									var name = member.Name;
									var decType = member.DeclaringType;

									// Build a new long name and try to find the type by name
									var longName = decType.FullName + "+" + name;
									var nestedType = _translator.FindType(longName);
									_translator.PushType(luaState, nestedType);
								}
								else
								{
									// Member type must be 'method'
									var methodWrapper = new LuaMethodWrapper(_translator, objType, methodName, bindingType);
									var wrapper = methodWrapper.InvokeFunction;

									if (cachedMember == null)
									{
										SetMemberCache(objType, methodName, wrapper);
									}

									_translator.PushFunction(luaState, wrapper);
									_translator.Push(luaState, true);
									return 2;
								}
							}
							else
							{
								// If we reach this point we found a static method, but can't use it in this context because the user passed in an instance
								_translator.ThrowError(luaState, "Can't pass instance to static method " + methodName);
								return 1;
							}

							break;
						}
					}
				}
				else
				{
					if (objType.UnderlyingSystemType != typeof(object))
					{
						objType = new(objType.UnderlyingSystemType.BaseType);
						continue;
					}

					// We want to throw an exception because merely returning 'nil' in this case
					// is not sufficient.  valid data members may return nil and therefore there must be some
					// way to know the member just doesn't exist.
					_translator.ThrowError(luaState, "Unknown member name " + methodName);
					return 1;
				}

				// Push false because we are NOT returning a function (see luaIndexFunction)
				_translator.Push(luaState, false);
				return 2;
			}
		}

		/// <summary>
		/// Checks if a MemberInfo object is cached, returning it or null.
		/// </summary>
		internal object CheckMemberCache(Type objType, string memberName)
			=> CheckMemberCache(new ProxyType(objType), memberName);

		internal object CheckMemberCache(ProxyType objType, string memberName)
		{
			if (!_memberCache.TryGetValue(objType, out var members))
			{
				return null;
			}

			if (members == null || !members.TryGetValue(memberName, out var memberValue))
			{
				return null;
			}

			return memberValue;
		}

		/// <summary>
		/// Stores a MemberInfo object in the member cache.
		/// </summary>
		internal void SetMemberCache(Type objType, string memberName, object member)
			=> SetMemberCache(new ProxyType(objType), memberName, member);

		internal void SetMemberCache(ProxyType objType, string memberName, object member)
		{
			Dictionary<object, object> members;

			if (_memberCache.TryGetValue(objType, out var memberCacheValue))
			{
				members = memberCacheValue;
			}
			else
			{
				members = new();
				_memberCache[objType] = members;
			}

			members[memberName] = member;
		}

		/// <summary>
		/// __newindex metafunction of CLR objects. Receives the object,
		/// the member name and the value to be stored as arguments. Throws
		/// and error if the assignment is invalid.
		/// </summary>
		private static int SetFieldOrProperty(IntPtr state)
		{
			var luaState = LuaState.FromIntPtr(state);
			var translator = ObjectTranslatorPool.Instance.Find(luaState);
			var instance = translator.MetaFunctionsInstance;
			var result = instance.SetFieldOrPropertyInternal(luaState);
			return translator.GetObject(luaState, -1) is LuaScriptException ? luaState.Error() : result;
		}

		private int SetFieldOrPropertyInternal(LuaState luaState)
		{
			var target = _translator.GetRawNetObject(luaState, 1);
			if (target == null)
			{
				_translator.ThrowError(luaState, "trying to index and invalid object reference");
				return 1;
			}

			// First try to look up the parameter as a property name
			var type = target.GetType();
			var didMember = TrySetMember(luaState, new(type), target, BindingFlags.Instance, out var detailMessage);

			if (didMember)
			{
				return 0; // Must have found the property name
			}

			// We didn't find a property name, now see if we can use a [] style this accessor to set array contents
			try
			{
				if (type.IsArray && luaState.IsNumber(2))
				{
					var index = (int)luaState.ToNumber(2);
					var arr = (Array)target;
					var val = _translator.GetAsType(luaState, 3, arr.GetType().GetElementType());
					arr.SetValue(val, index);
				}
				else
				{
					// Try to see if we have a this[] accessor
					var setter = type.GetMethod("set_Item");
					if (setter != null)
					{
						var args = setter.GetParameters();
						var valueType = args[1].ParameterType;

						// The new value the user specified 
						var val = _translator.GetAsType(luaState, 3, valueType);
						var indexType = args[0].ParameterType;
						var index = _translator.GetAsType(luaState, 2, indexType);

						// Just call the indexer - if out of bounds an exception will happen
						var methodArgs = new[] { index, val };
						setter.Invoke(target, methodArgs);
					}
					else
					{
						_translator.ThrowError(luaState, detailMessage); // Pass the original message from trySetMember because it is probably best
						return 1;
					}
				}
			}
			catch (Exception e)
			{
				ThrowError(luaState, e);
				return 1;
			}

			return 0;
		}

		/// <summary>
		/// Tries to set a named property or field
		/// </summary>
		/// <param name="luaState"></param>
		/// <param name="targetType"></param>
		/// <param name="target"></param>
		/// <param name="bindingType"></param>
		/// <param name="detailMessage"></param>
		/// <returns>false if unable to find the named member, true for success</returns>
		internal bool TrySetMember(LuaState luaState, ProxyType targetType, object target, BindingFlags bindingType, out string detailMessage)
		{
			detailMessage = null; // No error yet

			// If not already a string just return - we don't want to call tostring - which has the side effect of 
			// changing the lua typecode to string
			// Note: We don't use isstring because the standard lua C isstring considers either strings or numbers to
			// be true for isstring.
			if (luaState.Type(2) != LuaType.String)
			{
				detailMessage = "property names must be strings";
				return false;
			}

			// We only look up property names by string
			var fieldName = luaState.ToString(2, false);
			if (string.IsNullOrEmpty(fieldName) || !(char.IsLetter(fieldName[0]) || fieldName[0] == '_'))
			{
				detailMessage = "Invalid property name";
				return false;
			}

			// Find our member via reflection or the cache
			var member = (MemberInfo)CheckMemberCache(targetType, fieldName);
			if (member == null)
			{
				var members = targetType.GetMember(fieldName, bindingType | BindingFlags.Public);

				if (members.Length <= 0)
				{
					detailMessage = "field or property '" + fieldName + "' does not exist";
					return false;
				}

				member = members[0];
				SetMemberCache(targetType, fieldName, member);
			}

			switch (member.MemberType)
			{
				case MemberTypes.Field:
				{
					var field = (FieldInfo)member;
					var val = _translator.GetAsType(luaState, 3, field.FieldType);

					try
					{
						field.SetValue(target, val);
					}
					catch (Exception e)
					{
						detailMessage = "Error setting field: " + e.Message;
						return false;
					}

					return true;
				}
				case MemberTypes.Property:
				{
					var property = (PropertyInfo)member;
					var val = _translator.GetAsType(luaState, 3, property.PropertyType);

					try
					{
						property.SetValue(target, val, null);
					}
					catch (Exception e)
					{
						detailMessage = "Error setting property: " + e.Message;
						return false;
					}

					return true;
				}
				case MemberTypes.All:
				case MemberTypes.Constructor:
				case MemberTypes.Custom:
				case MemberTypes.Event:
				case MemberTypes.Method:
				case MemberTypes.NestedType:
				case MemberTypes.TypeInfo:
				default:
					detailMessage = "'" + fieldName + "' is not a .net field or property";
					return false;
			}
		}

		/// <summary>
		/// Writes to fields or properties, either static or instance. Throws an error
		/// if the operation is invalid.
		/// </summary>
		private int SetMember(LuaState luaState, ProxyType targetType, object target, BindingFlags bindingType)
		{
			var success = TrySetMember(luaState, targetType, target, bindingType, out var detail);
			if (!success)
			{
				_translator.ThrowError(luaState, detail);
				return 1;
			}

			return 0;
		}

		/// <summary>
		/// Convert a C# exception into a Lua error
		/// </summary>
		/// <param name="e"></param>
		/// <param name="luaState"></param>
		/// We try to look into the exception to give the most meaningful description
		internal void ThrowError(LuaState luaState, Exception e)
		{
			// If we got inside a reflection show what really happened
			if (e is TargetInvocationException te)
			{
				e = te.InnerException;
			}

			_translator.ThrowError(luaState, e);
		}

		/// <summary>
		/// __index metafunction of type references, works on static members.
		/// </summary>
		private static int GetClassMethod(IntPtr state)
		{
			var luaState = LuaState.FromIntPtr(state);
			var translator = ObjectTranslatorPool.Instance.Find(luaState);
			var instance = translator.MetaFunctionsInstance;
			var result = instance.GetClassMethodInternal(luaState);
			return translator.GetObject(luaState, -1) is LuaScriptException ? luaState.Error() : result;
		}

		private int GetClassMethodInternal(LuaState luaState)
		{
			if (_translator.GetRawNetObject(luaState, 1) is not ProxyType klass)
			{
				_translator.ThrowError(luaState, "Trying to index an invalid type reference");
				return 1;
			}

			if (luaState.IsNumber(2))
			{
				var size = (int)luaState.ToNumber(2);
				_translator.Push(luaState, Array.CreateInstance(klass.UnderlyingSystemType, size));
				return 1;
			}

			var methodName = luaState.ToString(2, false);
			if (string.IsNullOrEmpty(methodName))
			{
				luaState.PushNil();
				return 1;
			}

			return GetMember(luaState, klass, null, methodName, BindingFlags.Static);
		}

		/// <summary>
		/// __newindex function of type references, works on static members.
		/// </summary>
		private static int SetClassFieldOrProperty(IntPtr state)
		{
			var luaState = LuaState.FromIntPtr(state);
			var translator = ObjectTranslatorPool.Instance.Find(luaState);
			var instance = translator.MetaFunctionsInstance;
			var result = instance.SetClassFieldOrPropertyInternal(luaState);
			return translator.GetObject(luaState, -1) is LuaScriptException ? luaState.Error() : result;
		}

		private int SetClassFieldOrPropertyInternal(LuaState luaState)
		{
			if (_translator.GetRawNetObject(luaState, 1) is not ProxyType target)
			{
				_translator.ThrowError(luaState, "trying to index an invalid type reference");
				return 1;
			}

			return SetMember(luaState, target, null, BindingFlags.Static);
		}

		/// <summary>
		/// __call metafunction of Delegates.
		/// </summary>
		internal static int CallDelegate(IntPtr state)
		{
			var luaState = LuaState.FromIntPtr(state);
			var translator = ObjectTranslatorPool.Instance.Find(luaState);
			var instance = translator.MetaFunctionsInstance;
			var result = instance.CallDelegateInternal(luaState);
			return translator.GetObject(luaState, -1) is LuaScriptException ? luaState.Error() : result;
		}

		internal int CallDelegateInternal(LuaState luaState)
		{
			if (_translator.GetRawNetObject(luaState, 1) is not Delegate del)
			{
				_translator.ThrowError(luaState, "Trying to invoke a not delegate or callable value");
				return 1;
			}

			luaState.Remove(1);

			var validDelegate = new MethodCache();
			MethodBase methodDelegate = del.Method;
			var isOk = MatchParameters(luaState, methodDelegate, validDelegate, 0);

			if (isOk)
			{
				try
				{
					var result = methodDelegate.Invoke(methodDelegate.IsStatic ? null : del.Target, validDelegate.args);
					_translator.Push(luaState, result);
					return 1;
				}
				catch (TargetInvocationException e)
				{
					// Failure of method invocation
					if (_translator.interpreter.UseTraceback)
					{
						e.GetBaseException().Data["Traceback"] = _translator.interpreter.GetDebugTraceback();
					}

					return  _translator.Interpreter.SetPendingException(e.GetBaseException());
				}
				catch (Exception e)
				{
					return _translator.Interpreter.SetPendingException(e);
				}
			}

			_translator.ThrowError(luaState, "Cannot invoke delegate (invalid arguments for  " + methodDelegate.Name + ")");
			return 1;
		}

		/// <summary>
		/// __call metafunction of type references. Searches for and calls
		/// a constructor for the type. Returns nil if the constructor is not
		/// found or if the arguments are invalid. Throws an error if the constructor
		/// generates an exception.
		/// </summary>
		private static int CallConstructor(IntPtr state)
		{
			var luaState = LuaState.FromIntPtr(state);
			var translator = ObjectTranslatorPool.Instance.Find(luaState);
			var instance = translator.MetaFunctionsInstance;
			var result = instance.CallConstructorInternal(luaState);
			return translator.GetObject(luaState, -1) is LuaScriptException ? luaState.Error() : result;
		}

		private static ConstructorInfo[] ReorderConstructors(ConstructorInfo[] constructors)
		{
			if (constructors.Length < 2)
			{
				return constructors;
			}

			return constructors
				.GroupBy(c => c.GetParameters().Length)
				.SelectMany(g => g.OrderByDescending(ci => ci.ToString()))
				.ToArray();
		}

		private int CallConstructorInternal(LuaState luaState)
		{
			if (_translator.GetRawNetObject(luaState, 1) is not ProxyType klass)
			{
				_translator.ThrowError(luaState, "Trying to call constructor on an invalid type reference");
				return 1;
			}

			var validConstructor = new MethodCache();

			luaState.Remove(1);
			var constructors = klass.UnderlyingSystemType.GetConstructors();
			constructors = ReorderConstructors(constructors);
			foreach (var constructor in constructors)
			{
				var isConstructor = MatchParameters(luaState, constructor, validConstructor, 0);

				if (!isConstructor)
				{
					continue;
				}

				try
				{
					_translator.Push(luaState, constructor.Invoke(validConstructor.args));
				}
				catch (TargetInvocationException e)
				{
					ThrowError(luaState, e);
					return 1;
				}
				catch
				{
					luaState.PushNil();
				}

				return 1;
			}

			if (klass.UnderlyingSystemType.IsValueType)
			{
				var numLuaParams = luaState.GetTop();
				if (numLuaParams == 0)
				{
					_translator.Push(luaState, Activator.CreateInstance(klass.UnderlyingSystemType));
					return 1;
				}
			}

			var constructorName = constructors.Length == 0 ? "unknown" : constructors[0].Name;
			_translator.ThrowError(luaState, $"{klass.UnderlyingSystemType} does not contain constructor({constructorName}) argument match");
			return 1;
		}

		internal static object GetTargetObject(LuaState luaState, string operation, ObjectTranslator translator)
		{
			Type t;
			var target = translator.GetRawNetObject(luaState, 1);
			if (target != null)
			{
				t = target.GetType();
				if (t.HasMethod(operation))
				{
					return target;
				}
			}

			target = translator.GetRawNetObject(luaState, 2);
			if (target != null)
			{
				t = target.GetType();
				if (t.HasMethod(operation))
				{
					return target;
				}
			}

			return null;
		}

		internal static int MatchOperator(LuaState luaState, string operation, ObjectTranslator translator)
		{
			var validOperator = new MethodCache();

			var target = GetTargetObject(luaState, operation, translator);
			if (target == null)
			{
				translator.ThrowError(luaState, "Cannot call " + operation + " on a nil object");
				return 1;
			}

			var type = target.GetType();
			var operators = type.GetMethods(operation, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

			foreach (var op in operators)
			{
				var isOk = translator.MatchParameters(luaState, op, validOperator, 0);

				if (!isOk)
				{
					continue;
				}

				var result = op.Invoke(op.IsStatic ? null : target, validOperator.args);
				translator.Push(luaState, result);
				return 1;
			}

			translator.ThrowError(luaState, "Cannot call (" + operation + ") on object type " + type.Name);
			return 1;
		}

		internal static Array TableToArray(LuaState luaState, ExtractValue extractValue, Type paramArrayType, ref int startIndex, int count)
		{
			if (count == 0)
			{
				return Array.CreateInstance(paramArrayType, 0);
			}

			var luaParamValue = extractValue(luaState, startIndex);
			startIndex++;

			var paramArray = Array.CreateInstance(paramArrayType, count);
			paramArray.SetValue(luaParamValue, 0);
			for (var i = 1; i < count; i++)
			{
				var value = extractValue(luaState, startIndex);
				paramArray.SetValue(value, i);
				startIndex++;
			}

			return paramArray;

		}

		/// <summary>
		/// Matches a method against its arguments in the Lua stack. Returns
		/// if the match was successful. It it was also returns the information
		/// necessary to invoke the method.
		/// </summary>
		internal bool MatchParameters(LuaState luaState, MethodBase method, MethodCache methodCache, int skipParam)
		{
			var paramInfo = method.GetParameters();
			var currentLuaParam = 1;
			var nLuaParams = luaState.GetTop() - skipParam;
			var paramList = new List<object>();
			var outList = new List<int>();
			var argTypes = new List<MethodArgs>();

			foreach (var currentNetParam in paramInfo)
			{
				if (!currentNetParam.IsIn && currentNetParam.IsOut) // Skips out params 
				{
					paramList.Add(null);
					outList.Add(paramList.Count - 1);
					continue; // Type does not match, ignore if the parameter is optional
				}

				if (IsParamsArray(luaState, currentLuaParam, currentNetParam, out var extractValue))
				{
					var count = nLuaParams - currentLuaParam + 1;
					var paramArrayType = currentNetParam.ParameterType.GetElementType();

					var paramArray = TableToArray(luaState, extractValue, paramArrayType, ref currentLuaParam, count);
					paramList.Add(paramArray);

					var index = paramList.LastIndexOf(paramArray);
					var methodArg = new MethodArgs
					{
						Index = index,
						ExtractValue = extractValue,
						IsParamsArray = true,
						ParameterType = paramArrayType
					};

					argTypes.Add(methodArg);
					continue;
				}

				if (currentLuaParam > nLuaParams)
				{
					// Adds optional parameters
					if (!currentNetParam.IsOptional)
					{
						return false;
					}

					paramList.Add(currentNetParam.DefaultValue);
					continue;
				}

				if (IsTypeCorrect(luaState, currentLuaParam, currentNetParam, out extractValue))
				{
					// Type checking
					var value = extractValue(luaState, currentLuaParam);
					paramList.Add(value);

					var index = paramList.Count - 1;
					var methodArg = new MethodArgs
					{
						Index = index,
						ExtractValue = extractValue,
						ParameterType = currentNetParam.ParameterType
					};

					argTypes.Add(methodArg);

					if (currentNetParam.ParameterType.IsByRef)
					{
						outList.Add(index);
					}

					currentLuaParam++;
					continue;
				}

				if (currentNetParam.IsOptional)
				{
					paramList.Add(currentNetParam.DefaultValue);
					continue;
				}

				return false;
			}

			if (currentLuaParam != nLuaParams + 1) // Number of parameters does not match
			{
				return false;
			}

			methodCache.args = paramList.ToArray();
			methodCache.CachedMethod = method;
			methodCache.outList = outList.ToArray();
			methodCache.argTypes = argTypes.ToArray();

			return true;
		}

		/// <summary>
		/// Returns true if the type is set and assigns the extract value
		/// </summary>
		/// <param name="luaState"></param>
		/// <param name="currentLuaParam"></param>
		/// <param name="currentNetParam"></param>
		/// <param name="extractValue"></param>
		/// <returns></returns>
		private bool IsTypeCorrect(LuaState luaState, int currentLuaParam, ParameterInfo currentNetParam, out ExtractValue extractValue)
		{
			extractValue = _translator.typeChecker.CheckLuaType(luaState, currentLuaParam, currentNetParam.ParameterType);
			return extractValue != null;
		}

		private bool IsParamsArray(LuaState luaState, int currentLuaParam, ParameterInfo currentNetParam, out ExtractValue extractValue)
		{
			extractValue = null;

			if (!currentNetParam.GetCustomAttributes(typeof(ParamArrayAttribute), false).Any())
			{
				return false;
			}

			var paramElementType = currentNetParam.ParameterType.GetElementType();
			extractValue = _translator.typeChecker.CheckLuaType(luaState, currentLuaParam, paramElementType);
			return extractValue != null;
		}
	}
}
