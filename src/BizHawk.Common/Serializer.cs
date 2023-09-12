#nullable disable

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;

using BizHawk.Common.IOExtensions;
using BizHawk.Common.BufferExtensions;

namespace BizHawk.Common
{
	public unsafe class Serializer
	{
		public Serializer() { }

		public bool IsReader { get; private set; }

		public bool IsWriter => !IsReader;

		public bool IsText { get; private set; }

		public BinaryReader BinaryReader { get; private set; }

		public BinaryWriter BinaryWriter { get; private set; }

		public TextReader TextReader { get; private set; }

		public TextWriter TextWriter { get; private set; }

		public Serializer(BinaryWriter bw)
		{
			StartWrite(bw);
		}

		public Serializer(BinaryReader br)
		{
			StartRead(br);
		}

		public Serializer(TextWriter tw)
		{
			StartWrite(tw);
		}

		public Serializer(TextReader tr)
		{
			StartRead(tr);
		}

		public static Serializer CreateBinaryWriter(BinaryWriter bw) => new(bw);

		public static Serializer CreateBinaryReader(BinaryReader br) => new(br);

		public static Serializer CreateTextWriter(TextWriter tw) => new(tw);

		public static Serializer CreateTextReader(TextReader tr) => new(tr);

		public void StartWrite(BinaryWriter bw)
		{
			BinaryWriter = bw;
			IsReader = false;
		}

		public void StartRead(BinaryReader br)
		{
			BinaryReader = br;
			IsReader = true;
		}

		public void StartWrite(TextWriter tw)
		{
			TextWriter = tw;
			IsReader = false;
			IsText = true;
		}

		public void StartRead(TextReader tr)
		{
			TextReader = tr;
			IsReader = true;
			IsText = true;
			BeginTextBlock();
		}

		public void BeginSection(string name)
		{
			_sections.Push(name);
			if (IsText)
			{
				if (IsWriter)
				{
					TextWriter.WriteLine("[{0}]", name);
				}
				else
				{
					_sectionStack.Push(_currSection);
					_currSection = _currSection[name];
				}
			}
		}

		public void EndSection()
		{
			string name = _sections.Pop();
			if (IsText)
			{
				if (IsWriter)
				{
					TextWriter.WriteLine("[/{0}]", name);
				}
				else
				{
					_currSection = _sectionStack.Pop();
				}
			}
		}

		/// <exception cref="InvalidOperationException"><typeparamref name="T"/> does not inherit <see cref="Enum"/></exception>
		public void SyncEnum<T>(string name, ref T val) where T : struct
		{
			if (typeof(T).BaseType != typeof(Enum))
			{
				throw new InvalidOperationException();
			}
			
			if (IsText)
			{
				SyncEnumText(name, ref val);
			}
			else if (IsReader)
			{
				val = (T)Enum.ToObject(typeof(T), BinaryReader.ReadInt32());
			}
			else
			{
				BinaryWriter.Write(Convert.ToInt32(val));
			}
		}

		public void SyncEnumText<T>(string name, ref T val) where T : struct
		{
			if (IsReader)
			{
				if (Present(name))
				{
					val = (T)Enum.Parse(typeof(T), Item(name));
				}
			}
			else
			{
				TextWriter.WriteLine("{0} {1}", name, val);
			}
		}

		public void Sync(string name, ref byte[] val, bool useNull)
		{
			if (IsText)
			{
				SyncText(name, ref val, useNull);
			}
			else if (IsReader)
			{
				val = BinaryReader.ReadByteBuffer(useNull);
			}
			else
			{
				BinaryWriter.WriteByteBuffer(val);
			}
		}

		public void SyncText(string name, ref byte[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					val = Item(name).HexStringToBytes();
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				byte[] temp = val ?? Array.Empty<byte>();
				TextWriter.WriteLine("{0} {1}", name, temp.BytesToHexString());
			}
		}

