using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NLua.Method;

namespace NLua.GenerateEventAssembly
{
	internal class CodeGeneration
	{
		private readonly Dictionary<Type, LuaClassType> _classCollection = new();
		private readonly Dictionary<Type, Type> _delegateCollection = new();

		static CodeGeneration()
		{
		}

		private CodeGeneration()
		{
		}

		/// <summary>
		/// Singleton instance of the class
		/// </summary>
		public static CodeGeneration Instance { get; } = new();

		internal static void GetReturnTypesFromClass(Type klass, out Type[][] returnTypes)
		{
			var classMethods = klass.GetMethods();
			returnTypes = new Type[classMethods.Length][];

			var i = 0;

			foreach (var method in classMethods)
			{
				if (klass.IsInterface)
				{
					GetReturnTypesFromMethod(method, out returnTypes[i]);
					i++;
				}
				else if (!method.IsPrivate && !method.IsFinal && method.IsVirtual)
				{
					GetReturnTypesFromMethod(method, out returnTypes[i]);
					i++;
				}
			}
		}

		internal static void GetReturnTypesFromMethod(MethodInfo method, out Type[] returnTypes)
		{
			var paramInfo = method.GetParameters();
			var paramTypes = new Type[paramInfo.Length];
			var returnTypesList = new List<Type>();

			// Counts out and ref parameters, for later use, 
			// and creates the list of return types
			var returnType = method.ReturnType;
			returnTypesList.Add(returnType);

			for (var i = 0; i < paramTypes.Length; i++)
			{
				paramTypes[i] = paramInfo[i].ParameterType;

				if (paramTypes[i].IsByRef)
				{
					returnTypesList.Add(paramTypes[i].GetElementType());
				}
			}

			returnTypes = returnTypesList.ToArray();
		}

		public void RegisterLuaDelegateType(Type delegateType, Type luaDelegateType)
			=> _delegateCollection[delegateType] = luaDelegateType;

		public void RegisterLuaClassType(Type klass, Type luaClass)
		{
			LuaClassType luaClassType = default;
			luaClassType.klass = luaClass;
			GetReturnTypesFromClass(klass, out luaClassType.returnTypes);
			_classCollection[klass] = luaClassType;
		}

		/// <summary>
		/// Gets a delegate with delegateType that calls the luaFunc Lua function
		/// Caches the generated type.
		/// </summary>
		public Delegate GetDelegate(Type delegateType, LuaFunction luaFunc)
		{
			var returnTypes = new List<Type>();
			var luaDelegateType = _delegateCollection[delegateType];

			var methodInfo = delegateType.GetMethod("Invoke");
			returnTypes.Add(methodInfo!.ReturnType);

			returnTypes.AddRange(methodInfo.GetParameters()
				.Where(paramInfo => paramInfo.ParameterType.IsByRef)
				.Select(paramInfo => paramInfo.ParameterType));

			var luaDelegate = (LuaDelegate)Activator.CreateInstance(luaDelegateType);
			luaDelegate.Function = luaFunc;
			luaDelegate.ReturnTypes = returnTypes.ToArray();
			return Delegate.CreateDelegate(delegateType, luaDelegate, "CallFunction");
		}

		/// <summary>
		/// Gets an instance of an implementation of the klass interface or
		/// subclass of klass that delegates public virtual methods to the
		/// luaTable table.
		/// Caches the generated type.
		/// </summary>
		public object GetClassInstance(Type klass, LuaTable luaTable)
		{
			var luaClassType = _classCollection[klass];
			return Activator.CreateInstance(luaClassType.klass, luaTable, luaClassType.returnTypes);
		}
	}
}
