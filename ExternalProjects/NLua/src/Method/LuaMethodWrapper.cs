using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

using NLua.Exceptions;
using NLua.Extensions;
using NLua.Native;

namespace NLua.Method
{
	/// <summary>
	/// Argument extraction with type-conversion function
	/// </summary>
	internal delegate object ExtractValue(LuaState luaState, int stackPos);

	/// <summary>
	/// Wrapper class for methods/constructors accessed from Lua.
	/// </summary>
	internal class LuaMethodWrapper
	{
		internal readonly LuaNativeFunction InvokeFunction;

		internal readonly ObjectTranslator _translator;
		internal readonly MethodBase _method;

		internal readonly ExtractValue _extractTarget;
		internal readonly object _target;
		internal readonly bool _isStatic;

		internal readonly string _methodName;
		internal readonly MethodInfo[] _members;

		private readonly MethodCache _lastCalledMethod;

		/// <summary>
		/// Constructs the wrapper for a known MethodBase instance
		/// </summary>
		/// <param name="translator"></param>
		/// <param name="target"></param>
		/// <param name="targetType"></param>
		/// <param name="method"></param>
		public LuaMethodWrapper(ObjectTranslator translator, object target, ProxyType targetType, MethodBase method)
		{
			InvokeFunction = Call;
			_translator = translator;
			_target = target;
			_extractTarget = translator.typeChecker.GetExtractor(targetType);
			_lastCalledMethod = new();

			_method = method;
			_methodName = method.Name;
			_isStatic = method.IsStatic;
		}

		/// <summary>
		/// Constructs the wrapper for a known method name
		/// </summary>
		public LuaMethodWrapper(ObjectTranslator translator, ProxyType targetType, string methodName, BindingFlags bindingType)
		{
			InvokeFunction = Call;

			_translator = translator;
			_methodName = methodName;
			_extractTarget = translator.typeChecker.GetExtractor(targetType);
			_lastCalledMethod = new();

			_isStatic = (bindingType & BindingFlags.Static) == BindingFlags.Static;
			var methods = GetMethodsRecursively(targetType.UnderlyingSystemType, methodName, bindingType | BindingFlags.Public);
			_members = ReorderMethods(methods);
		}

		private static MethodInfo[] ReorderMethods(MethodInfo[] m)
		{
			if (m.Length < 2)
			{
				return m;
			}

			return m
				.GroupBy(c => c.GetParameters().Length)
				.SelectMany(g => g.OrderByDescending(ci => ci.ToString()))
				.ToArray();
		}

		internal static MethodInfo[] GetMethodsRecursively(Type type, string methodName, BindingFlags bindingType)
		{
			if (type == typeof(object))
			{
				return type.GetMethods(methodName, bindingType);
			}

			var methods = type.GetMethods(methodName, bindingType);
			var baseMethods = GetMethodsRecursively(type.BaseType, methodName, bindingType);
			return methods.Concat(baseMethods).ToArray();
		}

		/// <summary>
		/// Convert C# exceptions into Lua errors
		/// </summary>
		/// <returns>num of things on stack</returns>
		/// <param name="e">null for no pending exception</param>
		internal int SetPendingException(Exception e)
			=> _translator.interpreter.SetPendingException(e);

		internal int FillMethodArguments(LuaState luaState, int numStackToSkip)
		{
			var args = _lastCalledMethod.args;

			for (var i = 0; i < _lastCalledMethod.argTypes.Length; i++)
			{
				var type = _lastCalledMethod.argTypes[i];
				var index = i + 1 + numStackToSkip;
				if (_lastCalledMethod.argTypes[i].IsParamsArray)
				{
					var count = _lastCalledMethod.argTypes.Length - i;
					var paramArray = ObjectTranslator.TableToArray(luaState, type.ExtractValue, type.ParameterType, index, count);
					args[_lastCalledMethod.argTypes[i].Index] = paramArray;
				}
				else
				{
					args[type.Index] = type.ExtractValue(luaState, index);
				}

				if (_lastCalledMethod.args[_lastCalledMethod.argTypes[i].Index] == null &&
				    !luaState.IsNil(i + 1 + numStackToSkip))
				{
					return i + 1;
				}
			}

			return 0;
		}

		internal int PushReturnValue(LuaState luaState)
		{
			var nReturnValues = 0;
			// Pushes out and ref return values
			foreach (var t in _lastCalledMethod.outList)
			{
				nReturnValues++;
				_translator.Push(luaState, _lastCalledMethod.args[t]);
			}

			//  If not return void,we need add 1,
			//  or we will lost the function's return value 
			//  when call dotnet function like "int foo(arg1,out arg2,out arg3)" in Lua code 
			if (!_lastCalledMethod.IsReturnVoid && nReturnValues > 0)
			{
				nReturnValues++;
			}

			return nReturnValues < 1 ? 1 : nReturnValues;
		}

		internal int CallInvoke(LuaState luaState, MethodBase method, object targetObject)
		{
			if (!luaState.CheckStack(_lastCalledMethod.outList.Length + 6))
			{
				throw new LuaException("Lua stack overflow");
			}

			try
			{
				var result = method.IsConstructor
					? ((ConstructorInfo)method).Invoke(_lastCalledMethod.args)
					: method.Invoke(targetObject, _lastCalledMethod.args);

				_translator.Push(luaState, result);
			}
			catch (TargetInvocationException e)
			{
				// Failure of method invocation
				if (_translator.interpreter.UseTraceback) 
					e.GetBaseException().Data["Traceback"] = _translator.interpreter.GetDebugTraceback();
				return SetPendingException(e.GetBaseException());
			}
			catch (Exception e)
			{
				return SetPendingException(e);
			}

			return PushReturnValue(luaState);
		}

