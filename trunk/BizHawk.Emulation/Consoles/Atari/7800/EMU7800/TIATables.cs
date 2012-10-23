/*
 * TIATables.cs
 *
 * Mask tables for the Television Interface Adaptor class.  All derived from
 * Bradford Mott's Stella code.
 *
 * Copyright © 2003, 2004 Mike Murphy
 *
 */
namespace EMU7800.Core
{
    public static class TIATables
    {
        public static readonly TIACxPairFlags[] CollisionMask = BuildCollisionMaskTable();
        public static readonly uint[][] PFMask = BuildPFMaskTable();
        public static readonly bool[][] BLMask = BuildBLMaskTable();
        public static readonly bool[][][] MxMask = BuildMxMaskTable();
        public static readonly byte[][][] PxMask = BuildPxMaskTable();
        public static readonly byte[] GRPReflect = BuildGRPReflectTable();

        public static readonly int[] NTSCPalette =
        {
            0x000000, 0x000000, 0x4a4a4a, 0x4a4a4a,
            0x6f6f6f, 0x6f6f6f, 0x8e8e8e, 0x8e8e8e,
            0xaaaaaa, 0xaaaaaa, 0xc0c0c0, 0xc0c0c0,
            0xd6d6d6, 0xd6d6d6, 0xececec, 0xececec,

            0x484800, 0x484800, 0x69690f, 0x69690f,
            0x86861d, 0x86861d, 0xa2a22a, 0xa2a22a,
            0xbbbb35, 0xbbbb35, 0xd2d240, 0xd2d240,
            0xe8e84a, 0xe8e84a, 0xfcfc54, 0xfcfc54,

            0x7c2c00, 0x7c2c00, 0x904811, 0x904811,
            0xa26221, 0xa26221, 0xb47a30, 0xb47a30,
            0xc3903d, 0xc3903d, 0xd2a44a, 0xd2a44a,
            0xdfb755, 0xdfb755, 0xecc860, 0xecc860,

            0x901c00, 0x901c00, 0xa33915, 0xa33915,
            0xb55328, 0xb55328, 0xc66c3a, 0xc66c3a,
            0xd5824a, 0xd5824a, 0xe39759, 0xe39759,
            0xf0aa67, 0xf0aa67, 0xfcbc74, 0xfcbc74,

            0x940000, 0x940000, 0xa71a1a, 0xa71a1a,
            0xb83232, 0xb83232, 0xc84848, 0xc84848,
            0xd65c5c, 0xd65c5c, 0xe46f6f, 0xe46f6f,
            0xf08080, 0xf08080, 0xfc9090, 0xfc9090,

            0x840064, 0x840064, 0x97197a, 0x97197a,
            0xa8308f, 0xa8308f, 0xb846a2, 0xb846a2,
            0xc659b3, 0xc659b3, 0xd46cc3, 0xd46cc3,
            0xe07cd2, 0xe07cd2, 0xec8ce0, 0xec8ce0,

            0x500084, 0x500084, 0x68199a, 0x68199a,
            0x7d30ad, 0x7d30ad, 0x9246c0, 0x9246c0,
            0xa459d0, 0xa459d0, 0xb56ce0, 0xb56ce0,
            0xc57cee, 0xc57cee, 0xd48cfc, 0xd48cfc,

            0x140090, 0x140090, 0x331aa3, 0x331aa3,
            0x4e32b5, 0x4e32b5, 0x6848c6, 0x6848c6,
            0x7f5cd5, 0x7f5cd5, 0x956fe3, 0x956fe3,
            0xa980f0, 0xa980f0, 0xbc90fc, 0xbc90fc,

            0x000094, 0x000094, 0x181aa7, 0x181aa7,
            0x2d32b8, 0x2d32b8, 0x4248c8, 0x4248c8,
            0x545cd6, 0x545cd6, 0x656fe4, 0x656fe4,
            0x7580f0, 0x7580f0, 0x8490fc, 0x8490fc,

            0x001c88, 0x001c88, 0x183b9d, 0x183b9d,
            0x2d57b0, 0x2d57b0, 0x4272c2, 0x4272c2,
            0x548ad2, 0x548ad2, 0x65a0e1, 0x65a0e1,
            0x75b5ef, 0x75b5ef, 0x84c8fc, 0x84c8fc,

            0x003064, 0x003064, 0x185080, 0x185080,
            0x2d6d98, 0x2d6d98, 0x4288b0, 0x4288b0,
            0x54a0c5, 0x54a0c5, 0x65b7d9, 0x65b7d9,
            0x75cceb, 0x75cceb, 0x84e0fc, 0x84e0fc,

            0x004030, 0x004030, 0x18624e, 0x18624e,
            0x2d8169, 0x2d8169, 0x429e82, 0x429e82,
            0x54b899, 0x54b899, 0x65d1ae, 0x65d1ae,
            0x75e7c2, 0x75e7c2, 0x84fcd4, 0x84fcd4,

            0x004400, 0x004400, 0x1a661a, 0x1a661a,
            0x328432, 0x328432, 0x48a048, 0x48a048,
            0x5cba5c, 0x5cba5c, 0x6fd26f, 0x6fd26f,
            0x80e880, 0x80e880, 0x90fc90, 0x90fc90,

            0x143c00, 0x143c00, 0x355f18, 0x355f18,
            0x527e2d, 0x527e2d, 0x6e9c42, 0x6e9c42,
            0x87b754, 0x87b754, 0x9ed065, 0x9ed065,
            0xb4e775, 0xb4e775, 0xc8fc84, 0xc8fc84,

            0x303800, 0x303800, 0x505916, 0x505916,
            0x6d762b, 0x6d762b, 0x88923e, 0x88923e,
            0xa0ab4f, 0xa0ab4f, 0xb7c25f, 0xb7c25f,
            0xccd86e, 0xccd86e, 0xe0ec7c, 0xe0ec7c,

            0x482c00, 0x482c00, 0x694d14, 0x694d14,
            0x866a26, 0x866a26, 0xa28638, 0xa28638,
            0xbb9f47, 0xbb9f47, 0xd2b656, 0xd2b656,
            0xe8cc63, 0xe8cc63, 0xfce070, 0xfce070
        };

