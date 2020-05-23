using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BizHawk.Client.Common
{
	public static class MovieService
	{
		public static IMovie Get(string path, bool startsFromSavestate = false)
		{
			// TODO: change IMovies to take HawkFiles only and not path
			if (Path.GetExtension(path)?.EndsWith("tasproj") ?? false)
			{
				return new TasMovie(path, startsFromSavestate);
			}

			return new Bk2Movie(path);
		}

		public static string StandardMovieExtension => Bk2Movie.Extension;
		public static string TasMovieExtension => TasMovie.Extension;

		/// <summary>
		/// Gets a list of extensions for all <seealso cref="IMovie"/> implementations
		/// </summary>
		public static IEnumerable<string> MovieExtensions => new[] { Bk2Movie.Extension, TasMovie.Extension };

		public static bool IsValidMovieExtension(string ext)
		{
			return MovieExtensions.Contains(ext.ToLower().Replace(".", ""));
		}
	}
}
