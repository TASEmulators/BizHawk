using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public interface IMovieHeader : IDictionary<string, string>
	{
		SubtitleList Subtitles { get; }
		Dictionary<string, string> BoardProperties { get; }

		#region Dubious, should reconsider

		List<string> Comments { get; } // Consider making this a readonly list, or custom object, to control editing api

		Dictionary<string, string> Parameters { get; } //rename to Parameters, make a custom object, that controls what params are valid

		/// <summary>
		/// Adds the key value pair to header params.  If key already exists, value will be updated
		/// </summary>
		/// <param name="key"></param>
		/// <param name="value"></param>
		void AddHeaderLine(string key, string value); // delete in favor of AddHeaderFromLine

		//TODO: replace Movie Preload & Load functions with this
		/// <summary>
		/// Receives a line and attempts to add as a header, returns false if not a useable header line
		/// </summary>
		/// <param name="line"></param>
		/// <returns></returns>
		bool AddHeaderFromLine(string line); // rename to AddFromString, should be a property of HeaderParams
		#endregion
	}
}
