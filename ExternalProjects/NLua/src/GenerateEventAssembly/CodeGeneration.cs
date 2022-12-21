using System;
using System.Threading;
using System.Reflection;

using System.Reflection.Emit;
using System.Collections.Generic;
using NLua.Method;

namespace NLua
{
	internal class CodeGeneration
	{
		private readonly Dictionary<Type, LuaClassType> _classCollection = new Dictionary<Type, LuaClassType>();
		private readonly Dictionary<Type, Type> _delegateCollection = new Dictionary<Type, Type>();

		static CodeGeneration()
		{
		}

		private CodeGeneration()
		{
		}

		/// <summary>
		/// Singleton instance of the class
		/// </summary>
		public static CodeGeneration Instance { get; } = new CodeGeneration();

		/// <summary>
		/// Generates an event handler that calls a Lua function
		/// </summary>
		private Type GenerateEvent(Type eventHandlerType)
		{
			throw new NotImplementedException("Emit not available on .NET Standard ");
		}

		/// <summary>
		/// Generates a type that can be used for instantiating a delegate
		/// of the provided type, given a Lua function.
		/// </summary>
		private Type GenerateDelegate(Type delegateType)
		{
			throw new NotImplementedException("GenerateDelegate is not available on Windows Store, please register your LuaDelegate type with Lua.RegisterLuaDelegateType( yourDelegate, theLuaDelegateHandler) ");
		}

		internal void GetReturnTypesFromClass(Type klass, out Type[][] returnTypes)
		{
			var classMethods = klass.GetMethods();
			returnTypes = new Type[classMethods.Length][];

			int i = 0;

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

		/// <summary>
		/// Generates an implementation of klass, if it is an interface, or
		/// a subclass of klass that delegates its virtual methods to a Lua table.
		/// </summary>
		public void GenerateClass(Type klass, out Type newType, out Type[][] returnTypes)
		{
			throw new NotImplementedException("Emit not available on .NET Standard");
		}

		internal void GetReturnTypesFromMethod(MethodInfo method, out Type[] returnTypes)
		{
			var paramInfo = method.GetParameters();
			var paramTypes = new Type[paramInfo.Length];
			var returnTypesList = new List<Type>();

			// Counts out and ref parameters, for later use, 
			// and creates the list of return types
			int nOutParams = 0;
			int nOutAndRefParams = 0;
			var returnType = method.ReturnType;
			returnTypesList.Add(returnType);

			for (int i = 0; i < paramTypes.Length; i++)
			{
				paramTypes[i] = paramInfo[i].ParameterType;

				if (!paramInfo[i].IsIn && paramInfo[i].IsOut)
				{
					nOutParams++;
				}

				if (paramTypes[i].IsByRef)
				{
					returnTypesList.Add(paramTypes[i].GetElementType());
					nOutAndRefParams++;
				}
			}

			returnTypes = returnTypesList.ToArray();
		}

		/// <summary>
		/// Gets an event handler for the event type that delegates to the eventHandler Lua function.
		/// Caches the generated type.
		/// </summary>
		public LuaEventHandler GetEvent(Type eventHandlerType, LuaFunction eventHandler)
		{
			throw new NotImplementedException("Emit not available on .NET Standard");
		}

		public void RegisterLuaDelegateType(Type delegateType, Type luaDelegateType)
		{
			_delegateCollection[delegateType] = luaDelegateType;
		}

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
			Type luaDelegateType;

			if (_delegateCollection.ContainsKey(delegateType))
			{
				luaDelegateType = _delegateCollection[delegateType];
			}
			else
			{
				luaDelegateType = GenerateDelegate(delegateType);
				_delegateCollection[delegateType] = luaDelegateType;
			}

			var methodInfo = delegateType.GetMethod("Invoke");
			returnTypes.Add(methodInfo.ReturnType);

			foreach (ParameterInfo paramInfo in methodInfo.GetParameters())
			{
				if (paramInfo.ParameterType.IsByRef)
					returnTypes.Add(paramInfo.ParameterType);
			}

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
			LuaClassType luaClassType;

			if (_classCollection.ContainsKey(klass))
				luaClassType = _classCollection[klass];
			else
			{
				luaClassType = default;
				GenerateClass(klass, out luaClassType.klass, out luaClassType.returnTypes);
				_classCollection[klass] = luaClassType;
			}

			return Activator.CreateInstance(luaClassType.klass, new object[] {
				luaTable,
				luaClassType.returnTypes
			});
		}
	}
}
