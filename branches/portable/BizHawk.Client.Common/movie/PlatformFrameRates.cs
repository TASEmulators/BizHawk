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
				{ "A26", 59.9227510135505 },
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
	}
}
