using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Tests.Client.Common.Display
{
	[TestClass]
	public class InputDisplayTests
	{
		private const int MidValue = 100;
		private SimpleController _boolController = null!;
		private SimpleController _axisController = null!;

		[TestInitialize]
		public void Initializer()
		{
			_boolController = new(new ControllerDefinition("Dummy Gamepad") { BoolButtons = { "A" } }.MakeImmutable());
			_axisController = new(
				new ControllerDefinition("Dummy Gamepad")
					.AddXYPair("Stick{0}", AxisPairOrientation.RightAndUp, 0.RangeTo(200), MidValue)
					.MakeImmutable());
		}

		[TestMethod]
		public void Generate_BoolPressed_GeneratesMnemonic()
		{
			_boolController["A"] = true;
			var displayGenerator = new Bk2InputDisplayGenerator("NES", _boolController.Definition);
			var actual = displayGenerator.Generate(_boolController);
			Assert.AreEqual("A", actual);
		}

		[TestMethod]
		public void Generate_BoolUnPressed_GeneratesSpace()
		{
			_boolController["A"] = false;
			var displayGenerator = new Bk2InputDisplayGenerator("NES", _boolController.Definition);
			var actual = displayGenerator.Generate(_boolController);
			Assert.AreEqual(" ", actual);
		}

		[TestMethod]
		public void Generate_Floats()
		{
			var displayGenerator = new Bk2InputDisplayGenerator("NES", _axisController.Definition);
			var actual = displayGenerator.Generate(_axisController);
			Assert.AreEqual("    0,    0,", actual);
		}

		[TestMethod]
		public void Generate_MidRangeDisplaysEmpty()
		{
			_axisController.AcceptNewAxis("StickX", MidValue);
			var displayGenerator = new Bk2InputDisplayGenerator("NES", _axisController.Definition);
			var actual = displayGenerator.Generate(_axisController);
			Assert.AreEqual("          0,", actual);
		}
	}
}
