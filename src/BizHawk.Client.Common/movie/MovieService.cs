using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Client.Common
{
	public static class MovieService
	{
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

		public static bool IsCurrentTasVersion(string movieVersion)
		{
			var actual = ParseTasMovieVersion(movieVersion);
			return actual.HawkFloatEquality(TasMovie.CurrentVersion);
		}

		internal static double ParseTasMovieVersion(string movieVersion)
		{
			if (string.IsNullOrWhiteSpace(movieVersion))
			{
				return 1.0F;
			}

			var split = movieVersion
				.ToLower()
				.Split(new[] {"tasproj"}, StringSplitOptions.RemoveEmptyEntries);

			if (split.Length == 1)
			{
				return 1.0F;
			}

			var versionStr = split[1]
				.Trim()
				.Replace("v", "");

			if (double.TryParse(versionStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedWithPeriod))
			{
				return parsedWithPeriod;
			}

			// Accept .tasproj files written from <= 2.5 where the host culture settings used ','
			if (double.TryParse(versionStr, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsedWithComma))
			{
				return parsedWithComma;
			}

			return 1.0F;
		}
	}
}
