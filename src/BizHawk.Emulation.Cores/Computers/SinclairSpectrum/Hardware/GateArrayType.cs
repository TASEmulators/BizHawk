namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	public enum GateArrayType
	{
		/// <summary>
		/// CPC 464
		/// The first version of the Gate Array is the 40007 and was released with the CPC 464
		/// </summary>
		Amstrad40007,
		/// <summary>
		/// CPC 664
		/// Later, the CPC 664 came out fitted with the 40008 version (and at the same time, the CPC 464 was also upgraded with this version). 
		/// This version is pinout incompatible with the 40007 (that's why the upgraded 464 of this period have two Gate Array slots on the motherboard, 
		/// one for a 40007 and one for a 40008)
		/// </summary>
		Amstrad40008,
		/// <summary>
		/// CPC 6128
		/// The CPC 6128 was released with the 40010 version (and the CPC 464 and 664 manufactured at that time were also upgraded to this version). 
		/// The 40010 is pinout compatible with the previous 40008
		/// </summary>
		Amstrad40010,
		/// <summary>
		/// Costdown CPC
		/// In the last serie of CPC 464 and 6128 produced by Amstrad in 1988, a small ASIC chip have been used to reduce the manufacturing costs. 
		/// This ASIC emulates the Gate Array, the PAL and the CRTC 6845. And no, there is no extra features like on the Amstrad Plus. 
		/// The only noticeable difference seems to be about the RGB output levels which are not exactly the same than those produced with a real Gate Array
		/// </summary>
		Amstrad40226,
		/// <summary>
		/// Plus &amp; GX-4000
		/// All the Plus range is built upon a bigger ASIC chip which is integrating many features of the classic CPC (FDC, CRTC, PPI, Gate Array/PAL) and all 
		/// the new Plus specific features. The Gate Array on the Plus have a new register, named RMR2, to expand the ROM mapping functionnalities of the machine. 
		/// This register requires to be unlocked first to be available. And finally, the RGB levels produced by the ASIC on the Plus are noticeably differents
		/// </summary>
		Amstrad40489,
	}
}
