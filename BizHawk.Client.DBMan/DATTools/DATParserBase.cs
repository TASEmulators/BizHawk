using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace BizHawk.Client.DBMan
{
	public abstract class DATParser
	{
		/// <summary>
		/// Required to generate a GameDB file
		/// </summary>
		public abstract SystemType SysType { get; set; }

		/// <summary>
		/// Parses multiple DAT files and returns a single GamesDB format csv string
		/// </summary>
		/// <param name="filePath"></param>
		/// <returns></returns>
		public abstract string ParseDAT(string[] filePath);

		protected List<string> IncomingData = new List<string>();

		protected List<GameDB> Data = new List<GameDB>();

		protected StringBuilder sb = new StringBuilder();

		protected void AddCommentBlock(string comment)
		{
			sb.AppendLine(";;;;;;;;;;--------------------------------------------------;;;;;;;;;;");
			sb.AppendLine(";;; " + comment.Replace("\r\n", "\r\n;;; "));
			sb.AppendLine(";;;;;;;;;;--------------------------------------------------;;;;;;;;;;");
		}

		protected void AddCommentBlock(string[] comment)
		{
			sb.AppendLine(";;;;;;;;;;--------------------------------------------------;;;;;;;;;;");
			for (int i = 0; i < comment.Length; i++)
			{
				sb.AppendLine(";;; " + comment[i]);
			}
			sb.AppendLine(";;;;;;;;;;--------------------------------------------------;;;;;;;;;;");
		}

		protected void AppendCSVData(List<GameDB> data)
		{
			if (data == null || data.Count == 0)
			{
				sb.AppendLine(";");
				return;
			}
				
			foreach (var d in data)
			{
				// hash
				sb.Append(d.HASH);
				sb.Append("\t");
				// status
				sb.Append(d.Status);
				sb.Append("\t");
				// name
				sb.Append(d.Name);
				sb.Append("\t");
				// system
				sb.Append(d.System);

				// additional optional fields
				bool[] populated = new bool[4];
				if (d.Notes != null)
					populated[0] = true;
				if (d.MetaData != null)
					populated[1] = true;
				if (d.Region != null)
					populated[2] = true;
				if (d.ForcedCore != null)
					populated[3] = true;

				int last = 0;
				for (int i = 3; i >= 0; i--)
				{
					if (populated[i])
					{
						last = i;
						break;
					}
				}

				int cnt = 0;

				// notes
				if (d.Notes != null)
				{
					sb.Append("\t");
					sb.Append(d.Notes);
				}
				else if (cnt++ <= last)
				{
					sb.Append("\t");
				}
				// metadata
				if (d.MetaData != null)
				{
					sb.Append("\t");
					sb.Append(d.MetaData);
				}
				else if (cnt++ <= last)
				{
					sb.Append("\t");
				}
				// region
				if (d.Region != null)
				{
					sb.Append("\t");
					sb.Append(d.Region);
				}
				else if (cnt++ <= last)
				{
					sb.Append("\t");
				}
				// force core
				if (d.ForcedCore != null)
				{
					sb.Append("\t");
					sb.Append(d.ForcedCore);
				}

				sb.Append("\r\n");
			}
		}
	}

	/// <summary>
	/// DAT data is parsed into this object
	/// (every field is not always used)
	/// </summary>
	public class GameDB
	{
		// COL0:	Hash
		public string SHA1 { get; set; }
		public string MD5 { get; set; }
		// COL1:	Status code indicator
		public string Status { get; set; }
		// COL2:	Game title
		public string Name { get; set; }
		// COL3:	System code (must match what bizhawk uses in Emulation.Common/Database/Database.cs
		public string System { get; set; }
		// COL4:	Unknown - not currently parsed in database.cs, but some gamedb files use this for publisher/genre/etc
		public string Notes { get; set; }
		// COL5:	Metadata
		public string MetaData { get; set; }
		// COL6:	Region
		public string Region { get; set; }
		// COL7:	Forced Fore
		public string ForcedCore { get; set; }

		// prefer MD5 if available
		public string HASH
		{
			get
			{
				if (MD5.Trim() == "")
					return "sha1:" + SHA1;

				return MD5;
			}
		}

		/// <summary>
		/// Used to get the correct system code (that each gamedb csv needs)
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static string GetSystemCode(SystemType type)
		{
			switch (type)
			{
				case SystemType.P83:
					return "83P";
				case SystemType.X32:
					return "32X";
				default:
					return type.ToString();
			}
		}
	}

	public enum SystemType
	{
		SAT,
		PSP,
		PSX,
		GEN,
		PCFX,
		PCECD,
		GB,
		DGB,
		AppleII,
		C64,
		ZXSpectrum,
		AmstradCPC,
		SNES,
		NES,
		P83,
		GBC,
		A78,
		GBA,
		X32,
		GG,
		SG,
		SGX,
		A26,
		Coleco,
		INTV,
		N64,
		WSWAN,
		Lynx,
		VB,
		UZE,
		NGP
	}
}
