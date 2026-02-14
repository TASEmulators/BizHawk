using BizHawk.Client.Common;

namespace BizHawk.Tests.Client.Common.lua
{
	[DoNotParallelize]
	[TestClass]
	public class LuaEventTests
	{
		[TestMethod]
		public void ScriptWithRegisteredFunctionShowsAsRunning()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("register_frame_end.lua", true);

			// act
			context.RunYielding();

			// assert
			Assert.AreEqual(LuaFile.RunState.Running, context.GetScriptState(0));
		}

		[TestMethod]
		public void UnregisteringLastEventStopsScript()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("unregister_event.lua", true);
			context.RunYielding();
			Assert.AreEqual(LuaFile.RunState.Running, context.GetScriptState(0), "Test is invalid: arrange phase failed.");

			// act
			context.RunFrameWaiting();

			// assert
			Assert.AreEqual(LuaFile.RunState.Disabled, context.GetScriptState(0));
		}

		[TestMethod]
		public void ExitEventDoesNotKeepScriptActive()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("register_exit.lua", true);

			// act
			context.RunYielding();

			// assert
			Assert.AreEqual(LuaFile.RunState.Disabled, context.GetScriptState(0));
			context.AssertLogMatches("foo");
		}

		[TestMethod]
		public void ExceptionPrintsMessage()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("exception.lua", true);

			// act
			context.RunYielding();

			// assert
			Assert.AreEqual(1, context.loggedMessages.Count);
		}

		[TestMethod]
		public void ExceptionInCallbackPrintsMessage()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("callback_exception.lua", true);
			context.RunYielding();

			// act
			context.RunFrameWaiting();

			// assert
			Assert.AreEqual(1, context.loggedMessages.Count);
		}

		[TestMethod]
		public void ExceptionInCallbackDoesNotStopScript()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("callback_exception.lua", true);
			context.RunYielding();
			Assert.AreEqual(LuaFile.RunState.Running, context.GetScriptState(0), "Test is invalid: arrange phase failed.");

			// act
			context.RunFrameWaiting();

			// assert
			Assert.AreEqual(LuaFile.RunState.Running, context.GetScriptState(0));
		}

		// Lua itself should not know anything about the specialness of callbacks.
		// "special" callback here refers to events raised by C# callback systems like input polling, which use some different code
		[TestMethod]
		public void ExceptionInSpecialCallbackPrintsMessage()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("input_poll_exception.lua", true);
			context.RunYielding();

			// act
			context.RunFrameWaiting();

			// assert
			Assert.AreEqual(1, context.loggedMessages.Count);
		}

		[TestMethod]
		public void CurrentDirectoryIsSet()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("check_file_visible.lua", true);

			// act
			context.RunYielding();

			// assert
			context.AssertLogMatches("pass");
		}

		[TestMethod]
		public void CurrentDirectoryIsSetInCallback()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("check_file_visible_callback_1.lua", true);
			context.RunYielding();

			// act
			context.RunFrameWaiting();

			// assert
			context.AssertLogMatches("pass");
		}

		[TestMethod]
		public void CurrentDirectoryIsSetInSpecialCallback()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("check_file_visible_callback_2.lua", true);
			context.RunYielding();

			// act
			context.RunFrameWaiting();

			// assert
			context.AssertLogMatches("pass");
		}

		[TestMethod]
		public void CurrentDirectoryIsSetInCallbackRegisteredFromCallback()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("check_file_visible_callback_3.lua", true);
			context.RunFrameWaiting();

			// act
			context.RunFrameWaiting();
			context.RunFrameWaiting();

			// assert
			context.AssertLogMatches("pass");
		}

		[TestMethod]
		public void ExitingScriptUnregistersFunctions()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("register_exit.lua", true);

			// act
			context.RunYielding();

			// assert
			Assert.AreEqual(0, context.FunctionsRegisteredToScript(0).Count);
		}

		[TestMethod]
		public void StoppingScriptUnregistersFunctions()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("register_frame_end.lua", true);
			context.RunYielding();
			Assert.AreEqual(LuaFile.RunState.Running, context.GetScriptState(0), "Test is invalid: arrange phase failed.");

			// act
			context.StopScript(0);

			// assert
			Assert.AreEqual(0, context.FunctionsRegisteredToScript(0).Count);
		}

		[TestMethod]
		public void ScriptStoppedByExceptionUnregistersFunctions()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("register_then_exception.lua", true);

			// act
			context.RunYielding();

			// assert
			Assert.AreEqual(0, context.FunctionsRegisteredToScript(0).Count);
		}
	}
}