		public void Sync(string name, ref bool[] val, bool useNull)
		{
			if (IsText)
			{
				SyncText(name, ref val, useNull);
			}
			else if (IsReader)
			{
				val = BinaryReader.ReadByteBuffer(false).ToBoolBuffer();
				if (val == null && !useNull)
				{
					val = Array.Empty<bool>();
				}
			}
			else
			{
				BinaryWriter.WriteByteBuffer(val.ToUByteBuffer());
			}
		}

		public void SyncText(string name, ref bool[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					byte[] bytes = Item(name).HexStringToBytes();
					val = bytes.ToBoolBuffer();
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				bool[] temp = val ?? Array.Empty<bool>();
				TextWriter.WriteLine("{0} {1}", name, temp.ToUByteBuffer().BytesToHexString());
			}
		}
		public void Sync(string name, ref short[] val, bool useNull)
		{
			if (IsText)
			{
				SyncText(name, ref val, useNull);
			}
			else if (IsReader)
			{
				val = BinaryReader.ReadByteBuffer(false).ToShortBuffer();
				if (val == null && !useNull)
				{
					val = Array.Empty<short>();
				}
			}
			else
			{
				BinaryWriter.WriteByteBuffer(val.ToUByteBuffer());
			}
		}

		public void Sync(string name, ref ushort[] val, bool useNull)
		{
			if (IsText)
			{
				SyncText(name, ref val, useNull);
			}
			else if (IsReader)
			{
				val = BinaryReader.ReadByteBuffer(false).ToUShortBuffer();
				if (val == null && !useNull)
				{
					val = Array.Empty<ushort>();
				}
			}
			else
			{
				BinaryWriter.WriteByteBuffer(val.ToUByteBuffer());
			}
		}

		public void SyncText(string name, ref short[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					byte[] bytes = Item(name).HexStringToBytes();
					val = bytes.ToShortBuffer();
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				short[] temp = val ?? Array.Empty<short>();
				TextWriter.WriteLine("{0} {1}", name, temp.ToUByteBuffer().BytesToHexString());
			}
		}

		public void SyncText(string name, ref ushort[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					byte[] bytes = Item(name).HexStringToBytes();
					val = bytes.ToUShortBuffer();
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				ushort[] temp = val ?? Array.Empty<ushort>();
				TextWriter.WriteLine("{0} {1}", name, temp.ToUByteBuffer().BytesToHexString());
			}
		}

		public void Sync(string name, ref int[] val, bool useNull)
		{
			if (IsText)
			{
				SyncText(name, ref val, useNull);
			}
			else if (IsReader)
			{
				val = BinaryReader.ReadByteBuffer(false).ToIntBuffer();
				if (val == null && !useNull)
				{
					val = Array.Empty<int>();
				}
			}
			else
			{
				BinaryWriter.WriteByteBuffer(val.ToUByteBuffer());
			}
		}

		public void SyncText(string name, ref int[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					byte[] bytes = Item(name).HexStringToBytes();
					val = bytes.ToIntBuffer();
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				int[] temp = val ?? Array.Empty<int>();
				TextWriter.WriteLine("{0} {1}", name, temp.ToUByteBuffer().BytesToHexString());
			}
		}

		public void Sync(string name, ref uint[] val, bool useNull)
		{
			if (IsText)
			{
				SyncText(name, ref val, useNull);
			}
			else if (IsReader)
			{
				val = BinaryReader.ReadByteBuffer(false).ToUIntBuffer();
				if (val == null && !useNull)
				{
					val = Array.Empty<uint>();
				}
			}
			else
			{
				BinaryWriter.WriteByteBuffer(val.ToUByteBuffer());
			}
		}

		public void SyncText(string name, ref uint[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					byte[] bytes = Item(name).HexStringToBytes();
					val = bytes.ToUIntBuffer();
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				uint[] temp = val ?? Array.Empty<uint>();
				TextWriter.WriteLine("{0} {1}", name, temp.ToUByteBuffer().BytesToHexString());
			}
		}

