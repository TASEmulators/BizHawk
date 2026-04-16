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

#pragma warning disable BHI1600 //TODO disambiguate assert calls
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
			Assert.AreEqual("|  100,  100,|", actual);
		}

		[TestMethod]
		public void GenerateLogEntry_NoStupidHack()
		{
			var upController = new SimpleController(new ControllerDefinition("Dummy Gamepad") { BoolButtons = { "Up" } }.MakeImmutable());
			upController.Definition.BuildMnemonicsCache(VSystemID.Raw.NES);

			var logEntry = Bk2LogEntryGenerator.GenerateLogEntry(upController);
			Assert.AreEqual("|.|", logEntry);
		}

		[TestMethod]
		public void GenerateLogEntry_EmptyPlayerGroups()
		{
			var upController = new SimpleController(new ControllerDefinition("Dummy Gamepad") { BoolButtons = { "P2 Up" } }.MakeImmutable());
			upController.Definition.BuildMnemonicsCache(VSystemID.Raw.NES);

			var logEntry = Bk2LogEntryGenerator.GenerateLogEntry(upController);
			Assert.AreEqual("|||.|", logEntry);
		}

		[TestMethod]
		public void GenerateLogKey_EmptyPlayerGroups()
		{
			var upControllerDefinition = new ControllerDefinition("Dummy Gamepad") { BoolButtons = { "P2 Up" } }.MakeImmutable();
			upControllerDefinition.BuildMnemonicsCache(VSystemID.Raw.NES);

			var logKey = Bk2LogEntryGenerator.GenerateLogKey(upControllerDefinition);
			Assert.AreEqual("###P2 Up|", logKey);
		}

		[TestMethod]
		public void GenerateLogEntry_Bk2Controller()
		{
			var simpleController = new SimpleController(new ControllerDefinition("Dummy Gamepad") { BoolButtons = { "P1 Up", "P3 A" } }.MakeImmutable());
			simpleController.Definition.BuildMnemonicsCache(VSystemID.Raw.NES);

			var originalLogEntry = Bk2LogEntryGenerator.GenerateLogEntry(simpleController);
			var originalLogKey = Bk2LogEntryGenerator.GenerateLogKey(simpleController.Definition);

			// just for safety, should be covered by the above tests already
			Assert.AreEqual("||.||.|", originalLogEntry);
			Assert.AreEqual("##P1 Up|##P3 A|", originalLogKey);

			// ensure a Bk2Controller constructed with ControllerDefinition and LogKey
			// generates the exact same outputs as the original SimpleController
			Bk2Controller bk2Controller = new Bk2Controller(simpleController.Definition, originalLogKey);

			var newLogEntry = Bk2LogEntryGenerator.GenerateLogEntry(bk2Controller);
			Assert.AreEqual(originalLogEntry, newLogEntry);

			var newLogKey = Bk2LogEntryGenerator.GenerateLogKey(bk2Controller.Definition);
			Assert.AreEqual(originalLogKey, newLogKey);
		}
#pragma warning restore BHI1600
	}
}
