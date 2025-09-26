using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	[CLSCompliant(false)]
	public interface IMovieApi : IExternalApi
	{
		bool StartsFromSavestate();
		bool StartsFromSaveram();
		string Filename();
		IReadOnlyDictionary<string, object> GetInput(int frame, int? controller = null);
		string GetInputAsMnemonic(int frame);
		bool GetReadOnly();
		ulong GetRerecordCount();
		bool GetRerecordCounting();
		bool IsLoaded();
		int Length();
		string Mode();

		/// <summary>
		/// Resets the core to frame 0 with the currently loaded movie in playback mode.
		/// If <paramref name="path"/> is specified, attempts to load it, then continues with playback if it was successful.
		/// </summary>
		/// <returns>true iff successful</returns>
		bool PlayFromStart(string path = "");

		void Save(string filename = "");
		void SetReadOnly(bool readOnly);
		void SetRerecordCount(ulong count);
		void SetRerecordCounting(bool counting);

		void Stop(bool saveChanges = true);

		double GetFps();
		IReadOnlyDictionary<string, string> GetHeader();
		IReadOnlyList<string> GetComments();
		IReadOnlyList<string> GetSubtitles();
	}
}
