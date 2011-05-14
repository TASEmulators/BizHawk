using System;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections.Generic;

//a decent little subcode reference
//http://www.jbum.com/cdg_revealed.html

namespace BizHawk.Disc
{
	public class SubcodeStream
	{
		Stream source;
		long offset;
		public SubcodeStream(Stream source, long offset)
		{
			this.source = source;
			this.offset = offset;
			cached_decoder = new SubcodePacketDecoder(cached_buffer, 0);
		}
		int channel = 0;
		public char Channel
		{
			get { return (char)((7 - channel) + 'p'); }
			set { channel = SubcodePacketDecoder.NormalizeChannel(value); }
		}

		long Position { get; set; }

		int cached_addr = -1;
		SubcodePacketDecoder cached_decoder = null;
		byte[] cached_buffer = new byte[24];
		public int ReadByte()
		{
			int subcode_addr = (int)Position;
			int subcode_byte = subcode_addr & 1;
			subcode_addr /= 2;
			subcode_addr *= 24;
			if (subcode_addr != cached_addr)
			{
				cached_decoder.Reset();
				source.Position = offset + subcode_addr;
				if (source.Read(cached_buffer, 0, 24) != 24)
					return -1;
				cached_addr = subcode_addr;
			}
			Position = Position + 1;
			ushort val = cached_decoder.ReadChannel(channel);
			val >>= (8 * subcode_byte);
			val &= 0xFF;
			return (int)val;
		}
	}

	class SubcodePacketDecoder
	{
		internal static int NormalizeChannel(char channel)
		{
			int channum;
			if (channel >= 'P' && channel <= 'W') channum = channel - 'P';
			else if (channel >= 'p' && channel <= 'w') channum = (channel - 'p');
			else throw new InvalidOperationException("invalid channel specified");
			channum = 7 - channum;
			return channum;
		}

		public void Reset()
		{
			cached = false;
		}
		byte[] buffer;
		int offset;
		public SubcodePacketDecoder(byte[] buffer, int offset)
		{
			this.buffer = buffer;
			this.offset = offset;
		}
		byte command { get { return buffer[offset + 0]; } set { buffer[offset + 0] = value; } }
		byte instruction { get { return buffer[offset + 1]; } set { buffer[offset + 1] = value; } }

		public int parityQ_offset { get { return offset + 2; } }
		public int data_offset { get { return offset + 4; } }
		public int parityP_offset { get { return offset + 20; } }

		public byte ReadData(int index)
		{
			return buffer[data_offset + index];
		}

		public ushort ReadChannel(char channel)
		{
			return ReadChannel(NormalizeChannel(channel));
		}

		bool cached;
		ushort[] decoded_channels = new ushort[8];
		public ushort ReadChannel(int channum)
		{
			if (!cached)
			{
				decoded_channels = new ushort[8];
				for (int i = 0; i < 8; i++)
					decoded_channels[i] = DecodeChannel(i);
			}
			return decoded_channels[channum];
		}

		ushort DecodeChannel(int channum)
		{
			int ret = 0;
			for (int i = 0; i < 16; i++)
			{
				ret |= ((ReadData(i) >> channum) & 1) << i;
			}
			return (ushort)ret;
		}
	}
}
