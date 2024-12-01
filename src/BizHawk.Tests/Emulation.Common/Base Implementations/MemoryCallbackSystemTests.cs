using System.Collections.Generic;
using System.Linq;
using BizHawk.Emulation.Common;

namespace BizHawk.Tests.Emulation.Common
{
	[TestClass]
	public class MemoryCallbackSystemTests
	{
		private const string ScopeA = "Scope A";

		private const string ScopeB = "Scope B";

		private MemoryCallbackSystem _memoryCallbackSystem = null!;

		private TestCallbackHolder _testCallbacks = null!;

		[TestInitialize]
		public void TestInitialize()
		{
			_memoryCallbackSystem = new(new[] { ScopeA, ScopeB });
			_testCallbacks = new();
		}

		[TestMethod]
		public void TestAddRemoveEvents()
		{
			MemoryCallback callback1 = new(ScopeA, MemoryCallbackType.Read, "Callback 1", _testCallbacks.Callback1, null, null);
			MemoryCallback callback2 = new(ScopeA, MemoryCallbackType.Read, "Callback 2", _testCallbacks.Callback2, null, null);
			MemoryCallback callback3 = new(ScopeA, MemoryCallbackType.Read, "Callback 3", _testCallbacks.Callback3, null, null);

			List<IMemoryCallback> callbackAddedRaised = new();
			List<IMemoryCallback> callbackRemovedRaised = new();

			_memoryCallbackSystem.CallbackAdded += callback => callbackAddedRaised.Add(callback);
			_memoryCallbackSystem.CallbackRemoved += callback => callbackRemovedRaised.Add(callback);

			_memoryCallbackSystem.Add(callback1);
			_memoryCallbackSystem.Add(callback2);
			_memoryCallbackSystem.Add(callback3);

			CollectionAssert.AreEqual(
				new[] { callback1, callback2, callback3 },
				callbackAddedRaised,
				"CallbackAdded events not raised correctly");

			_memoryCallbackSystem.Remove(callback3.Callback);
			_memoryCallbackSystem.Remove(callback1.Callback);
			_memoryCallbackSystem.Remove(callback2.Callback);

			CollectionAssert.AreEqual(
				new[] { callback3, callback1, callback2 },
				callbackRemovedRaised,
				"CallbackRemoved events not raised correctly");
		}

		[TestMethod]
		public void TestActiveChangedEvent()
		{
			MemoryCallback readCallback = new(ScopeA, MemoryCallbackType.Read, "Callback 1", _testCallbacks.Callback1, null, null);
			MemoryCallback writeCallback = new(ScopeA, MemoryCallbackType.Write, "Callback 2", _testCallbacks.Callback2, null, null);
			MemoryCallback execCallback = new(ScopeA, MemoryCallbackType.Execute, "Callback 3", _testCallbacks.Callback3, null, null);

			var activeChangedInvoked = false;
			_memoryCallbackSystem.ActiveChanged += () => activeChangedInvoked = true;

			Assert.IsFalse(_memoryCallbackSystem.HasReads);
			Assert.IsFalse(_memoryCallbackSystem.HasWrites);
			Assert.IsFalse(_memoryCallbackSystem.HasExecutes);

			_memoryCallbackSystem.Add(readCallback);
			Assert.IsTrue(activeChangedInvoked);
			Assert.IsTrue(_memoryCallbackSystem.HasReads);
			Assert.IsFalse(_memoryCallbackSystem.HasWrites);
			Assert.IsFalse(_memoryCallbackSystem.HasExecutes);

			activeChangedInvoked = false;
			_memoryCallbackSystem.Add(writeCallback);
			Assert.IsTrue(activeChangedInvoked);
			Assert.IsTrue(_memoryCallbackSystem.HasReads);
			Assert.IsTrue(_memoryCallbackSystem.HasWrites);
			Assert.IsFalse(_memoryCallbackSystem.HasExecutes);

			activeChangedInvoked = false;
			_memoryCallbackSystem.Add(execCallback);
			Assert.IsTrue(activeChangedInvoked);
			Assert.IsTrue(_memoryCallbackSystem.HasReads);
			Assert.IsTrue(_memoryCallbackSystem.HasWrites);
			Assert.IsTrue(_memoryCallbackSystem.HasExecutes);

			activeChangedInvoked = false;
			_memoryCallbackSystem.Remove(execCallback.Callback);
			Assert.IsTrue(activeChangedInvoked);
			Assert.IsTrue(_memoryCallbackSystem.HasReads);
			Assert.IsTrue(_memoryCallbackSystem.HasWrites);
			Assert.IsFalse(_memoryCallbackSystem.HasExecutes);

			activeChangedInvoked = false;
			_memoryCallbackSystem.RemoveAll(new[] { writeCallback.Callback });
			Assert.IsTrue(activeChangedInvoked);
			Assert.IsTrue(_memoryCallbackSystem.HasReads);
			Assert.IsFalse(_memoryCallbackSystem.HasWrites);
			Assert.IsFalse(_memoryCallbackSystem.HasExecutes);

			activeChangedInvoked = false;
			_memoryCallbackSystem.Clear();
			Assert.IsTrue(activeChangedInvoked);
			Assert.IsFalse(_memoryCallbackSystem.HasReads);
			Assert.IsFalse(_memoryCallbackSystem.HasWrites);
			Assert.IsFalse(_memoryCallbackSystem.HasExecutes);
		}

