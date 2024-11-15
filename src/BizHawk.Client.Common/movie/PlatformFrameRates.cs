using System.Collections.Generic;

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
			["NES"] = 60.098813897440515532, // discussion here: http://forums.nesdev.com/viewtopic.php?t=492 ; a rational expression would be (19687500 / 11) / ((341*262-0.529780.5)/3) -> (118125000 / 1965513) -> 60.098813897440515529533511098629 (so our chosen number is very close)
			["NES_PAL"] = 50.006977968268290849,
			["FDS"] = 60.098813897440515532,
			["FDS_PAL"] = 50.006977968268290849,
			["SNES"] = 21477272.0 / (4 * 341 * 262 - 2), // 60.0988118623
			["SNES_PAL"] = 21281370.0 / (4 * 341 * 312), // 50.0069789082
			["SGB"] = 21477272.0 / (4 * 341 * 262 - 2), // 60.0988118623
			["SGB_PAL"] = 21281370.0 / (4 * 341 * 312), // 50.0069789082
			["BSX"] = 21477272.0 / (4 * 341 * 262 - 2), // 60.0988118623
			["PCE"] = 7159090.90909090 / 455 / 263, // 59.8261054535
			["PCECD"] = 7159090.90909090 / 455 / 263, // 59.8261054535
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
			["A26"] = 315000000.0 / 88.0 / 262.0 / 228.0, // 59.922751013550531429197560173856

			// this pal clock ref is exact
			["A26_PAL"] = 3546895.0 / 312.0 / 228.0, // 49.860759671614934772829509671615

			["A78"] = 59.9227510135505,
			["Coleco"] = 59.9227510135505,

			// according to http://problemkaputt.de/psx-spx.htm
			//["PSX"] = 44100.0 * 768 * 11 / 7 / 263 / 3413, // 59.292862562
			//["PSX_PAL"] = 44100.0 * 768 * 11 / 7 / 314 / 3406, // 49.7645593576
			// according to https://github.com/TASEmulators/mednafen/blob/740d63996fc7cebffd39ee253a29ee434965db21/src/psx/gpu.cpp
			["PSX"] = 502813668.0 / 8388608, //59.940060138702392578125
			["PSX_PAL"] = 419432765.0 / 8388608, //50.00028192996978759765625

			// according to https://github.com/TASEmulators/mednafen/blob/382ff1b8d293c9a862497706808cbb79b2cecbfb/src/ss/vdp2.cpp#L904-L907
			["SAT"] = 8734090909.0 / 145852525, // = 1746818181.8 / 61 / 4 / 455 / ((263 + 262.5) / 2.0) ≈ 59.8830284837
			["SAT_PAL"] = 62500.0 / 1251, // = 1734687500.0 / 61 / 4 / 455 / ((313 + 312.5) / 2.0) ≈ 49.9600319744

			["C64_PAL"] = PALCarrier * 2 / 9 / 312 / 63,
			["C64_NTSC"] = NTSCCarrier * 2 / 7 / 263 / 65,
			["C64_NTSC_OLD"] = NTSCCarrier * 2 / 7 / 262 / 64,
			["C64_DREAN"] = PALNCarrier * 2 / 7 / 312 / 65,
			["INTV"] = 59.92,

			["ZXSpectrum_PAL"] = 50.080128205,
			["AmstradCPC_PAL"] = 50.08012820512821,	// = 1 / ((1024 * 312) / 16,000,000)

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
			=> Rates.TryGetValue(systemId + (pal ? "_PAL" : ""), out var d) ? d : 60.0;
	}
}
