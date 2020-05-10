using System;

namespace BizHawk.Emulation.DiscSystem.CUE
{
	[Flags]
	public enum CueTrackFlags
	{
		None = 0,
		PRE = 1, //Pre-emphasis enabled (audio tracks only)
		DCP = 2, //Digital copy permitted
		DATA = 4, //Set automatically by cue-processing equipment, here for completeness
		_4CH = 8, //Four channel audio
		SCMS = 64, //Serial copy management system (not supported by all recorders) (??)
	}

	//All audio files (WAVE, AIFF, and MP3) must be in 44.1KHz 16-bit stereo format.
	//BUT NOTE: MP3 can be VBR and the length can't be known without decoding the whole thing. 
	//But, some ideas: 
	//1. we could operate ffmpeg differently to retrieve the length, which maybe it can do without having to decode the entire thing
	//2. we could retrieve it from an ID3 if present.
	//3. as a last resort, since MP3 is the annoying case usually, we could include my c# mp3 parser and sum the length (test the performance, this might be reasonably fast on par with ECM parsing)
	//NOTE: once deciding the length, we would have to stick with it! samples would have to be discarded or inserted to make the track work out
	//but we COULD effectively achieve stream-loading mp3 discs, with enough work.
	public enum CueFileType
	{
		Unspecified,
		BINARY, //Intel binary file (least significant byte first)
		MOTOROLA, //Motorola binary file (most significant byte first)
		AIFF, //Audio AIFF file
		WAVE, //Audio WAVE file
		MP3, //Audio MP3 file
	}

	public enum CueTrackType
	{
		Unknown,
		Audio, //Audio/Music (2352)
		CDG, //Karaoke CD+G (2448)
		Mode1_2048, //CDROM Mode1 Data (cooked)
		Mode1_2352, //CDROM Mode1 Data (raw)
		Mode2_2336, //CDROM-XA Mode2 Data (could contain form 1 or form 2)
		Mode2_2352, //CDROM-XA Mode2 Data (but there's no reason to distinguish this from Mode1_2352 other than to alert us that the entire session should be XA
		CDI_2336, //CDI Mode2 Data
		CDI_2352 //CDI Mode2 Data
	}
}