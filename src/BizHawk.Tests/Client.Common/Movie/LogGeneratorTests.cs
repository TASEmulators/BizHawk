using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Common.Tests.Client.Common.Movie
{
	[TestClass]
	public class LogGeneratorTests
	{
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
			Assert.IsNotNull(lg);
			Assert.AreEqual("|!|", actual);
		}
	}
}