		[TestMethod]
		public void TestReadCallbacks()
		{
			MemoryCallback callback1 = new(ScopeA, MemoryCallbackType.Read, "Callback 1", _testCallbacks.Callback1, 0x12345678, null);
			MemoryCallback callback2 = new(ScopeA, MemoryCallbackType.Write, "Callback 2", _testCallbacks.Callback2, 0x12345678, null);
			MemoryCallback callback3 = new(ScopeA, MemoryCallbackType.Execute, "Callback 3", _testCallbacks.Callback3, 0x12345678, null);

			_memoryCallbackSystem.Add(callback1);
			_memoryCallbackSystem.Add(callback2);
			_memoryCallbackSystem.Add(callback3);

			_memoryCallbackSystem.CallMemoryCallbacks(0x12345678, 1, (uint) MemoryCallbackFlags.AccessRead, ScopeA);
			_memoryCallbackSystem.CallMemoryCallbacks(0x12345678, 2, (uint) MemoryCallbackFlags.AccessRead, ScopeA);
			_memoryCallbackSystem.CallMemoryCallbacks(0x12345678, 3, (uint) MemoryCallbackFlags.AccessRead, ScopeB);
			_memoryCallbackSystem.CallMemoryCallbacks(0x23456789, 4, (uint) MemoryCallbackFlags.AccessRead, ScopeA);

			CollectionAssert.AreEqual(
				new[]
				{
					(0x12345678U, 1U, (uint) MemoryCallbackFlags.AccessRead),
					(0x12345678U, 2U, (uint) MemoryCallbackFlags.AccessRead),
				},
				_testCallbacks.Callback1Invocations,
				"Read callbacks not invoked correctly");
			Assert.AreEqual(0, _testCallbacks.Callback2Invocations.Count, "Write callback invoked unexpectedly");
			Assert.AreEqual(0, _testCallbacks.Callback3Invocations.Count, "Exec callback invoked unexpectedly");
		}

