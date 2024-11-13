using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Tests.Client.Common.Movie
{
	[TestClass]
	public class LogGeneratorTests
	{
		private SimpleController _boolController = null!;
		private SimpleController _axisController = null!;

		[TestInitialize]
		public void Initializer()
		{
			_boolController = new(new ControllerDefinition("Dummy Gamepad") { BoolButtons = { "A" } }.MakeImmutable());
			_boolController.Definition.BuildMnemonicsCache(VSystemID.Raw.NES);
			_axisController = new(
				new ControllerDefinition("Dummy Gamepad")
					.AddXYPair("Stick{0}", AxisPairOrientation.RightAndUp, 0.RangeTo(200), 100)
					.MakeImmutable());
			_axisController.Definition.BuildMnemonicsCache(VSystemID.Raw.NES);
		}

		[TestMethod]
		public void GenerateLogEntry_ExclamationForUnknownButtons()
		{
			SimpleController controller = new(new ControllerDefinition("Dummy Gamepad") { BoolButtons = { "Unknown Button" } }.MakeImmutable());
			controller.Definition.BuildMnemonicsCache(VSystemID.Raw.NES);
			controller["Unknown Button"] = true;
			var actual = Bk2LogEntryGenerator.GenerateLogEntry(controller);
			Assert.AreEqual("|!|", actual);
		}

		[TestMethod]
		public void GenerateLogEntry_BoolPressed_GeneratesMnemonic()
		{
			_boolController["A"] = true;
			var actual = Bk2LogEntryGenerator.GenerateLogEntry(_boolController);
			Assert.AreEqual("|A|", actual);
		}

		[TestMethod]
		public void GenerateLogEntry_BoolUnPressed_GeneratesPeriod()
		{
			_boolController["A"] = false;
			var actual = Bk2LogEntryGenerator.GenerateLogEntry(_boolController);
			Assert.AreEqual("|.|", actual);
		}

		[TestMethod]
		public void GenerateLogEntry_Floats()
		{
			var actual = Bk2LogEntryGenerator.GenerateLogEntry(_axisController);
			Assert.AreEqual("|    0,    0,|", actual);
		}
	}
}
