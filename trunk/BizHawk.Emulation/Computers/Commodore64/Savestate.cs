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

		// TODO: when disk support is finished, set this flag according to if any writes to disk were done
		public bool SaveRamModified
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			Dictionary<string, StateParameters> state = new Dictionary<string, StateParameters>();
		}
		public void SaveStateText(TextWriter writer)
		{ 
		}
		public void StoreSaveRam(byte[] data)
		{
		}
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
			string line = "";

			while (!reader.EndOfStream && !(line.Contains("[") && line.Contains("]")))
			{
				line = reader.ReadLine();
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

		public void Load(string key, out byte val)
		{
			val = (byte)(this[key] & 0xFF);
		}

		public void Load(string key, out int val)
		{
			val = this[key];
		}

		public void Load(string key, out bool val)
		{
			val = this[key] != 0;
		}

		public void Save(string key, byte val)
		{
			this[key] = (int)val;
		}

		public void Save(string key, int val)
		{
			this[key] = val;
		}

		public void Save(string key, bool val)
		{
			this[key] = val ? 1 : 0;
		}
	}
}
