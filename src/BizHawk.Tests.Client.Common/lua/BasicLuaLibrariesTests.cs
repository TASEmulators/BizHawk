using BizHawk.Client.Common;

namespace BizHawk.Tests.Client.Common.lua
{
	[DoNotParallelize]
	[TestClass]
	public class BasicLuaTests
	{
		[TestMethod]
		public void CanPrint()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("say_foo.lua", true);

			// act
			context.RunYielding();

			// assert
			context.AssertLogMatches("foo");
		}

		[TestMethod]
		public void ScriptCanStart()
		{
			// arrange
			LuaTestContext context = new();

			// act
			context.AddScript("say_foo.lua", true);

			// assert
			Assert.AreEqual(LuaFile.RunState.Running, context.GetScriptState(0));
		}

		[TestMethod]
		public void ScriptCanStop()
		{
			// arrange
			LuaTestContext context = new();
			context.AddScript("say_foo.lua", true);

			// act
			context.RunYielding();

			// assert
			Assert.AreEqual(LuaFile.RunState.Disabled, context.GetScriptState(0));
		}
	}
}
