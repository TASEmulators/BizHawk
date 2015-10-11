using System;
using System.Linq;
using System.Text;
using System.IO;
using System.Collections.Generic;

namespace BizHawk.Emulation.DiscSystem
{
	/// <summary>
	/// Represents a TOC entry discovered in the Q subchannel data of the lead-in track by the reader. These are stored redundantly.
	/// It isn't clear whether we need anything other than the SubchannelQ data, so I abstracted this in case we need it.
	/// </summary>
	public class RawTOCEntry
	{
		public SubchannelQ QData;
	}

	public enum DiscInterface
	{
		BizHawk, MednaDisc, LibMirage
	}

	public enum SessionFormat
	{
		None = -1,
		Type00_CDROM_CDDA = 0x00,
		Type10_CDI = 0x10,
		Type20_CDXA = 0x20
	}

	/// <summary>
	/// encapsulates a 2 digit BCD number as used various places in the CD specs
	/// </summary>
	public struct BCD2
	{
		/// <summary>
		/// The raw BCD value. you can't do math on this number! but you may be asked to supply it to a game program.
		/// The largest number it can logically contain is 99
		/// </summary>
		public byte BCDValue;

		/// <summary>
		/// The derived decimal value. you can do math on this! the largest number it can logically contain is 99.
		/// </summary>
		public int DecimalValue
		{
			get { return (BCDValue & 0xF) + ((BCDValue >> 4) & 0xF) * 10; }
			set { BCDValue = IntToBCD(value); }
		}

		/// <summary>
		/// makes a BCD2 from a decimal number. don't supply a number > 99 or you might not like the results
		/// </summary>
		public static BCD2 FromDecimal(int d)
		{
			return new BCD2 { DecimalValue = d };
		}

		public static BCD2 FromBCD(byte b)
		{
			return new BCD2 { BCDValue = b };
		}

		public static int BCDToInt(byte n)
		{
			var bcd = new BCD2();
			bcd.BCDValue = n;
			return bcd.DecimalValue;
		}

		public static byte IntToBCD(int n)
		{
			int ones;
			int tens = Math.DivRem(n, 10, out ones);
			return (byte)((tens << 4) | ones);
		}

		public override string ToString()
		{
			return BCDValue.ToString("X2");
		}
	}

	public static class MSF
	{
		public static int ToInt(int m, int s, int f)
		{
				return m * 60 * 75 + s * 75 + f;
		}
	}

	/// <summary>
	/// todo - rename to MSF? It can specify durations, so maybe it should be not suggestive of timestamp
	/// TODO - can we maybe use BCD2 in here
	/// </summary>
	public struct Timestamp
	{
		/// <summary>
		/// Checks if the string is a legit MSF. It's strict.
		/// </summary>
		public static bool IsMatch(string str)
		{
			return new Timestamp(str).Valid;
		}

		/// <summary>
		/// creates a timestamp from a string in the form mm:ss:ff
		/// </summary>
		public Timestamp(string str)
		{
			if (str.Length != 8) goto BOGUS;
			if (str[0] < '0' || str[0] > '9') goto BOGUS;
			if (str[1] < '0' || str[1] > '9') goto BOGUS;
			if (str[2] != ':') goto BOGUS;
			if (str[3] < '0' || str[3] > '9') goto BOGUS;
			if (str[4] < '0' || str[4] > '9') goto BOGUS;
			if (str[5] != ':') goto BOGUS;
			if (str[6] < '0' || str[6] > '9') goto BOGUS;
			if (str[7] < '0' || str[7] > '9') goto BOGUS;
			MIN = (byte)((str[0] - '0') * 10 + (str[1] - '0'));
			SEC = (byte)((str[3] - '0') * 10 + (str[4] - '0'));
			FRAC = (byte)((str[6] - '0') * 10 + (str[7] - '0'));
			Valid = true;
			Negative = false;
			return;
		BOGUS:
			MIN = SEC = FRAC = 0;
			Valid = false;
			Negative = false;
			return;
		}

		/// <summary>
		/// The string representation of the MSF
		/// </summary>
		public string Value
		{
			get
			{
				if (!Valid) return "--:--:--";
				return string.Format("{0}{1:D2}:{2:D2}:{3:D2}", Negative ? '-' : '+', MIN, SEC, FRAC);
			}
		}

		public readonly byte MIN, SEC, FRAC;
		public readonly bool Valid, Negative;

		/// <summary>
		/// The fully multiplied out flat-address Sector number
		/// </summary>
		public int Sector { get { return MIN * 60 * 75 + SEC * 75 + FRAC; } }

		/// <summary>
		/// creates timestamp from the supplied MSF
		/// </summary>
		public Timestamp(int m, int s, int f)
		{
			MIN = (byte)m;
			SEC = (byte)s;
			FRAC = (byte)f;
			Valid = true;
			Negative = false;
		}

		/// <summary>
		/// creates timestamp from supplied SectorNumber
		/// </summary>
		public Timestamp(int SectorNumber)
		{
			if (SectorNumber < 0)
			{
				SectorNumber = -SectorNumber;
				Negative = true;
			}
			else Negative = false;
			MIN = (byte)(SectorNumber / (60 * 75));
			SEC = (byte)((SectorNumber / 75) % 60);
			FRAC = (byte)(SectorNumber % 75);
			Valid = true;
		}

		public override string ToString()
		{
			return Value;
		}
	}
}