        public static readonly int[] PALPalette =
        {
            0x000000, 0x000000, 0x2b2b2b, 0x2b2b2b,
            0x525252, 0x525252, 0x767676, 0x767676,
            0x979797, 0x979797, 0xb6b6b6, 0xb6b6b6,
            0xd2d2d2, 0xd2d2d2, 0xececec, 0xececec,

            0x000000, 0x000000, 0x2b2b2b, 0x2b2b2b,
            0x525252, 0x525252, 0x767676, 0x767676,
            0x979797, 0x979797, 0xb6b6b6, 0xb6b6b6,
            0xd2d2d2, 0xd2d2d2, 0xececec, 0xececec,

            0x805800, 0x000000, 0x96711a, 0x2b2b2b,
            0xab8732, 0x525252, 0xbe9c48, 0x767676,
            0xcfaf5c, 0x979797, 0xdfc06f, 0xb6b6b6,
            0xeed180, 0xd2d2d2, 0xfce090, 0xececec,

            0x445c00, 0x000000, 0x5e791a, 0x2b2b2b,
            0x769332, 0x525252, 0x8cac48, 0x767676,
            0xa0c25c, 0x979797, 0xb3d76f, 0xb6b6b6,
            0xc4ea80, 0xd2d2d2, 0xd4fc90, 0xececec,

            0x703400, 0x000000, 0x89511a, 0x2b2b2b,
            0xa06b32, 0x525252, 0xb68448, 0x767676,
            0xc99a5c, 0x979797, 0xdcaf6f, 0xb6b6b6,
            0xecc280, 0xd2d2d2, 0xfcd490, 0xececec,

            0x006414, 0x000000, 0x1a8035, 0x2b2b2b,
            0x329852, 0x525252, 0x48b06e, 0x767676,
            0x5cc587, 0x979797, 0x6fd99e, 0xb6b6b6,
            0x80ebb4, 0xd2d2d2, 0x90fcc8, 0xececec,

            0x700014, 0x000000, 0x891a35, 0x2b2b2b,
            0xa03252, 0x525252, 0xb6486e, 0x767676,
            0xc95c87, 0x979797, 0xdc6f9e, 0xb6b6b6,
            0xec80b4, 0xd2d2d2, 0xfc90c8, 0xececec,

            0x005c5c, 0x000000, 0x1a7676, 0x2b2b2b,
            0x328e8e, 0x525252, 0x48a4a4, 0x767676,
            0x5cb8b8, 0x979797, 0x6fcbcb, 0xb6b6b6,
            0x80dcdc, 0xd2d2d2, 0x90ecec, 0xececec,

            0x70005c, 0x000000, 0x841a74, 0x2b2b2b,
            0x963289, 0x525252, 0xa8489e, 0x767676,
            0xb75cb0, 0x979797, 0xc66fc1, 0xb6b6b6,
            0xd380d1, 0xd2d2d2, 0xe090e0, 0xececec,

            0x003c70, 0x000000, 0x195a89, 0x2b2b2b,
            0x2f75a0, 0x525252, 0x448eb6, 0x767676,
            0x57a5c9, 0x979797, 0x68badc, 0xb6b6b6,
            0x79ceec, 0xd2d2d2, 0x88e0fc, 0xececec,

            0x580070, 0x000000, 0x6e1a89, 0x2b2b2b,
            0x8332a0, 0x525252, 0x9648b6, 0x767676,
            0xa75cc9, 0x979797, 0xb76fdc, 0xb6b6b6,
            0xc680ec, 0xd2d2d2, 0xd490fc, 0xececec,

            0x002070, 0x000000, 0x193f89, 0x2b2b2b,
            0x2f5aa0, 0x525252, 0x4474b6, 0x767676,
            0x578bc9, 0x979797, 0x68a1dc, 0xb6b6b6,
            0x79b5ec, 0xd2d2d2, 0x88c8fc, 0xececec,

            0x340080, 0x000000, 0x4a1a96, 0x2b2b2b,
            0x5f32ab, 0x525252, 0x7248be, 0x767676,
            0x835ccf, 0x979797, 0x936fdf, 0xb6b6b6,
            0xa280ee, 0xd2d2d2, 0xb090fc, 0xececec,

            0x000088, 0x000000, 0x1a1a9d, 0x2b2b2b,
            0x3232b0, 0x525252, 0x4848c2, 0x767676,
            0x5c5cd2, 0x979797, 0x6f6fe1, 0xb6b6b6,
            0x8080ef, 0xd2d2d2, 0x9090fc, 0xececec,

            0x000000, 0x000000, 0x2b2b2b, 0x2b2b2b,
            0x525252, 0x525252, 0x767676, 0x767676,
            0x979797, 0x979797, 0xb6b6b6, 0xb6b6b6,
            0xd2d2d2, 0xd2d2d2, 0xececec, 0xececec,

            0x000000, 0x000000, 0x2b2b2b, 0x2b2b2b,
            0x525252, 0x525252, 0x767676, 0x767676,
            0x979797, 0x979797, 0xb6b6b6, 0xb6b6b6,
            0xd2d2d2, 0xd2d2d2, 0xececec, 0xececec
        };

