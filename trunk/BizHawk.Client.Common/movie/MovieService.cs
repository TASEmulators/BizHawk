using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public static class MovieService
	{
		public static IMovie Get(string path)
		{
			// TODO: open the file and determine the format, and instantiate the appropriate implementation
			// Currently we just assume it is a bkm implementation
			// TODO: change IMovies to take HawkFiles only and not path
			return new BkmMovie(path);
		}

		/// <summary>
		/// Gets the file extension for the default movie implementation used in the client
		/// </summary>
		public static string DefaultExtension
		{
			get { return "bkm"; }
		}

		/// <summary>
		/// Returns a list of extensions for all IMovie implementations
		/// </summary>
		public static IEnumerable<string> MovieExtensions
		{
			// Movies 2.0 TODO: consider using reflection to find IMovie implementations
			get
			{
				yield return "bkm";
				yield return "bk2";
				yield return "tasproj";
			}
		}

		/// <summary>
		/// Creates a default instance of the default implementation, 
		/// no path is specified so this is in a minimal state that would not be able to be saved
		/// </summary>
		public static IMovie DefaultInstance
		{
			get
			{
				return new BkmMovie();
			}
		}
	}
}
