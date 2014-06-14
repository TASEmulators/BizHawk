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
			// Currently we just use the file extension
			// TODO: change IMovies to take HawkFiles only and not path
			// TOOD: tasproj
			if (Path.GetExtension(path).EndsWith("bk2"))
			{
				return new Bk2Movie(path);
			}
			else
			{
				return new BkmMovie(path);
			}
		}

		/// <summary>
		/// Gets the file extension for the default movie implementation used in the client
		/// </summary>
		public static string DefaultExtension
		{
			get
			{
				if (VersionInfo.DeveloperBuild)
				{
					return "bk2";
				}

				return "bkm";
			}
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

		public static bool IsValidMovieExtension(string ext)
		{
			if (MovieExtensions.Contains(ext.ToLower().Replace(".", "")))
			{
				return true;
			}

			return false;
		}

		/// <summary>
		/// Creates a default instance of the default implementation, 
		/// no path is specified so this is in a minimal state that would not be able to be saved
		/// </summary>
		public static IMovie DefaultInstance
		{
			get
			{
				if (VersionInfo.DeveloperBuild)
				{
					return new Bk2Movie();
				}

				return new BkmMovie();
			}
		}
	}
}
