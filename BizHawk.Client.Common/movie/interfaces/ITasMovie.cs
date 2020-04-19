using System;
using System.Collections.Generic;
using System.ComponentModel;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface ITasMovie : IMovie, INotifyPropertyChanged
	{
		void FlagChanges();
		void ClearChanges();
		bool BindMarkersToInput { get; set; }
		IMovieChangeLog ChangeLog { get; set; }
		IStateManager TasStateManager { get; }
		Func<string> ClientSettingsForSave { set; }
		Action<string> GetClientSettingsOnLoad { set; }
		ITasMovieRecord this[int index] { get; }
		ITasSession Session { get; }
		TasMovieMarkerList Markers { get; }
		ITasBranchCollection Branches { get; }
		TasLagLog TasLagLog { get; }

		int CurrentBranch { get; set; }

		void ToggleBoolState(int frame, string buttonName);
		void SetFloatState(int frame, string buttonName, float val);
		void SetFloatStates(int frame, int count, string buttonName, float val);
		void SetBoolState(int frame, string buttonName, bool val);
		void SetBoolStates(int frame, int count, string buttonName, bool val);

		TasBranch GetBranch(int index); // TODO: this can simply be an extension method, or do the logic inline, or implement directly into the branch class

		string NewBranchText { get; set; }

		IStringLog GetLogEntries(); // TODO: code smell, extension method? IMovie should expose this too? should be a property?

		int LastEditedFrame { get; }
		int LastStatedFrame { get; }

		void InsertInput(int frame, string inputState);
		void InsertInput(int frame, IEnumerable<string> inputLog);
		void InsertInput(int frame, IEnumerable<IController> inputStates);

		void InsertEmptyFrame(int frame, int count = 1, bool fromHistory = false);
		int CopyOverInput(int frame, IEnumerable<IController> inputStates);

		void RemoveFrame(int frame);
		void RemoveFrames(ICollection<int> frames);
		void RemoveFrames(int removeStart, int removeUpTo, bool fromHistory = false);
		void SetFrame(int frame, string source);

		IStringLog VerificationLog { get; }

		void ClearGreenzone(); // TODO: extension method?

		Guid BranchGuidByIndex(int index); // TODO: extension method
		TasBranch GetBranch(Guid id); // TODO: extension method

		void SwapBranches(int b1, int b2);
		void UpdateBranch(TasBranch old, TasBranch newBranch);

		string DisplayValue(int frame, string buttonName);

		// TODO: delete these and just hit TasLagLog directly
		void RemoveLagHistory(int frame);
		void InsertLagHistory(int frame, bool isLag);
		void SetLag(int frame, bool? value);

		bool LastPositionStable { get; set; }

		void LoadBranch(TasBranch branch);

		// TODO: extension method
		IStringLog CloneInput();
	}
}
