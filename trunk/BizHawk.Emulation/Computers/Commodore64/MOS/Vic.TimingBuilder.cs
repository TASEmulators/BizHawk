using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
    sealed public partial class Vic
    {
        static public int[] TimingBuilder_BA(int[] fetch)
        {
            int baRestart = 7;
            int bacounter = 0;
            int start = 0;
            int length = fetch.Length;
            int[] result = new int[length];
            int[] spriteBA = new int[8];
            int charBA = 0;

            while (true)
            {
                if (fetch[start] == 0)
                    break;
                start++;
            }

            while (true)
            {
                if (fetch[start] == 0x200)
                    break;
                start--;
            }

            if (start < 0)
                start += length;
            int offset = start;

            while (true)
            {
                int ba = 0x0888;

                if (fetch[offset] == 0x200)
                    charBA = baRestart;
                else if ((fetch[offset] & 0xFF00) == 0x0000)
                    spriteBA[fetch[offset] & 0x007] = baRestart;

                for (int i = 0; i < 8; i++)
                {
                    if (spriteBA[i] > 0)
                    {
                        ba <<= 4;
                        ba |= i;
                            spriteBA[i]--;
                    }
                }
                ba &= 0x0FFF;

                if (charBA > 0)
                {
                    ba = 0x1000;
                    charBA--;
                }

                result[offset] = ba;

                offset--;
                if (offset < 0)
                    offset += length;

                if (offset == start)
                    break;
            }

            for (int i = 0; i < length; i += 2)
            {
                result[i] = result[i + 1];
            }

            return result;
        }

        static public int[] TimingBuilder_Fetch(int[] timing, int sprite)
        {
            int length = timing.Length;
            int[] result = new int[length];
            int offset;
            int index = -1;
            int refreshCounter = 0;
            bool spriteActive = false;
            int spriteIndex = 0;
            int spritePhase = 0;
            int charCounter = 0;

            for (int i = 0; i < length; i++)
            {
                result[i++] = 0x500;
                result[i] = 0x100;
            }

            while (true)
            {
                index++;
                if (index >= length)
                    index -= length;
                offset = timing[index];

                if (charCounter > 0)
                {
                    result[index] = (charCounter & 1) == 0 ? 0x200 : 0x300;
                    charCounter--;
                    if (charCounter == 0)
                        break;
                }

                if (refreshCounter > 0)
                {
                    result[index] = (refreshCounter & 1) == 0 ? 0x500 : 0x100;
                    refreshCounter--;
                    if (refreshCounter == 0)
                        charCounter = 80;
                }

                if (offset == sprite)
                {
                    spriteActive = true;
                }

                if (spriteActive)
                {
                    result[index] = (spriteIndex | (spritePhase << 4));
                    spritePhase++;
                    if (spritePhase == 4)
                    {
                        spritePhase = 0;
                        spriteIndex++;
                        if (spriteIndex == 8)
                        {
                            spriteActive = false;
                            refreshCounter = 9;
                        }
                    }
                }
            }

            return result.ToArray();
        }

        static public int[] TimingBuilder_XRaster(int start, int width, int count, int delayOffset, int delayAmount)
        {
            List<int> result = new List<int>();
            int rasterX = start;
            bool delayed = false;
            count >>= 2;

            for (int i = 0; i < count; i++)
            {
                result.Add(rasterX);

                if (!delayed)
                {
                    rasterX += 4;
                    if (rasterX >= width)
                        rasterX -= width;
                }
                else
                {
                    delayAmount--;
                    if (delayAmount <= 0)
                        delayed = false;
                    continue;
                }

                if (rasterX == delayOffset && delayAmount > 0)
                    delayed = true;
            }

            return result.ToArray();
        }
    }
}
