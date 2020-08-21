using System.IO;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BizHawk.Tests.Client.Common.Movie
{
	[TestClass]
	public class ZwinderStateManagerTests
	{
		[TestMethod]
		public void SaveCreateRoundTrip()
		{
			var ms = new MemoryStream();
			var zw = new ZwinderStateManager(f => false);
			zw.SaveStateHistory(new BinaryWriter(ms));
			var buff = ms.ToArray();
			var rms = new MemoryStream(buff, false);

			var zw2 = ZwinderStateManager.Create(new BinaryReader(rms), new ZwinderStateManagerSettings(), f => false);

			// TODO: we could assert more things here to be thorough
			Assert.IsNotNull(zw2);
			Assert.AreEqual(zw.Settings.CurrentBufferSize, zw2.Settings.CurrentBufferSize);
			Assert.AreEqual(zw.Settings.RecentBufferSize, zw2.Settings.RecentBufferSize);
		}

		[TestMethod]
		public void CountEvictWorks()
		{
			using var zb = new ZwinderBuffer(new RewindConfig
			{
				BufferSize = 1,
				TargetFrameLength = 1
			});
			var ss = new StateSource
			{
				PaddingData = new byte[10]
			};
			var stateCount = 0;
			for (int i = 0; i < 1000000; i++)
			{
				zb.Capture(i, s => ss.SaveStateBinary(new BinaryWriter(s)), j => stateCount--, true);
				stateCount++;
			}
			Assert.AreEqual(zb.Count, stateCount);
		}

		[TestMethod]
		public void SaveCreateBufferRoundTrip()
		{
			var buff = new ZwinderBuffer(new RewindConfig
			{
				UseCompression = false,
				BufferSize = 1,
				TargetFrameLength = 10
			});
			var ss = new StateSource { PaddingData = new byte[500] };
			for (var frame = 0; frame < 2090; frame++)
			{
				ss.Frame = frame;
				buff.Capture(frame, (s) => ss.SaveStateBinary(new BinaryWriter(s)));
			}
			// states are 504 bytes large, buffer is 1048576 bytes large
			Assert.AreEqual(buff.Count, 2080);
			Assert.AreEqual(buff.GetState(0).Frame, 10);
			Assert.AreEqual(buff.GetState(2079).Frame, 2089);
			Assert.AreEqual(StateSource.GetFrameNumberInState(buff.GetState(0).GetReadStream()), 10);
			Assert.AreEqual(StateSource.GetFrameNumberInState(buff.GetState(2079).GetReadStream()), 2089);

			var ms = new MemoryStream();
			buff.SaveStateBinary(new BinaryWriter(ms));
			ms.Position = 0;
			var buff2 = ZwinderBuffer.Create(new BinaryReader(ms));

			Assert.AreEqual(buff.Size, buff2.Size);
			Assert.AreEqual(buff.Used, buff2.Used);
			Assert.AreEqual(buff2.Count, 2080);
			Assert.AreEqual(buff2.GetState(0).Frame, 10);
			Assert.AreEqual(buff2.GetState(2079).Frame, 2089);
			Assert.AreEqual(StateSource.GetFrameNumberInState(buff2.GetState(0).GetReadStream()), 10);
			Assert.AreEqual(StateSource.GetFrameNumberInState(buff2.GetState(2079).GetReadStream()), 2089);
		}

		[TestMethod]
		public void StateBeforeFrame()
		{
			var ss = new StateSource { PaddingData = new byte[1000] };
			var zw = new ZwinderStateManager(new ZwinderStateManagerSettings
			{
				CurrentUseCompression = false,
				CurrentBufferSize = 1,
				CurrentTargetFrameLength = 10000,

				RecentUseCompression = false,
				RecentBufferSize = 1,
				RecentTargetFrameLength = 100000,

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
			var actual = StateSource.GetFrameNumberInState(kvp.Value);
			Assert.AreEqual(kvp.Key, actual);
			Assert.IsTrue(actual <= 10440);
		}

		[TestMethod]
		public void Last_Correct_WhenReservedGreaterThanCurrent()
		{
			const int futureReservedFrame = 1000;
			var ss = new StateSource { PaddingData = new byte[1000] };
			var zw = new ZwinderStateManager(new ZwinderStateManagerSettings());
			
			var ms = new MemoryStream();
			ss.SaveStateBinary(new BinaryWriter(ms));
			zw.Engage(ms.ToArray());

			zw.CaptureReserved(futureReservedFrame, ss);
			for (int i = 1; i < 10; i++)
			{
				zw.Capture(i, ss);
			}

			Assert.AreEqual(futureReservedFrame, zw.Last);
		}

		[TestMethod]
		public void Last_Correct_WhenCurrentIsLast()
		{
			const int totalCurrentFrames = 10;
			var ss = new StateSource { PaddingData = new byte[1000] };
			var zw = new ZwinderStateManager(new ZwinderStateManagerSettings());
			
			var ms = new MemoryStream();
			ss.SaveStateBinary(new BinaryWriter(ms));
			zw.Engage(ms.ToArray());

			for (int i = 1; i < totalCurrentFrames; i++)
			{
				zw.Capture(i, ss);
			}

			Assert.AreEqual(totalCurrentFrames - 1, zw.Last);
		}

		[TestMethod]
		public void HasState_Correct_WhenReservedGreaterThanCurrent()
		{
			const int futureReservedFrame = 1000;
			var ss = new StateSource { PaddingData = new byte[1000] };
			var zw = new ZwinderStateManager(new ZwinderStateManagerSettings());
			
			var ms = new MemoryStream();
			ss.SaveStateBinary(new BinaryWriter(ms));
			zw.Engage(ms.ToArray());

			zw.CaptureReserved(futureReservedFrame, ss);
			for (int i = 1; i < 10; i++)
			{
				zw.Capture(i, ss);
			}

			var actual = zw.HasState(futureReservedFrame);
			Assert.IsTrue(actual);
		}

		[TestMethod]
		public void HasState_Correct_WhenCurrentIsLast()
		{
			const int totalCurrentFrames = 10;
			var ss = new StateSource { PaddingData = new byte[1000] };
			var zw = new ZwinderStateManager(new ZwinderStateManagerSettings());
			
			var ms = new MemoryStream();
			ss.SaveStateBinary(new BinaryWriter(ms));
			zw.Engage(ms.ToArray());

			for (int i = 1; i < totalCurrentFrames; i++)
			{
				zw.Capture(i, ss);
			}

			var actual = zw.HasState(totalCurrentFrames - 1);
			Assert.IsTrue(actual);
		}

		[TestMethod]
		public void GetStateClosestToFrame_Correct_WhenReservedGreaterThanCurrent()
		{
			const int futureReservedFrame = 1000;
			var ss = new StateSource { PaddingData = new byte[1000] };
			var zw = new ZwinderStateManager(new ZwinderStateManagerSettings());
			
			var ms = new MemoryStream();
			ss.SaveStateBinary(new BinaryWriter(ms));
			zw.Engage(ms.ToArray());

			zw.CaptureReserved(futureReservedFrame, ss);
			for (int i = 1; i < 10; i++)
			{
				zw.Capture(i, ss);
			}

			var actual = zw.GetStateClosestToFrame(futureReservedFrame + 1);

			Assert.IsNotNull(actual);
			Assert.AreEqual(futureReservedFrame, actual.Key);
		}

		[TestMethod]
		public void GetStateClosestToFrame_Correct_WhenCurrentIsLast()
		{
			const int totalCurrentFrames = 10;
			var ss = new StateSource { PaddingData = new byte[1000] };
			var zw = new ZwinderStateManager(new ZwinderStateManagerSettings());
			
			var ms = new MemoryStream();
			ss.SaveStateBinary(new BinaryWriter(ms));
			zw.Engage(ms.ToArray());

			for (int i = 1; i < totalCurrentFrames; i++)
			{
				zw.Capture(i, ss);
			}

			var actual = zw.GetStateClosestToFrame(totalCurrentFrames);

			Assert.AreEqual(totalCurrentFrames - 1, actual.Key);
		}

		private class StateSource : IStatable
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