		public void Sync(string name, ref float[] val, bool useNull)
		{
			if (IsText)
			{
				SyncText(name, ref val, useNull);
			}
			else if (IsReader)
			{
				val = BinaryReader.ReadByteBuffer(false).ToFloatBuffer();
				if (val == null && !useNull)
				{
					val = Array.Empty<float>();
				}
			}
			else
			{
				BinaryWriter.WriteByteBuffer(val.ToUByteBuffer());
			}
		}

		public void SyncText(string name, ref float[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					byte[] bytes = Item(name).HexStringToBytes();
					val = bytes.ToFloatBuffer();
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				float[] temp = val ?? Array.Empty<float>();
				TextWriter.WriteLine("{0} {1}", name, temp.ToUByteBuffer().BytesToHexString());
			}
		}

		public void Sync(string name, ref double[] val, bool useNull)
		{
			if (IsText)
			{
				SyncText(name, ref val, useNull);
			}
			else if (IsReader)
			{
				val = BinaryReader.ReadByteBuffer(false).ToDoubleBuffer();
				if (val == null && !useNull)
				{
					val = Array.Empty<double>();
				}
			}
			else
			{
				BinaryWriter.WriteByteBuffer(val.ToUByteBuffer());
			}
		}

		public void SyncText(string name, ref double[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					byte[] bytes = Item(name).HexStringToBytes();
					val = bytes.ToDoubleBuffer();
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				double[] temp = val ?? Array.Empty<double>();
				TextWriter.WriteLine("{0} {1}", name, temp.ToUByteBuffer().BytesToHexString());
			}
		}

