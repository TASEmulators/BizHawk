using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IInputMovie : IExternalApi
	{
		IList<string> Comments { get; }

		string Filename { get; }

		double FramesPerSecond { get; }

		IDictionary<string, string> Header { get; }

		bool IsLoaded { get; }

		bool IsReadOnly { get; set; }

		bool IsRerecordCounting { get; set; }

		double Length { get; }

		string Mode { get; }

		ulong RerecordCount { get; set; }

		bool StartsFromSaveram { get; }

		bool StartsFromSavestate { get; }

		IList<string> Subtitles { get; }

		IDictionary<string, dynamic> GetInput(int frame, int? controller = null);

		string GetInputAsMnemonic(int frame);

		void Save(string filename = null);

		void Stop();
	}
}
