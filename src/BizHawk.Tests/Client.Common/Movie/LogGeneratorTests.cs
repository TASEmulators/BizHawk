using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Common.Tests.Client.Common.Movie
{
	[TestClass]
	public class LogGeneratorTests
	{
		private SimpleController _boolController = null!;
		private Bk2LogEntryGenerator _lg = null!;
		private SimpleController _floatController = null!;
		private Bk2LogEntryGenerator _floatLg = null!;

		[TestInitialize]
		public void Initializer()
		{
			_boolController = new SimpleController
			{
				Definition = new ControllerDefinition { BoolButtons = { "A" } }
			};

			_floatController = new SimpleController
			{
				Definition = new ControllerDefinition
				{
					AxisControls = { "StickX", "StickY" },
					AxisRanges =
					{
						new ControllerDefinition.AxisRange(0, 100, 200),
						new ControllerDefinition.AxisRange(0, 100, 200)
					}
				}
			};

			_lg = new Bk2LogEntryGenerator("NES", _boolController);
			_floatLg = new Bk2LogEntryGenerator("NES", _floatController);
		}

		[TestMethod]
		public void GenerateLogEntry_ExclamationForUnknownButtons()
		{
			var controller = new SimpleController
			{
				Definition = new ControllerDefinition
				{
					BoolButtons = new List<string> {"Unknown Button"}
				},
				["Unknown Button"] = true
			};

			var lg = new Bk2LogEntryGenerator("NES", controller);
			var actual = lg.GenerateLogEntry();
			Assert.AreEqual("|!|", actual);
		}

		[TestMethod]
		public void GenerateLogEntry_BoolPressed_GeneratesMnemonic()
		{
			_boolController["A"] = true;
			var actual = _lg.GenerateLogEntry();
			Assert.AreEqual("|A|", actual);
		}

		[TestMethod]
		public void GenerateLogEntry_BoolUnPressed_GeneratesPeriod()
		{
			_boolController["A"] = false;
			var actual = _lg.GenerateLogEntry();
			Assert.AreEqual("|.|", actual);
		}

		[TestMethod]
		public void GenerateLogEntry_Floats()
		{
			var actual = _floatLg.GenerateLogEntry();
			Assert.AreEqual("|    0,    0,|", actual);
		}

		[TestMethod]
		public void GenerateInputDisplay_BoolPressed_GeneratesMnemonic()
		{
			_boolController["A"] = true;
			var actual = _lg.GenerateInputDisplay();
			Assert.AreEqual("A", actual);
		}

		[TestMethod]
		public void GenerateInputDisplay_BoolUnPressed_GeneratesSpace()
		{
			_boolController["A"] = false;
			var actual = _lg.GenerateInputDisplay();
			Assert.AreEqual(" ", actual);
		}

		[TestMethod]
		public void GenerateInputDisplay_Floats()
		{
			var actual = _floatLg.GenerateInputDisplay();
			Assert.AreEqual("    0,    0,", actual);
		}
	}
}