        static uint[][] BuildPFMaskTable()
        {
            var tabl = new uint[2][];
            tabl[0] = new uint[160];
            tabl[1] = new uint[160];

            for (var i = 0; i < 20; i++)
            {
                uint mask = 0;
                if (i < 4)
                {
                    mask = (uint)(1 << i);
                }
                else if (i < 12)
                {
                    mask = (uint)(1 << (11 + 4 - i));
                }
                else if (i < 20)
                {
                    mask = (uint)(1 << i);
                }
                for (var j = 0; j < 4; j++)
                {
                    // for non-reflected mode
                    tabl[0][4 * i + j] = mask;
                    tabl[0][80 + 4 * i + j] = mask;

                    // for reflected mode
                    tabl[1][4 * i + j] = mask;
                    tabl[1][159 - 4 * i - j] = mask;
                }
            }
            return tabl;
        }

        static bool[][] BuildBLMaskTable()
        {
            var tabl = new bool[4][];
            for (var size = 0; size < 4; size++)
            {
                tabl[size] = new bool[160];
                for (var i = 0; i < 160; i++)
                {
                    tabl[size][i] = false;
                }
                for (var i = 0; i < (1 << size); i++)
                {
                    tabl[size][i] = true;
                }
            }
            return tabl;
        }

        static bool[][][] BuildMxMaskTable()
        {
            var tabl = new bool[4][][];
            for (var i = 0; i < 4; i++)
            {
                tabl[i] = new bool[8][];
                for (var j = 0; j < 8; j++)
                {
                    tabl[i][j] = new bool[160];
                    for (var k = 0; k < 160; k++)
                    {
                        tabl[i][j][k] = false;
                    }
                }
            }

            for (var size = 0; size < 4; size++)
            {
                for (var i = 0; i < (1 << size); i++)
                {
                    tabl[size][0][i] = true;

                    tabl[size][1][i] = true;
                    tabl[size][1][i + 16] = true;

                    tabl[size][2][i] = true;
                    tabl[size][2][i + 32] = true;

                    tabl[size][3][i] = true;
                    tabl[size][3][i + 16] = true;
                    tabl[size][3][i + 32] = true;

                    tabl[size][4][i] = true;
                    tabl[size][4][i + 64] = true;

                    tabl[size][5][i] = true;

                    tabl[size][6][i] = true;
                    tabl[size][6][i + 32] = true;
                    tabl[size][6][i + 64] = true;

                    tabl[size][7][i] = true;
                }
            }
            return tabl;
        }

