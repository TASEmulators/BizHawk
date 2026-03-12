namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
    /// Information about a Track.
    /// </summary>
    public class DiscTrack
    {
    	//Notable omission:
    		//a list of Indices. It's difficult to reliably construct it.
    		//Notably, mednafen can't readily produce it.
    		//Indices may need scanning sector by sector.
    		//It's unlikely that any software would be needing indices anyway.
    		//We should add another index scanning service if that's ever needed.
    		//(note: a CCD should contain indices, but it's not clear whether it's required. logically it shouldn't be)
    	//Notable omission:
    		//Length of the track.
    		//How should this be defined? Between which indices? It's really hard.

    	//These omissions could be handled by ReadStructure() policies which permit the scanning of the entire disc.
    	//After that, they could be cached in here.

    	/// <summary>
    	/// The number of the track (1-indexed)
    	/// </summary>
    	public int Number;

    	/// <summary>
    	/// The Mode of the track (0 is Audio, 1 and 2 are data)
    	/// This is heuristically determined.
    	/// Actual sector contents may vary
    	/// </summary>
    	public int Mode;

    	/// <summary>
    	/// Is this track a Data track?
    	/// </summary>
    	public bool IsData => !IsAudio;

    	/// <summary>
    	/// Is this track an Audio track?
    	/// </summary>
    	public bool IsAudio => Mode == 0;

    	/// <summary>
    	/// The 'control' properties of the track expected to be found in the track's subQ.
    	/// However, this is what's indicated by the disc TOC.
    	/// Actual sector contents may vary.
    	/// </summary>
    	public EControlQ Control;

    	/// <summary>
    	/// The starting LBA of the track (index 1).
    	/// </summary>
    	public int LBA;

    	/// <summary>
    	/// The next track in the session. null for the leadout track of a session.
    	/// </summary>
    	public DiscTrack NextTrack;

		/// <summary>
		/// The Type of a track as specified in the TOC Q-Subchannel data from the control flags.
		/// Could also be 4-Channel Audio, but we'll handle that later if needed
		/// </summary>
    	public enum ETrackType
		{
			/// <summary>
			/// The track type isn't always known.. it can take this value til its populated
			/// </summary>
			Unknown,

			/// <summary>
			/// Data track( TOC Q control 0x04 flag set )
			/// </summary>
			Data,

			/// <summary>
			/// Audio track( TOC Q control 0x04 flag clear )
			/// </summary>
			Audio,
		}
    }
}