		internal bool IsMethodCached(LuaState luaState, int numArgsPassed, int skipParams)
		{
			if (_lastCalledMethod.CachedMethod == null)
			{
				return false;
			}

			if (numArgsPassed != _lastCalledMethod.argTypes.Length)
			{
				return false;
			}

			// If there is no method overloads, is ok to use the cached method
			if (_members == null || _members.Length == 1)
			{
				return true;
			}

			return _translator.MatchParameters(luaState, _lastCalledMethod.CachedMethod, _lastCalledMethod, skipParams);
		}

		internal int CallMethodFromName(LuaState luaState)
		{
			object targetObject = null;

			if (!_isStatic)
			{
				targetObject = _extractTarget(luaState, 1);
			}

			var numStackToSkip = _isStatic ? 0 : 1; // If this is an instance invoke we will have an extra arg on the stack for the targetObject
			var numArgsPassed = luaState.GetTop() - numStackToSkip;

			// Cached?
			if (IsMethodCached(luaState, numArgsPassed, numStackToSkip))
			{
				var method = _lastCalledMethod.CachedMethod;

				if (!luaState.CheckStack(_lastCalledMethod.outList.Length + 6))
				{
					throw new LuaException("Lua stack overflow");
				}

				var invalidArgNum = FillMethodArguments(luaState, 0);
				if (invalidArgNum != 0)
				{
					_translator.ThrowError(luaState, $"Argument number {invalidArgNum} is invalid");
					return 1;
				}

				return CallInvoke(luaState, method, targetObject);
			}

			// If we are running an instance variable, we can now pop the targetObject from the stack
			if (!_isStatic)
			{
				if (targetObject == null)
				{
					_translator.ThrowError(luaState, $"instance method '{_methodName}' requires a non null target object");
					return 1;
				}

				luaState.Remove(1); // Pops the receiver
			}

			var hasMatch = false;
			string candidateName = null;

			foreach (var member in _members)
			{
				if (member.ReflectedType == null)
				{
					continue;
				}

				candidateName = member.ReflectedType.Name + "." + member.Name;
				var isMethod = _translator.MatchParameters(luaState, member, _lastCalledMethod, 0);

				if (isMethod)
				{
					hasMatch = true;
					break;
				}
			}

			if (!hasMatch)
			{
				var msg = candidateName == null
					? "Invalid arguments to method call"
					: "Invalid arguments to method: " + candidateName;
				_translator.ThrowError(luaState, msg);
				return 1;
			}

			return _lastCalledMethod.CachedMethod.ContainsGenericParameters
				? CallInvokeOnGenericMethod(luaState, (MethodInfo)_lastCalledMethod.CachedMethod, targetObject)
				: CallInvoke(luaState, _lastCalledMethod.CachedMethod, targetObject);
		}

		internal int CallInvokeOnGenericMethod(LuaState luaState, MethodInfo methodToCall, object targetObject)
		{
			// Need to make a concrete type of the generic method definition
			var typeArgs = new List<Type>();
			var parameters = methodToCall.GetParameters();
			for (var i = 0; i < parameters.Length; i++)
			{
				var parameter = parameters[i];

				if (!parameter.ParameterType.IsGenericParameter)
				{
					continue;
				}

				typeArgs.Add(_lastCalledMethod.args[i].GetType());
			}

			var concreteMethod = methodToCall.MakeGenericMethod(typeArgs.ToArray());
			_translator.Push(luaState, concreteMethod.Invoke(targetObject, _lastCalledMethod.args));
			return PushReturnValue(luaState);
		}

		/// <summary>
		/// Calls the method. Receives the arguments from the Lua stack
		/// and returns values in it.
		/// </summary>
		internal int Call(IntPtr state)
		{
			var luaState = LuaState.FromIntPtr(state);
			var targetObject = _target;

			if (!luaState.CheckStack(5))
			{
				throw new LuaException("Lua stack overflow");
			}

			SetPendingException(null);

			// Method from name
			if (_method == null)
			{
				return CallMethodFromName(luaState);
			}

			// Method from MethodBase instance
			if (!_method.ContainsGenericParameters)
			{
				if (!_method.IsStatic && !_method.IsConstructor && targetObject == null)
				{
					targetObject = _extractTarget(luaState, 1);
					luaState.Remove(1); // Pops the receiver
				}

				// Cached?
				if (IsMethodCached(luaState, luaState.GetTop(), 0))
				{
					if (!luaState.CheckStack(_lastCalledMethod.outList.Length + 6))
					{
						throw new LuaException("Lua stack overflow");
					}

					var invalidArgNum = FillMethodArguments(luaState, 0);
					if (invalidArgNum != 0)
					{
						_translator.ThrowError(luaState, $"Argument number {invalidArgNum} is invalid");
						return 1;
					}
				}
				else if (!_translator.MatchParameters(luaState, _method,  _lastCalledMethod, 0))
				{
					_translator.ThrowError(luaState, "Invalid arguments to method call");
					return 1;
				}
			}
			else
			{
				if (!_method.IsGenericMethodDefinition)
				{
					_translator.ThrowError(luaState, "Unable to invoke method on generic class as the current method is an open generic method");
					return 1;
				}

				_translator.MatchParameters(luaState, _method, _lastCalledMethod, 0);

				return CallInvokeOnGenericMethod(luaState, (MethodInfo) _method, targetObject);
			}

			if (_isStatic)
			{
				targetObject = null;
			}

			return CallInvoke(luaState, _lastCalledMethod.CachedMethod, targetObject);
		}
	}
}
