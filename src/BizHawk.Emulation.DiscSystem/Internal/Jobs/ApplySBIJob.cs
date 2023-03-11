// TODO - generate correct Q subchannel CRC
namespace BizHawk.Emulation.DiscSystem
{
	internal class ApplySBIJob
	{
		/// <summary>
		/// applies an SBI file to the disc
		/// </summary>
		public void Run(Disc disc, SBI.SubQPatchData sbi, bool asMednafen)
		{
			//TODO - could implement as a blob, to avoid allocating so many byte buffers

			//save this, it's small, and we'll want it for disc processing a/b checks
			disc.Memos["sbi"] = sbi;

			var dsr = new DiscSectorReader(disc);

			var n = sbi.ABAs.Count;
			var b = 0;
			for (var i = 0; i < n; i++)
			{
				var lba = sbi.ABAs[i] - 150;

				//create a synthesizer which can return the patched data
				var ss_patchq = new SS_PatchQ { Original = disc._Sectors[lba + 150] };
				var subQbuf = ss_patchq.Buffer_SubQ;

				//read the old subcode
				dsr.ReadLBA_SubQ(lba, subQbuf, 0);

				//insert patch
				disc._Sectors[lba + 150] = ss_patchq;

				//apply SBI patch
				for (var j = 0; j < 12; j++)
				{
					var patch = sbi.subq[b++];
					if (patch == -1) continue;
					subQbuf[j] = (byte)patch;
				}

				//Apply mednafen hacks
				//The reasoning here is that we know we expect these sectors to have a wrong checksum. therefore, generate a checksum, and make it wrong
				//However, this seems senseless to me. The whole point of the SBI data is that it stores the patches needed to generate an acceptable subQ, right?
				if (asMednafen)
				{
					SynthUtils.SubQ_SynthChecksum(subQbuf, 0);
					subQbuf[10] ^= 0xFF;
					subQbuf[11] ^= 0xFF;
				}
			}
		}
	}
}