        static byte[][][] BuildPxMaskTable()
        {
            // [suppress mode, nusiz, pixel]
            // suppress=1: suppress on
            // suppress=0: suppress off
            var tabl = new byte[2][][]; //2 8 160
            tabl[0] = new byte[8][];
            tabl[1] = new byte[8][];
            for (var nusiz = 0; nusiz < 8; nusiz++)
            {
                tabl[0][nusiz] = new byte[160];
                tabl[1][nusiz] = new byte[160];
                for (var hpos = 0; hpos < 160; hpos++)
                {
                    // nusiz:
                    // 0: one copy
                    // 1: two copies-close
                    // 2: two copies-med
                    // 3: three copies-close
                    // 4: two copies-wide
                    // 5: double size player
                    // 6: 3 copies medium
                    // 7: quad sized player
                    tabl[0][nusiz][hpos] = tabl[1][nusiz][hpos] = 0;
                    if (nusiz >= 0 && nusiz <= 4 || nusiz == 6)
                    {
                        if (hpos >= 0 && hpos < 8)
                        {
                            tabl[0][nusiz][hpos] = (byte)(1 << (7 - hpos));
                        }
                    }
                    if (nusiz == 1 || nusiz == 3)
                    {
                        if (hpos >= 16 && hpos < 24)
                        {
                            tabl[0][nusiz][hpos] = (byte)(1 << (23 - hpos));
                            tabl[1][nusiz][hpos] = (byte)(1 << (23 - hpos));
                        }
                    }
                    if (nusiz == 2 || nusiz == 3 || nusiz == 6)
                    {
                        if (hpos >= 32 && hpos < 40)
                        {
                            tabl[0][nusiz][hpos] = (byte)(1 << (39 - hpos));
                            tabl[1][nusiz][hpos] = (byte)(1 << (39 - hpos));
                        }
                    }
                    if (nusiz == 4 || nusiz == 6)
                    {
                        if (hpos >= 64 && hpos < 72)
                        {
                            tabl[0][nusiz][hpos] = (byte)(1 << (71 - hpos));
                            tabl[1][nusiz][hpos] = (byte)(1 << (71 - hpos));
                        }
                    }
                    if (nusiz == 5)
                    {
                        if (hpos >= 0 && hpos < 16)
                        {
                            tabl[0][nusiz][hpos] = (byte)(1 << ((15 - hpos) >> 1));
                        }
                    }
                    if (nusiz == 7)
                    {
                        if (hpos >= 0 && hpos < 32)
                        {
                            tabl[0][nusiz][hpos] = (byte)(1 << ((31 - hpos) >> 2));
                        }
                    }
                }

                var shift = nusiz == 5 || nusiz == 7 ? 2 : 1;
                while (shift-- > 0)
                {
                    for (var i = 159; i > 0; i--)
                    {
                        tabl[0][nusiz][i] = tabl[0][nusiz][i - 1];
                        tabl[1][nusiz][i] = tabl[1][nusiz][i - 1];
                    }
                    tabl[0][nusiz][0] = tabl[1][nusiz][0] = 0;
                }
            }
            return tabl;
        }

