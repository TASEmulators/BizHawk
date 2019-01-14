/*
* Copyright (c) 2007 innoSysTec (R) GmbH, Germany. All rights reserved.
* Original author: Edmund Wagner
* Creation date: 21.06.2007
*
* the unrar licence applies to all junrar source and binary distributions 
* you are not allowed to use this source to re-create the RAR compression algorithm
*/

using System;
using SharpCompress.Compressors.Rar.UnpackV1.Decode;

namespace SharpCompress.Compressors.Rar.UnpackV1
{
    internal partial class Unpack
    {
        private readonly MultDecode[] MD = new MultDecode[4];

        private readonly byte[] UnpOldTable20 = new byte[PackDef.MC20 * 4];

        private int UnpAudioBlock, UnpChannels, UnpCurChannel, UnpChannelDelta;

        private readonly AudioVariables[] AudV = new AudioVariables[4];

        private readonly LitDecode LD = new LitDecode();

        private readonly DistDecode DD = new DistDecode();

        private readonly LowDistDecode LDD = new LowDistDecode();

        private readonly RepDecode RD = new RepDecode();

        private readonly BitDecode BD = new BitDecode();

        private static readonly int[] LDecode =
        {
            0, 1, 2, 3, 4, 5, 6, 7, 8, 10, 12, 14, 16, 20, 24, 28, 32, 40, 48,
            56, 64, 80, 96, 112, 128, 160, 192, 224
        };

        private static readonly byte[] LBits =
        {
            0, 0, 0, 0, 0, 0, 0, 0, 1, 1, 1, 1, 2, 2, 2, 2, 3, 3, 3, 3, 4, 4, 4,
            4, 5, 5, 5, 5
        };

        private static readonly int[] DDecode =
        {
            0, 1, 2, 3, 4, 6, 8, 12, 16, 24, 32, 48, 64, 96, 128, 192, 256, 384,
            512, 768, 1024, 1536, 2048, 3072, 4096, 6144, 8192, 12288, 16384,
            24576, 32768, 49152, 65536, 98304, 131072, 196608, 262144, 327680,
            393216, 458752, 524288, 589824, 655360, 720896, 786432, 851968,
            917504, 983040
        };

        private static readonly int[] DBits =
        {
            0, 0, 0, 0, 1, 1, 2, 2, 3, 3, 4, 4, 5, 5, 6, 6, 7, 7, 8, 8, 9, 9, 10,
            10, 11, 11, 12, 12, 13, 13, 14, 14, 15, 15, 16, 16, 16, 16, 16, 16, 16
            , 16, 16, 16, 16, 16, 16, 16
        };

        private static readonly int[] SDDecode = {0, 4, 8, 16, 32, 64, 128, 192};

        private static readonly int[] SDBits = {2, 2, 3, 4, 5, 6, 6, 6};

