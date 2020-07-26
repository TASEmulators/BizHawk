using System.IO;
using BizHawk.Client.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BizHawk.Common.Tests.Client.Common.Movie
{
	[TestClass]
	public class ZwinderStateManagerTests
	{
		[TestMethod]
		public void SaveCreateRoundTrip()
		{
			var ms = new MemoryStream();
			var zw = new ZwinderStateManager();
			zw.SaveStateHistory(new BinaryWriter(ms));
			var buff = ms.ToArray();
			var rms = new MemoryStream(buff, false);

			var zw2 = ZwinderStateManager.Create(new BinaryReader(rms), new ZwinderStateManagerSettingsWIP());

			// TODO: we could assert more things here to be thorough
			Assert.IsNotNull(zw2);
			Assert.AreEqual(zw.Settings.Current.BufferSize, zw2.Settings.Current.BufferSize);
			Assert.AreEqual(zw.Settings.Recent.BufferSize, zw2.Settings.Recent.BufferSize);
		}
	}
}
