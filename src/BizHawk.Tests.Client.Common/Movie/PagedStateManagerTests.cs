using System.Collections.Generic;
using System.IO;
using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Tests.Client.Common.Movie
{
	[TestClass]
	public class PagedStateManagerTests
	{
		// Current PagedStateManager uses 4KiB pages (with 4092 bytes of state data per page)
		private const int PAGE_COUNT = 256;
		private const int STATE_BYTES_PER_PAGE = 4092;

		private void WithRatioVariety(Action<PagedStateManager> action, IStatable source, List<int>? reserved = null)
		{
			reserved ??= new();

			PagedStateManager.PagedSettings settings = MakeDefaultSettings(2);
			PagedStateManager manager = new(settings, (f) => reserved.Contains(f));
			manager.Engage(source.CloneSavestate());

			action(manager);
			manager.InvalidateAfter(0);

			settings.NewToMidRatio = 10;
			manager = (PagedStateManager)manager.UpdateSettings(settings);
			action(manager);
			manager.InvalidateAfter(0);

			settings.NewToMidRatio = 0;
			manager = (PagedStateManager)manager.UpdateSettings(settings);
			action(manager);
		}

		private PagedStateManager.PagedSettings MakeDefaultSettings(double ratio = 2)
		{
			return new()
			{
				TotalMemoryLimitMB = 1,
				FramesBetweenNewStates = 1,
				FramesBetweenMidStates = 4,
				FramesBetweenOldStates = 12,
				NewToMidRatio = ratio,
			};
		}

		private IStatable CreateStateSource(int size = 8) => new StateSource { PaddingData = new byte[size - 4] };

		[TestMethod]
		public void CanSaveAndLoad()
		{
			IStatable ss = CreateStateSource(8);
			PagedStateManager manager = new(MakeDefaultSettings(), (f) => false);
			manager.Settings.FramesBetweenSavedStates = 1;
			manager.Engage(ss.CloneSavestate());
			for (int i = 0; i < 20; i++)
				manager.Capture(i, ss);

			int stateCount = manager.Count;

			MemoryStream ms = new();
			manager.SaveStateHistory(new BinaryWriter(ms));
			manager.Dispose();

			manager = new(MakeDefaultSettings(), (f) => false);
			ms.Seek(0, SeekOrigin.Begin);
			manager.LoadStateHistory(new BinaryReader(ms));
			manager.Engage(ss.CloneSavestate());

			Assert.AreEqual(stateCount, manager.Count);
		}

		[TestMethod]
		public void Last_Correct_WhenReservedGreaterThanCurrent()
		{
			const int futureReservedFrame = 1000;
			IStatable ss = CreateStateSource();
			WithRatioVariety((manager) =>
			{
				// Arrange
				manager.Capture(futureReservedFrame, ss);
				for (int i = 1; i < 20; i++)
				{
					manager.Capture(i, ss);
				}

				// Act
				var actual = manager.Last;

				// Assert
				Assert.AreEqual(futureReservedFrame, actual);
			}, ss, [ futureReservedFrame ]);
		}

		[TestMethod]
		public void Last_Correct_WhenNewIsLast()
		{
			const int totalCurrentFrames = 20;
			IStatable ss = CreateStateSource();

			WithRatioVariety((manager) =>
			{
				for (int i = 1; i < totalCurrentFrames; i++)
				{
					manager.Capture(i, ss);
				}

				Assert.AreEqual(totalCurrentFrames - 1, manager.Last);
			}, ss);
		}

		[TestMethod]
		public void HasState_Correct_WhenReservedGreaterThanNew()
		{
			const int futureReservedFrame = 1000;
			IStatable ss = CreateStateSource();
			WithRatioVariety((manager) =>
			{
				manager.Capture(futureReservedFrame, ss);
				for (int i = 1; i < 20; i++)
				{
					manager.Capture(i, ss);
				}

				Assert.IsTrue(manager.HasState(futureReservedFrame));
			}, ss, [ futureReservedFrame ]);
		}

		[TestMethod]
		public void HasState_Correct_WhenNewIsLast()
		{
			const int totalCurrentFrames = 20;
			IStatable ss = CreateStateSource();
			WithRatioVariety((manager) =>
			{
				for (int i = 1; i < totalCurrentFrames; i++)
				{
					manager.Capture(i, ss);
				}

				Assert.IsTrue(manager.HasState(totalCurrentFrames - 1));
			}, ss);
		}

		[TestMethod]
		public void GetStateClosestToFrame_Correct_WhenReservedGreaterThanNew()
		{
			const int futureReservedFrame = 1000;
			IStatable ss = CreateStateSource();
			WithRatioVariety((manager) =>
			{
				manager.Capture(futureReservedFrame, ss);
				for (int i = 1; i < 10; i++)
				{
					manager.Capture(i, ss);
				}

				Assert.AreEqual(futureReservedFrame, manager.GetStateClosestToFrame(futureReservedFrame + 1).Key);
			}, ss, [ futureReservedFrame ]);
		}

		[TestMethod]
		public void GetStateClosestToFrame_Correct_WhenNewIsLast()
		{
			const int totalCurrentFrames = 20;
			IStatable ss = CreateStateSource();
			WithRatioVariety((manager) =>
			{
				for (int i = 1; i < totalCurrentFrames; i++)
				{
					manager.Capture(i, ss);
				}

				Assert.AreEqual(totalCurrentFrames - 1, manager.GetStateClosestToFrame(totalCurrentFrames).Key);
			}, ss);
		}

		[TestMethod]
		public void InvalidateAfter_Correct_WhenReservedGreaterThanNew()
		{
			const int futureReservedFrame = 1000;
			IStatable ss = CreateStateSource();
			WithRatioVariety((manager) =>
			{
				manager.Capture(futureReservedFrame, ss);
				for (int i = 1; i < 10; i++)
				{
					manager.Capture(i, ss);
				}

				manager.InvalidateAfter(futureReservedFrame - 1);

				Assert.IsFalse(manager.HasState(futureReservedFrame));
			}, ss, [ futureReservedFrame ]);
		}

		[TestMethod]
		public void InvalidateAfter_Correct_WhenNewIsLast()
		{
			const int totalCurrentFrames = 10;
			IStatable ss = CreateStateSource();
			WithRatioVariety((manager) =>
			{
				for (int i = 1; i < totalCurrentFrames; i++)
				{
					manager.Capture(i, ss);
				}

				manager.InvalidateAfter(totalCurrentFrames - 1);

				Assert.IsFalse(manager.HasState(totalCurrentFrames));
			}, ss);
		}

		[TestMethod]
		public void Count_NoReserved()
		{
			const int totalFrames = 20;
			IStatable ss = CreateStateSource();
			WithRatioVariety((manager) =>
			{
				for (int i = 1; i < totalFrames; i++)
				{
					manager.Capture(i, ss);
				}

				Assert.AreEqual(totalFrames, manager.Count);
			}, ss);
		}

		[TestMethod]
		public void Count_WithReserved()
		{
			const int totalFrames = 20;
			const int reservedFrame = 1000;
			IStatable ss = CreateStateSource();
			WithRatioVariety((manager) =>
			{
				manager.Capture(reservedFrame, ss);
				for (int i = 1; i < totalFrames; i++)
				{
					manager.Capture(i, ss);
				}

				Assert.AreEqual(totalFrames + 1, manager.Count);
			}, ss, [ reservedFrame ]);
		}

		[TestMethod]
		public void Clear_KeepsZeroState()
		{
			const int reservedFrame = 1000;
			IStatable ss = CreateStateSource();
			WithRatioVariety((manager) =>
			{
				manager.Capture(reservedFrame, ss);
				for (int i = 1; i < 10; i++)
				{
					manager.Capture(i, ss);
				}

				manager.Clear();

				Assert.IsTrue(manager.HasState(0));
			}, ss, [ reservedFrame ]);
		}

		[TestMethod]
		public void TestKeepsAtLeastAncientInterval()
		{
			IStatable ss = CreateStateSource();
			WithRatioVariety((manager) =>
			{
				// Load branch with frame number almost at two old intervals
				int branchFrame = manager.Settings.FramesBetweenOldStates * 2 - 1;
				manager.Capture(branchFrame, ss, true);
				// Rewind to frame 0, play far enough that it will kick states before the branch.
				for (int i = 0; i < 2000; i++)
					manager.Capture(i, ss);
				// ASSERT: There are no gaps larger than the ancient interval
				int lastState = 0;
				while (lastState < branchFrame)
				{
					int nextState = manager.GetStateClosestToFrame(lastState + manager.Settings.FramesBetweenOldStates).Key;
					Assert.AreNotEqual(lastState, nextState, "FramesBetweenOldStates was not respected.");
					lastState = nextState;
				}
			}, ss);
		}

		[TestMethod]
		public void RecapturesExpectedNumberOfSmallStates()
		{
			IStatable ss = CreateStateSource(8);
			WithRatioVariety((manager) =>
			{
				for (int i = 0; i < PAGE_COUNT; i++)
					manager.Capture(i, ss);
				manager.InvalidateAfter(0);
				for (int i = 0; i < PAGE_COUNT; i++)
					manager.Capture(i, ss);

				Assert.IsTrue(manager.HasState(1));
				manager.Capture(PAGE_COUNT, ss);
				Assert.IsFalse(manager.HasState(1));
			}, ss);
		}

		[TestMethod]
		public void RecapturesExpectedNumberOfLargeStates()
		{
			IStatable ss = CreateStateSource(11000);
			const int PAGES_PER_STATE = (11000 - 1) / STATE_BYTES_PER_PAGE + 1;
			const int MAX_STATES = PAGE_COUNT / PAGES_PER_STATE;

			WithRatioVariety((manager) =>
			{
				for (int i = 0; i < MAX_STATES; i++)
					manager.Capture(i, ss);
				manager.InvalidateAfter(0);
				for (int i = 0; i < MAX_STATES; i++)
					manager.Capture(i, ss);

				Assert.IsTrue(manager.HasState(1));
				manager.Capture(MAX_STATES, ss);
				Assert.IsFalse(manager.HasState(1));
			}, ss);
		}

		[TestMethod]
		public void DoesNotCaptureEachFrameWhenFullOfOldStates()
		{
			IStatable ss = CreateStateSource();
			WithRatioVariety((manager) =>
			{
				int endFrame = PAGE_COUNT * manager.Settings.FramesBetweenOldStates;
				for (int i = 0; i <= endFrame; i++)
					manager.Capture(i, ss);

				int frame = endFrame;
				// We're on a frame where we expect to have an "old" state.
				Assert.IsTrue(manager.HasState(frame));
				// But the next few won't.
				for (int i = 0; i < manager.Settings.FramesBetweenOldStates - 1; i++)
				{
					frame++;
					manager.Capture(frame, ss);
					Assert.IsFalse(manager.HasState(frame));
				}
				// And next one once again should capture.
				frame++;
				manager.Capture(frame, ss);
				Assert.IsTrue(manager.HasState(frame));
			}, ss);
		}

		[TestMethod]
		public void BeginsUsingMidInterval()
		{
			IStatable ss = CreateStateSource(8);
			WithRatioVariety((manager) =>
			{
				for (int i = 0; i < PAGE_COUNT; i++)
					manager.Capture(i, ss);

				for (int i = PAGE_COUNT; i < PAGE_COUNT + manager.Settings.FramesBetweenMidStates - 1; i++)
				{
					// Has state before capture, drops it after
					int theFrame = i - (PAGE_COUNT - 1);
					Assert.IsTrue(manager.HasState(theFrame));
					manager.Capture(i, ss);
					Assert.IsFalse(manager.HasState(theFrame));
				}
				// Has state before capture, keeps it after because we've reached FramesBetweenMidStates
				Assert.IsTrue(manager.HasState(manager.Settings.FramesBetweenMidStates));
				manager.Capture(manager.Settings.FramesBetweenMidStates + PAGE_COUNT, ss);
				Assert.IsTrue(manager.HasState(manager.Settings.FramesBetweenMidStates));
			}, ss);
		}

		[TestMethod]
		public void UsesNewIntervalInGapWhenPossible()
		{
			IStatable ss = CreateStateSource(8);
			WithRatioVariety((manager) =>
			{
				for (int i = 1000; i < 1020; i++)
					manager.Capture(i, ss);

				for (int i = 0; i < 20; i++)
					manager.Capture(i, ss);

				// Every frame 0-20 should have a state now, since the buffer isn't full yet.
				for (int i = 0; i < 20; i += manager.Settings.FramesBetweenNewStates)
					Assert.IsTrue(manager.HasState(i));
			}, ss);
		}

		[TestMethod]
		public void BeginsUsingMidIntervalInGap()
		{
			IStatable ss = CreateStateSource(8);
			WithRatioVariety((manager) =>
			{
				const int futureStateCount = 20;
				for (int i = 1000; i < 1000 + futureStateCount; i++)
					manager.Capture(i, ss);

				const int bufferFullFrame = PAGE_COUNT - futureStateCount;
				for (int i = 0; i < bufferFullFrame; i++)
					manager.Capture(i, ss);

				for (int i = bufferFullFrame; i < bufferFullFrame + manager.Settings.FramesBetweenMidStates - 1; i++)
				{
					// Has state before capture, drops it after
					int theFrame = i - (bufferFullFrame - 1);
					Assert.IsTrue(manager.HasState(theFrame));
					manager.Capture(i, ss);
					Assert.IsFalse(manager.HasState(theFrame));
				}
				// Has state before capture, keeps it after because we've reached FramesBetweenMidStates
				Assert.IsTrue(manager.HasState(manager.Settings.FramesBetweenMidStates));
				manager.Capture(manager.Settings.FramesBetweenMidStates + bufferFullFrame, ss);
				Assert.IsTrue(manager.HasState(manager.Settings.FramesBetweenMidStates));
			}, ss);
		}

		[TestMethod]
		public void BufferTooSmallDoesntBreakEverything()
		{
			PagedStateManager.PagedSettings settings = MakeDefaultSettings();
			IStatable giantSource = CreateStateSource(settings.TotalMemoryLimitMB * 1024 * 1024 + 4);
			IStatable smallSource = CreateStateSource();
			PagedStateManager manager = new(settings, (f) => false);
			manager.Engage(smallSource.CloneSavestate());

			manager.Capture(1, smallSource);

			Assert.Throws<Exception>(() => manager.Capture(2, giantSource));

			// Do we still have the use of all pages?
			for (int i = 0; i < PAGE_COUNT * settings.FramesBetweenOldStates; i += settings.FramesBetweenOldStates)
				manager.Capture(i, smallSource);
			Assert.AreEqual(PAGE_COUNT, manager.Count);
		}

		[TestMethod]
		public void CanHandleLargeBuffer()
		{
			// This should ensure there aren't any bugs like using a 32-bit value where a 64-bit one is needed.
			IStatable ss = CreateStateSource(STATE_BYTES_PER_PAGE * 999 + 1);
			PagedStateManager manager = new(new()
			{
				TotalMemoryLimitMB = 4400,
				FramesBetweenNewStates = 1,
				FramesBetweenMidStates = 1,
				FramesBetweenOldStates = 1,
			}, (f) => false);
			manager.Engage(ss.CloneSavestate());
			int expectedStates = (manager.Settings.TotalMemoryLimitMB * 1024 / 4) / 1000;

			for (int i = 0; i < expectedStates * 1.2; i++)
				manager.Capture(i, ss);

			Assert.AreEqual(expectedStates, manager.Count);

			manager.InvalidateAfter(0);
			for (int i = 0; i < expectedStates * 1.2; i++)
				manager.Capture(i, ss);

			Assert.AreEqual(expectedStates, manager.Count);
		}

		[TestMethod]
		public void KeepsReservedFrames()
		{
			const int reservedFrame1 = 7;
			int reservedFrame2 = 0;

			IStatable ss = CreateStateSource();
			PagedStateManager manager = new(MakeDefaultSettings(), (f) => f == reservedFrame1 || f == reservedFrame2);
			manager.Engage(ss.CloneSavestate());

			for (int i = 0; i < PAGE_COUNT; i++)
				manager.Capture(i, ss);

			// Reserve a frame after capture
			reservedFrame2 = 13;
			for (int i = PAGE_COUNT; i < PAGE_COUNT * 2; i++)
				manager.Capture(i, ss);

			Assert.IsTrue(manager.HasState(reservedFrame1));
			Assert.IsTrue(manager.HasState(reservedFrame2));
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