        private void unpack20(bool solid)
        {
            int Bits;

            if (suspended)
            {
                unpPtr = wrPtr;
            }
            else
            {
                UnpInitData(solid);
                if (!unpReadBuf())
                {
                    return;
                }
                if (!solid)
                {
                    if (!ReadTables20())
                    {
                        return;
                    }
                }
                --destUnpSize;
            }

            while (destUnpSize >= 0)
            {
                unpPtr &= PackDef.MAXWINMASK;

                if (inAddr > readTop - 30)
                {
                    if (!unpReadBuf())
                    {
                        break;
                    }
                }
                if (((wrPtr - unpPtr) & PackDef.MAXWINMASK) < 270 && wrPtr != unpPtr)
                {
                    oldUnpWriteBuf();
                    if (suspended)
                    {
                        return;
                    }
                }
                if (UnpAudioBlock != 0)
                {
                    int AudioNumber = this.decodeNumber(MD[UnpCurChannel]);

                    if (AudioNumber == 256)
                    {
                        if (!ReadTables20())
                        {
                            break;
                        }
                        continue;
                    }
                    window[unpPtr++] = DecodeAudio(AudioNumber);
                    if (++UnpCurChannel == UnpChannels)
                    {
                        UnpCurChannel = 0;
                    }
                    --destUnpSize;
                    continue;
                }

                int Number = this.decodeNumber(LD);
                if (Number < 256)
                {
                    window[unpPtr++] = (byte)Number;
                    --destUnpSize;
                    continue;
                }
                if (Number > 269)
                {
                    int Length = LDecode[Number -= 270] + 3;
                    if ((Bits = LBits[Number]) > 0)
                    {
                        Length += Utility.URShift(GetBits(), (16 - Bits));
                        AddBits(Bits);
                    }

                    int DistNumber = this.decodeNumber(DD);
                    int Distance = DDecode[DistNumber] + 1;
                    if ((Bits = DBits[DistNumber]) > 0)
                    {
                        Distance += Utility.URShift(GetBits(), (16 - Bits));
                        AddBits(Bits);
                    }

                    if (Distance >= 0x2000)
                    {
                        Length++;
                        if (Distance >= 0x40000L)
                        {
                            Length++;
                        }
                    }

                    CopyString20(Length, Distance);
                    continue;
                }
                if (Number == 269)
                {
                    if (!ReadTables20())
                    {
                        break;
                    }
                    continue;
                }
                if (Number == 256)
                {
                    CopyString20(lastLength, lastDist);
                    continue;
                }
                if (Number < 261)
                {
                    int Distance = oldDist[(oldDistPtr - (Number - 256)) & 3];
                    int LengthNumber = this.decodeNumber(RD);
                    int Length = LDecode[LengthNumber] + 2;
                    if ((Bits = LBits[LengthNumber]) > 0)
                    {
                        Length += Utility.URShift(GetBits(), (16 - Bits));
                        AddBits(Bits);
                    }
                    if (Distance >= 0x101)
                    {
                        Length++;
                        if (Distance >= 0x2000)
                        {
                            Length++;
                            if (Distance >= 0x40000)
                            {
                                Length++;
                            }
                        }
                    }
                    CopyString20(Length, Distance);
                    continue;
                }
                if (Number < 270)
                {
                    int Distance = SDDecode[Number -= 261] + 1;
                    if ((Bits = SDBits[Number]) > 0)
                    {
                        Distance += Utility.URShift(GetBits(), (16 - Bits));
                        AddBits(Bits);
                    }
                    CopyString20(2, Distance);
                }
            }
            ReadLastTables();
            oldUnpWriteBuf();
        }

        private void CopyString20(int Length, int Distance)
        {
            lastDist = oldDist[oldDistPtr++ & 3] = Distance;
            lastLength = Length;
            destUnpSize -= Length;

            int DestPtr = unpPtr - Distance;
            if (DestPtr < PackDef.MAXWINSIZE - 300 && unpPtr < PackDef.MAXWINSIZE - 300)
            {
                window[unpPtr++] = window[DestPtr++];
                window[unpPtr++] = window[DestPtr++];
                while (Length > 2)
                {
                    Length--;
                    window[unpPtr++] = window[DestPtr++];
                }
            }
            else
            {
                while ((Length--) != 0)
                {
                    window[unpPtr] = window[DestPtr++ & PackDef.MAXWINMASK];
                    unpPtr = (unpPtr + 1) & PackDef.MAXWINMASK;
                }
            }
        }

