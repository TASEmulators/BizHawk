//TODO - call on unmanaged code in mednadisc if available to do deinterleaving faster. be sure to benchmark it though..

//a decent little subcode reference
//http://www.jbum.com/cdg_revealed.html

//NOTES: the 'subchannel Q' stuff here has a lot to do with the q-Mode 1. q-Mode 2 is different,
//and q-Mode 1 technically is defined a little differently in the lead-in area, although the fields align so the data structures can be reused

//Q subchannel basic structure: (quick ref: https://en.wikipedia.org/wiki/Compact_Disc_subcode)
//Byte 1: (aka `status`)
// q-Control: 4 bits (i.e. flags)
// q-Mode: 4 bits (aka ADR; WHY is this called ADR?)
//q-Data: other stuff depending on q-Mode and type of track
//q-CRC: CRC of preceding

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// Control bit flags for the Q Subchannel.
	/// </summary>
	[Flags]
	public enum EControlQ
	{
		None = 0,
		PRE = 1, //Pre-emphasis enabled (audio tracks only)
		DCP = 2, //Digital copy permitted
		DATA = 4, //set for data tracks, clear for audio tracks
		_4CH = 8, //Four channel audio
	}

	/// <summary>
	/// Why did I make this a struct? I thought there might be a shitton of these and I was trying to cut down on object creation churn during disc-loading.
	/// But I ended up mostly just having a shitton of byte[] for each buffer (I could improve that later to possibly reference a blob on top of a MemoryStream)
	/// So, I should probably change that.
	/// </summary>
	public struct SubchannelQ
	{
		/// <summary>
		/// ADR and CONTROL
		/// TODO - make BCD2? PROBABLY NOT. I DONT KNOW.
		/// </summary>
		public byte q_status;

		/// <summary>
		/// normal track: BCD indication of the current track number
		/// leadin track: should be 0
		/// </summary>
		public BCD2 q_tno;

		/// <summary>
		/// normal track: BCD indication of the current index
		/// leadin track: 'POINT' field used to ID the TOC entry #
		/// </summary>				
		public BCD2 q_index;

		/// <summary>
		/// These are the initial set of timestamps. Meaning varies:
		/// check yellowbook 22.3.3 and 22.3.4
		/// leadin track: unknown
		/// user information track: relative timestamp
		/// leadout: relative timestamp
		/// TODO - why are these BCD2? having things in BCD2 is freaking annoying, I should only make them BCD2 when serializing into a subchannel Q buffer
		/// EDIT - elsewhere I rambled "why not BCD2?". geh. need to make a final organized approach
		/// </summary>
		public BCD2 min, sec, frame;

		/// <summary>
		/// This is supposed to be zero.. but CCD format stores it, so maybe it's useful for copy protection or something
		/// </summary>
		public byte zero;

		/// <summary>
		/// These are the second set of timestamps.  Meaning varies:
		/// check yellowbook 22.3.3 and 22.3.4
		/// leadin track q-mode 1: TOC entry, absolute MSF of track
		/// user information track: absolute timestamp
		/// leadout: absolute timestamp
		/// </summary>
		public BCD2 ap_min, ap_sec, ap_frame;

		/// <summary>
		/// Don't assume this CRC is correct, in the case of some copy protections it is intended to be wrong.
		/// Furthermore, it is meaningless (and in BizHawk, unpopulated) for a TOC Entry
		/// (since an invalid CRC on a [theyre redundantly/duplicately stored] toc entry would cause it to get discarded in favor of another one with a correct CRC)
		/// </summary>
		public ushort q_crc;

		/// <summary>
		/// Retrieves the initial set of timestamps (min,sec,frac) as a convenient Timestamp
		/// </summary>
		public int Timestamp {
			get => MSF.ToInt(min.DecimalValue, sec.DecimalValue, frame.DecimalValue);
			set {
				var ts = new Timestamp(value);
				min.DecimalValue = ts.MIN; sec.DecimalValue = ts.SEC; frame.DecimalValue = ts.FRAC;
			}
		}

		/// <summary>
		/// Retrieves the second set of timestamps (ap_min, ap_sec, ap_frac) as a convenient Timestamp.
		/// TODO - rename everything AP here, it's nonsense. (the P is)
		/// </summary>
		public int AP_Timestamp {
			get => MSF.ToInt(ap_min.DecimalValue, ap_sec.DecimalValue, ap_frame.DecimalValue);
			set {
				var ts = new Timestamp(value);
				ap_min.DecimalValue = ts.MIN; ap_sec.DecimalValue = ts.SEC; ap_frame.DecimalValue = ts.FRAC;
			}
		}

		/// <summary>
		/// sets the status byte from the provided adr/qmode and control values
		/// </summary>
		public void SetStatus(byte adr_or_qmode, EControlQ control)
		{
			q_status = ComputeStatus(adr_or_qmode, control);
		}

		/// <summary>
		/// computes a status byte from the provided adr/qmode and control values
		/// </summary>
		public static byte ComputeStatus(int adr_or_qmode, EControlQ control)
		{
			return (byte)(adr_or_qmode | (((int)control) << 4));
		}

		/// <summary>
		/// Retrives the ADR field of the q_status member (low 4 bits)
		/// </summary>
		public int ADR => q_status & 0xF;

		/// <summary>
		/// Retrieves the CONTROL field of the q_status member (high 4 bits)
		/// </summary>
		public EControlQ CONTROL => (EControlQ)((q_status >> 4) & 0xF);
	}
	
}
