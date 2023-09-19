using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BizHawk.Tests
{
	[TestClass]
	public class PreTests
	{
		[AssemblyInitialize]
		public static void PreTestsMethod(TestContext context)
		{
			// This method will run only once, before all tests.
			// So this seems a good place to initialize static classes.
			BizHawk.Client.Common.ApiManager.FindApis(BizHawk.Client.Common.ReflectionCache.Types);
		}
	}

}
