using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.InteropServices;

using NLua.Method;
using NLua.Exceptions;
using NLua.Extensions;

namespace NLua
{
	public class ObjectTranslator
	{
		// Compare cache entries by exact reference to avoid unwanted aliases
		private class ReferenceComparer : IEqualityComparer<object>
		{
			public new bool Equals(object x, object y)
			{
				if (x != null && y != null && x.GetType() == y.GetType() && x.GetType().IsValueType && y.GetType().IsValueType)
					return x.Equals(y); // Special case for boxed value types
				return ReferenceEquals(x, y);
			}

			public int GetHashCode(object obj)
			{
				return obj.GetHashCode();
			}
		}

		private static readonly LuaNativeFunction _registerTableFunction = RegisterTable;
		private static readonly LuaNativeFunction _unregisterTableFunction = UnregisterTable;
		private static readonly LuaNativeFunction _getMethodSigFunction = GetMethodSignature;
		private static readonly LuaNativeFunction _getConstructorSigFunction = GetConstructorSignature;
		private static readonly LuaNativeFunction _importTypeFunction = ImportType;
		private static readonly LuaNativeFunction _loadAssemblyFunction = LoadAssembly;
		private static readonly LuaNativeFunction _ctypeFunction = CType;
		private static readonly LuaNativeFunction _enumFromIntFunction = EnumFromInt;

		// object to object #
		internal readonly Dictionary<object, int> _objectsBackMap = new Dictionary<object, int>(new ReferenceComparer());
		// object # to object (FIXME - it should be possible to get object address as an object #)
		internal readonly Dictionary<int, object> _objects = new Dictionary<int, object>();

		internal readonly ConcurrentQueue<int> finalizedReferences = new ConcurrentQueue<int>();

		internal EventHandlerContainer PendingEvents = new EventHandlerContainer();
		internal MetaFunctions metaFunctions;
		internal List<Assembly> assemblies;
		internal CheckType typeChecker;
		internal Lua interpreter;

		/// <summary>
		/// We want to ensure that objects always have a unique ID
		/// </summary>
		internal int _nextObj;

		public MetaFunctions MetaFunctionsInstance => metaFunctions;
		public Lua Interpreter => interpreter;
		public IntPtr Tag => _tagPtr;

		internal readonly IntPtr _tagPtr;

		public ObjectTranslator(Lua interpreter, LuaState luaState)
		{
			_tagPtr = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(int)));
			this.interpreter = interpreter;
			typeChecker = new CheckType(this);
			metaFunctions = new MetaFunctions(this);
			assemblies = new List<Assembly>();

