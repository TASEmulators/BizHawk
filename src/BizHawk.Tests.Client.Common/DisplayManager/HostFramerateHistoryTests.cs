using System.Globalization;
using System.Linq;

using BizHawk.Client.Common;

namespace BizHawk.Tests.Client.Common.DisplayManager
{
	[TestClass]
	public sealed class HostFramerateHistoryTests
	{
#pragma warning disable MA0136
		[DataRow(33072057529L, """
		33074588684;14;51.4
		33077169269;33;102.9
		33079723059;41;141.6
		33082254924;44;163.1
		33084766767;45;173.8
		33087291462;44;174.1
		""")]
		[DataRow(477369643997L, """
		477372189861;20;74.6
		477374714213;44;141.3
		477377271911;43;159.3
		477379827150;43;165.3
		477382334920;43;169.4
		477384853460;43;170.3
		477387415846;42;166.0
		""")]
		[DataRow(0L, """
		2500000;25;96.0
		5000000;25;98.7
		7500000;25;99.6
		10000000;25;99.9
		12500000;25;100.0
		15000000;25;100.0
		17500000;25;100.0
		20000000;25;100.0
		""")]
		[DataRow(0L, """
		2500000;30;116.0
		5000000;20;92.0
		7500000;30;110.7
		10000000;20;90.2
		12500000;30;110.1
		15000000;20;90.0
		17500000;30;110.0
		20000000;20;90.0
		""")]
#pragma warning restore MA0136
		[TestMethod]
		public void TestCalc(long initTimestamp, string iterations)
		{
			HostFramerateHistory history = new(10_000_000L, initTimestamp);
			foreach (var (i, fields) in iterations.Split('\n').Select(static line => line.Split(';')).Index())
			{
				var numFrames = uint.Parse(fields[1]);
				history.IncrementCounterBy(numFrames);
				var currentTimestamp = long.Parse(fields[0]);
				Assert.IsTrue(history.MaybeRevise(currentTimestamp, out _), $"iter {i}: enough time should have passed to revise");
				var expectedFramerate = double.Parse(fields[2], CultureInfo.InvariantCulture);
				Assert.AreEqual(expectedFramerate, history.Framerate, delta: 0.1, $"iter {i}: new estimate");
			}
		}
	}
}
