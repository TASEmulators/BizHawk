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
		/// Gets a list of extensions for all <see cref="IMovie"/> implementations
		/// </summary>
		public static IEnumerable<string> MovieExtensions => new[] { Bk2Movie.Extension, TasMovie.Extension };

		public static bool IsValidMovieExtension(string ext) => MovieExtensions.Contains(ext.Replace(".", ""), StringComparer.OrdinalIgnoreCase);

		public static bool IsCurrentTasVersion(string movieVersion)
		{
			double actual = ParseTasMovieVersion(movieVersion);
			return actual.HawkFloatEquality(TasMovie.CurrentVersion);
		}

		internal static double ParseTasMovieVersion(string movieVersion)
		{
			if (string.IsNullOrWhiteSpace(movieVersion))
			{
				return 1.0F;
			}

			string[] split = movieVersion
				.ToLowerInvariant()
				.Split(new[] {"tasproj"}, StringSplitOptions.RemoveEmptyEntries);

			if (split.Length == 1)
			{
				return 1.0F;
			}

			string versionStr = split[1]
				.Trim()
				.Replace("v", "");

			if (double.TryParse(versionStr, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedWithPeriod))
			{
				return parsedWithPeriod;
			}

			// Accept .tasproj files written from <= 2.5 where the host culture settings used ','
			if (double.TryParse(versionStr.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedWithComma))
			{
				return parsedWithComma;
			}

			return 1.0F;
		}
	}
}
