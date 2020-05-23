using System;

namespace BizHawk.API.ApiHawk
{
	/// <remarks>
	/// Changes from 2.4.2:
	/// <list type="bullet">
	/// <item><description>method <c>void ClearInMemoryStates()</c> replaced with <see cref="ClearSnapshots"/></description></item>
	/// <item><description>method <c>string SaveCoreStateToMemory()</c> replaced with <see cref="CreateSnapshot"/> (return type changed to Guid struct)</description></item>
	/// <item><description>method <c>void LoadCoreStateFromMemory(string identifier)</c> replaced with <see cref="LoadSnapshotWithID"/> (type of first parameter changed to Guid struct)</description></item>
	/// <item><description>method <c>void DeleteState(string identifier)</c> replaced with <see cref="RemoveSnapshotWithID"/> (type of first parameter changed to Guid struct)</description></item>
	/// </list>
	/// </remarks>
	public interface IMemorySaveStateLib
	{
		void ClearSnapshots();

		Guid CreateSnapshot();

		void LoadSnapshotWithID(Guid guid);

		void RemoveSnapshotWithID(Guid guid);
	}
}
