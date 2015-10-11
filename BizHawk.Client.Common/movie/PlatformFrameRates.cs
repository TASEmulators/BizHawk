using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public class PlatformFrameRates
	{
		// these are political numbers, designed to be in accord with tasvideos.org tradition. theyre not necessarily mathematical factualities (although they may be in some cases)
		// it would be nice if we could turn this into a rational expression natively, and also, to write some comments about the derivation and ideal valees (since this seems to be where theyre all collected)
		// are we collecting them anywhere else? for avi-writing code perhaps?

		// just some constants, according to specs
		private static readonly double PAL_CARRIER = 15625 * 283.75 + 25;	   //  4.43361875 MHz
		private static readonly double NTSC_CARRIER = 4500000 * 227.5 / 286;   //  3.579545454... MHz
		private static readonly double PAL_N_CARRIER = 15625 * 229.25 + 25;		// 3.58205625 MHz

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
				{ "VBOY", (20000000.0 / (259 * 384 * 4)) },  //50.2734877735
				{ "Lynx", 16000000.0 / ( 16 * 105 * 159 ) }, // 59.89817310572028
				{ "WSWAN", (3072000.0 / (159 * 256)) }, //75.4716981132
				{ "GB", 262144.0 / 4389.0 }, //59.7275005696
				{ "GBC", 262144.0 / 4389.0 }, //59.7275005696
				{ "GBA", 262144.0 / 4389.0 }, //59.7275005696 
				{ "GEN", 53693175 / (3420.0 * 262) },
				{ "GEN_PAL", 53203424 / (3420.0 * 313) },
				// while the number of scanlines per frame is software controlled and variable, we
				// enforce exactly 262 (NTSC) 312 (PAL) per reference time frame
				{ "A26", 315000000.0 / 88.0 / 262.0 / 228.0 }, // 59.922751013550531429197560173856
				// this pal clock ref is exact
				{ "A26_PAL", 3546895.0 / 312.0 / 228.0 }, // 49.860759671614934772829509671615

				{ "A78", 59.9227510135505 },
				{ "Coleco", 59.9227510135505 },

				//according to http://problemkaputt.de/psx-spx.htm
				{"PSX", 44100.0*768*11/7/263/3413}, //59.292862562
				{"PSX_PAL", 44100.0*768*11/7/314/3406}, //49.7645593576

				{"C64_PAL", PAL_CARRIER*2/9/312/63},
				{"C64_NTSC", NTSC_CARRIER*2/7/263/65},
				{"C64_NTSC_OLD", NTSC_CARRIER*2/7/262/64},
				{"C64_DREAN", PAL_N_CARRIER*2/7/312/65},

				//according to ryphecha, using
				//clocks[2] = { 53.693182e06, 53.203425e06 }; //ntsc console, pal console
				//lpf[2][2] = { { 263, 262.5 }, { 314, 312.5 } }; //ntsc,pal; noninterlaced, interlaced
				//cpl[2] = { 3412.5, 3405 }; //ntsc mode, pal mode
				//PAL PS1: 0, PAL Mode: 0, Interlaced: 0 --- 59.826106 (53.693182e06/(263*3412.5))
				//PAL PS1: 0, PAL Mode: 0, Interlaced: 1 --- 59.940060 (53.693182e06/(262.5*3412.5))
				//PAL PS1: 1, PAL Mode: 1, Interlaced: 0 --- 49.761427 (53.203425e06/(314*3405))
				//PAL PS1: 1, PAL Mode: 1, Interlaced: 1 --- 50.000282(53.203425e06/(312.5*3405))
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