		[TestMethod]
		public void TestWriteCallbacks()
		{
			MemoryCallback callback1 = new(ScopeA, MemoryCallbackType.Read, "Callback 1", _testCallbacks.Callback1, 0x12345678, null);
			MemoryCallback callback2 = new(ScopeA, MemoryCallbackType.Write, "Callback 2", _testCallbacks.Callback2, 0x12345678, null);
			MemoryCallback callback3 = new(ScopeA, MemoryCallbackType.Execute, "Callback 3", _testCallbacks.Callback3, 0x12345678, null);

			_memoryCallbackSystem.Add(callback1);
			_memoryCallbackSystem.Add(callback2);
			_memoryCallbackSystem.Add(callback3);

			_memoryCallbackSystem.CallMemoryCallbacks(0x12345678, 1, (uint) MemoryCallbackFlags.AccessWrite, ScopeA);
			_memoryCallbackSystem.CallMemoryCallbacks(0x12345678, 2, (uint) MemoryCallbackFlags.AccessWrite, ScopeA);
			_memoryCallbackSystem.CallMemoryCallbacks(0x12345678, 3, (uint) MemoryCallbackFlags.AccessWrite, ScopeB);
			_memoryCallbackSystem.CallMemoryCallbacks(0x23456789, 4, (uint) MemoryCallbackFlags.AccessWrite, ScopeA);

			CollectionAssert.AreEqual(
				new[]
				{
					(0x12345678U, 1U, (uint) MemoryCallbackFlags.AccessWrite),
					(0x12345678U, 2U, (uint) MemoryCallbackFlags.AccessWrite),
				},
				_testCallbacks.Callback2Invocations,
				"Write callbacks not invoked correctly");
			Assert.AreEqual(0, _testCallbacks.Callback1Invocations.Count, "Read callback invoked unexpectedly");
			Assert.AreEqual(0, _testCallbacks.Callback3Invocations.Count, "Exec callback invoked unexpectedly");
		}

		[TestMethod]
		public void TestExecCallbacks()
		{
			MemoryCallback callback1 = new(ScopeA, MemoryCallbackType.Read, "Callback 1", _testCallbacks.Callback1, 0x12345678, null);
			MemoryCallback callback2 = new(ScopeA, MemoryCallbackType.Write, "Callback 2", _testCallbacks.Callback2, 0x12345678, null);
			MemoryCallback callback3 = new(ScopeA, MemoryCallbackType.Execute, "Callback 3", _testCallbacks.Callback3, 0x12345678, null);

			_memoryCallbackSystem.Add(callback1);
			_memoryCallbackSystem.Add(callback2);
			_memoryCallbackSystem.Add(callback3);

			_memoryCallbackSystem.CallMemoryCallbacks(0x12345678, 1, (uint) MemoryCallbackFlags.AccessExecute, ScopeA);
			_memoryCallbackSystem.CallMemoryCallbacks(0x12345678, 2, (uint) MemoryCallbackFlags.AccessExecute, ScopeA);
			_memoryCallbackSystem.CallMemoryCallbacks(0x12345678, 3, (uint) MemoryCallbackFlags.AccessExecute, ScopeB);
			_memoryCallbackSystem.CallMemoryCallbacks(0x23456789, 4, (uint) MemoryCallbackFlags.AccessExecute, ScopeA);

			CollectionAssert.AreEqual(
				new[]
				{
					(0x12345678U, 1U, (uint) MemoryCallbackFlags.AccessExecute),
					(0x12345678U, 2U, (uint) MemoryCallbackFlags.AccessExecute),
				},
				_testCallbacks.Callback3Invocations,
				"Exec callbacks not invoked correctly");
			Assert.AreEqual(0, _testCallbacks.Callback1Invocations.Count, "Read callback invoked unexpectedly");
			Assert.AreEqual(0, _testCallbacks.Callback2Invocations.Count, "Write callback invoked unexpectedly");
		}

		[TestMethod]
		public void TestAddingCallbackWithinCallback()
		{
			MemoryCallback callback2 = new(ScopeA, MemoryCallbackType.Read, "Callback 2", _testCallbacks.Callback2, null, null);
			MemoryCallback callback3 = new(ScopeA, MemoryCallbackType.Read, "Callback 3", _testCallbacks.Callback3, null, null);

			var callback1invoked = false;
			MemoryCallbackDelegate callback = (_, _, _) =>
			{
				callback1invoked = true;
				_memoryCallbackSystem.Add(callback2);
				_memoryCallbackSystem.Add(callback3);
			};

			MemoryCallback callback1 = new(ScopeA, MemoryCallbackType.Read, "Callback 1", callback, null, null);

			_memoryCallbackSystem.Add(callback1);
			_memoryCallbackSystem.CallMemoryCallbacks(0, 0, (uint) MemoryCallbackFlags.AccessRead, ScopeA);

			Assert.IsTrue(callback1invoked, "Callback 1 not invoked");
			CollectionAssert.AreEqual(
				new[] { callback1, callback2, callback3 },
				_memoryCallbackSystem.ToList(),
				"Callback list is incorrect");
		}

