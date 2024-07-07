using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using NLua.Exceptions;
using NLua.Extensions;
using NLua.GenerateEventAssembly;
using NLua.Method;
using NLua.Native;

// ReSharper disable UnusedMember.Global

namespace NLua
{
	// ReSharper disable once ClassNeverInstantiated.Global
	public class Lua : IDisposable
	{
		// We need to keep this in a managed reference so the delegate doesn't get garbage collected
		private static readonly LuaNativeFunction _panicCallback = PanicCallback;

		private readonly LuaGlobals _globals = new();

		/// <summary>
		/// True while a script is being executed
		/// </summary>
		// ReSharper disable once UnusedAutoPropertyAccessor.Global
		public bool IsExecuting { get; private set; }

		internal LuaState State { get; private set; }

		internal ObjectTranslator Translator { get; private set; }

		// The commented code bellow is the initLua, the code assigned here is minified for size/performance reasons.
		private const string InitLuanet = @"local a={}local rawget=rawget;local b=luanet.import_type;local c=luanet.load_assembly;luanet.error,luanet.type=error,type;function a:__index(d)local e=rawget(self,'.fqn')e=(e and e..'.'or'')..d;local f=rawget(luanet,d)or b(e)if f==nil then pcall(c,e)f={['.fqn']=e}setmetatable(f,a)end;rawset(self,d,f)return f end;function a:__call(...)error('No such type: '..rawget(self,'.fqn'),2)end;luanet['.fqn']=false;setmetatable(luanet,a)luanet.load_assembly('mscorlib')";
		//@"local metatable = {}
		//       local rawget = rawget
		//       local import_type = luanet.import_type
		//       local load_assembly = luanet.load_assembly
		//       luanet.error, luanet.type = error, type
		//       -- Lookup a .NET identifier component.
		//       function metatable:__index(key) -- key is e.g. 'Form'
		//           -- Get the fully-qualified name, e.g. 'System.Windows.Forms.Form'
		//           local fqn = rawget(self,'.fqn')
		//           fqn = ((fqn and fqn .. '.') or '') .. key

		//           -- Try to find either a luanet function or a CLR type
		//           local obj = rawget(luanet,key) or import_type(fqn)

		//           -- If key is neither a luanet function or a CLR type, then it is simply
		//           -- an identifier component.
		//           if obj == nil then
		//               -- It might be an assembly, so we load it too.
		//               pcall(load_assembly,fqn)
		//               obj = { ['.fqn'] = fqn }
		//               setmetatable(obj, metatable)
		//           end

		//           -- Cache this lookup
		//           rawset(self, key, obj)
		//           return obj
		//       end

		//       -- A non-type has been called; e.g. foo = System.Foo()
		//       function metatable:__call(...)
		//           error('No such type: ' .. rawget(self,'.fqn'), 2)
		//       end

		//       -- This is the root of the .NET namespace
		//       luanet['.fqn'] = false
		//       setmetatable(luanet, metatable)

		//       -- Preload the mscorlib assembly
		//       luanet.load_assembly('mscorlib')";

		private const string ClrPackage = @"if not luanet then require'luanet'end;local a,b=luanet.import_type,luanet.load_assembly;local c={__index=function(d,e)local f=rawget(d,e)if f==nil then f=a(d.packageName.."".""..e)if f==nil then f=a(e)end;d[e]=f end;return f end}function luanet.namespace(g)if type(g)=='table'then local h={}for i=1,#g do h[i]=luanet.namespace(g[i])end;return unpack(h)end;local j={packageName=g}setmetatable(j,c)return j end;local k,l;local function m()l={}k={__index=function(n,e)for i,d in ipairs(l)do local f=d[e]if f then _G[e]=f;return f end end end}setmetatable(_G,k)end;function CLRPackage(o,p)p=p or o;local q=pcall(b,o)return luanet.namespace(p)end;function import(o,p)if not k then m()end;if not p then local i=o:find('%.dll$')if i then p=o:sub(1,i-1)else p=o end end;local j=CLRPackage(o,p)table.insert(l,j)return j end;function luanet.make_array(r,s)local t=r[#s]for i,u in ipairs(s)do t:SetValue(u,i-1)end;return t end;function luanet.each(v)local w=v:GetEnumerator()return function()if w:MoveNext()then return w.Current end end end";
		//@"---
		//--- This lua module provides auto importing of .net classes into a named package.
		//--- Makes for super easy use of LuaInterface glue
		//---
		//--- example:
		//---   Threading = CLRPackage(""System"", ""System.Threading"")
		//---   Threading.Thread.Sleep(100)
		//---
		//--- Extensions:
		//--- import() is a version of CLRPackage() which puts the package into a list which is used by a global __index lookup,
		//--- and thus works rather like C#'s using statement. It also recognizes the case where one is importing a local
		//--- assembly, which must end with an explicit .dll extension.

