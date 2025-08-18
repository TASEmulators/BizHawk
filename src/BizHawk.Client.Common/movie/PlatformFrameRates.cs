using System.Collections.Generic;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public static class PlatformFrameRates
	{
		// these are political numbers, designed to be in accord with tasvideos.org tradition. they're not necessarily mathematical factualities (although they may be in some cases)
		// it would be nice if we could turn this into a rational expression natively, and also, to write some comments about the derivation and ideal values (since this seems to be where they're all collected)
		// are we collecting them anywhere else? for avi-writing code perhaps?

		// just some constants, according to specs
		private static readonly double PALCarrier = (15625 * 283.75) + 25; // 4.43361875 MHz
		private static readonly double NTSCCarrier = 4500000 * 227.5 / 286; // 3.579545454... MHz
		private static readonly double PALNCarrier = (15625 * 229.25) + 25; // 3.58205625 MHz

		private static readonly Dictionary<string, double> Rates = new Dictionary<string, double>
		{
			["Panasonic3DO"] = 60.0, // The emulator (Opera-Libretro) reports exact 60.0 for NTSC https://github.com/libretro/opera-libretro/blob/67a29e60a4d194b675c9272b21b61eaa022f3ba3/libopera/opera_region.c#L10
			["Panasonic3DO_PAL"] = 50.0, // The emulator (Opera-Libretro) reports exact 50.0 for PAL https://github.com/libretro/opera-libretro/blob/67a29e60a4d194b675c9272b21b61eaa022f3ba3/libopera/opera_region.c#L17
			["NES"] = 60.098813897440515, // per https://forums.nesdev.org/viewtopic.php?p=3783#p3783 the nominal value is (19687500/11) / ((341*262 - 0.5) / 3) = 39375000/655171 ≈ 60.09881389744051553 (so our chosen number, which is approximately 60.09881389744051461, is very close)
			["FDS"] = 60.098813897440515, // ditto
			["NES_PAL"] = 50.00697796826829, // per https://forums.nesdev.org/viewtopic.php?p=3783#p3783 the nominal value is 1662607 / (341*312/3.2) = 3325214/66495 ≈ 50.0069779682682908 (so our chosen number, which is approximately 50.0069779682682877, is very close)
			["FDS_PAL"] = 50.00697796826829, // ditto

			["SNES"] = 21477272.0 / (4 * 341 * 262 - 2), // 60.0988118623
			["SNES_PAL"] = 21281370.0 / (4 * 341 * 312), // 50.0069789082
			["SGB"] = 21477272.0 / (4 * 341 * 262 - 2), // 60.0988118623
			["SGB_PAL"] = 21281370.0 / (4 * 341 * 312), // 50.0069789082
			["BSX"] = 21477272.0 / (4 * 341 * 262 - 2), // 60.0988118623
			["PCE"] = 2250000.0 / 37609.0, // = 78750000 / 11 / 455 / 263 ≈ 59.8261054535
			["PCECD"] = 2250000.0 / 37609.0, // = 78750000 / 11 / 455 / 263 ≈ 59.8261054535
			["SMS"] = 3579545 / 262.0 / 228.0, // 59.9227434043
			["SMS_PAL"] = 3546893 / 313.0 / 228.0, // 49.7014320946
			["GG"] = 3579545 / 262.0 / 228.0, // 59.9227434043
			["GG_PAL"] = 3546893 / 313.0 / 228.0, // 49.7014320946
			["SG"] = 3579545 / 262.0 / 228.0, // 59.9227434043
			["SG_PAL"] = 3546893 / 313.0 / 228.0, // 49.7014320946
			["NGP"] = 6144000.0 / (515 * 198), // 60.2530155928
			["VB"] = 20000000.0 / (259 * 384 * 4),  // 50.2734877735
			["Lynx"] = 16000000.0 / (16 * 105 * 159), // 59.89817310572028
			["WSWAN"] = 3072000.0 / (159 * 256), // 75.4716981132
			["GB"] = 262144.0 / 4389.0, // 59.7275005696
			["GBC"] = 262144.0 / 4389.0, // 59.7275005696

			["GBA"] = 262144.0 / 4389.0, // 59.7275005696
			["NDS"] = 33513982.0 / 560190.0, // 59.8260982881
			["3DS"] = 268111856.0 / 4481136.0, // 59.8312249394
			["GEN"] = 53693175 / (3420.0 * 262),
			["GEN_PAL"] = 53203424 / (3420.0 * 313),

			["Jaguar"] = 60,
			["Jaguar_PAL"] = 50,

			// while the number of scanlines per frame is software controlled and variable, we
			// enforce exactly 262 (NTSC) 312 (PAL) per reference time frame
			["A26"] = 1640625.0 / 27379.0, // = 315000000 / 88 / 262 / 228 ≈ 59.922751013550531429197560173856
			["A78"] = 1640625.0 / 27379.0, // ditto
			["Coleco"] = 1640625.0 / 27379.0, // ditto
			// this pal clock ref is exact
			["A26_PAL"] = 3546895.0 / 71136.0, // = 3546895 / 312 / 228 ≈ 49.860759671614934772829509671615

#if true // according to https://github.com/TASEmulators/mednafen/blob/740d63996fc7cebffd39ee253a29ee434965db21/src/psx/gpu.cpp [failed verification—there are several expressions but none of them match these]
			["PSX"] = 502813668.0 / 8388608.0, // 59.940060138702392578125
			["PSX_PAL"] = 419432765.0 / 8388608.0, // 50.00028192996978759765625
#else // according to https://problemkaputt.de/psx-spx.htm#gputimings
			["PSX"] = 44100.0 * 768 * 11 / 7 / 263 / 3413, // 59.292862562
			["PSX_PAL"] = 44100.0 * 768 * 11 / 7 / 314 / 3406, // 49.7645593576
#endif

			// according to https://github.com/TASEmulators/mednafen/blob/382ff1b8d293c9a862497706808cbb79b2cecbfb/src/ss/vdp2.cpp#L904-L907
			["SAT"] = 8734090909.0 / 145852525, // = 1746818181.8 / 61 / 4 / 455 / ((263 + 262.5) / 2.0) ≈ 59.883028483737256
			["SAT_PAL"] = 62500.0 / 1251, // = 1734687500.0 / 61 / 4 / 455 / ((313 + 312.5) / 2.0) ≈ 49.960031974420467

			["Doom"] = 35.0,

			// reverse-engineering of https://github.com/TASEmulators/libretro-uae/blob/ccecb1ead642c1bbe391308b88a7ffa9478b918d/libretro/libretro-core.h#L254-L255 which seems to be based on https://eab.abime.net/showthread.php?t=51883
			["Amiga"] = 2250000.0 / 37609, // = NTSCCarrier / 227.5 / 263 ≈ 59.826105453481879
			["Amiga_PAL"] = 3546895.0 / 71051, // = 28.37516*1000000 / 8 / 227 / 313 ≈ 49.920409283472435

			["C64_PAL"] = PALCarrier * 2 / 9 / 312 / 63,
			["C64_NTSC"] = NTSCCarrier * 2 / 7 / 263 / 65,
			["C64_NTSC_OLD"] = NTSCCarrier * 2 / 7 / 262 / 64,
			["C64_DREAN"] = PALNCarrier * 2 / 7 / 312 / 65,
			["INTV"] = 1498.0 / 25.0, // = 59.92

			["ZXSpectrum_PAL"] = 15625.0 / 312.0, // = 3500000 / 224 / 312 ≈ 50.0801282051282051
			["AmstradCPC_PAL"] = 15625.0 / 312.0, // ditto (progressive mode)

			["UZE"] = 1125000.0 / 18733.0, // = 8 * 315000000 / 88 / 1820 / 262 ≈ 60.05444936742646666
			["VEC"] = 50,
			["O2"] = 89478485.0 / 1495643, // 59.8260982065907439141559850846
			["O2_PAL"] = 89478485.0 / 1800319, // 49.70146124103561646574857011

			["TIC80"] = 60,

			["ChannelF"] = 234375.0 / 3872.0, // (NTSCCarrier * 8 / 7) / (256 * 264)
			// note: ChannelF II PAL timings might be slightly different...
			["ChannelF_PAL"] = 15625.0 / 312.0, // 4000000 / (256 * 312)
		};

		public static double GetFrameRate(string systemId, bool pal)
		{
			var key = pal ? $"{systemId}_PAL" : systemId;
			if (Rates.TryGetValue(key, out var d)) return d;
			//TODO fallback PAL-->NTSC?
			Util.DebugWriteLine($"missing framerate for system {key}");
			return 60.0; //TODO 50 for PAL?
		}
	}
}