		[TestMethod]
		public void TestRemovingCallbackWithinCallback()
		{
			MemoryCallback callback1 = new(ScopeA, MemoryCallbackType.Read, "Callback 1", _testCallbacks.Callback1, null, null);

			var callback2invoked = false;
			MemoryCallbackDelegate callback = (_, _, _) =>
			{
				callback2invoked = true;
				_memoryCallbackSystem.Remove(callback1.Callback);
			};

			MemoryCallback callback2 = new(ScopeA, MemoryCallbackType.Read, "Callback 2", callback, null, null);
			MemoryCallback callback3 = new(ScopeA, MemoryCallbackType.Read, "Callback 3", _testCallbacks.Callback3, null, null);

			_memoryCallbackSystem.Add(callback1);
			_memoryCallbackSystem.Add(callback2);
			_memoryCallbackSystem.Add(callback3);

			_memoryCallbackSystem.CallMemoryCallbacks(0, 0, (uint) MemoryCallbackFlags.AccessRead, ScopeA);

			Assert.AreEqual(1, _testCallbacks.Callback1Invocations.Count, "Callback 1 not invoked correctly");
			Assert.IsTrue(callback2invoked, "Callback 2 not invoked");
			Assert.AreEqual(1, _testCallbacks.Callback3Invocations.Count, "Callback 3 not invoked correctly");
			CollectionAssert.AreEqual(
				new[] { callback2, callback3 },
				_memoryCallbackSystem.ToList(),
				"Callback list is incorrect");
		}

		[TestMethod]
		public void TestRemovingSelfWithinCallback()
		{
			MemoryCallback callback1 = new(ScopeA, MemoryCallbackType.Read, "Callback 1", _testCallbacks.Callback1, null, null);

			MemoryCallback? callback2 = null;
			var callback2invoked = false;
			MemoryCallbackDelegate callback = (_, _, _) =>
			{
				callback2invoked = true;
				_memoryCallbackSystem.Remove(callback2!.Callback);
			};

			callback2 = new(ScopeA, MemoryCallbackType.Read, "Callback 2", callback, null, null);
			MemoryCallback callback3 = new(ScopeA, MemoryCallbackType.Read, "Callback 3", _testCallbacks.Callback3, null, null);

			_memoryCallbackSystem.Add(callback1);
			_memoryCallbackSystem.Add(callback2);
			_memoryCallbackSystem.Add(callback3);

			_memoryCallbackSystem.CallMemoryCallbacks(0, 0, (uint) MemoryCallbackFlags.AccessRead, ScopeA);

			Assert.AreEqual(1, _testCallbacks.Callback1Invocations.Count, "Callback 1 not invoked correctly");
			Assert.IsTrue(callback2invoked, "Callback 2 not invoked");
			Assert.AreEqual(1, _testCallbacks.Callback3Invocations.Count, "Callback 3 not invoked correctly");
			CollectionAssert.AreEqual(
				new[] { callback1, callback3 },
				_memoryCallbackSystem.ToList(),
				"Callback list is incorrect");
		}

		private sealed class TestCallbackHolder
		{
			public List<(uint Address, uint Value, uint Flags)> Callback1Invocations { get; } = new();

			public List<(uint Address, uint Value, uint Flags)> Callback2Invocations { get; } = new();

			public List<(uint Address, uint Value, uint Flags)> Callback3Invocations { get; } = new();

			public void Callback1(uint address, uint value, uint flags)
				=> Callback1Invocations.Add((address, value, flags));

			public void Callback2(uint address, uint value, uint flags)
				=> Callback2Invocations.Add((address, value, flags));

			public void Callback3(uint address, uint value, uint flags)
				=> Callback3Invocations.Add((address, value, flags));

			public void Clear()
			{
				Callback1Invocations.Clear();
				Callback2Invocations.Clear();
				Callback3Invocations.Clear();
			}
		}
	}
}