		//--- Alternatively, luanet.namespace can be used for convenience without polluting the global namespace:
		//---   local sys,sysi = luanet.namespace {'System','System.IO'}
		//--    sys.Console.WriteLine(""we are at {0}"",sysi.Directory.GetCurrentDirectory())


		//-- LuaInterface hosted with stock Lua interpreter will need to explicitly require this...
		//if not luanet then require 'luanet' end

		//local import_type, load_assembly = luanet.import_type, luanet.load_assembly

		//local mt = {
		//    --- Lookup a previously unfound class and add it to our table
		//    __index = function(package, classname)
		//        local class = rawget(package, classname)
		//        if class == nil then
		//            class = import_type(package.packageName .. ""."" .. classname)
		//            if class == nil then class = import_type(classname) end
		//            package[classname] = class		-- keep what we found around, so it will be shared
		//        end
		//        return class
		//    end
		//}

		//function luanet.namespace(ns)
		//    if type(ns) == 'table' then
		//        local res = {}
		//        for i = 1,#ns do
		//            res[i] = luanet.namespace(ns[i])
		//        end
		//        return unpack(res)
		//    end
		//    -- FIXME - table.packageName could instead be a private index (see Lua 13.4.4)
		//    local t = { packageName = ns }
		//    setmetatable(t,mt)
		//    return t
		//end

		//local globalMT, packages

		//local function set_global_mt()
		//    packages = {}
		//    globalMT = {
		//        __index = function(T,classname)
		//                for i,package in ipairs(packages) do
		//                    local class = package[classname]
		//                    if class then
		//                        _G[classname] = class
		//                        return class
		//                    end
		//                end
		//        end
		//    }
		//    setmetatable(_G, globalMT)
		//end

		//--- Create a new Package class
		//function CLRPackage(assemblyName, packageName)
		//  -- a sensible default...
		//  packageName = packageName or assemblyName
		//  local ok = pcall(load_assembly,assemblyName)			-- Make sure our assembly is loaded
		//  return luanet.namespace(packageName)
		//end

		//function import (assemblyName, packageName)
		//    if not globalMT then
		//        set_global_mt()
		//    end
		//    if not packageName then
		//        local i = assemblyName:find('%.dll$')
		//        if i then packageName = assemblyName:sub(1,i-1)
		//        else packageName = assemblyName end
		//    end
		//    local t = CLRPackage(assemblyName,packageName)
		//    table.insert(packages,t)
		//    return t
		//end


		//function luanet.make_array (tp,tbl)
		//    local arr = tp[#tbl]
		//    for i,v in ipairs(tbl) do
		//        arr:SetValue(v,i-1)
		//    end
		//    return arr
		//end

		//function luanet.each(o)
		//   local e = o:GetEnumerator()
		//   return function()
		//      if e:MoveNext() then
		//        return e.Current
		//     end
		//   end
		//end
		//";

		public bool UseTraceback { get; set; } = false;

		/// <summary>
		/// The maximum number of recursive steps to take when adding global reference variables.  Defaults to 2.
		/// </summary>
		public int MaximumRecursion
		{
			get => _globals.MaximumRecursion;
			set => _globals.MaximumRecursion = value;
		}

		/// <summary>
		/// An alphabetically sorted list of all globals (objects, methods, etc.) externally added to this Lua instance
		/// </summary>
		/// <remarks>Members of globals are also listed. The formatting is optimized for text input auto-completion.</remarks>
		public IEnumerable<string> Globals => _globals.Globals;

		/// <summary>
		/// Get the thread object of this state.
		/// </summary>
		public LuaThread Thread
		{
			get
			{
				var oldTop = State.GetTop();
				State.PushThread();
				var returnValue = Translator.GetObject(State, -1);

				State.SetTop(oldTop);
				return (LuaThread)returnValue;
			}
		}

