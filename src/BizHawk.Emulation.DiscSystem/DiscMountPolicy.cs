namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// General disc policies to be logically applied at mounting time. The choices are irreversible once a disc is loaded.
	/// Maybe these are only for CUEs, but maybe not. Not sure yet.
	/// Could put caching policies here too (cached ecm calculations, etc.)
	/// </summary>
	public class DiscMountPolicy
	{
		/// <summary>
		/// "At the beginning of a Pause (i.e. Index = 00) the relative time is
		/// --A-- set to the duration of the Pause.
		/// During the Pause this relative time decreases and
		/// --B-- equals zero in the last Section"
		/// This is a contradiction.
		/// By choosing true, mode A is selected, and the final sector of the pause is -1.
		///  (I like this better. Defaulting until proven otherwise [write test case here])
		/// By choosing false, mode B is selected, and the final sector of the pause is 0.
		///  (Mednafen does it this way)
		/// Discs (including PSX) exist using A, or B, or possibly (reference please) neither.
		/// </summary>
		public bool CUE_PregapContradictionModeA = true;

		/// <summary>
		/// Mednafen sets mode2 pregap sectors as XA Form2 sectors.
		/// This is almost surely not right in every case.
		/// </summary>
		public bool CUE_PregapMode2_As_XA_Form2 = true;

		/// <summary>
		/// Mednafen loads SBI files oddly
		/// </summary>
		public bool SBI_As_Mednafen = true;

		public void SetForPSX()
		{
			CUE_PregapContradictionModeA = false;
			CUE_PregapMode2_As_XA_Form2 = true;
			SBI_As_Mednafen = true;
		}
	}

}