			CreateLuaObjectList(luaState);
			CreateIndexingMetaFunction(luaState);
			CreateBaseClassMetatable(luaState);
			CreateClassMetatable(luaState);
			CreateFunctionMetatable(luaState);
			SetGlobalFunctions(luaState);

		}

		/// <summary>
		/// Sets up the list of objects in the Lua side
		/// </summary>
		private void CreateLuaObjectList(LuaState luaState)
		{
			luaState.PushString("luaNet_objects");
			luaState.NewTable();
			luaState.NewTable();
			luaState.PushString("__mode");
			luaState.PushString("v");
			luaState.SetTable(-3);
			luaState.SetMetaTable(-2);
			luaState.SetTable((int)LuaRegistry.Index);
		}

		/// <summary>
		/// Registers the indexing function of CLR objects
		/// passed to Lua
		/// </summary>
		private void CreateIndexingMetaFunction(LuaState luaState)
		{
			luaState.PushString("luaNet_indexfunction");
			luaState.DoString(MetaFunctions.LuaIndexFunction);
			luaState.RawSet(LuaRegistry.Index);
		}

		/// <summary>
		/// Creates the metatable for superclasses (the base
		/// field of registered tables)
		/// </summary>
		/// <param name="luaState"></param>
		private void CreateBaseClassMetatable(LuaState luaState)
		{
			luaState.NewMetaTable("luaNet_searchbase");
			luaState.PushString("__gc");
			luaState.PushCFunction(MetaFunctions.GcFunction);
			luaState.SetTable(-3);
			luaState.PushString("__tostring");
			luaState.PushCFunction(MetaFunctions.ToStringFunction);
			luaState.SetTable(-3);
			luaState.PushString("__index");
			luaState.PushCFunction(MetaFunctions.BaseIndexFunction);
			luaState.SetTable(-3);
			luaState.PushString("__newindex");
			luaState.PushCFunction(MetaFunctions.NewIndexFunction);
			luaState.SetTable(-3);
			luaState.SetTop(-2);
		}

		/// <summary>
		/// Creates the metatable for type references
		/// </summary>
		private void CreateClassMetatable(LuaState luaState)
		{
			luaState.NewMetaTable("luaNet_class");
			luaState.PushString("__gc");
			luaState.PushCFunction(MetaFunctions.GcFunction);
			luaState.SetTable(-3);
			luaState.PushString("__tostring");
			luaState.PushCFunction(MetaFunctions.ToStringFunction);
			luaState.SetTable(-3);
			luaState.PushString("__index");
			luaState.PushCFunction(MetaFunctions.ClassIndexFunction);
			luaState.SetTable(-3);
			luaState.PushString("__newindex");
			luaState.PushCFunction(MetaFunctions.ClassNewIndexFunction);
			luaState.SetTable(-3);
			luaState.PushString("__call");
			luaState.PushCFunction(MetaFunctions.CallConstructorFunction);
			luaState.SetTable(-3);
			luaState.SetTop(-2);
		}

		/// <summary>
		/// Registers the global functions used by NLua
		/// </summary>
		private void SetGlobalFunctions(LuaState luaState)
		{
			luaState.PushCFunction(MetaFunctions.IndexFunction);
			luaState.SetGlobal("get_object_member");
			luaState.PushCFunction(_importTypeFunction);
			luaState.SetGlobal("import_type");
			luaState.PushCFunction(_loadAssemblyFunction);
			luaState.SetGlobal("load_assembly");
			luaState.PushCFunction(_registerTableFunction);
			luaState.SetGlobal("make_object");
			luaState.PushCFunction(_unregisterTableFunction);
			luaState.SetGlobal("free_object");
			luaState.PushCFunction(_getMethodSigFunction);
			luaState.SetGlobal("get_method_bysig");
			luaState.PushCFunction(_getConstructorSigFunction);
			luaState.SetGlobal("get_constructor_bysig");
			luaState.PushCFunction(_ctypeFunction);
			luaState.SetGlobal("ctype");
			luaState.PushCFunction(_enumFromIntFunction);
			luaState.SetGlobal("enum");
		}

		/// <summary>
		/// Creates the metatable for delegates
		/// </summary>
		private void CreateFunctionMetatable(LuaState luaState)
		{
			luaState.NewMetaTable("luaNet_function");
			luaState.PushString("__gc");
			luaState.PushCFunction(MetaFunctions.GcFunction);
			luaState.SetTable(-3);
			luaState.PushString("__call");
			luaState.PushCFunction(MetaFunctions.ExecuteDelegateFunction);
			luaState.SetTable(-3);
			luaState.SetTop(-2);
		}

		/// <summary>
		/// Passes errors (argument e) to the Lua interpreter
		/// </summary>
		internal void ThrowError(LuaState luaState, object e)
		{
			// We use this to remove anything pushed by luaL_where
			int oldTop = luaState.GetTop();

			// Stack frame #1 is our C# wrapper, so not very interesting to the user
			// Stack frame #2 must be the lua code that called us, so that's what we want to use
			luaState.Where(1);
			var curlev = PopValues(luaState, oldTop);

			// Determine the position in the script where the exception was triggered
			string errLocation = string.Empty;

			if (curlev.Length > 0)
				errLocation = curlev[0].ToString();

			string message = e as string;

			if (message != null)
			{
				// Wrap Lua error (just a string) and store the error location
				if (interpreter.UseTraceback) 
					message += Environment.NewLine + interpreter.GetDebugTraceback();
				e = new LuaScriptException(message, errLocation);
			}
			else
			{
				var ex = e as Exception;

				if (ex != null)
				{
					// Wrap generic .NET exception as an InnerException and store the error location
					if (interpreter.UseTraceback) ex.Data["Traceback"] = interpreter.GetDebugTraceback();
					e = new LuaScriptException(ex, errLocation);
				}
			}

			Push(luaState, e);
		}

		/// <summary>
		/// Implementation of load_assembly. Throws an error
		/// if the assembly is not found.
		/// </summary>
		private static int LoadAssembly(IntPtr luaState)
		{
			var state = LuaState.FromIntPtr(luaState);
			var translator = ObjectTranslatorPool.Instance.Find(state);
			int result = translator.LoadAssemblyInternal(state);
			var exception = translator.GetObject(state, -1) as LuaScriptException;

			if (exception != null)
				return state.Error();
			return result;
		}

		private int LoadAssemblyInternal(LuaState luaState)
		{
			try
			{
				string assemblyName = luaState.ToString(1, false);
				Assembly assembly = null;
				Exception exception = null;

				try
				{
					assembly = Assembly.Load(assemblyName);
				}
				catch (BadImageFormatException)
				{
					// The assemblyName was invalid.  It is most likely a path.
				}
				catch (FileNotFoundException e)
				{
					exception = e;
				}

				if (assembly == null)
				{
					try
					{
						assembly = Assembly.Load(AssemblyName.GetAssemblyName(assemblyName));
					}
					catch (FileNotFoundException e)
					{
						exception = e;
					}
					if (assembly == null)
					{
						AssemblyName mscor = assemblies[0].GetName();
						AssemblyName name = new AssemblyName();
						name.Name = assemblyName;
						name.CultureInfo = mscor.CultureInfo;
						name.Version = mscor.Version;
						name.SetPublicKeyToken(mscor.GetPublicKeyToken());
						name.SetPublicKey(mscor.GetPublicKey());
						assembly = Assembly.Load(name);

						if (assembly != null)
							exception = null;
					}
					if (exception != null)
					{
						ThrowError(luaState, exception);
						return 1;
					}
				}
				if (assembly != null && !assemblies.Contains(assembly))
					assemblies.Add(assembly);
			}
			catch (Exception e)
			{
				ThrowError(luaState, e);
				return 1;
			}
			return 0;
		}

		internal Type FindType(string className)
		{
			foreach (var assembly in assemblies)
			{
				var klass = assembly.GetType(className);

				if (klass != null)
					return klass;
			}
			return null;
		}

		public bool TryGetExtensionMethod(Type type, string name, out MethodInfo method)
		{
			method = GetExtensionMethod(type, name);
			return method != null;
		}

		public MethodInfo GetExtensionMethod(Type type, string name)
		{
			return type.GetExtensionMethod(name, assemblies);
		}

		/// <summary>
		/// Implementation of import_type. Returns nil if the
		/// type is not found.
		/// </summary>
		private static int ImportType(IntPtr luaState)
		{
			var state = LuaState.FromIntPtr(luaState);
			var translator = ObjectTranslatorPool.Instance.Find(state);
			return translator.ImportTypeInternal(state);
		}

		private int ImportTypeInternal(LuaState luaState)
		{
			string className = luaState.ToString(1, false);
			var klass = FindType(className);

			if (klass != null)
				PushType(luaState, klass);
			else
				luaState.PushNil();

			return 1;
		}

		/// <summary>
		/// Implementation of make_object. Registers a table (first
		/// argument in the stack) as an object subclassing the
		/// type passed as second argument in the stack.
		/// </summary>
		private static int RegisterTable(IntPtr luaState)
		{
			var state = LuaState.FromIntPtr(luaState);
			var translator = ObjectTranslatorPool.Instance.Find(state);
			int result = translator.RegisterTableInternal(state);
			var exception = translator.GetObject(state, -1) as LuaScriptException;

			if (exception != null)
				return state.Error();
			return result;
		}

		private int RegisterTableInternal(LuaState luaState)
		{
			if (luaState.Type(1) != LuaType.Table)
			{
				ThrowError(luaState, "register_table: first arg is not a table");
				return 1;
			}

			LuaTable luaTable = GetTable(luaState, 1);
			string superclassName = luaState.ToString(2, false);

			if (string.IsNullOrEmpty(superclassName))
			{
				ThrowError(luaState, "register_table: superclass name can not be null");
				return 1;
			}

			var klass = FindType(superclassName);

			if (klass == null)
			{
				ThrowError(luaState, "register_table: can not find superclass '" + superclassName + "'");
				return 1;
			}

			// Creates and pushes the object in the stack, setting
			// it as the  metatable of the first argument
			object obj = CodeGeneration.Instance.GetClassInstance(klass, luaTable);
			PushObject(luaState, obj, "luaNet_metatable");
			luaState.NewTable();
			luaState.PushString("__index");
			luaState.PushCopy(-3);
			luaState.SetTable(-3);
			luaState.PushString("__newindex");
			luaState.PushCopy(-3);
			luaState.SetTable(-3);
			luaState.SetMetaTable(1);

			// Pushes the object again, this time as the base field
			// of the table and with the luaNet_searchbase metatable
			luaState.PushString("base");
			int index = AddObject(obj);
			PushNewObject(luaState, obj, index, "luaNet_searchbase");
			luaState.RawSet(1);

			return 0;
		}

		/// <summary>
		/// Implementation of free_object. Clears the metatable and the
		/// base field, freeing the created object for garbage-collection
		/// </summary>
		private static int UnregisterTable(IntPtr luaState)
		{
			var state = LuaState.FromIntPtr(luaState);
			var translator = ObjectTranslatorPool.Instance.Find(state);
			int result = translator.UnregisterTableInternal(state);
			var exception = translator.GetObject(state, -1) as LuaScriptException;

			if (exception != null)
				return state.Error();
			return result;
		}

		private int UnregisterTableInternal(LuaState luaState)
		{

			if (!luaState.GetMetaTable(1))
			{
				ThrowError(luaState, "unregister_table: arg is not valid table");
				return 1;
			}

			luaState.PushString("__index");
			luaState.GetTable(-2);
			object obj = GetRawNetObject(luaState, -1);

			if (obj == null)
			{
				ThrowError(luaState, "unregister_table: arg is not valid table");
				return 1;
			}

			var luaTableField = obj.GetType().GetField("__luaInterface_luaTable");

			if (luaTableField == null)
			{
				ThrowError(luaState, "unregister_table: arg is not valid table");
				return 1;
			}

			// ReSharper disable once PossibleNullReferenceException
			luaTableField.SetValue(obj, null);
			luaState.PushNil();
			luaState.SetMetaTable(1);
			luaState.PushString("base");
			luaState.PushNil();
			luaState.SetTable(1);

			return 0;
		}

		/// <summary>
		/// Implementation of get_method_bysig. Returns nil
		/// if no matching method is not found.
		/// </summary>
		private static int GetMethodSignature(IntPtr luaState)
		{
			var state = LuaState.FromIntPtr(luaState);
			var translator = ObjectTranslatorPool.Instance.Find(state);
			int result = translator.GetMethodSignatureInternal(state);
			var exception = translator.GetObject(state, -1) as LuaScriptException;

			if (exception != null)
				return state.Error();
			return result;
		}

		private int GetMethodSignatureInternal(LuaState luaState) //-V3009
		{
			ProxyType klass;
			object target;
			int udata = luaState.CheckUObject(1, "luaNet_class");

			if (udata != -1)
			{
				klass = (ProxyType)_objects[udata];
				target = null;
			}
			else
			{
				target = GetRawNetObject(luaState, 1);

				if (target == null)
				{
					ThrowError(luaState, "get_method_bysig: first arg is not type or object reference");
					return 1;
				}

				klass = new ProxyType(target.GetType());
			}

			string methodName = luaState.ToString(2, false);
			var signature = new Type[luaState.GetTop() - 2];

			for (int i = 0; i < signature.Length; i++)
				signature[i] = FindType(luaState.ToString(i + 3, false));

			try
			{
				var method = klass.GetMethod(methodName, BindingFlags.Public | BindingFlags.Static |
					BindingFlags.Instance, signature);
				var wrapper = new LuaMethodWrapper(this, target, klass, method);
				LuaNativeFunction invokeDelegate = wrapper.InvokeFunction;
				PushFunction(luaState, invokeDelegate);
			}
			catch (Exception e)
			{
				ThrowError(luaState, e);
			}

			return 1;
		}

		/// <summary>
		/// Implementation of get_constructor_bysig. Returns nil
		/// if no matching constructor is found.
		/// </summary>
		/// <param name="luaState"></param>
		/// <returns></returns>
		private static int GetConstructorSignature(IntPtr luaState)
		{
			var state = LuaState.FromIntPtr(luaState);
			var translator = ObjectTranslatorPool.Instance.Find(state);
			int result = translator.GetConstructorSignatureInternal(state);
			var exception = translator.GetObject(state, -1) as LuaScriptException;

			if (exception != null)
				return state.Error();
			return result;
		}

		private int GetConstructorSignatureInternal(LuaState luaState) //-V3009
		{
			ProxyType klass = null;
			int udata = luaState.CheckUObject(1, "luaNet_class");

			if (udata != -1)
				klass = (ProxyType)_objects[udata];

			if (klass == null)
			{ 
				ThrowError(luaState, "get_constructor_bysig: first arg is invalid type reference");
				return 1;
			}

			var signature = new Type[luaState.GetTop() - 1];

			for (int i = 0; i < signature.Length; i++)
				signature[i] = FindType(luaState.ToString(i + 2, false));

			try
			{
				ConstructorInfo constructor = klass.UnderlyingSystemType.GetConstructor(signature);
				var wrapper = new LuaMethodWrapper(this, null, klass, constructor);
				var invokeDelegate = wrapper.InvokeFunction;
				PushFunction(luaState, invokeDelegate);
			}
			catch (Exception e)
			{
				ThrowError(luaState, e);
			}
			return 1;
		}

		/// <summary>
		/// Pushes a type reference into the stack
		/// </summary>
		internal void PushType(LuaState luaState, Type t)
		{
			PushObject(luaState, new ProxyType(t), "luaNet_class");
		}

		/// <summary>
		/// Pushes a delegate into the stack
		/// </summary>
		internal void PushFunction(LuaState luaState, LuaNativeFunction func)
		{
			PushObject(luaState, func, "luaNet_function");
		}

		/// <summary>
		/// Pushes a CLR object into the Lua stack as an userdata
		/// with the provided metatable
		/// </summary>
		internal void PushObject(LuaState luaState, object o, string metatable)
		{
			int index = -1;

			// Pushes nil
			if (o == null)
			{
				luaState.PushNil();
				return;
			}

			// Object already in the list of Lua objects? Push the stored reference.
			bool found = (!o.GetType().IsValueType || o.GetType().IsEnum) && _objectsBackMap.TryGetValue(o, out index);

			if (found)
			{
				luaState.GetMetaTable("luaNet_objects");
				luaState.RawGetInteger(-1, index);

				// Note: starting with lua5.1 the garbage collector may remove weak reference items (such as our luaNet_objects values) when the initial GC sweep 
				// occurs, but the actual call of the __gc finalizer for that object may not happen until a little while later.  During that window we might call
				// this routine and find the element missing from luaNet_objects, but collectObject() has not yet been called.  In that case, we go ahead and call collect
				// object here
				// did we find a non nil object in our table? if not, we need to call collect object
				var type = luaState.Type(-1);
				if (type != LuaType.Nil)
				{
					luaState.Remove(-2); // drop the metatable - we're going to leave our object on the stack
					return;
				}

				// MetaFunctions.dumpStack(this, luaState);
				luaState.Remove(-1); // remove the nil object value
				luaState.Remove(-1); // remove the metatable
				CollectObject(o, index); // Remove from both our tables and fall out to get a new ID
			}

			index = AddObject(o);
			PushNewObject(luaState, o, index, metatable);
		}

		/// <summary>
		/// Pushes a new object into the Lua stack with the provided
		/// metatable
		/// </summary>
		private void PushNewObject(LuaState luaState, object o, int index, string metatable)
		{
			if (metatable == "luaNet_metatable")
			{
				// Gets or creates the metatable for the object's type
				luaState.GetMetaTable(o.GetType().AssemblyQualifiedName);

				if (luaState.IsNil(-1))
				{
					luaState.SetTop(-2);
					luaState.NewMetaTable(o.GetType().AssemblyQualifiedName);
					luaState.PushString("cache");
					luaState.NewTable();
					luaState.RawSet(-3);
					luaState.PushLightUserData(_tagPtr);
					luaState.PushNumber(1);
					luaState.RawSet(-3);
					luaState.PushString("__index");
					luaState.PushString("luaNet_indexfunction");
					luaState.RawGet(LuaRegistry.Index);
					luaState.RawSet(-3);
					luaState.PushString("__gc");
					luaState.PushCFunction(MetaFunctions.GcFunction);
					luaState.RawSet(-3);
					luaState.PushString("__tostring");
					luaState.PushCFunction(MetaFunctions.ToStringFunction);
					luaState.RawSet(-3);
					luaState.PushString("__newindex");
					luaState.PushCFunction(MetaFunctions.NewIndexFunction);
					luaState.RawSet(-3);
					// Bind C# operator with Lua metamethods (__add, __sub, __mul)
					RegisterOperatorsFunctions(luaState, o.GetType());
					RegisterCallMethodForDelegate(luaState, o);
				}
			}
			else
				luaState.GetMetaTable(metatable);

			// Stores the object index in the Lua list and pushes the
			// index into the Lua stack
			luaState.GetMetaTable("luaNet_objects");
			luaState.NewUData(index);
			luaState.PushCopy(-3);
			luaState.Remove(-4);
			luaState.SetMetaTable(-2);
			luaState.PushCopy(-1);
			luaState.RawSetInteger(-3, index);
			luaState.Remove(-2);
		}

		internal void RegisterCallMethodForDelegate(LuaState luaState, object o)
		{
			if (!(o is Delegate))
				return;

			luaState.PushString("__call");
			luaState.PushCFunction(MetaFunctions.CallDelegateFunction);
			luaState.RawSet(-3);
		}

		internal void RegisterOperatorsFunctions(LuaState luaState, Type type)
		{
			if (type.HasAdditionOperator())
			{
				luaState.PushString("__add");
				luaState.PushCFunction(MetaFunctions.AddFunction);
				luaState.RawSet(-3);
			}
			if (type.HasSubtractionOperator())
			{
				luaState.PushString("__sub");
				luaState.PushCFunction(MetaFunctions.SubtractFunction);
				luaState.RawSet(-3);
			}
			if (type.HasMultiplyOperator())
			{
				luaState.PushString("__mul");
				luaState.PushCFunction(MetaFunctions.MultiplyFunction);
				luaState.RawSet(-3);
			}
			if (type.HasDivisionOperator())
			{
				luaState.PushString("__div");
				luaState.PushCFunction(MetaFunctions.DivisionFunction);
				luaState.RawSet(-3);
			}
			if (type.HasModulusOperator())
			{
				luaState.PushString("__mod");
				luaState.PushCFunction(MetaFunctions.ModulosFunction);
				luaState.RawSet(-3);
			}
			if (type.HasUnaryNegationOperator())
			{
				luaState.PushString("__unm");
				luaState.PushCFunction(MetaFunctions.UnaryNegationFunction);
				luaState.RawSet(-3);
			}
			if (type.HasEqualityOperator())
			{
				luaState.PushString("__eq");
				luaState.PushCFunction(MetaFunctions.EqualFunction);
				luaState.RawSet(-3);
			}
			if (type.HasLessThanOperator())
			{
				luaState.PushString("__lt");
				luaState.PushCFunction(MetaFunctions.LessThanFunction);
				luaState.RawSet(-3);
			}
			if (type.HasLessThanOrEqualOperator())
			{
				luaState.PushString("__le");
				luaState.PushCFunction(MetaFunctions.LessThanOrEqualFunction);
				luaState.RawSet(-3);
			}
		}

		/// <summary>
		/// Gets an object from the Lua stack with the desired type, if it matches, otherwise
		/// returns null.
		/// </summary>
		internal object GetAsType(LuaState luaState, int stackPos, Type paramType)
		{
			var extractor = typeChecker.CheckLuaType(luaState, stackPos, paramType);
			return extractor != null ? extractor(luaState, stackPos) : null;
		}

		/// <summary>
		/// Given the Lua int ID for an object remove it from our maps
		/// </summary>
		/// <param name = "udata"></param>
		internal void CollectObject(int udata)
		{
			object o;
			bool found = _objects.TryGetValue(udata, out o);

			// The other variant of collectObject might have gotten here first, in that case we will silently ignore the missing entry
			if (found)
				CollectObject(o, udata);
		}

		/// <summary>
		/// Given an object reference, remove it from our maps
		/// </summary>
		/// <param name = "o"></param>
		/// <param name = "udata"></param>
		private void CollectObject(object o, int udata)
		{
			_objects.Remove(udata);
			if (!o.GetType().IsValueType || o.GetType().IsEnum)
				_objectsBackMap.Remove(o);
		}

		private int AddObject(object obj)
		{
			// New object: inserts it in the list
			int index = _nextObj++;
			_objects[index] = obj;

			if (!obj.GetType().IsValueType || obj.GetType().IsEnum)
				_objectsBackMap[obj] = index;

			return index;
		}

		/// <summary>
		/// Gets an object from the Lua stack according to its Lua type.
		/// </summary>
		internal object GetObject(LuaState luaState, int index)
		{
			LuaType type = luaState.Type(index);

			switch (type)
			{
				case LuaType.Number:
					{
						if (luaState.IsInteger(index))
							return luaState.ToInteger(index);

						return luaState.ToNumber(index);
					}
				case LuaType.String:
						return luaState.ToString(index, false);
				case LuaType.Boolean:
						return luaState.ToBoolean(index);
				case LuaType.Table:
						return GetTable(luaState, index);
				case LuaType.Function:
						return GetFunction(luaState, index);
				case LuaType.UserData:
					{
						int udata = luaState.ToNetObject(index, Tag);
						return udata != -1 ? _objects[udata] : GetUserData(luaState, index);
					}
				case LuaType.Thread:
						return GetThread(luaState, index);
				default:
						return null;
			}
		}

		/// <summary>
		/// Gets the table in the index positon of the Lua stack.
		/// </summary>
		internal LuaTable GetTable(LuaState luaState, int index)
		{
			// Before create new tables, check if there is any finalized object to clean.
			CleanFinalizedReferences(luaState);

			luaState.PushCopy(index);
			int reference = luaState.Ref(LuaRegistry.Index);
			if (reference == -1)
				return null;
			return new LuaTable(reference, interpreter);
		}

		/// <summary>
		/// Gets the thread in the index positon of the Lua stack.
		/// </summary>
		internal LuaThread GetThread(LuaState luaState, int index)
		{
			// Before create new tables, check if there is any finalized object to clean.
			CleanFinalizedReferences(luaState);

			luaState.PushCopy(index);
			int reference = luaState.Ref(LuaRegistry.Index);
			if (reference == -1)
				return null;
			return new LuaThread(reference, interpreter);
		}

		/// <summary>
		/// Gets the userdata in the index positon of the Lua stack.
		/// </summary>
		internal LuaUserData GetUserData(LuaState luaState, int index)
		{
			// Before create new tables, check if there is any finalized object to clean.
			CleanFinalizedReferences(luaState);

			luaState.PushCopy(index);
			int reference = luaState.Ref(LuaRegistry.Index);
			if (reference == -1)
				return null;
			return new LuaUserData(reference, interpreter);
		}

		/// <summary>
		/// Gets the function in the index positon of the Lua stack.
		/// </summary>
		internal LuaFunction GetFunction(LuaState luaState, int index)
		{
			// Before create new tables, check if there is any finalized object to clean.
			CleanFinalizedReferences(luaState);

			luaState.PushCopy(index);
			int reference = luaState.Ref(LuaRegistry.Index);
			if (reference == -1)
				return null;
			return new LuaFunction(reference, interpreter);
		}

		/// <summary>
		/// Gets the CLR object in the index positon of the Lua stack. Returns
		/// delegates as Lua functions.
		/// </summary>
		internal object GetNetObject(LuaState luaState, int index)
		{
			int idx = luaState.ToNetObject(index, Tag);
			return idx != -1 ? _objects[idx] : null;
		}

		/// <summary>
		/// Gets the CLR object in the index position of the Lua stack. Returns
		/// delegates as is.
		/// </summary>
		internal object GetRawNetObject(LuaState luaState, int index)
		{
			int udata = luaState.RawNetObj(index);
			return udata != -1 ? _objects[udata] : null;
		}

		/// <summary>
		/// Gets the values from the provided index to
		/// the top of the stack and returns them in an array.
		/// </summary>
		internal object[] PopValues(LuaState luaState, int oldTop)
		{
			int newTop = luaState.GetTop();

			if (oldTop == newTop)
				return Array.Empty<object>();

			var returnValues = new List<object>();
			for (int i = oldTop + 1; i <= newTop; i++)
				returnValues.Add(GetObject(luaState, i));

			luaState.SetTop(oldTop);
			return returnValues.ToArray();
		}

		/// <summary>
		/// Gets the values from the provided index to
		/// the top of the stack and returns them in an array, casting
		/// them to the provided types.
		/// </summary>
		internal object[] PopValues(LuaState luaState, int oldTop, Type[] popTypes)
		{
			int newTop = luaState.GetTop();

			if (oldTop == newTop)
				return Array.Empty<object>();

			int iTypes;
			var returnValues = new List<object>();

			if (popTypes[0] == typeof(void))
				iTypes = 1;
			else
				iTypes = 0;

			for (int i = oldTop + 1; i <= newTop; i++)
			{
				returnValues.Add(GetAsType(luaState, i, popTypes[iTypes]));
				iTypes++;
			}

			luaState.SetTop(oldTop);
			return returnValues.ToArray();
		}

		// The following line doesn't work for remoting proxies - they always return a match for 'is'
		// else if (o is ILuaGeneratedType)
		private static bool IsILua(object o)
		{
			if (o is ILuaGeneratedType)
			{
				// Make sure we are _really_ ILuaGenerated
				var typ = o.GetType();
				return typ.GetInterface("ILuaGeneratedType", true) != null;
			}
			return false;
		}

		/// <summary>
		/// Pushes the object into the Lua stack according to its type.
		/// </summary>
		internal void Push(LuaState luaState, object o)
		{
			if (o == null)
				luaState.PushNil();
			else if (o is sbyte sb)
				luaState.PushInteger(sb);
			else if(o is byte bt)
				luaState.PushInteger(bt);
			else if(o is short s)
				luaState.PushInteger(s);
			else if (o is ushort us)
				luaState.PushInteger(us);
			else if (o is int i)
				luaState.PushInteger(i);
			else if (o is uint ui)
				luaState.PushInteger(ui);
			else if (o is long l)
				luaState.PushInteger(l);
			else if (o is ulong ul)
				luaState.PushInteger((long)ul);
			else if (o is char ch)
				luaState.PushInteger(ch);
			else if (o is float fl)
				luaState.PushNumber(fl);
			else if(o is decimal dc)
				luaState.PushNumber((double)dc);
			else if(o is double db)
				luaState.PushNumber(db);
			else if (o is string str)
				luaState.PushString(str);
			else if (o is bool b)
				luaState.PushBoolean(b);
			else if (IsILua(o))
				((ILuaGeneratedType)o).LuaInterfaceGetLuaTable().Push(luaState);
			else if (o is LuaTable table)
				table.Push(luaState);
			else if (o is LuaThread thread)
				thread.Push(luaState);
			else if (o is LuaNativeFunction nativeFunction)
				PushFunction(luaState, nativeFunction);
			else if (o is LuaFunction luaFunction)
				luaFunction.Push(luaState);
			else if (o is LuaUserData userData)
				userData.Push(luaState);
			else
				PushObject(luaState, o, "luaNet_metatable");
		}

		/// <summary>
		/// Checks if the method matches the arguments in the Lua stack, getting
		/// the arguments if it does.
		/// </summary>
		internal bool MatchParameters(LuaState luaState, MethodBase method, MethodCache methodCache, int skipParam)
		{
			return metaFunctions.MatchParameters(luaState, method, methodCache, skipParam);
		}

		internal Array TableToArray(LuaState luaState, ExtractValue extractValue, Type paramArrayType, int startIndex, int count)
		{
			return metaFunctions.TableToArray(luaState, extractValue, paramArrayType, ref startIndex, count);
		}

		private Type TypeOf(LuaState luaState, int idx)
		{
			int udata = luaState.CheckUObject(idx, "luaNet_class");
			if (udata == -1)
				return null;

			var pt = (ProxyType)_objects[udata];
			return pt.UnderlyingSystemType;
		}

		internal static int PushError(LuaState luaState, string msg)
		{
			luaState.PushNil();
			luaState.PushString(msg);
			return 2;
		}

		private static int CType(IntPtr luaState)
		{
			var state = LuaState.FromIntPtr(luaState);
			var translator = ObjectTranslatorPool.Instance.Find(state);
			return translator.CTypeInternal(state);
		}

		internal int CTypeInternal(LuaState luaState)
		{
			Type t = TypeOf(luaState,1);
			if (t == null)
				return PushError(luaState, "Not a CLR Class");

			PushObject(luaState, t, "luaNet_metatable");
			return 1;
		}

		private static int EnumFromInt(IntPtr luaState)
		{
			var state = LuaState.FromIntPtr(luaState);
			var translator = ObjectTranslatorPool.Instance.Find(state);
			return translator.EnumFromIntInternal(state);
		}

		internal int EnumFromIntInternal(LuaState luaState)
		{
			Type t = TypeOf(luaState, 1);
			if (t == null || !t.IsEnum)
				return PushError(luaState, "Not an Enum.");

			object res = null;
			LuaType lt = luaState.Type(2);
			if (lt == LuaType.Number)
			{
				int ival = (int)luaState.ToNumber(2);
				res = Enum.ToObject(t, ival);
			}
			else if (lt == LuaType.String)
			{
				string sflags = luaState.ToString(2, false);
				string err = null;
				try
				{
					res = Enum.Parse(t, sflags, true);
				}
				catch (ArgumentException e)
				{
					err = e.Message;
				}
				if (err != null)
					return PushError(luaState, err);
			}
			else
			{
				return PushError(luaState, "Second argument must be a integer or a string.");
			}
			PushObject(luaState, res, "luaNet_metatable");
			return 1;
		}

		internal void AddFinalizedReference(int reference)
		{
			finalizedReferences.Enqueue(reference);
		}

		internal void CleanFinalizedReferences(LuaState state)
		{
			if (finalizedReferences.Count == 0)
				return;

			int reference;

			while (finalizedReferences.TryDequeue(out reference))
				state.Unref(LuaRegistry.Index, reference);
		}
	}
}
