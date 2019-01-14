using SharpCompress.Compressors.Rar.VM;

namespace SharpCompress.Compressors.Rar.UnpackV1
{
    internal static class UnpackUtility
    {
//!!! TODO rename methods
        internal static uint DecodeNumber(this BitInput input, Decode.Decode dec) {
            return (uint)input.decodeNumber(dec);
        }

        internal static int decodeNumber(this BitInput input, Decode.Decode dec)
        {
            int bits;
            long bitField = input.GetBits() & 0xfffe;

            //        if (bitField < dec.getDecodeLen()[8]) {
            //			if (bitField < dec.getDecodeLen()[4]) {
            //				if (bitField < dec.getDecodeLen()[2]) {
            //					if (bitField < dec.getDecodeLen()[1]) {
            //						bits = 1;
            //					} else {
            //						bits = 2;
            //					}
            //				} else {
            //					if (bitField < dec.getDecodeLen()[3]) {
            //						bits = 3;
            //					} else {
            //						bits = 4;
            //					}
            //				}
            //			} else {
            //				if (bitField < dec.getDecodeLen()[6]) {
            //					if (bitField < dec.getDecodeLen()[5])
            //						bits = 5;
            //					else
            //						bits = 6;
            //				} else {
            //					if (bitField < dec.getDecodeLen()[7]) {
            //						bits = 7;
            //					} else {
            //						bits = 8;
            //					}
            //				}
            //			}
            //		} else {
            //			if (bitField < dec.getDecodeLen()[12]) {
            //				if (bitField < dec.getDecodeLen()[10])
            //					if (bitField < dec.getDecodeLen()[9])
            //						bits = 9;
            //					else
            //						bits = 10;
            //				else if (bitField < dec.getDecodeLen()[11])
            //					bits = 11;
            //				else
            //					bits = 12;
            //			} else {
            //				if (bitField < dec.getDecodeLen()[14]) {
            //					if (bitField < dec.getDecodeLen()[13]) {
            //						bits = 13;
            //					} else {
            //						bits = 14;
            //					}
            //				} else {
            //					bits = 15;
            //				}
            //			}
            //		}
            //		addbits(bits);
            //		int N = dec.getDecodePos()[bits]
            //				+ (((int) bitField - dec.getDecodeLen()[bits - 1]) >>> (16 - bits));
            //		if (N >= dec.getMaxNum()) {
            //			N = 0;
            //		}
            //		return (dec.getDecodeNum()[N]);
            int[] decodeLen = dec.DecodeLen;
            if (bitField < decodeLen[8])
            {
                if (bitField < decodeLen[4])
                {
                    if (bitField < decodeLen[2])
                    {
                        if (bitField < decodeLen[1])
                        {
                            bits = 1;
                        }
                        else
                        {
                            bits = 2;
                        }
                    }
                    else
                    {
                        if (bitField < decodeLen[3])
                        {
                            bits = 3;
                        }
                        else
                        {
                            bits = 4;
                        }
                    }
                }
                else
                {
                    if (bitField < decodeLen[6])
                    {
                        if (bitField < decodeLen[5])
                        {
                            bits = 5;
                        }
                        else
                        {
                            bits = 6;
                        }
                    }
                    else
                    {
                        if (bitField < decodeLen[7])
                        {
                            bits = 7;
                        }
                        else
                        {
                            bits = 8;
                        }
                    }
                }
            }
            else
            {
                if (bitField < decodeLen[12])
                {
                    if (bitField < decodeLen[10])
                    {
                        if (bitField < decodeLen[9])
                        {
                            bits = 9;
                        }
                        else
                        {
                            bits = 10;
                        }
                    }
                    else if (bitField < decodeLen[11])
                    {
                        bits = 11;
                    }
                    else
                    {
                        bits = 12;
                    }
                }
                else
                {
                    if (bitField < decodeLen[14])
                    {
                        if (bitField < decodeLen[13])
                        {
                            bits = 13;
                        }
                        else
                        {
                            bits = 14;
                        }
                    }
                    else
                    {
                        bits = 15;
                    }
                }
            }
            input.AddBits(bits);
            int N = dec.DecodePos[bits] + (Utility.URShift(((int)bitField - decodeLen[bits - 1]), (16 - bits)));
            if (N >= dec.MaxNum)
            {
                N = 0;
            }
            return (dec.DecodeNum[N]);
        }

        internal static void makeDecodeTables(byte[] lenTab, int offset, Decode.Decode dec, int size)
        {
            int[] lenCount = new int[16];
            int[] tmpPos = new int[16];
            int i;
            long M, N;

            Utility.Fill(lenCount, 0); // memset(LenCount,0,sizeof(LenCount));

            Utility.Fill(dec.DecodeNum, 0); // memset(Dec->DecodeNum,0,Size*sizeof(*Dec->DecodeNum));

            for (i = 0; i < size; i++)
            {
                lenCount[lenTab[offset + i] & 0xF]++;
            }
            lenCount[0] = 0;
            for (tmpPos[0] = 0, dec.DecodePos[0] = 0, dec.DecodeLen[0] = 0, N = 0, i = 1; i < 16; i++)
            {
                N = 2 * (N + lenCount[i]);
                M = N << (15 - i);
                if (M > 0xFFFF)
                {
                    M = 0xFFFF;
                }
                dec.DecodeLen[i] = (int)M;
                tmpPos[i] = dec.DecodePos[i] = dec.DecodePos[i - 1] + lenCount[i - 1];
            }

            for (i = 0; i < size; i++)
            {
                if (lenTab[offset + i] != 0)
                {
                    dec.DecodeNum[tmpPos[lenTab[offset + i] & 0xF]++] = i;
                }
            }
            dec.MaxNum = size;
        }
    }
}