using System;

using NLua.Exceptions;
using NLua.Extensions;
using NLua.Native;

namespace NLua
{
	public class LuaThread : LuaBase, IEquatable<LuaThread>, IEquatable<Lua>
	{
		private readonly ObjectTranslator _translator;

		internal LuaState State { get; }

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
				var returnValue = _translator.GetObject(mainThread, -1);

				mainThread.SetTop(oldTop);
				return (LuaThread)returnValue;
			}
		}

		public LuaThread(int reference, Lua interpreter): base(reference, interpreter)
		{
			State = interpreter.GetThreadState(reference);
			_translator = interpreter.Translator;
		}

		/// <summary>
		/// Resumes this thread
		/// </summary>
		// ReSharper disable once UnusedMember.Global
		public LuaStatus Resume()
		{
			// We leave nothing on the stack if we error
			var oldMainTop = State.MainThread.GetTop();
			var oldCoTop = State.GetTop();

			var ret = State.Resume(null, 0);
			if (ret is LuaStatus.OK or LuaStatus.Yield)
			{
				return ret;
			}

			var coErr = _translator.GetObject(State, -1);
			var mainErr = _translator.GetObject(State.MainThread, -1);
			State.SetTop(oldCoTop);
			State.MainThread.SetTop(oldMainTop);

			if (coErr is LuaScriptException coLuaEx)
			{
				throw coLuaEx;
			}

			if (mainErr is LuaScriptException mainLuaEx)
			{
				throw mainLuaEx;
			}

			if (coErr != null)
			{
				throw new LuaScriptException(coErr.ToString(), string.Empty);
			}

			throw new LuaScriptException($"Unknown Lua Error (status = {ret})", string.Empty);
		}

		/// <summary>
		/// Yields this thread
		/// </summary>
		// ReSharper disable once UnusedMember.Global
		public void Yield()
			=> State.Yield(0);

		/// <summary>
		/// Pushes this thread into the Lua stack
		/// </summary>
		internal void Push(LuaState luaState)
			=> luaState.GetRef(_Reference);

		public override string ToString()
			=> "thread";

		public override bool Equals(object obj) => obj switch
		{
			LuaThread thread => State == thread.State,
			Lua interpreter => State == interpreter.State,
			_ => base.Equals(obj)
		};

		public override int GetHashCode()
			=> base.GetHashCode();

		public bool Equals(LuaThread other) => State == other?.State;
		public bool Equals(Lua other) => State == other?.State;

		public static explicit operator LuaThread(Lua interpreter) => interpreter.Thread;

		public static bool operator ==(LuaThread threadA, LuaThread threadB) => threadA?.State == threadB?.State;
		public static bool operator !=(LuaThread threadA, LuaThread threadB) => threadA?.State != threadB?.State;

		public static bool operator ==(LuaThread thread, Lua interpreter) => thread?.State == interpreter?.State;
		public static bool operator !=(LuaThread thread, Lua interpreter) => thread?.State != interpreter?.State;
		public static bool operator ==(Lua interpreter, LuaThread thread) => interpreter?.State == thread?.State;
		public static bool operator !=(Lua interpreter, LuaThread thread) => interpreter?.State != thread?.State;
	}
}