		public void Sync(string name, ref Bit val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void SyncText(string name, ref Bit val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		public void Sync(string name, ref byte val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void Sync(string name, ref ushort val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void Sync(string name, ref uint val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void Sync(string name, ref sbyte val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void Sync(string name, ref short val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void Sync(string name, ref int val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void Sync(string name, ref long val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void Sync(string name, ref ulong val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void Sync(string name, ref float val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void Sync(string name, ref double val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		public void Sync(string name, ref bool val)
		{
			if (IsText)
			{
				SyncText(name, ref val);
			}
			else if (IsReader)
			{
				Read(ref val);
			}
			else
			{
				Write(ref val);
			}
		}

		/// <exception cref="InvalidOperationException"><see cref="IsReader"/> is <see langword="false"/> and <paramref name="name"/> is longer than <paramref name="length"/> chars</exception>
		public void SyncFixedString(string name, ref string val, int length)
		{
			// TODO - this could be made more efficient perhaps just by writing values right out of the string..
			if (IsReader)
			{
				char[] buf = new char[length];
				if (IsText)
				{
					TextReader.Read(buf, 0, length);
				}
				else
				{
					BinaryReader.Read(buf, 0, length);
				}

				int len = 0;
				for (; len < length; len++)
				{
					if (buf[len] == 0)
					{
						break;
					}
				}

				val = new string(buf, 0, len);
			}
			else
			{
				if (name.Length > length)
				{
					throw new InvalidOperationException($"{nameof(SyncFixedString)} too long");
				}

				char[] buf = val.ToCharArray();
				char[] remainder = new char[length - buf.Length];
				if (IsText)
				{
					TextWriter.Write(buf);
					TextWriter.Write(remainder);
				}
				else
				{
					BinaryWriter.Write(buf);
					BinaryWriter.Write(remainder);
				}
			}
		}

		public void SyncDelta<T>(string name, T[] original, T[] current)
			where T : unmanaged
		{
			if (IsReader)
			{
				byte[] delta = Array.Empty<byte>();
				Sync(name, ref delta, useNull: false);
				DeltaSerializer.ApplyDelta<T>(original, current, delta);
			}
			else
			{
				byte[] delta = DeltaSerializer.GetDelta<T>(original, current).ToArray(); // TODO: don't create array here (need .net update to write span to binary writer)
				Sync(name, ref delta, useNull: false);
			}
		}

		private readonly Stack<string> _sections = new();
		private Section _readerSection, _currSection;
		private readonly Stack<Section> _sectionStack = new();

		private void BeginTextBlock()
		{
			if (!IsText || IsWriter)
			{
				return;
			}

			_readerSection = new Section();
			Stack<Section> ss = new();
			ss.Push(_readerSection);
			var curs = _readerSection;

			System.Text.RegularExpressions.Regex rxEnd = new(@"\[/(.*?)\]", System.Text.RegularExpressions.RegexOptions.Compiled);
			System.Text.RegularExpressions.Regex rxBegin = new(@"\[(.*?)\]", System.Text.RegularExpressions.RegexOptions.Compiled);

			// read the entire file into a data structure for flexi-parsing
			string str;
			while ((str = TextReader.ReadLine()) != null)
			{
				var end = rxEnd.Match(str);
				var begin = rxBegin.Match(str);
				if (end.Success)
				{
					string name = end.Groups[1].Value;
					if (name != curs.Name)
					{
						throw new InvalidOperationException("Mis-formed savestate blob");
					}

					curs = ss.Pop();
					
					// consume no data past the end of the last proper section
					if (curs == _readerSection)
					{
						_currSection = curs;
						return;
					}
				}
				else if (begin.Success)
				{
					string name = begin.Groups[1].Value;
					ss.Push(curs);
					Section news = new() { Name = name };
					curs.Add(name, news);
					curs = news;
				}
				else
				{
					// add to current section
					if (str.Trim().Length == 0)
					{
						continue;
					}

					string[] parts = str.Split(' ');
					string key = parts[0];

					// UGLY: adds whole string instead of splitting the key. later, split the key, and have the individual Sync methods give up that responsibility
					curs.Items.Add(key, parts[1]);
				}
			}

			_currSection = _readerSection;
		}

		private string Item(string key) => _currSection.Items[key];

		private bool Present(string key) => _currSection.Items.ContainsKey(key);

		private void SyncBuffer(string name, int elemsize, int len, void* ptr)
		{
			if (IsReader)
			{
				byte[] temp = null;
				Sync(name, ref temp, false);
				int todo = Math.Min(temp.Length, len * elemsize);
				System.Runtime.InteropServices.Marshal.Copy(temp, 0, new IntPtr(ptr), todo);
			}
			else
			{
				int todo = len * elemsize;
				byte[] temp = new byte[todo];
				System.Runtime.InteropServices.Marshal.Copy(new IntPtr(ptr), temp, 0, todo);
				Sync(name, ref temp, false);
			}
		}

		private void SyncText(string name, ref byte val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void SyncText(string name, ref ushort val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void SyncText(string name, ref uint val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void SyncText(string name, ref sbyte val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void SyncText(string name, ref short val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void SyncText(string name, ref int val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void SyncText(string name, ref long val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void SyncText(string name, ref ulong val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void SyncText(string name, ref float val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void SyncText(string name, ref double val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void SyncText(string name, ref bool val)
		{
			if (IsReader)
			{
				ReadText(name, ref val);
			}
			else
			{
				WriteText(name, ref val);
			}
		}

		private void Read(ref Bit val) => val = BinaryReader.ReadBit();

		private void Write(ref Bit val) => BinaryWriter.WriteBit(val);

		private void ReadText(string name, ref Bit val)
		{
			if (Present(name))
			{
				val = int.Parse(Item(name));
			}
		}

		private void WriteText(string name, ref Bit val) => TextWriter.WriteLine("{0} {1}", name, (int)val);

		private void Read(ref byte val) => val = BinaryReader.ReadByte();

		private void Write(ref byte val) => BinaryWriter.Write(val);

		private void ReadText(string name, ref byte val)
		{
			if (Present(name))
			{
				val = byte.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}
		private void WriteText(string name, ref byte val) => TextWriter.WriteLine("{0} 0x{1:X2}", name, val);

		private void Read(ref ushort val) => val = BinaryReader.ReadUInt16();

		private void Write(ref ushort val) => BinaryWriter.Write(val);

		private void ReadText(string name, ref ushort val)
		{
			if (Present(name))
			{
				val = ushort.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref ushort val) => TextWriter.WriteLine("{0} 0x{1:X4}", name, val);

		private void Read(ref uint val)
		{
			{ val = BinaryReader.ReadUInt32(); }
		}

		private void Write(ref uint val) => BinaryWriter.Write(val);

		private void ReadText(string name, ref uint val)
		{
			if (Present(name))
			{
				val = uint.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref uint val) => TextWriter.WriteLine("{0} 0x{1:X8}", name, val);

		private void Read(ref sbyte val) => val = BinaryReader.ReadSByte();

		private void Write(ref sbyte val) => BinaryWriter.Write(val);

		private void ReadText(string name, ref sbyte val)
		{
			if (Present(name))
			{
				val = sbyte.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref sbyte val) => TextWriter.WriteLine("{0} 0x{1:X2}", name, val);

		private void Read(ref short val) => val = BinaryReader.ReadInt16();

		private void Write(ref short val) => BinaryWriter.Write(val);

		private void ReadText(string name, ref short val)
		{
			if (Present(name))
			{
				val = short.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref short val) => TextWriter.WriteLine("{0} 0x{1:X4}", name, val);

		private void Read(ref int val) => val = BinaryReader.ReadInt32();

		private void Write(ref int val) => BinaryWriter.Write(val);

		private void ReadText(string name, ref int val)
		{
			if (Present(name))
			{
				val = int.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref int val) => TextWriter.WriteLine("{0} 0x{1:X8}", name, val);

		private void Read(ref long val) => val = BinaryReader.ReadInt64();

		private void Write(ref long val) => BinaryWriter.Write(val);

		private void Read(ref ulong val) => val = BinaryReader.ReadUInt64();

		private void Write(ref ulong val) => BinaryWriter.Write(val);

		private void ReadText(string name, ref long val)
		{
			if (Present(name))
			{
				val = long.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref long val) => TextWriter.WriteLine("{0} 0x{1:X16}", name, val);

		private void ReadText(string name, ref ulong val)
		{
			if (Present(name))
			{
				val = ulong.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref ulong val) => TextWriter.WriteLine("{0} 0x{1:X16}", name, val);

		private void Read(ref float val) => val = BinaryReader.ReadSingle();

		private void Write(ref float val) => BinaryWriter.Write(val);

		private void Read(ref double val) => val = BinaryReader.ReadDouble();

		private void Write(ref double val) => BinaryWriter.Write(val);

		private void ReadText(string name, ref float val)
		{
			if (Present(name))
			{
				val = float.Parse(Item(name), NumberFormatInfo.InvariantInfo);
			}
		}

		private void WriteText(string name, ref float val) => TextWriter.WriteLine("{0} {1}", name, val);

		private void ReadText(string name, ref double val)
		{
			if (Present(name))
			{
				val = double.Parse(Item(name), NumberFormatInfo.InvariantInfo);
			}
		}

		private void WriteText(string name, ref double val) => TextWriter.WriteLine("{0} {1}", name, val);

		private void Read(ref bool val) => val = BinaryReader.ReadBoolean();

		private void Write(ref bool val) => BinaryWriter.Write(val);

		private void ReadText(string name, ref bool val)
		{
			if (Present(name))
			{
				val = bool.Parse(Item(name));
			}
		}
		private void WriteText(string name, ref bool val) => TextWriter.WriteLine("{0} {1}", name, val);

		private sealed class Section : Dictionary<string, Section>
		{
			public string Name = "";
			public readonly Dictionary<string, string> Items = new();
		}
	}
}