        static byte[] BuildGRPReflectTable()
        {
            var tabl = new byte[256];

            for (var i = 0; i < 256; i++)
            {
                var s = (byte)i;
                var r = (byte)0;
                for (var j = 0; j < 8; j++)
                {
                    r <<= 1;
                    r |= (byte)(s & 1);
                    s >>= 1;
                }
                tabl[i] = r;
            }
            return tabl;
        }

        static bool tstCx(int i, TIACxFlags cxf1, TIACxFlags cxf2)
        {
            var f1 = (int)cxf1;
            var f2 = (int)cxf2;
            return ((i & f1) != 0) && ((i & f2) != 0);
        }

        static TIACxPairFlags[] BuildCollisionMaskTable()
        {
            var tabl = new TIACxPairFlags[64];

            for (var i = 0; i < 64; i++)
            {
                tabl[i] = 0;
                if (tstCx(i, TIACxFlags.M0, TIACxFlags.P1)) { tabl[i] |= TIACxPairFlags.M0P1; }
                if (tstCx(i, TIACxFlags.M0, TIACxFlags.P0)) { tabl[i] |= TIACxPairFlags.M0P0; }
                if (tstCx(i, TIACxFlags.M1, TIACxFlags.P0)) { tabl[i] |= TIACxPairFlags.M1P0; }
                if (tstCx(i, TIACxFlags.M1, TIACxFlags.P1)) { tabl[i] |= TIACxPairFlags.M1P1; }
                if (tstCx(i, TIACxFlags.P0, TIACxFlags.PF)) { tabl[i] |= TIACxPairFlags.P0PF; }
                if (tstCx(i, TIACxFlags.P0, TIACxFlags.BL)) { tabl[i] |= TIACxPairFlags.P0BL; }
                if (tstCx(i, TIACxFlags.P1, TIACxFlags.PF)) { tabl[i] |= TIACxPairFlags.P1PF; }
                if (tstCx(i, TIACxFlags.P1, TIACxFlags.BL)) { tabl[i] |= TIACxPairFlags.P1BL; }
                if (tstCx(i, TIACxFlags.M0, TIACxFlags.PF)) { tabl[i] |= TIACxPairFlags.M0PF; }
                if (tstCx(i, TIACxFlags.M0, TIACxFlags.BL)) { tabl[i] |= TIACxPairFlags.M0BL; }
                if (tstCx(i, TIACxFlags.M1, TIACxFlags.PF)) { tabl[i] |= TIACxPairFlags.M1PF; }
                if (tstCx(i, TIACxFlags.M1, TIACxFlags.BL)) { tabl[i] |= TIACxPairFlags.M1BL; }
                if (tstCx(i, TIACxFlags.BL, TIACxFlags.PF)) { tabl[i] |= TIACxPairFlags.BLPF; }
                if (tstCx(i, TIACxFlags.P0, TIACxFlags.P1)) { tabl[i] |= TIACxPairFlags.P0P1; }
                if (tstCx(i, TIACxFlags.M0, TIACxFlags.M1)) { tabl[i] |= TIACxPairFlags.M0M1; }
            }
            return tabl;
        }
    }
}
