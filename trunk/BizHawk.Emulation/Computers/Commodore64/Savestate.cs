using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class C64 : IEmulator
	{
		public void ClearSaveRam()
		{
		}
		public void LoadStateBinary(BinaryReader br)
		{
		}
		public void LoadStateText(TextReader reader)
		{
		}
		public byte[] ReadSaveRam()
		{
			return null;
		}
		public bool SaveRamModified
		{
			get;
			set;
		}
		public void SaveStateBinary(BinaryWriter bw)
		{
		}
		public void SaveStateText(TextWriter writer)
		{ 
		}
		public void StoreSaveRam(byte[] data)
		{
		}
	}

	public class State
	{
		private Dictionary<string, StateParameters> paramList = new Dictionary<string, StateParameters>();


	}

	public class StateParameters
	{
		private Dictionary<string, int> integerList = new Dictionary<string, int>();

		public int this[string key]
		{
			get
			{
				if (integerList.ContainsKey(key))
				{
					return integerList[key];
				}
				return 0;
			}
			set
			{
				integerList[key] = value;
			}
		}

		public void ExportBinary(Stream target)
		{
			BinaryWriter writer = new BinaryWriter(target);

			writer.Write((Int32)integerList.Count);
			foreach (KeyValuePair<string, int> kv in integerList)
			{
				writer.Write(kv.Key);
				writer.Write(kv.Value);
			}

			writer.Flush();
		}

		public void ExportText(Stream target)
		{
			StringBuilder sb = new StringBuilder();

			foreach (KeyValuePair<string, int> kv in integerList)
			{
				sb.Append(kv.Key + "=");
				sb.AppendLine(kv.Value.ToString());
			}

			StreamWriter writer = new StreamWriter(target);
			writer.Write(sb.ToString());
			writer.Flush();
		}

		public void ImportBinary(Stream source)
		{
			BinaryReader reader = new BinaryReader(source);

			int count = reader.ReadInt32();
			for (int i = 0; i < count; i++)
			{
				string key = reader.ReadString();
				int val = reader.ReadInt32();
				integerList[key] = val;
			}
		}

		public void ImportText(Stream source)
		{
			StreamReader reader = new StreamReader(source);

			while (!reader.EndOfStream)
			{
				string line = reader.ReadLine();
				int equalsIndex = line.IndexOf("=");

				if (equalsIndex >= 0 && equalsIndex < (line.Length - 1))
				{
					string key = line.Substring(0, equalsIndex - 1);
					string val = line.Substring(equalsIndex + 1);

					if (val.Length > 0 && key.Length > 0)
					{
						integerList[key] = int.Parse(val);
					}
				}
			}
		}
	}
}