		/// <summary>
		/// Get the main thread object
		/// </summary>
		public LuaThread MainThread
		{
			get
			{
				var mainThread = State.MainThread;
				var oldTop = mainThread.GetTop();
				mainThread.PushThread();
				var returnValue = Translator.GetObject(mainThread, -1);

				mainThread.SetTop(oldTop);
				return (LuaThread)returnValue;
			}
		}

		public Lua(bool openLibs = true)
		{
			State = new(openLibs);
			Init();
			State.AtPanic(_panicCallback);
		}

		internal void Init()
		{
			State.PushString("NLua_Loaded");
			State.PushBoolean(true);
			State.SetTable((int)LuaRegistry.Index);
			State.NewTable();
			State.SetGlobal("luanet");
			State.PushGlobalTable();
			State.GetGlobal("luanet");
			State.PushString("getmetatable");
			State.GetGlobal("getmetatable");
			State.SetTable(-3);
			State.PopGlobalTable();
			Translator = new(this, State);

			ObjectTranslatorPool.Instance.Add(State, Translator);

			State.PopGlobalTable();
			State.DoString(InitLuanet);
		}

		public void Close()
		{
			if (State == null)
			{
				return;
			}

			State.Close();
			ObjectTranslatorPool.Instance.Remove(State);
			State = null;
		}

		internal static int PanicCallback(IntPtr state)
		{
			var luaState = LuaState.FromIntPtr(state);
			var reason = $"Unprotected error in call to Lua API ({luaState.ToString(-1, false)})";
			throw new LuaException(reason);
		}

		/// <summary>
		/// Assuming we have a Lua error string sitting on the stack, throw a C# exception out to the user's app
		/// </summary>
		/// <exception cref = "LuaScriptException">Thrown if the script caused an exception</exception>
		private void ThrowExceptionFromError(int oldTop)
		{
			var err = Translator.GetObject(State, -1);
			State.SetTop(oldTop);

			// A pre-wrapped exception - just rethrow it (stack trace of InnerException will be preserved)
			if (err is LuaScriptException luaEx)
			{
				throw luaEx;
			}

			// A non-wrapped Lua error (best interpreted as a string) - wrap it and throw it
			err ??= "Unknown Lua Error";
			throw new LuaScriptException(err.ToString(), string.Empty);
		}

		/// <summary>
		/// Push a debug.traceback reference onto the stack, for a pcall function to use as error handler. (Remember to increment any top-of-stack markers!)
		/// </summary>
		private static int PushDebugTraceback(LuaState luaState, int argCount)
		{
			luaState.GetGlobal("debug");
			luaState.GetField(-1, "traceback");
			luaState.Remove(-2);
			var errIndex = -argCount - 2;
			luaState.Insert(errIndex);
			return errIndex;
		}

		/// <summary>
		/// <para>Return a debug.traceback() call result (a multi-line string, containing a full stack trace, including C calls.</para>
		/// <para>Note: it won't return anything unless the interpreter is in the middle of execution - that is, it only makes sense to call it from a method called from Lua, or during a coroutine yield.</para>
		/// </summary>
		internal string GetDebugTraceback()
		{
			var oldTop = State.GetTop();
			State.GetGlobal("debug"); // stack: debug
			State.GetField(-1, "traceback"); // stack: debug,traceback
			State.Remove(-2); // stack: traceback
			State.PCall(0, -1, 0);
			return Translator.PopValues(State, oldTop)[0] as string;
		}

