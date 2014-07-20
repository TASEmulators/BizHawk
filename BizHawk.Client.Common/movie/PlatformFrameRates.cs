using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public class PlatformFrameRates
	{
		// these are political numbers, designed to be in accord with tasvideos.org tradition. theyre not necessarily mathematical factualities (although they may be in some cases)
		// it would be nice if we could turn this into a rational expression natively, and also, to write some comments about the derivation and ideal valees (since this seems to be where theyre all collected)
		// are we collecting them anywhere else? for avi-writing code perhaps?
		private static readonly Dictionary<string, double> _rates = new Dictionary<string, double>
			{
				{ "NES", 60.098813897440515532 }, // discussion here: http://forums.nesdev.com/viewtopic.php?t=492 ; a rational expression would be (19687500 / 11) / ((341*262-0.529780.5)/3) -> (118125000 / 1965513) -> 60.098813897440515529533511098629 (so our chosen number is very close)
				{ "NES_PAL", 50.006977968268290849 },
				{ "FDS", 60.098813897440515532 },
				{ "FDS_PAL", 50.006977968268290849 },
				{ "SNES", (double)21477272 / (4 * 341 * 262) }, //60.098475521
				{ "SNES_PAL", (double)21281370 / (4 * 341 * 312) }, //50.0069789082
				{ "SGB", (double)21477272 / (4 * 341 * 262) }, //60.098475521
				{ "SGB_PAL", (double)21281370 / (4 * 341 * 312) }, //50.0069789082
				{ "PCE", (7159090.90909090 / 455 / 263) }, //59.8261054535
				{ "PCECD", (7159090.90909090 / 455 / 263) }, //59.8261054535
				{ "SMS", (3579545 / 262.0 / 228.0) }, //59.9227434043
				{ "SMS_PAL", (3546893 / 313.0 / 228.0) }, //49.7014320946
				{ "GG", (3579545 / 262.0 / 228.0) }, //59.9227434043
				{ "GG_PAL", (3546893 / 313.0 / 228.0) }, //49.7014320946
				{ "SG", (3579545 / 262.0 / 228.0) }, //59.9227434043
				{ "SG_PAL", (3546893 / 313.0 / 228.0) }, //49.7014320946
				{ "NGP", (6144000.0 / (515 * 198)) }, //60.2530155928
				{ "VBOY", (20000000 / (259 * 384 * 4)) },  //50.2734877735
				{ "LYNX", 59.8 },
				{ "WSWAN", (3072000.0 / (159 * 256)) }, //75.4716981132
				{ "GB", 262144.0 / 4389.0 }, //59.7275005696
				{ "GBC", 262144.0 / 4389.0 }, //59.7275005696
				{ "GBA", 262144.0 / 4389.0 }, //59.7275005696 

				// while the number of scanlines per frame is software controlled and variable, we
				// enforce exactly 262 (NTSC) 312 (PAL) per reference time frame
				{ "A26", 315000000.0 / 88.0 / 262.0 / 228.0 }, // 59.922751013550531429197560173856
				// this pal clock ref is exact
				{ "A26_PAL", 3546895.0 / 312.0 / 228.0 }, // 49.860759671614934772829509671615

				{ "A78", 59.9227510135505 },
				{ "Coleco", 59.9227510135505 }
			};

		public double this[string systemId, bool pal]
		{
			get
			{
				var key = systemId + (pal ? "_PAL" : string.Empty);
				if (_rates.ContainsKey(key))
				{
					return _rates[key];
				}

				return 60.0;
			}
		}

		public TimeSpan MovieTime(IMovie movie)
		{
			var dblseconds = GetSeconds(movie);
			var seconds = (int)(dblseconds % 60);
			var days = seconds / 86400;
			var hours = seconds / 3600;
			var minutes = (seconds / 60) % 60;
			var milliseconds = (int)((dblseconds - seconds) * 1000);
			return new TimeSpan(days, hours, minutes, seconds, milliseconds);
		}

		private double Fps(IMovie movie)
		{
			var system = movie.HeaderEntries[HeaderKeys.PLATFORM];
			var pal = movie.HeaderEntries.ContainsKey(HeaderKeys.PAL) &&
				movie.HeaderEntries[HeaderKeys.PAL] == "1";

				return this[system, pal];
		}

		private double GetSeconds(IMovie movie)
		{
			double frames = movie.InputLogLength;

			if (frames < 1)
			{
				return 0;
			}

			return frames / Fps(movie);
		}
	}
}
