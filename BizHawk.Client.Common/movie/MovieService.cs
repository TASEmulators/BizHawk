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
			if (Path.GetExtension(path).EndsWith("tasproj"))
			{
				return new TasMovie(path);
			}

			if (Path.GetExtension(path).EndsWith("bkm"))
			{
				var bkm = new BkmMovie(path);
				bkm.Load(false);

				// Hackery to fix how things used to work
				if (bkm.SystemID == "GBC")
				{
					bkm.SystemID = "GB";
				}

				return bkm.ToBk2();
			}

			// Default to bk2
			return new Bk2Movie(path);
		}

		/// <summary>
		/// Gets the file extension for the default movie implementation used in the client
		/// </summary>
		public static string DefaultExtension
		{
			get
			{
				return "bk2";
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
				return new Bk2Movie();
			}
		}
	}
}
