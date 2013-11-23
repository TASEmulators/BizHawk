using System.IO;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface IMovie
	{
		
		int Rerecords { get; set; }
		
		string Filename { get; set; }

		bool IsCountingRerecords { get; set; }
		bool IsActive { get; }
		bool IsPlaying { get; }
		bool IsRecording { get; }
		bool IsFinished { get; }
		bool Changes { get; }
		bool Loaded { get; }

		bool Load();
		void Save();
		void SaveAs();
		void Stop(bool saveChanges = true);

		#region Editing API

		void ClearFrame(int frame);
		void ModifyFrame(string record, int frame);
		void AppendFrame(string record);
		void InsertFrame(string record, int frame);
		void InsertBlankFrame(int frame);
		void DeleteFrame(int frame);
		void TruncateMovie(int frame);

		#endregion

		#region Dubious, should reconsider
		void CommitFrame(int frameNum, IController source); //why pass in frameNum? Calling api 
		void PokeFrame(int frameNum, string input); //Why does this exist as something different than Commit Frame?
		void CaptureState(); //Movie code should manage wheter it needs to capture a state
		LoadStateResult CheckTimeLines(TextReader reader, bool onlyGuid, bool ignoreGuidMismatch, out string errorMessage); //No need to return a status, no reason to have hacky flags, no need to pass a textreader
		string GetTime(bool preLoad); //Rename to simply: Time, and make it a DateTime
		void DumpLogIntoSavestateText(TextWriter writer); //Why pass a Textwriter, just make a string property that is the inputlog as text
		void LoadLogFromSavestateText(TextReader reader, bool isMultitracking); //Pass in the text? do we need to care if it is multitracking, and can't hte movie already know that?
		int? Frames { get; } //Nullable is a hack, also why does calling code need to know the number of frames, can that be minimized?
		int RawFrames { get; } //Hacky to need two different frame properties

		void Finish(); //Why isn't the movie in charge of this?
		void StartRecording(bool truncate = true); //Why do we need to truncate or not truncate? Why isn't the object in charge of this decision?

		void StartPlayback(); //Poorly named for what it does, SetToPlay() perhaps? Also, this seems like too much power to give the calling code
		void SwitchToRecord(); //Ditto
		void SwitchToPlay(); //Dubious that it is needed

		bool FrameLagged(int frame); //SHould be a property of a Record object
		byte[] GetState(int frame); //Should be a property of a Record object
		string GetInput(int frame); //Should be a property of a Record object
		byte[] InitState { get; } //Should be a record object?
		
		bool StartsFromSavestate { get; set; } //Why is this settable!!!

		MovieHeader Header { get; } //Don't expose this!!!
		MovieLog LogDump { get; } //Don't expose this!!!
		SubtitleList Subtitles { get; } //Don't expose this!!!

		int StateFirstIndex { get; } //What do these do?
		int StateLastIndex { get; }
		#endregion
	}
}

//TODO: delete this and refactor code that uses it!
public enum LoadStateResult { Pass, GuidMismatch, TimeLineError, FutureEventError, NotInRecording, EmptyLog, MissingFrameNumber }