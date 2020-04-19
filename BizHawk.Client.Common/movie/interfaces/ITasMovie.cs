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
		IMovieChangeLog ChangeLog { get; }
		IStateManager TasStateManager { get; }
		Func<string> ClientSettingsForSave { set; }
		Action<string> GetClientSettingsOnLoad { set; }
		ITasMovieRecord this[int index] { get; }
		ITasSession Session { get; }
		TasMovieMarkerList Markers { get; }
		ITasBranchCollection Branches { get; }
		TasLagLog LagLog { get; }

		int CurrentBranch { get; set; }

		void ToggleBoolState(int frame, string buttonName);
		void SetFloatState(int frame, string buttonName, float val);
		void SetFloatStates(int frame, int count, string buttonName, float val);
		void SetBoolState(int frame, string buttonName, bool val);
		void SetBoolStates(int frame, int count, string buttonName, bool val);

		string NewBranchText { get; set; }

		IStringLog GetLogEntries();

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

		string DisplayValue(int frame, string buttonName);

		bool LastPositionStable { get; set; }

		void LoadBranch(TasBranch branch);
	}
}