        private bool ReadTables20()
        {
            byte[] BitLength = new byte[PackDef.BC20];
            byte[] Table = new byte[PackDef.MC20 * 4];
            int TableSize, N, I;
            if (inAddr > readTop - 25)
            {
                if (!unpReadBuf())
                {
                    return (false);
                }
            }
            int BitField = GetBits();
            UnpAudioBlock = (BitField & 0x8000);

            if (0 == (BitField & 0x4000))
            {
                // memset(UnpOldTable20,0,sizeof(UnpOldTable20));
                Utility.Fill(UnpOldTable20, (byte)0);
            }
            AddBits(2);

            if (UnpAudioBlock != 0)
            {
                UnpChannels = ((Utility.URShift(BitField, 12)) & 3) + 1;
                if (UnpCurChannel >= UnpChannels)
                {
                    UnpCurChannel = 0;
                }
                AddBits(2);
                TableSize = PackDef.MC20 * UnpChannels;
            }
            else
            {
                TableSize = PackDef.NC20 + PackDef.DC20 + PackDef.RC20;
            }
            for (I = 0; I < PackDef.BC20; I++)
            {
                BitLength[I] = (byte)(Utility.URShift(GetBits(), 12));
                AddBits(4);
            }
            UnpackUtility.makeDecodeTables(BitLength, 0, BD, PackDef.BC20);
            I = 0;
            while (I < TableSize)
            {
                if (inAddr > readTop - 5)
                {
                    if (!unpReadBuf())
                    {
                        return (false);
                    }
                }
                int Number = this.decodeNumber(BD);
                if (Number < 16)
                {
                    Table[I] = (byte)((Number + UnpOldTable20[I]) & 0xf);
                    I++;
                }
                else if (Number == 16)
                {
                    N = (Utility.URShift(GetBits(), 14)) + 3;
                    AddBits(2);
                    while (N-- > 0 && I < TableSize)
                    {
                        Table[I] = Table[I - 1];
                        I++;
                    }
                }
                else
                {
                    if (Number == 17)
                    {
                        N = (Utility.URShift(GetBits(), 13)) + 3;
                        AddBits(3);
                    }
                    else
                    {
                        N = (Utility.URShift(GetBits(), 9)) + 11;
                        AddBits(7);
                    }
                    while (N-- > 0 && I < TableSize)
                    {
                        Table[I++] = 0;
                    }
                }
            }
            if (inAddr > readTop)
            {
                return (true);
            }
            if (UnpAudioBlock != 0)
            {
                for (I = 0; I < UnpChannels; I++)
                {
                    UnpackUtility.makeDecodeTables(Table, I * PackDef.MC20, MD[I], PackDef.MC20);
                }
            }
            else
            {
                UnpackUtility.makeDecodeTables(Table, 0, LD, PackDef.NC20);
                UnpackUtility.makeDecodeTables(Table, PackDef.NC20, DD, PackDef.DC20);
                UnpackUtility.makeDecodeTables(Table, PackDef.NC20 + PackDef.DC20, RD, PackDef.RC20);
            }

            // memcpy(UnpOldTable20,Table,sizeof(UnpOldTable20));
            for (int i = 0; i < UnpOldTable20.Length; i++)
            {
                UnpOldTable20[i] = Table[i];
            }
            return (true);
        }

        private void unpInitData20(bool Solid)
        {
            if (!Solid)
            {
                UnpChannelDelta = UnpCurChannel = 0;
                UnpChannels = 1;

                // memset(AudV,0,sizeof(AudV));
                AudV[0] = new AudioVariables();
                AudV[1] = new AudioVariables();
                AudV[2] = new AudioVariables();
                AudV[3] = new AudioVariables();

                // memset(UnpOldTable20,0,sizeof(UnpOldTable20));
                Utility.Fill(UnpOldTable20, (byte)0);
            }
        }

        private void ReadLastTables()
        {
            if (readTop >= inAddr + 5)
            {
                if (UnpAudioBlock != 0)
                {
                    if (this.decodeNumber(MD[UnpCurChannel]) == 256)
                    {
                        ReadTables20();
                    }
                }
                else
                {
                    if (this.decodeNumber(LD) == 269)
                    {
                        ReadTables20();
                    }
                }
            }
        }

