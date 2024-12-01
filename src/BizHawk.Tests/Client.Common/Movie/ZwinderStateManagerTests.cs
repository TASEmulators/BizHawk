using System.IO;
using System.Linq;
using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Tests.Client.Common.Movie
{
#if !(NETSTANDARD2_1_OR_GREATER || NETCOREAPP2_1_OR_GREATER)
	internal static class Extensions
	{
		public static void NextBytes(this Random rng, Span<byte> buf)
		{
			var a = buf.ToArray();
			rng.NextBytes(a);
			a.CopyTo(buf);
		}

		public static int Read(this BinaryReader br, Span<byte> buf)
		{
			var a = buf.ToArray();
			var result = br.Read(a, index: 0, count: a.Length);
			a.CopyTo(buf);
			return result;
		}

		public static void Write(this BinaryWriter bw, ReadOnlySpan<byte> buf)
			=> bw.Write(buf.ToArray());

		public static void Write(this Stream stream, ReadOnlySpan<byte> buf)
			=> stream.Write(buf.ToArray(), offset: 0, count: buf.Length);
	}
#endif

	[TestClass]
	public class ZwinderStateManagerTests
	{
		private ZwinderStateManager CreateSmallZwinder(IStatable ss)
		{
			var zw = new ZwinderStateManager(new ZwinderStateManagerSettings
			{
				CurrentBufferSize = 1,
				CurrentTargetFrameLength = 10000,

				RecentBufferSize = 1,
				RecentTargetFrameLength = 100000,

				AncientStateInterval = 50000
			}, f => false);

			var ms = new MemoryStream();
			ss.SaveStateBinary(new BinaryWriter(ms));
			zw.Engage(ms.ToArray());
			return zw;
		}

		private IStatable CreateStateSource() => new StateSource {PaddingData = new byte[1000]};

		[TestMethod]
		public void SaveCreateRoundTrip()
		{
			var ms = new MemoryStream();
			var zw = new ZwinderStateManager(new ZwinderStateManagerSettings
			{
				CurrentBufferSize = 16,
				CurrentTargetFrameLength = 10000,

				RecentBufferSize = 16,
				RecentTargetFrameLength = 100000,

				AncientStateInterval = 50000
			}, f => false);
			zw.SaveStateHistory(new BinaryWriter(ms));
			var buff = ms.ToArray();
			var rms = new MemoryStream(buff, false);

			var zw2 = ZwinderStateManager.Create(new BinaryReader(rms), zw.Settings, f => false);

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
			RewindConfig config = new RewindConfig
			{
				BufferSize = 1,
				TargetFrameLength = 10
			};
			var buff = new ZwinderBuffer(config);
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
			var buff2 = ZwinderBuffer.Create(new BinaryReader(ms), config);

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
				CurrentBufferSize = 1,
				CurrentTargetFrameLength = 10000,

				RecentBufferSize = 1,
				RecentTargetFrameLength = 100000,

				AncientStateInterval = 50000
			}, f => false);
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
			var (f, data) = zw.GetStateClosestToFrame(10440);
			var actual = StateSource.GetFrameNumberInState(data);
			Assert.AreEqual(f, actual);
			Assert.IsTrue(actual <= 10440);
		}

		[TestMethod]
		public void Last_Correct_WhenReservedGreaterThanCurrent()
		{
			// Arrange
			const int futureReservedFrame = 1000;
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);
			
			zw.CaptureReserved(futureReservedFrame, ss);
			for (int i = 1; i < 20; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			var actual = zw.Last;

			// Assert
			Assert.AreEqual(futureReservedFrame, actual);
		}

		[TestMethod]
		public void Last_Correct_WhenCurrentIsLast()
		{
			// Arrange
			const int totalCurrentFrames = 20;
			const int expectedFrameGap = 9;
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);

			for (int i = 1; i < totalCurrentFrames; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			var actual = zw.Last;

			// Assert
			Assert.AreEqual(totalCurrentFrames - expectedFrameGap, actual);
		}

		[TestMethod]
		public void HasState_Correct_WhenReservedGreaterThanCurrent()
		{
			// Arrange
			const int futureReservedFrame = 1000;
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);

			zw.CaptureReserved(futureReservedFrame, ss);
			for (int i = 1; i < 20; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			var actual = zw.HasState(futureReservedFrame);

			// Assert
			Assert.IsTrue(actual);
		}

		[TestMethod]
		public void HasState_Correct_WhenCurrentIsLast()
		{
			// Arrange
			const int totalCurrentFrames = 20;
			const int expectedFrameGap = 9;
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);

			for (int i = 1; i < totalCurrentFrames; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			var actual = zw.HasState(totalCurrentFrames - expectedFrameGap);

			// Assert
			Assert.IsTrue(actual);
		}

		[TestMethod]
		public void GetStateClosestToFrame_Correct_WhenReservedGreaterThanCurrent()
		{
			// Arrange
			const int futureReservedFrame = 1000;
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);

			zw.CaptureReserved(futureReservedFrame, ss);
			for (int i = 1; i < 10; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			var actual = zw.GetStateClosestToFrame(futureReservedFrame + 1);

			// Assert
			Assert.IsNotNull(actual);
			Assert.AreEqual(futureReservedFrame, actual.Key);
		}

		[TestMethod]
		public void GetStateClosestToFrame_Correct_WhenCurrentIsLast()
		{
			// Arrange
			const int totalCurrentFrames = 20;
			const int expectedFrameGap = 9;
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);

			for (int i = 1; i < totalCurrentFrames; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			var actual = zw.GetStateClosestToFrame(totalCurrentFrames);

			// Assert
			Assert.AreEqual(totalCurrentFrames - expectedFrameGap, actual.Key);
		}

		[TestMethod]
		public void InvalidateAfter_Correct_WhenReservedGreaterThanCurrent()
		{
			// Arrange
			const int futureReservedFrame = 1000;
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);

			zw.CaptureReserved(futureReservedFrame, ss);
			for (int i = 1; i < 10; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			zw.InvalidateAfter(futureReservedFrame - 1);

			// Assert
			Assert.IsFalse(zw.HasState(futureReservedFrame));
		}

		[TestMethod]
		public void InvalidateAfter_Correct_WhenCurrentIsLast()
		{
			// Arrange
			const int totalCurrentFrames = 10;
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);

			for (int i = 1; i < totalCurrentFrames; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			zw.InvalidateAfter(totalCurrentFrames - 1);

			// Assert
			Assert.IsFalse(zw.HasState(totalCurrentFrames));
		}

		[TestMethod]
		public void Count_NoReserved()
		{
			// Arrange
			const int totalCurrentFrames = 20;
			const int expectedFrameGap = 10;
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);

			for (int i = 1; i < totalCurrentFrames; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			var actual = zw.Count;

			// Assert
			var expected = (totalCurrentFrames / expectedFrameGap) + 1;
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void Count_WithReserved()
		{
			// Arrange
			const int totalCurrentFrames = 20;
			const int expectedFrameGap = 10;
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);

			zw.CaptureReserved(1000, ss);
			for (int i = 1; i < totalCurrentFrames; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			var actual = zw.Count;

			// Assert
			var expected = (totalCurrentFrames / expectedFrameGap) + 2;
			Assert.AreEqual(expected, actual);
		}

		[TestMethod]
		public void StateCache()
		{
			var ss = CreateStateSource();
			var zw = new ZwinderStateManager(new ZwinderStateManagerSettings
			{
				CurrentBufferSize = 2,
				CurrentTargetFrameLength = 1000,
				RecentBufferSize = 2,
				RecentTargetFrameLength = 1000,
				AncientStateInterval = 100
			}, f => false);

			for (int i = 0; i < 1000; i += 200)
			{
				zw.CaptureReserved(i, ss);
			}

			for (int i = 400; i < 1000; i += 400)
			{
				zw.EvictReserved(i);
			}

			for (int i = 0; i < 10000; i++)
			{
				zw.Capture(i, ss);
			}

			zw.Capture(101, ss);

			var allStates = zw.AllStates()
				.Select(s => s.Frame)
				.ToList();

			for (int i = 0; i < 10000; i++)
			{
				var actual = zw.HasState(i);
				var expected = allStates.Contains(i);
				Assert.AreEqual(expected, actual);
			}
		}

		[TestMethod]
		public void Clear_KeepsZeroState()
		{
			// Arrange
			var ss = CreateStateSource();
			using var zw = CreateSmallZwinder(ss);

			zw.CaptureReserved(1000, ss);
			for (int i = 1; i < 10; i++)
			{
				zw.Capture(i, ss);
			}

			// Act
			zw.Clear();

			// Assert
			Assert.AreEqual(1, zw.AllStates().Count());
			Assert.AreEqual(0, zw.AllStates().Single().Frame);
		}

		[TestMethod]
		public void WhatIfTheHeadStateWrapsAround()
		{
			var ss = new StateSource
			{
				PaddingData = new byte[400 * 1000]
			};
			using var zw = new ZwinderBuffer(new RewindConfig
			{
				BufferSize = 1,
				TargetFrameLength = 1
			});

			// Need to get data in the zwinderbuffer so that the last state, and the last state in particular, wraps around
			ss.Frame = 1;
			zw.Capture(1, s => ss.SaveStateBinary(new BinaryWriter(s)), null, true);
			ss.Frame = 2;
			zw.Capture(2, s => ss.SaveStateBinary(new BinaryWriter(s)), null, true);
			ss.Frame = 3;
			zw.Capture(3, s => ss.SaveStateBinary(new BinaryWriter(s)), null, true);

			zw.SaveStateBinary(new BinaryWriter(new MemoryStream()));
		}

		[TestMethod]
		public void TestReadByteCorruption()
		{
			using var zw = new ZwinderBuffer(new RewindConfig
			{
				BufferSize = 1,
				TargetFrameLength = 1
			});
			zw.Capture(0, s =>
			{
				s.Write(new byte[] { 1, 2, 3, 4 });
			});
			zw.Capture(1, s =>
			{
				s.Write(new byte[] { 5, 6, 7, 8 });
			});
			var state = zw.GetState(0);
			Assert.AreEqual(0, state.Frame);
			Assert.AreEqual(4, state.Size);
			Assert.AreEqual(1, state.GetReadStream().ReadByte());
		}

		[TestMethod]
		public void TestReadBytesCorruption()
		{
			using var zw = new ZwinderBuffer(new RewindConfig
			{
				BufferSize = 1,
				TargetFrameLength = 1
			});
			zw.Capture(0, s =>
			{
				s.Write(new byte[] { 1, 2, 3, 4 });
			});
			zw.Capture(1, s =>
			{
				s.Write(new byte[] { 5, 6, 7, 8 });
			});
			var state = zw.GetState(0);
			Assert.AreEqual(0, state.Frame);
			Assert.AreEqual(4, state.Size);
			var bb = new byte[2];
			_ = state.GetReadStream().Read(bb, offset: 0, count: bb.Length);
			Assert.AreEqual(1, bb[0]);
			Assert.AreEqual(2, bb[1]);
		}

		[TestMethod]
		public void TestWriteByteCorruption()
		{
			using var zw = new ZwinderBuffer(new RewindConfig
			{
				BufferSize = 1,
				TargetFrameLength = 1
			});
			zw.Capture(0, s =>
			{
				s.WriteByte(1);
				s.WriteByte(2);
				s.WriteByte(3);
				s.WriteByte(4);
			});
			zw.Capture(1, s =>
			{
				s.WriteByte(5);
				s.WriteByte(6);
				s.WriteByte(7);
				s.WriteByte(8);
			});
			zw.GetState(0).GetReadStream(); // Rewinds the backing store
			zw.Capture(2, s =>
			{
				s.WriteByte(9);
				s.WriteByte(10);
				s.WriteByte(11);
				s.WriteByte(12);
			});

			var state = zw.GetState(0);
			Assert.AreEqual(0, state.Frame);
			Assert.AreEqual(4, state.Size);
			Assert.AreEqual(1, state.GetReadStream().ReadByte());
		}

		[TestMethod]
		public void BufferStressTest()
		{
			var r = new Random(8675309);
			using var zw = new ZwinderBuffer(new RewindConfig
			{
				BufferSize = 1,
				TargetFrameLength = 1
			});
			var buff = new byte[40000];

			for (int round = 0; round < 10; round++)
			{
				for (int i = 0; i < 500; i++)
				{
					zw.Capture(i, s =>
					{
						var length = r.Next(40000);
						var bw = new BinaryWriter(s);
						var bytes = buff.AsSpan(0, length);
						r.NextBytes(bytes);
						bw.Write(length);
						bw.Write(bytes);
						bw.Write(CRC32.Calculate(bytes));
					});
				}
				for (int i = 0; i < zw.Count; i++)
				{
					var info = zw.GetState(i);
					var s = info.GetReadStream();
					var br = new BinaryReader(s);
					var length = info.Size;
					if (length != br.ReadInt32() + 8)
						throw new Exception("Length field corrupted");
					var bytes = buff.AsSpan(0, length - 8);
					br.Read(bytes);
					if (br.ReadUInt32() != CRC32.Calculate(bytes))
						throw new Exception("Data or CRC field corrupted");
				}
			}
		}

		private class StateSource : IStatable
		{
			public int Frame { get; set; }
			public byte[] PaddingData { get; set; } = Array.Empty<byte>();

			public bool AvoidRewind => false;

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
