using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Client.Common.MovieConversionExtensions;

namespace BizHawk.Client.Common
{
	public static class MovieService
	{
		public static IMovie Get(string path)
		{
			// TODO: change IMovies to take HawkFiles only and not path
			if (Path.GetExtension(path).EndsWith("bk2"))
			{
				return new Bk2Movie(path);
			}

			if (Path.GetExtension(path).EndsWith("tasproj"))
			{
				return new TasMovie(path);
			}

			var movie = new BkmMovie(path);

			if (VersionInfo.DeveloperBuild)
			{
				movie.Load();
				return movie.ToBk2();
			}

			return movie;
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