		/// <summary>
		/// Convert C# exceptions into Lua errors
		/// </summary>
		/// <returns>num of things on stack</returns>
		/// <param name = "e">null for no pending exception</param>
		internal int SetPendingException(Exception e)
		{
			if (e == null)
			{
				return 0;
			}

			Translator.ThrowError(State, e);
			return 1;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name = "chunk"></param>
		/// <param name = "name"></param>
		/// <returns></returns>
		public LuaFunction LoadString(string chunk, string name)
		{
			var oldTop = State.GetTop();
			IsExecuting = true;

			try
			{
				if (State.LoadString(chunk, name) != LuaStatus.OK)
				{
					ThrowExceptionFromError(oldTop);
				}
			}
			finally
			{
				IsExecuting = false;
			}

			var result = Translator.GetFunction(State, -1);
			Translator.PopValues(State, oldTop);
			return result;
		}

		/// <summary>
		/// 
		/// </summary>
		/// <param name = "chunk"></param>
		/// <param name = "name"></param>
		/// <returns></returns>
		public LuaFunction LoadString(byte[] chunk, string name)
		{
			var oldTop = State.GetTop();
			IsExecuting = true;

			try
			{
				if (State.LoadBuffer(chunk, name) != LuaStatus.OK)
				{
					ThrowExceptionFromError(oldTop);
				}
			}
			finally
			{
				IsExecuting = false;
			}

			var result = Translator.GetFunction(State, -1);
			Translator.PopValues(State, oldTop);
			return result;
		}

		/// <summary>
		/// Load a File on, and return a LuaFunction to execute the file loaded (useful to see if the syntax of a file is ok)
		/// </summary>
		/// <param name = "fileName"></param>
		/// <returns></returns>
		public LuaFunction LoadFile(string fileName)
		{
			var oldTop = State.GetTop();

			if (State.LoadFile(fileName) != LuaStatus.OK)
			{
				ThrowExceptionFromError(oldTop);
			}

			var result = Translator.GetFunction(State, -1);
			Translator.PopValues(State, oldTop);
			return result;
		}

		/// <summary>
		/// Executes a Lua chunk and returns all the chunk's return values in an array.
		/// </summary>
		/// <param name = "chunk">Chunk to execute</param>
		/// <param name = "chunkName">Name to associate with the chunk. Defaults to "chunk".</param>
		/// <returns></returns>
		public object[] DoString(byte[] chunk, string chunkName = "chunk")
		{
			var oldTop = State.GetTop();
			IsExecuting = true;

			if (State.LoadBuffer(chunk, chunkName) != LuaStatus.OK)
			{
				ThrowExceptionFromError(oldTop);
			}

			var errorFunctionIndex = 0;

			if (UseTraceback)
			{
				errorFunctionIndex = PushDebugTraceback(State, 0);
				oldTop++;
			}

			try
			{
				if (State.PCall(0, -1, errorFunctionIndex) != LuaStatus.OK)
				{
					ThrowExceptionFromError(oldTop);
				}

				return Translator.PopValues(State, oldTop);
			}
			finally
			{
				IsExecuting = false;
			}
		}

		/// <summary>
		/// Executes a Lua chunk and returns all the chunk's return values in an array.
		/// </summary>
		/// <param name = "chunk">Chunk to execute</param>
		/// <param name = "chunkName">Name to associate with the chunk. Defaults to "chunk".</param>
		/// <returns></returns>
		public object[] DoString(string chunk, string chunkName = "chunk")
		{
			var oldTop = State.GetTop();
			IsExecuting = true;

			if (State.LoadString(chunk, chunkName) != LuaStatus.OK)
			{
				ThrowExceptionFromError(oldTop);
			}

			var errorFunctionIndex = 0;

			if (UseTraceback)
			{
				errorFunctionIndex = PushDebugTraceback(State, 0);
				oldTop++;
			}

			try
			{
				if (State.PCall(0, -1, errorFunctionIndex) != LuaStatus.OK)
				{
					ThrowExceptionFromError(oldTop);
				}

				return Translator.PopValues(State, oldTop);
			}
			finally
			{
				IsExecuting = false;
			}
		}

		/// <summary>
		/// Executes a Lua file and returns all the chunk's return
		/// values in an array
		/// </summary>
		public object[] DoFile(string fileName)
		{
			var oldTop = State.GetTop();

			if (State.LoadFile(fileName) != LuaStatus.OK)
			{
				ThrowExceptionFromError(oldTop);
			}

			IsExecuting = true;

			var errorFunctionIndex = 0;
			if (UseTraceback)
			{
				errorFunctionIndex = PushDebugTraceback(State, 0);
				oldTop++;
			}

			try
			{
				if (State.PCall(0, -1, errorFunctionIndex) != LuaStatus.OK)
				{
					ThrowExceptionFromError(oldTop);
				}

				return Translator.PopValues(State, oldTop);
			}
			finally
			{
				IsExecuting = false;
			}
		}

		public object GetObjectFromPath(string fullPath)
		{
			var oldTop = State.GetTop();
			var path = FullPathToArray(fullPath);
			State.GetGlobal(path[0]);
			var returnValue = Translator.GetObject(State, -1);

			if (path.Length > 1)
			{
				var luaBase = returnValue as LuaBase;
				var remainingPath = new string[path.Length - 1];
				Array.Copy(path, 1, remainingPath, 0, path.Length - 1);
				returnValue = GetObject(remainingPath);
				luaBase?.Dispose();
			}

			State.SetTop(oldTop);
			return returnValue;
		}

		public void SetObjectToPath(string fullPath, object value)
		{
			var oldTop = State.GetTop();
			var path = FullPathToArray(fullPath);

			if (path.Length == 1)
			{
				Translator.Push(State, value);
				State.SetGlobal(fullPath);
			}
			else
			{
				State.GetGlobal(path[0]);
				var remainingPath = new string[path.Length - 1];
				Array.Copy(path, 1, remainingPath, 0, path.Length - 1);
				SetObject(remainingPath, value);
			}

			State.SetTop(oldTop);

			// Globals auto-complete
			if (value == null)
			{
				// Remove now obsolete entries
				_globals.RemoveGlobal(fullPath);
			}
			else
			{
				// Add new entries
				if (!_globals.Contains(fullPath))
				{
					_globals.RegisterGlobal(fullPath, value.GetType(), 0);
				}
			}
		}

		/// <summary>
		/// Indexer for global variables from the LuaInterpreter
		/// Supports navigation of tables by using . operator
		/// </summary>
		/// <param name="fullPath"></param>
		/// <returns></returns>
		public object this[string fullPath]
		{
			get
			{
				// Silently convert Lua integer to double for backward compatibility with index[] operator
				var obj = GetObjectFromPath(fullPath);
				if (obj is long l)
				{
					return (double)l;
				}

				return obj;
			}
			set => SetObjectToPath(fullPath, value);
		}

		/// <summary>
		/// Navigates a table in the top of the stack, returning
		/// the value of the specified field
		/// </summary>
		/// <param name="remainingPath"></param>
		/// <returns></returns>
		internal object GetObject(string[] remainingPath)
		{
			object returnValue = null;

			foreach (var t in remainingPath)
			{
				State.PushString(t);
				State.GetTable(-2);
				returnValue = Translator.GetObject(State, -1);

				if (returnValue == null)
				{
					break;
				}
			}

			return returnValue;
		}

		/// <summary>
		/// Gets a numeric global variable
		/// </summary>
		public double GetNumber(string fullPath)
		{
			// Silently convert Lua integer to double for backward compatibility with GetNumber method
			var obj = GetObjectFromPath(fullPath);
			if (obj is long l)
			{
				return l;
			}

			return (double)obj;
		}

		public int GetInteger(string fullPath)
		{
			var result = GetObjectFromPath(fullPath);
			if (result == null)
			{
				return 0;
			}

			return (int)(long)result;
		}

		public long GetLong(string fullPath)
		{
			var result = GetObjectFromPath(fullPath);
			if (result == null)
			{
				return 0L;
			}

			return (long)result;
		}

		/// <summary>
		/// Gets a string global variable
		/// </summary>
		public string GetString(string fullPath)
		{
			var obj = GetObjectFromPath(fullPath);
			return obj?.ToString();
		}

		/// <summary>
		/// Gets a table global variable
		/// </summary>
		public LuaTable GetTable(string fullPath)
			=> (LuaTable)GetObjectFromPath(fullPath);

		/// <summary>
		/// Gets a table global variable as an object implementing
		/// the interfaceType interface
		/// </summary>
		public object GetTable(Type interfaceType, string fullPath)
			=> CodeGeneration.Instance.GetClassInstance(interfaceType, GetTable(fullPath));

		/// <summary>
		/// Gets a thread global variable
		/// </summary>
		public LuaThread GetThread(string fullPath)
			=> (LuaThread)GetObjectFromPath(fullPath);

		/// <summary>
		/// Gets a function global variable
		/// </summary>
		public LuaFunction GetFunction(string fullPath)
		{
			var obj = GetObjectFromPath(fullPath);
			if (obj is LuaFunction luaFunction)
			{
				return luaFunction;
			}

			luaFunction = new((LuaNativeFunction)obj, this);
			return luaFunction;
		}

		/// <summary>
		/// Register a delegate type to be used to convert Lua functions to C# delegates (useful for iOS where there is no dynamic code generation)
		/// type delegateType
		/// </summary>
		public void RegisterLuaDelegateType(Type delegateType, Type luaDelegateType)
			=> CodeGeneration.Instance.RegisterLuaDelegateType(delegateType, luaDelegateType);

		public void RegisterLuaClassType(Type klass, Type luaClass)
			=> CodeGeneration.Instance.RegisterLuaClassType(klass, luaClass);

		public void LoadCLRPackage()
			=> State.DoString(ClrPackage);

		/// <summary>
		/// Gets a function global variable as a delegate of
		/// type delegateType
		/// </summary>
		public Delegate GetFunction(Type delegateType, string fullPath)
			=> CodeGeneration.Instance.GetDelegate(delegateType, GetFunction(fullPath));

		/// <summary>
		/// Calls the object as a function with the provided arguments and
		/// casting returned values to the types in returnTypes before returning
		/// them in an array
		/// </summary>
		internal object[] CallFunction(object function, object[] args, Type[] returnTypes = null)
		{
			var nArgs = 0;
			var oldTop = State.GetTop();

			if (!State.CheckStack(args.Length + 6))
			{
				throw new LuaException("Lua stack overflow");
			}

			Translator.Push(State, function);

			if (args.Length > 0)
			{
				nArgs = args.Length;

				foreach (var t in args)
				{
					Translator.Push(State, t);
				}
			}

			IsExecuting = true;

			try
			{
				var errfunction = 0;
				if (UseTraceback)
				{
					errfunction = PushDebugTraceback(State, nArgs);
					oldTop++;
				}

				var error = State.PCall(nArgs, -1, errfunction);
				if (error != LuaStatus.OK)
				{
					ThrowExceptionFromError(oldTop);
				}
			}
			finally
			{
				IsExecuting = false;
			}

			return returnTypes != null
				? Translator.PopValues(State, oldTop, returnTypes)
				: Translator.PopValues(State, oldTop);
		}

		/// <summary>
		/// Navigates a table to set the value of one of its fields
		/// </summary>
		internal void SetObject(string[] remainingPath, object val)
		{
			for (var i = 0; i < remainingPath.Length - 1; i++)
			{
				State.PushString(remainingPath[i]);
				State.GetTable(-2);
			}

			State.PushString(remainingPath[remainingPath.Length - 1]);
			Translator.Push(State, val);
			State.SetTable(-3);
		}

		/// <summary>
		/// Creates a new empty table
		/// </summary>
		public LuaTable NewTable()
		{
			var oldTop = State.GetTop();
			State.NewTable();
			var ret = Translator.GetTable(State, -1);
			State.SetTop(oldTop);
			return ret;
		}

		internal static string[] FullPathToArray(string fullPath)
			=> fullPath.SplitWithEscape('.', '\\').ToArray();

		/// <summary>
		/// Creates a new table as a global variable or as a field
		/// inside an existing table
		/// </summary>
		/// <param name="fullPath"></param>
		public void NewTable(string fullPath)
		{
			var path = FullPathToArray(fullPath);
			var oldTop = State.GetTop();

			if (path.Length == 1)
			{
				State.NewTable();
				State.SetGlobal(fullPath);
			}
			else
			{
				State.GetGlobal(path[0]);

				for (var i = 1; i < path.Length - 1; i++)
				{
					State.PushString(path[i]);
					State.GetTable(-2);
				}

				State.PushString(path[path.Length - 1]);
				State.NewTable();
				State.SetTable(-3);
			}

			State.SetTop(oldTop);
		}

		public Dictionary<object, object> GetTableDict(LuaTable table)
		{
			if (table == null)
			{
				throw new ArgumentNullException(nameof(table));
			}

			var dict = new Dictionary<object, object>();
			var oldTop = State.GetTop();
			Translator.Push(State, table);
			State.PushNil();

			while (State.Next(-2))
			{
				dict[Translator.GetObject(State, -2)] = Translator.GetObject(State, -1);
				State.SetTop(-2);
			}

			State.SetTop(oldTop);
			return dict;
		}

		internal void DisposeInternal(int reference, bool finalized)
		{
			if (finalized && Translator != null)
			{
				Translator.AddFinalizedReference(reference);
				return;
			}

			if (State != null && !finalized)
			{
				State.Unref(reference);
			}
		}

		/// <summary>
		/// Gets a field of the table corresponding to the provided reference
		/// using rawget (do not use metatables)
		/// </summary>
		internal object RawGetObject(int reference, string field)
		{
			var oldTop = State.GetTop();
			State.GetRef(reference);
			State.PushString(field);
			State.RawGet(-2);
			var obj = Translator.GetObject(State, -1);
			State.SetTop(oldTop);
			return obj;
		}

		/// <summary>
		/// Gets a field of the table or userdata corresponding to the provided reference
		/// </summary>
		internal object GetObject(int reference, string field)
		{
			var oldTop = State.GetTop();
			State.GetRef(reference);
			var returnValue = GetObject(FullPathToArray(field));
			State.SetTop(oldTop);
			return returnValue;
		}

		/// <summary>
		/// Gets a numeric field of the table or userdata corresponding the the provided reference
		/// </summary>
		internal object GetObject(int reference, object field)
		{
			var oldTop = State.GetTop();
			State.GetRef(reference);
			Translator.Push(State, field);
			State.GetTable(-2);
			var returnValue = Translator.GetObject(State, -1);
			State.SetTop(oldTop);
			return returnValue;
		}

		/// <summary>
		/// Sets a field of the table or userdata corresponding the the provided reference
		/// to the provided value
		/// </summary>
		internal void SetObject(int reference, string field, object val)
		{
			var oldTop = State.GetTop();
			State.GetRef(reference);
			SetObject(FullPathToArray(field), val);
			State.SetTop(oldTop);
		}

		/// <summary>
		/// Sets a numeric field of the table or userdata corresponding the the provided reference
		/// to the provided value
		/// </summary>
		internal void SetObject(int reference, object field, object val)
		{
			var oldTop = State.GetTop();
			State.GetRef(reference);
			Translator.Push(State, field);
			Translator.Push(State, val);
			State.SetTable(-3);
			State.SetTop(oldTop);
		}

		/// <summary>
		/// Gets the luaState from the thread
		/// </summary>
		internal LuaState GetThreadState(int reference)
		{
			var oldTop = State.GetTop();
			State.GetRef(reference);
			var state = State.ToThread(-1);
			State.SetTop(oldTop);
			return state;
		}

		/// <summary>
		/// Creates a new empty thread
		/// </summary>
		public LuaThread NewThread()
		{
			var oldTop = State.GetTop();

			State.NewThread();
			var thread = (LuaThread)Translator.GetObject(State, -1);

			State.SetTop(oldTop);
			return thread;
		}

		/// <summary>
		/// Creates a new coroutine thread
		/// </summary>
		public LuaThread NewThread(LuaFunction function)
		{
			var oldTop = State.GetTop();

			var state = State.NewThread();
			var thread = (LuaThread)Translator.GetObject(State, -1);

			Translator.Push(State, function);
			State.XMove(state, 1);

			State.SetTop(oldTop);
			return thread;
		}

		public LuaFunction RegisterFunction(string path, MethodBase function)
			=> RegisterFunction(path, null, function);

		/// <summary>
		/// Registers an object's method as a Lua function (global or table field)
		/// The method may have any signature
		/// </summary>
		public LuaFunction RegisterFunction(string path, object target, MethodBase function)
		{
			// We leave nothing on the stack when we are done
			var oldTop = State.GetTop();
			var wrapper = new LuaMethodWrapper(Translator, target, new(function.DeclaringType), function);

			Translator.Push(State, new LuaNativeFunction(wrapper.InvokeFunction));

			var value = Translator.GetObject(State, -1);
			SetObjectToPath(path, value);

			var f = GetFunction(path);
			State.SetTop(oldTop);
			return f;
		}

		/// <summary>
		/// Compares the two values referenced by ref1 and ref2 for equality
		/// </summary>
		internal bool CompareRef(int ref1, int ref2)
		{
			var top = State.GetTop();
			State.GetRef(ref1);
			State.GetRef(ref2);
			var equal = State.AreEqual(-1, -2);
			State.SetTop(top);
			return equal;
		}

		internal void PushCSFunction(LuaNativeFunction function)
			=> Translator.PushFunction(State, function);

		~Lua()
		{
			Dispose();
		}

		public void Dispose()
		{
			if (Translator != null)
			{
				Translator.PendingEvents.Dispose();
				if (Translator.Tag != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(Translator.Tag);
				}

				Translator = null;
			}

			Close();
			GC.SuppressFinalize(this);
		}
	}
}
