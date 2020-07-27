using System.IO;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
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

			var zw2 = ZwinderStateManager.Create(new BinaryReader(rms), new ZwinderStateManagerSettings());

			// TODO: we could assert more things here to be thorough
			Assert.IsNotNull(zw2);
			Assert.AreEqual(zw.Settings.Current.BufferSize, zw2.Settings.Current.BufferSize);
			Assert.AreEqual(zw.Settings.Recent.BufferSize, zw2.Settings.Recent.BufferSize);
		}

		[TestMethod]
		public void SomethingSomething()
		{
			var ss = new StateSource { PaddingData = new byte[1000] };
			var zw = new ZwinderStateManager(new ZwinderStateManagerSettings
			{
				Current = new RewindConfig
				{
					UseCompression = false,
					BufferSize = 1,
					TargetFrameLength = 10000,
				},
				Recent = new RewindConfig
				{
					UseCompression = false,
					BufferSize = 1,
					TargetFrameLength = 100000,
				},
				AncientStateInterval = 50000
			});
			{
				var ms = new MemoryStream();
				ss.SaveStateBinary(new BinaryWriter(ms));
				zw.Engage(ms.ToArray());
			}
			for (int frame = 0; frame <= 10440; frame++)
			{
				ss.Frame = frame;
				zw.Capture(frame, ss);
			}
			var kvp = zw.GetStateClosestToFrame(10440);
			var frameActul = StateSource.GetFrameNumberInState(kvp.Value);
			Assert.Equals(kvp.Key, frameActul);
			Assert.IsTrue(frameActul < 10440);
		}

		private class StateSource : IBinaryStateable
		{
			public int Frame { get; set; }
			public byte[] PaddingData { get; set; } = new byte[0];
			public void LoadStateBinary(BinaryReader reader)
			{
				Frame = reader.ReadInt32();
				reader.Read(PaddingData, 0, PaddingData.Length);
			}

			public void SaveStateBinary(BinaryWriter writer)
			{
				writer.Write(Frame);
				writer.Write(PaddingData);
			}

			public static int GetFrameNumberInState(Stream stream)
			{
				var ss = new StateSource();
				ss.LoadStateBinary(new BinaryReader(stream));
				return ss.Frame;
			}
		}
	}
}
