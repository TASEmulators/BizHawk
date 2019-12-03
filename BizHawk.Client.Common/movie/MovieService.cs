using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BizHawk.Client.Common
{
	public static class MovieService
	{
		public static IMovie Get(string path)
		{
			// TODO: change IMovies to take HawkFiles only and not path
			if (Path.GetExtension(path)?.EndsWith("tasproj") ?? false)
			{
				return new TasMovie(path);
			}

			return new Bk2Movie(path);
		}

		/// <summary>
		/// Gets the file extension for the default movie implementation used in the client
		/// </summary>
		public static string DefaultExtension => "bk2";

		/// <summary>
		/// Gets a list of extensions for all <seealso cref="IMovie"/> implementations
		/// </summary>
		public static IEnumerable<string> MovieExtensions => new[] { "bk2", "tasproj" };

		public static bool IsValidMovieExtension(string ext)
		{
			return MovieExtensions.Contains(ext.ToLower().Replace(".", ""));
		}

		/// <summary>
		/// Creates a default instance of the default implementation, 
		/// no path is specified so this is in a minimal state that would not be able to be saved
		/// </summary>
		public static IMovie DefaultInstance => new Bk2Movie();
	}
}
