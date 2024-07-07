namespace BizHawk.Emulation.DiscSystem
{
	internal static class SynthUtils
	{
		/// <summary>
		/// Calculates the checksum of the provided Q subchannel buffer and emplaces it
		/// </summary>
		/// <param name="buf12">12 byte Q subchannel buffer: input and output buffer for operation</param>
		/// <param name="offset">location within buffer of Q subchannel</param>
		public static ushort SubQ_SynthChecksum(byte[] buf12, int offset)
		{
			var crc16 = CRC16_CCITT.Calculate(buf12, offset, 10);

			//CRC is stored inverted and big endian
			buf12[offset + 10] = (byte)(~(crc16 >> 8));
			buf12[offset + 11] = (byte)(~(crc16));

			return crc16;
		}

		/// <summary>
		/// Calculates the checksum of the provided Q subchannel buffer
		/// </summary>
		public static ushort SubQ_CalcChecksum(byte[] buf12, int offset)
		{
			return CRC16_CCITT.Calculate(buf12, offset, 10);
		}

		/// <summary>
		/// Serializes the provided SubchannelQ structure into a buffer
		/// Returns the crc, calculated or otherwise.
		/// </summary>
		public static ushort SubQ_Serialize(byte[] buf12, int offset, ref SubchannelQ sq)
		{
			buf12[offset + 0] = sq.q_status;
			buf12[offset + 1] = sq.q_tno.BCDValue;
			buf12[offset + 2] = sq.q_index.BCDValue;
			buf12[offset + 3] = sq.min.BCDValue;
			buf12[offset + 4] = sq.sec.BCDValue;
			buf12[offset + 5] = sq.frame.BCDValue;
			buf12[offset + 6] = sq.zero;
			buf12[offset + 7] = sq.ap_min.BCDValue;
			buf12[offset + 8] = sq.ap_sec.BCDValue;
			buf12[offset + 9] = sq.ap_frame.BCDValue;

			return SubQ_SynthChecksum(buf12, offset);
		}

		/// <summary>
		/// Synthesizes the typical subP data into the provided buffer depending on the indicated pause flag
		/// </summary>
		public static void SubP(byte[] buffer12, int offset, bool pause)
		{
			var val = (byte)(pause ? 0xFF : 0x00);
			for (var i = 0; i < 12; i++)
				buffer12[offset + i] = val;
		}

		/// <summary>
		/// Synthesizes a data sector header
		/// </summary>
		public static void SectorHeader(byte[] buffer16, int offset, int LBA, byte mode)
		{
			buffer16[offset + 0] = 0x00;
			for (var i = 1; i < 11; i++) buffer16[offset + i] = 0xFF;
			buffer16[offset + 11] = 0x00;
			var ts = new Timestamp(LBA + 150);
			buffer16[offset + 12] = BCD2.IntToBCD(ts.MIN);
			buffer16[offset + 13] = BCD2.IntToBCD(ts.SEC);
			buffer16[offset + 14] = BCD2.IntToBCD(ts.FRAC);
			buffer16[offset + 15] = mode;
		}

		/// <summary>
		/// Synthesizes a Mode2 sector subheader
		/// </summary>
		public static void SectorSubHeader(byte[] buffer8, int offset, byte form)
		{
			// see mirage_sector_generate_subheader
			for (var i = 0; i < 8; i++) buffer8[offset + i] = 0;
			if (form == 2)
			{
				// these are just 0 in form 1
				buffer8[offset + 2] = 0x20;
				buffer8[offset + 5] = 0x20;
			}
		}

		/// <summary>
		/// Synthesizes the EDC checksum for a Mode 1 data sector (and puts it in place)
		/// </summary>
		public static void EDC_Mode1(byte[] buf2352, int offset)
		{
			var edc = ECM.EDC_Calc(buf2352, offset, 2064);
			ECM.PokeUint(buf2352, offset + 2064, edc);
		}

		/// <summary>
		/// Synthesizes the EDC checksum for a Mode 2 Form 1 data sector (and puts it in place)
		/// </summary>
		public static void EDC_Mode2_Form1(byte[] buf2352, int offset)
		{
			var edc = ECM.EDC_Calc(buf2352, offset + 16, 2048 + 8);
			ECM.PokeUint(buf2352, offset + 2072, edc);
		}

		/// <summary>
		/// Synthesizes the EDC checksum for a Mode 2 Form 2 data sector (and puts it in place)
		/// </summary>
		public static void EDC_Mode2_Form2(byte[] buf2352, int offset)
		{
			var edc = ECM.EDC_Calc(buf2352, offset + 16, 2324 + 8);
			ECM.PokeUint(buf2352, offset + 2348, edc);
		}


		/// <summary>
		/// Synthesizes the complete ECM data (EDC + ECC) for a Mode 1 data sector (and puts it in place)
		/// Make sure everything else in the sector header and userdata is done before calling this
		/// </summary>
		public static void ECM_Mode1(byte[] buf2352, int offset)
		{
			//EDC
			EDC_Mode1(buf2352, offset);

			//reserved, zero
			for (var i = 0; i < 8; i++) buf2352[offset + 2068 + i] = 0;

			//ECC
			ECM.ECC_Populate(buf2352, offset, buf2352, offset, false);
		}

		/// <summary>
		/// Synthesizes the complete ECM data (Subheader + EDC + ECC) for a Mode 2 Form 1 data sector (and puts it in place)
		/// Make sure everything else in the sector header and userdata is done before calling this
		/// </summary>
		public static void ECM_Mode2_Form1(byte[] buf2352, int offset)
		{
			//Subheader
			SectorSubHeader(buf2352, offset + 16, 1);

			//EDC
			EDC_Mode2_Form1(buf2352, offset);

			//ECC
			ECM.ECC_Populate(buf2352, offset, buf2352, offset, false);
		}

		/// <summary>
		/// Synthesizes the complete ECM data (Subheader + EDC) for a Mode 2 Form 2 data sector (and puts it in place)
		/// Make sure everything else in the userdata is done before calling this
		/// </summary>
		public static void ECM_Mode2_Form2(byte[] buf2352, int offset)
		{
			//Subheader
			SectorSubHeader(buf2352, offset + 16, 2);

			//EDC
			EDC_Mode2_Form2(buf2352, offset);

			//note that Mode 2 Form 2 does not have ECC
		}

		/// <summary>
		/// Converts the useful (but unrealistic) deinterleaved subchannel data into the useless (but realistic) interleaved format.
		/// in_buf and out_buf should not overlap
		/// </summary>
		public static void InterleaveSubcode(byte[] in_buf, int in_buf_index, byte[] out_buf, int out_buf_index)
		{
			for (var d = 0; d < 12; d++)
			{
				for (var bitpoodle = 0; bitpoodle < 8; bitpoodle++)
				{
					var rawb = 0;

					for (var ch = 0; ch < 8; ch++)
					{
						rawb |= ((in_buf[ch * 12 + d + in_buf_index] >> (7 - bitpoodle)) & 1) << (7 - ch);
					}
					out_buf[(d << 3) + bitpoodle + out_buf_index] = (byte)rawb;
				}
			}
		}

		/// <summary>
		/// Converts the useless (but realistic) interleaved subchannel data into a useful (but unrealistic) deinterleaved format.
		/// in_buf and out_buf should not overlap
		/// </summary>
		public static void DeinterleaveSubcode(byte[] in_buf, int in_buf_index, byte[] out_buf, int out_buf_index)
		{
			for (var i = 0; i < 96; i++)
				out_buf[i] = 0;

			for (var ch = 0; ch < 8; ch++)
			{
				for (var i = 0; i < 96; i++)
				{
					out_buf[(ch * 12) + (i >> 3) + out_buf_index] |= (byte)(((in_buf[i + in_buf_index] >> (7 - ch)) & 0x1) << (7 - (i & 0x7)));
				}
			}
		}

		/// <summary>
		/// Converts the useful (but unrealistic) deinterleaved data into the useless (but realistic) interleaved subchannel format.
		/// </summary>
		public static unsafe void InterleaveSubcodeInplace(byte[] buf, int buf_index)
		{
			var out_buf = stackalloc byte[96];

			for (var i = 0; i < 96; i++)
				out_buf[i] = 0;

			for (var d = 0; d < 12; d++)
			{
				for (var bitpoodle = 0; bitpoodle < 8; bitpoodle++)
				{
					var rawb = 0;

					for (var ch = 0; ch < 8; ch++)
					{
						rawb |= ((buf[ch * 12 + d + buf_index] >> (7 - bitpoodle)) & 1) << (7 - ch);
					}
					out_buf[(d << 3) + bitpoodle] = (byte)rawb;
				}
			}

			for (var i = 0; i < 96; i++)
				buf[i + buf_index] = out_buf[i];
		}

		/// <summary>
		/// Converts the useless (but realistic) interleaved subchannel data into a useful (but unrealistic) deinterleaved format.
		/// </summary>
		public static unsafe void DeinterleaveSubcodeInplace(byte[] buf, int buf_index)
		{
			var out_buf = stackalloc byte[96];

			for (var i = 0; i < 96; i++)
				out_buf[i] = 0;

			for (var ch = 0; ch < 8; ch++)
			{
				for (var i = 0; i < 96; i++)
				{
					out_buf[(ch * 12) + (i >> 3)] |= (byte)(((buf[i + buf_index] >> (7 - ch)) & 0x1) << (7 - (i & 0x7)));
				}
			}

			for (var i = 0; i < 96; i++)
				buf[i + buf_index] = out_buf[i];
		}
	}
}