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

		#region Public

		public bool IsReader
		{
			get { return _isReader; }
		}

		public bool IsWriter
		{
			get { return !IsReader; }
		}

		public bool IsText
		{
			get { return _isText; }
		}

		public BinaryReader BinaryReader
		{
			get { return _br; }
		}

		public BinaryWriter BinaryWriter
		{
			get { return _bw; }
		}

		public TextReader TextReader
		{
			get { return _tr; }
		}

		public TextWriter TextWriter
		{
			get { return _tw; } 
		}

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

		public static Serializer CreateBinaryWriter(BinaryWriter bw)
		{
			return new Serializer(bw);
		}

		public static Serializer CreateBinaryReader(BinaryReader br)
		{
			return new Serializer(br);
		}

		public static Serializer CreateTextWriter(TextWriter tw)
		{
			return new Serializer(tw);
		}

		public static Serializer CreateTextReader(TextReader tr)
		{
			return new Serializer(tr);
		}

		public void StartWrite(BinaryWriter bw)
		{
			_bw = bw;
			_isReader = false;
		}

		public void StartRead(BinaryReader br)
		{
			_br = br;
			_isReader = true;
		}

		public void StartWrite(TextWriter tw)
		{
			_tw = tw;
			_isReader = false;
			_isText = true;
		}

		public void StartRead(TextReader tr)
		{
			_tr = tr;
			_isReader = true;
			_isText = true;
			BeginTextBlock();
		}

		public void BeginSection(string name)
		{
			this._sections.Push(name);
			if (IsText)
			{
				if (IsWriter)
				{
					_tw.WriteLine("[{0}]", name);
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
			var name = this._sections.Pop();
			if (IsText)
			{
				if (IsWriter)
				{
					_tw.WriteLine("[/{0}]", name);
				}
				else
				{
					_currSection = _sectionStack.Pop();
				}
			}
		}

		public void SyncEnum<T>(string name, ref T val) where T : struct
		{
			if (typeof(T).BaseType != typeof(Enum))
			{
				throw new InvalidOperationException();
			}
			else if (_isText)
			{
				SyncEnumText(name, ref val);
			}
			else if (IsReader)
			{
				val = (T)Enum.ToObject(typeof(T), _br.ReadInt32());
			}
			else
			{
				_bw.Write(Convert.ToInt32(val));
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
				_tw.WriteLine("{0} {1}", name, val);
			}
		}

		public void Sync(string name, ref ByteBuffer byteBuf)
		{
			SyncBuffer(name, 1, byteBuf.Len, byteBuf.Ptr);
		}

		public void Sync(string name, ref IntBuffer byteBuf)
		{
			SyncBuffer(name, 4, byteBuf.Len, byteBuf.Ptr);
		}

		public void Sync(string name, ref byte[] val, bool useNull)
		{
			if (IsText)
			{
				SyncText(name, ref val, useNull);
			}
			else if (IsReader)
			{
				val = Util.ReadByteBuffer(_br, useNull);
			}
			else
			{
				Util.WriteByteBuffer(_bw, val);
			}
		}

		public void SyncText(string name, ref byte[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					val = Util.HexStringToBytes(Item(name));
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				var temp = val ?? new byte[0];
				_tw.WriteLine("{0} {1}", name, temp.BytesToHexString());
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
				val = Util.ByteBufferToBoolBuffer(Util.ReadByteBuffer(_br, false));
				if (val == null && !useNull)
				{
					val = new bool[0];
				}
			}
			else
			{
				Util.WriteByteBuffer(_bw, Util.BoolBufferToByteBuffer(val));
			}
		}

		public void SyncText(string name, ref bool[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					var bytes = Util.HexStringToBytes(Item(name));
					val = Util.ByteBufferToBoolBuffer(bytes);
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				var temp = val ?? new bool[0];
				_tw.WriteLine("{0} {1}", name, Util.BoolBufferToByteBuffer(temp).BytesToHexString());
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
				val = Util.ByteBufferToShortBuffer(Util.ReadByteBuffer(_br, false));
				if (val == null && !useNull)
				{
					val = new short[0];
				}
			}
			else
			{
				Util.WriteByteBuffer(_bw, Util.ShortBufferToByteBuffer(val));
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
				val = Util.ByteBufferToUshortBuffer(Util.ReadByteBuffer(_br, false));
				if (val == null && !useNull)
				{
					val = new ushort[0];
				}
			}
			else
			{
				Util.WriteByteBuffer(_bw, Util.UshortBufferToByteBuffer(val));
			}
		}

		public void SyncText(string name, ref short[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					var bytes = Util.HexStringToBytes(Item(name));
					val = Util.ByteBufferToShortBuffer(bytes);
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				var temp = val ?? new short[0];
				_tw.WriteLine("{0} {1}", name, Util.ShortBufferToByteBuffer(temp).BytesToHexString());
			}
		}

		public void SyncText(string name, ref ushort[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					var bytes = Util.HexStringToBytes(Item(name));
					val = Util.ByteBufferToUshortBuffer(bytes);
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				var temp = val ?? new ushort[0];
				_tw.WriteLine("{0} {1}", name, Util.UshortBufferToByteBuffer(temp).BytesToHexString());
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
				val = Util.ByteBufferToIntBuffer(Util.ReadByteBuffer(_br, false));
				if (val == null && !useNull)
				{
					val = new int[0];
				}
			}
			else
			{
				Util.WriteByteBuffer(_bw, Util.IntBufferToByteBuffer(val));
			}
		}

		public void SyncText(string name, ref int[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					var bytes = Util.HexStringToBytes(Item(name));
					val = Util.ByteBufferToIntBuffer(bytes);
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				var temp = val ?? new int[0];
				_tw.WriteLine("{0} {1}", name, Util.IntBufferToByteBuffer(temp).BytesToHexString());
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
				val = Util.ByteBufferToUintBuffer(Util.ReadByteBuffer(_br, false));
				if (val == null && !useNull)
				{
					val = new uint[0];
				}
			}
			else
			{
				Util.WriteByteBuffer(_bw, Util.UintBufferToByteBuffer(val));
			}
		}

		public void SyncText(string name, ref uint[] val, bool useNull)
		{
			if (IsReader)
			{
				if (Present(name))
				{
					var bytes = Util.HexStringToBytes(Item(name));
					val = Util.ByteBufferToUintBuffer(bytes);
				}

				if (val != null && val.Length == 0 && useNull)
				{
					val = null;
				}
			}
			else
			{
				var temp = val ?? new uint[0];
				_tw.WriteLine("{0} {1}", name, Util.UintBufferToByteBuffer(temp).BytesToHexString());
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

		public void SyncFixedString(string name, ref string val, int length)
		{
			// TODO - this could be made more efficient perhaps just by writing values right out of the string..
			if (IsReader)
			{
				var buf = new char[length];
				if (_isText)
				{
					_tr.Read(buf, 0, length);
				}
				else
				{
					_br.Read(buf, 0, length);
				}

				var len = 0;
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
					throw new InvalidOperationException("SyncFixedString too long");
				}

				var buf = val.ToCharArray();
				var remainder = new char[length - buf.Length];
				if (IsText)
				{
					_tw.Write(buf);
					_tw.Write(remainder);
				}
				else
				{
					_bw.Write(buf);
					_bw.Write(remainder);
				}
			}
		}

		#endregion

		#region Privates

		private BinaryReader _br;
		private BinaryWriter _bw;
		private TextReader _tr;
		private TextWriter _tw;

		private bool _isText;
		private bool _isReader;
		private readonly Stack<string> _sections = new Stack<string>();
		private Section _readerSection, _currSection;
		private readonly Stack<Section> _sectionStack = new Stack<Section>();

		private void BeginTextBlock()
		{
			if (!IsText || IsWriter)
			{
				return;
			}

			_readerSection = new Section();
			var ss = new Stack<Section>();
			ss.Push(_readerSection);
			var curs = _readerSection;

			var rxEnd = new System.Text.RegularExpressions.Regex(@"\[/(.*?)\]", System.Text.RegularExpressions.RegexOptions.Compiled);
			var rxBegin = new System.Text.RegularExpressions.Regex(@"\[(.*?)\]", System.Text.RegularExpressions.RegexOptions.Compiled);

			// read the entire file into a data structure for flexi-parsing
			string str;
			while ((str = _tr.ReadLine()) != null)
			{
				var end = rxEnd.Match(str);
				var begin = rxBegin.Match(str);
				if (end.Success)
				{
					var name = end.Groups[1].Value;
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
					var name = begin.Groups[1].Value;
					ss.Push(curs);
					var news = new Section { Name = name };
					if (!curs.ContainsKey(name))
					{
						curs[name] = news;
					}
					else
					{
						throw new Exception(string.Format("Duplicate key \"{0}\" in serializer savestate!", name));
					}

					curs = news;
				}
				else
				{
					// add to current section
					if (str.Trim().Length == 0)
					{
						continue;
					}

					var parts = str.Split(' ');
					var key = parts[0];

					// UGLY: adds whole string instead of splitting the key. later, split the key, and have the individual Sync methods give up that responsibility
					if (!curs.Items.ContainsKey(key))
					{
						curs.Items[key] = parts[1];
					}
					else
					{
						throw new Exception(string.Format("Duplicate key \"{0}\" in serializer savestate!", key));
					}
				}
			}

			_currSection = _readerSection;
		}

		private string Item(string key)
		{
			return _currSection.Items[key];
		}

		private bool Present(string key)
		{
			return _currSection.Items.ContainsKey(key);
		}

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
				var temp = new byte[todo];
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

		private void Read(ref Bit val)
		{
			val = _br.ReadBit();
		}

		private void Write(ref Bit val)
		{
			_bw.WriteBit(val);
		}

		private void ReadText(string name, ref Bit val)
		{
			if (Present(name))
			{
				val = int.Parse(this.Item(name));
			}
		}

		private void WriteText(string name, ref Bit val)
		{
			_tw.WriteLine("{0} {1}", name, (int)val);
		}

		private void Read(ref byte val)
		{
			val = _br.ReadByte();
		}

		private void Write(ref byte val)
		{
			_bw.Write(val);
		}

		private void ReadText(string name, ref byte val)
		{
			if (Present(name))
			{
				val = byte.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}
		private void WriteText(string name, ref byte val)
		{
			_tw.WriteLine("{0} 0x{1:X2}", name, val);
		}

		private void Read(ref ushort val)
		{
			val = _br.ReadUInt16();
		}

		private void Write(ref ushort val)
		{
			_bw.Write(val);
		}

		private void ReadText(string name, ref ushort val)
		{
			if (Present(name))
			{
				val = ushort.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref ushort val)
		{
			_tw.WriteLine("{0} 0x{1:X4}", name, val);
		}

		private void Read(ref uint val)
		{
			{ val = _br.ReadUInt32(); }
		}

		private void Write(ref uint val)
		{
			_bw.Write(val);
		}

		private void ReadText(string name, ref uint val)
		{
			if (Present(name))
			{
				val = uint.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref uint val)
		{
			_tw.WriteLine("{0} 0x{1:X8}", name, val);
		}

		private void Read(ref sbyte val)
		{
			val = _br.ReadSByte();
		}

		private void Write(ref sbyte val)
		{
			_bw.Write(val);
		}

		private void ReadText(string name, ref sbyte val)
		{
			if (Present(name))
			{
				val = sbyte.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref sbyte val)
		{
			_tw.WriteLine("{0} 0x{1:X2}", name, val);
		}

		private void Read(ref short val)
		{
			val = _br.ReadInt16();
		}

		private void Write(ref short val)
		{
			_bw.Write(val);
		}

		private void ReadText(string name, ref short val)
		{
			if (Present(name))
			{
				val = short.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref short val)
		{
			_tw.WriteLine("{0} 0x{1:X4}", name, val);
		}

		private void Read(ref int val)
		{
			val = _br.ReadInt32();
		}

		private void Write(ref int val)
		{
			_bw.Write(val);
		}

		private void ReadText(string name, ref int val)
		{
			if (Present(name))
			{
				val = int.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref int val)
		{
			_tw.WriteLine("{0} 0x{1:X8}", name, val);
		}

		private void Read(ref long val)
		{
			val = _br.ReadInt64();
		}

		private void Write(ref long val)
		{
			_bw.Write(val);
		}

		private void Read(ref ulong val)
		{
			val = _br.ReadUInt64();
		}

		private void Write(ref ulong val)
		{
			_bw.Write(val);
		}

		private void ReadText(string name, ref long val)
		{
			if (Present(name))
			{
				val = long.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref long val)
		{
			_tw.WriteLine("{0} 0x{1:X16}", name, val);
		}

		private void ReadText(string name, ref ulong val)
		{
			if (Present(name))
			{
				val = ulong.Parse(Item(name).Replace("0x", ""), NumberStyles.HexNumber);
			}
		}

		private void WriteText(string name, ref ulong val)
		{
			_tw.WriteLine("{0} 0x{1:X16}", name, val);
		}

		private void Read(ref float val)
		{
			val = _br.ReadSingle();
		}

		private void Write(ref float val)
		{
			_bw.Write(val);
		}

		private void ReadText(string name, ref float val)
		{
			if (Present(name))
			{
				val = float.Parse(Item(name));
			}
		}

		private void WriteText(string name, ref float val)
		{
			_tw.WriteLine("{0} {1}", name, val);
		}

		private void Read(ref bool val)
		{
			val = _br.ReadBoolean();
		}

		private void Write(ref bool val)
		{
			_bw.Write(val);
		}

		private void ReadText(string name, ref bool val)
		{
			if (Present(name))
			{
				val = bool.Parse(Item(name));
			}
		}
		private void WriteText(string name, ref bool val)
		{
			_tw.WriteLine("{0} {1}", name, val);
		}

		private sealed class Section : Dictionary<string, Section>
		{
			public string Name = string.Empty;
			public readonly Dictionary<string, string> Items = new Dictionary<string, string>();
		}

		#endregion
	}
}
