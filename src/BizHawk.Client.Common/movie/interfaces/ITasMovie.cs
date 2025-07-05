using System.Collections.Generic;
using System.ComponentModel;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface ITasMovie : IMovie, INotifyPropertyChanged, IDisposable
	{
		bool BindMarkersToInput { get; set; }

		IMovieChangeLog ChangeLog { get; }
		IStateManager TasStateManager { get; }
		Func<string> InputRollSettingsForSave { get; set; }
		string InputRollSettings { get; }
		ITasMovieRecord this[int index] { get; }
		ITasSession TasSession { get; }
		TasMovieMarkerList Markers { get; }
		ITasBranchCollection Branches { get; }
		TasLagLog LagLog { get; }
		IStringLog VerificationLog { get; }
		int LastEditedFrame { get; }
		bool LastEditWasRecording { get; }

		/// <summary>
		/// Called whenever the movie is modified in a way that could invalidate savestates in the movie's state history.
		/// Called regardless of whether any states were actually invalidated.
		/// The parameter is the last frame number who's savestate (if it exists) is still valid. That is, the first of the modified frames.
		/// </summary>
		Action<int> GreenzoneInvalidated { get; set; }

		string DisplayValue(int frame, string buttonName);
		void FlagChanges();
		void ClearChanges();

		/// <summary>
		/// Replaces the given frame's input with an empty frame
		/// </summary>
		void ClearFrame(int frame);

		void GreenzoneCurrentFrame();
		void ToggleBoolState(int frame, string buttonName);
		void SetAxisState(int frame, string buttonName, int val);
		void SetAxisStates(int frame, int count, string buttonName, int val);
		void SetBoolState(int frame, string buttonName, bool val);
		void SetBoolStates(int frame, int count, string buttonName, bool val);
		void InsertInput(int frame, string inputState);
		void InsertInput(int frame, IEnumerable<string> inputLog);
		void InsertInput(int frame, IEnumerable<IController> inputStates);

		void InsertEmptyFrame(int frame, int count = 1);
		void CopyOverInput(int frame, IEnumerable<IController> inputStates);

		void RemoveFrame(int frame);
		void RemoveFrames(ICollection<int> frames);
		/// <summary>
		/// Remove all frames between removeStart and removeUpTo (excluding removeUpTo).
		/// </summary>
		/// <param name="removeStart">The first frame to remove.</param>
		/// <param name="removeUpTo">The frame after the last frame to remove.</param>
		void RemoveFrames(int removeStart, int removeUpTo);
		void SetFrame(int frame, string source);

		void LoadBranch(TasBranch branch);

		void CopyVerificationLog(IEnumerable<string> log);

		bool IsReserved(int frame);
	}
}
