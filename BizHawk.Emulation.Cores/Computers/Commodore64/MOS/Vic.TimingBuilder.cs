using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Vic
	{
	    [SaveState.DoNotSave] private const int BorderLeft38 = 0x023;
	    [SaveState.DoNotSave] private const int BorderLeft40 = 0x01C;
	    [SaveState.DoNotSave] private const int BorderRight38 = 0x153;
	    [SaveState.DoNotSave] private const int BorderRight40 = 0x15C;
        [SaveState.DoNotSave] private const int BorderTop25 = 0x033;
        [SaveState.DoNotSave] private const int BorderTop24 = 0x037;
        [SaveState.DoNotSave] private const int BorderBottom25 = 0x0FB;
        [SaveState.DoNotSave] private const int BorderBottom24 = 0x0F7;
        [SaveState.DoNotSave] private const int FirstDmaLine = 0x030;
        [SaveState.DoNotSave] private const int LastDmaLine = 0x0F7;

        // The special actions taken by the Vic are in the same order and interval on all chips, just different offsets.
        [SaveState.DoNotSave]
        private static readonly int[] TimingBuilderCycle14Act = {
			PipelineUpdateVc, 0,
			PipelineSpriteCrunch, 0,
			PipelineUpdateMcBase, 0,
        };

		// This builds a table of special actions to take on each half-cycle. Cycle14 is the X-raster position where
		// pre-display operations happen, and Cycle55 is the X-raster position where post-display operations happen.
		public static int[] TimingBuilder_Act(int[] timing, int cycle14, int sprite0Ba, int sprDisp)
		{
			var result = new List<int>();

			var length = timing.Length;
			for (var i = 0; i < length; i++)
			{
				while (i < result.Count)
					i++;
				if (timing[i] == cycle14)
					result.AddRange(TimingBuilderCycle14Act);
				else
					result.Add(0);
			}
			for (var i = 0; i < length; i++)
			{
				// pipeline raster X delay
				if (timing[(i + 1) % length] == timing[i])
					result[i] |= PipelineHoldX;

				// pipeline border checks
				if (timing[i] == (BorderLeft40 & 0xFFC))
					result[i] |= PipelineBorderLeft1;
				if (timing[i] == (BorderLeft38 & 0xFFC))
					result[i] |= PipelineBorderLeft0;
				if (timing[i] == (BorderRight38 & 0xFFC))
					result[i] |= PipelineBorderRight0;
				if (timing[i] == (BorderRight40 & 0xFFC))
					result[i] |= PipelineBorderRight1;

                // right side timing
			    if (timing[i] == 0x0158)
			        result[i] |= PipelineSpriteExpansion;
			    if (timing[i] == 0x0168)
			        result[i] |= PipelineUpdateRc;
			    if (timing[i] == sprite0Ba || timing[i] == sprite0Ba + 8)
			        result[i] |= PipelineSpriteDma;
			    if (timing[i] == sprDisp)
			        result[i] |= PipelineSpriteDisplay;

			}

			return result.ToArray();
		}

		// This builds a table of how the BA pin is supposed to act on each half-cycle.
		public static int[] TimingBuilder_BA(int[] fetch)
		{
			const int baRestart = 7;
			var start = 0;
			var length = fetch.Length;
			var result = new int[length];
			var spriteBa = new int[8];
			var charBa = 0;

			while (true)
			{
				if (fetch[start] == FetchTypeSprite)
					break;
				start++;
			}

			while (true)
			{
				if (fetch[start] == FetchTypeColor)
					break;
				start--;
			}

			if (start < 0)
				start += length;
			var offset = start;

			while (true)
			{
				var ba = BaTypeNone;

				if (fetch[offset] == FetchTypeColor)
					charBa = baRestart;
				else if ((fetch[offset] & 0xFF00) == FetchTypeSprite)
					spriteBa[fetch[offset] & 0x007] = baRestart;

				for (var i = 0; i < 8; i++)
				{
					if (spriteBa[i] > 0)
					{
						ba <<= 4;
						ba |= i;
						spriteBa[i]--;
					}
				}
				ba &= 0x0FFF;

				if (charBa > 0)
				{
					ba = BaTypeCharacter;
					charBa--;
				}

				result[offset] = ba;

				offset--;
				if (offset < 0)
					offset += length;

				if (offset == start)
					break;
			}

			for (var i = 0; i < length; i += 2)
			{
				result[i] = result[i + 1];
			}

			return result;
		}

		// This builds a table of the fetch operations to take on each half-cycle.
		public static int[] TimingBuilder_Fetch(int[] timing, int sprite)
		{
			var length = timing.Length;
			var result = new int[length];
		    var index = -1;
			var refreshCounter = 0;
			var spriteActive = false;
			var spriteIndex = 0;
			var spritePhase = 0;
			var charCounter = 0;

			for (var i = 0; i < length; i++)
			{
                result[i++] = FetchTypeIdle;
                result[i] = FetchTypeNone;
            }

            while (true)
			{
				index++;
				if (index >= length)
					index -= length;
				var offset = timing[index];

				if (charCounter > 0)
				{
					result[index] = (charCounter & 1) == 0 ? FetchTypeColor : FetchTypeGraphics;
					charCounter--;
					if (charCounter == 0)
						break;
				}

				if (refreshCounter > 0)
				{
					result[index] = (refreshCounter & 1) == 0 ? FetchTypeNone : FetchTypeRefresh;
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

		// This uses the vBlank values to determine the height of the visible screen.
	    private static int TimingBuilder_ScreenHeight(int vblankStart, int vblankEnd, int lines)
		{
            if (vblankStart < 0 || vblankEnd < 0)
            {
                return lines;
            }

            var offset = vblankEnd;
			var result = 0;
			while (true)
			{
				if (offset >= lines)
					offset -= lines;
				if (offset == vblankStart)
					return result;
				offset++;
				result++;
			}
		}

		// This uses the hBlank values to determine the width of the visible screen.
	    private static int TimingBuilder_ScreenWidth(IList<int> timing, int hblankStart, int hblankEnd)
		{
	        if (hblankStart < 0 || hblankEnd < 0)
	        {
	            return timing.Count * 4;
	        }

			var length = timing.Count;
			var result = 0;
			var offset = 0;

			while (timing[offset] != hblankEnd) { offset = (offset + 1) % length; }
			while (timing[offset] != hblankStart) { offset = (offset + 1) % length; result++; }

			return (result * 4);
		}

		// This builds the table of X-raster positions. Start marks the position where the
		// Y-raster is incremented. Width is the position where the X-raster is reset to zero. Count
		// is the width of a rasterline in pixels. DelayOffset is the X-raster position where lag begins
		// (specifically on an NTSC 6567R8) and DelayAmount is the number of positions to lag.
		public static int[] TimingBuilder_XRaster(int start, int width, int count, int delayOffset, int delayAmount)
		{
			var result = new List<int>();
			var rasterX = start;
			var delayed = false;
			count >>= 2;
			delayAmount >>= 2;

			for (var i = 0; i < count; i++)
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