        private byte DecodeAudio(int Delta)
        {
            AudioVariables v = AudV[UnpCurChannel];
            v.ByteCount = v.ByteCount + 1;
            v.D4 = v.D3;
            v.D3 = v.D2; // ->D3=V->D2;
            v.D2 = v.LastDelta - v.D1; // ->D2=V->LastDelta-V->D1;
            v.D1 = v.LastDelta; // V->D1=V->LastDelta;

            // int PCh=8*V->LastChar+V->K1*V->D1 +V->K2*V->D2 +V->K3*V->D3
            // +V->K4*V->D4+ V->K5*UnpChannelDelta;
            int PCh = 8 * v.LastChar + v.K1 * v.D1;
            PCh += v.K2 * v.D2 + v.K3 * v.D3;
            PCh += v.K4 * v.D4 + v.K5 * UnpChannelDelta;
            PCh = (Utility.URShift(PCh, 3)) & 0xFF;

            int Ch = PCh - Delta;

            int D = ((byte)Delta) << 3;

            v.Dif[0] += Math.Abs(D); // V->Dif[0]+=abs(D);
            v.Dif[1] += Math.Abs(D - v.D1); // V->Dif[1]+=abs(D-V->D1);
            v.Dif[2] += Math.Abs(D + v.D1); // V->Dif[2]+=abs(D+V->D1);
            v.Dif[3] += Math.Abs(D - v.D2); // V->Dif[3]+=abs(D-V->D2);
            v.Dif[4] += Math.Abs(D + v.D2); // V->Dif[4]+=abs(D+V->D2);
            v.Dif[5] += Math.Abs(D - v.D3); // V->Dif[5]+=abs(D-V->D3);
            v.Dif[6] += Math.Abs(D + v.D3); // V->Dif[6]+=abs(D+V->D3);
            v.Dif[7] += Math.Abs(D - v.D4); // V->Dif[7]+=abs(D-V->D4);
            v.Dif[8] += Math.Abs(D + v.D4); // V->Dif[8]+=abs(D+V->D4);
            v.Dif[9] += Math.Abs(D - UnpChannelDelta); // V->Dif[9]+=abs(D-UnpChannelDelta);
            v.Dif[10] += Math.Abs(D + UnpChannelDelta); // V->Dif[10]+=abs(D+UnpChannelDelta);

            v.LastDelta = (byte)(Ch - v.LastChar);
            UnpChannelDelta = v.LastDelta;
            v.LastChar = Ch; // V->LastChar=Ch;

            if ((v.ByteCount & 0x1F) == 0)
            {
                int MinDif = v.Dif[0], NumMinDif = 0;
                v.Dif[0] = 0; // ->Dif[0]=0;
                for (int I = 1; I < v.Dif.Length; I++)
                {
                    if (v.Dif[I] < MinDif)
                    {
                        MinDif = v.Dif[I];
                        NumMinDif = I;
                    }
                    v.Dif[I] = 0;
                }
                switch (NumMinDif)
                {
                    case 1:
                        if (v.K1 >= -16)
                        {
                            v.K1 = v.K1 - 1; // V->K1--;
                        }
                        break;

                    case 2:
                        if (v.K1 < 16)
                        {
                            v.K1 = v.K1 + 1; // V->K1++;
                        }
                        break;

                    case 3:
                        if (v.K2 >= -16)
                        {
                            v.K2 = v.K2 - 1; // V->K2--;
                        }
                        break;

                    case 4:
                        if (v.K2 < 16)
                        {
                            v.K2 = v.K2 + 1; // V->K2++;
                        }
                        break;

                    case 5:
                        if (v.K3 >= -16)
                        {
                            v.K3 = v.K3 - 1;
                        }
                        break;

                    case 6:
                        if (v.K3 < 16)
                        {
                            v.K3 = v.K3 + 1;
                        }
                        break;

                    case 7:
                        if (v.K4 >= -16)
                        {
                            v.K4 = v.K4 - 1;
                        }
                        break;

                    case 8:
                        if (v.K4 < 16)
                        {
                            v.K4 = v.K4 + 1;
                        }
                        break;

                    case 9:
                        if (v.K5 >= -16)
                        {
                            v.K5 = v.K5 - 1;
                        }
                        break;

                    case 10:
                        if (v.K5 < 16)
                        {
                            v.K5 = v.K5 + 1;
                        }
                        break;
                }
            }
            return ((byte)Ch);
        }
    }
}