using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

using static BizHawk.Emulation.Common.ControllerDefinition;

namespace BizHawk.Common.Tests.Client.Common.Movie
{
	[TestClass]
	public class LogGeneratorTests
	{
		private SimpleController _boolController = null!;
		private SimpleController _floatController = null!;

		[TestInitialize]
		public void Initializer()
		{
			_boolController = new SimpleController
			{
				Definition = new ControllerDefinition { BoolButtons = { "A" } }
			};

			_floatController = new SimpleController
			{
				Definition = new ControllerDefinition().AddXYPair("Stick{0}", AxisPairOrientation.RightAndUp, 0, 100, 200)
			};
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
			var lg = new Bk2LogEntryGenerator("NES", _boolController);
			var actual = lg.GenerateLogEntry();
			Assert.AreEqual("|A|", actual);
		}

		[TestMethod]
		public void GenerateLogEntry_BoolUnPressed_GeneratesPeriod()
		{
			_boolController["A"] = false;
			var lg = new Bk2LogEntryGenerator("NES", _boolController);
			var actual = lg.GenerateLogEntry();
			Assert.AreEqual("|.|", actual);
		}

		[TestMethod]
		public void GenerateLogEntry_Floats()
		{
			var lg = new Bk2LogEntryGenerator("NES", _floatController);
			var actual = lg.GenerateLogEntry();
			Assert.AreEqual("|    0,    0,|", actual);
		}
	}
}