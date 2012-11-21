using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class VicIINew : IVideoProvider
	{
		public void SyncState(Serializer ser)
		{
			// internal
			ser.Sync("RC", ref RC);
			ser.Sync("VC", ref VC);
			ser.Sync("VCBASE", ref VCBASE);
			ser.Sync("VMLI", ref VMLI);

			// external
			ser.Sync("BMM", ref BMM);
			ser.Sync("BxC", ref BxC, false);
			ser.Sync("CB", ref CB);
			ser.Sync("CSEL", ref CSEL);
			ser.Sync("DEN", ref DEN);
			ser.Sync("EC", ref EC);
			ser.Sync("ECM", ref ECM);
			ser.Sync("ELP", ref ELP);
			ser.Sync("EMBC", ref EMBC);
			ser.Sync("EMMC", ref EMMC);
			ser.Sync("ERST", ref ERST);
			ser.Sync("ILP", ref ILP);
			ser.Sync("IMBC", ref IMBC);
			ser.Sync("IMMC", ref IMMC);
			ser.Sync("IRQ", ref IRQ);
			ser.Sync("IRST", ref IRST);
			ser.Sync("LPX", ref LPX);
			ser.Sync("LPY", ref LPY);
			ser.Sync("MCM", ref MCM);
			ser.Sync("MMx", ref MMx, false);
			ser.Sync("RASTER", ref RASTER);
			ser.Sync("RES", ref RES);
			ser.Sync("RSEL", ref RSEL);
			ser.Sync("VM", ref VM);
			ser.Sync("XSCROLL", ref XSCROLL);
			ser.Sync("YSCROLL", ref YSCROLL);

			// state
			ser.Sync("BADLINE", ref badline);
			ser.Sync("BITMAPCOLUMN", ref bitmapColumn);
			ser.Sync("BITMAPDATA", ref bitmapData);
			ser.Sync("BORDERONMAIN", ref borderOnMain);
			ser.Sync("BORDERONVERTICAL", ref borderOnVertical);
			ser.Sync("CENTERENABLED", ref centerEnabled);
			ser.Sync("CHARACTERDATA", ref characterData);
			ser.Sync("CHARACTERDATABUS", ref characterDataBus);
			ser.Sync("CHARMEM", ref characterMemory, false);
			ser.Sync("COLORDATA", ref colorData);
			ser.Sync("COLORDATABUS", ref colorDataBus);
			ser.Sync("COLORMEM", ref colorMemory, false);
			ser.Sync("DISPLAYENABLED", ref displayEnabled);
			ser.Sync("IDLE", ref idle);
			ser.Sync("PLOTTERBUFFERINDEX", ref plotterBufferIndex);
			ser.Sync("PLOTTERDATA", ref plotterData);
			ser.Sync("PLOTTERDATABUFFER", ref plotterDataBuffer, false);
			ser.Sync("PLOTTERDELAY", ref plotterDelay);
			ser.Sync("PLOTTERPIXEL", ref plotterPixel);
			ser.Sync("PLOTTERPIXELBUFFER", ref plotterPixelBuffer, false);
			ser.Sync("RASTERINTERRUPTLINE", ref rasterInterruptLine);
			ser.Sync("RASTERX", ref rasterX);
			ser.Sync("REFRESHADDRESS", ref refreshAddress);

			// pipeline
			ser.Sync("CYCLE", ref cycle);
			ser.Sync("PIPELINEGACCESS", ref pipelineGAccess);
			ser.Sync("PIPELINEMEMORYBUSY", ref pipelineMemoryBusy);

			// sprites
			for (int i = 0; i < 8; i++)
			{
				string iTag = i.ToString();
				ser.Sync("MC" + iTag, ref sprites[i].MC);
				ser.Sync("MCBASE" + iTag, ref sprites[i].MCBASE);
				ser.Sync("MD" + iTag, ref sprites[i].MD);
				ser.Sync("MDMA" + iTag, ref sprites[i].MDMA);
				ser.Sync("MPTR" + iTag, ref sprites[i].MPTR);
				ser.Sync("MSR" + iTag, ref sprites[i].MSR);
				ser.Sync("MxXEToggle" + iTag, ref sprites[i].MxXEToggle);
				ser.Sync("MxYEToggle" + iTag, ref sprites[i].MxYEToggle);

				ser.Sync("MxC" + iTag, ref sprites[i].MxC);
				ser.Sync("MxD" + iTag, ref sprites[i].MxD);
				ser.Sync("MxDP" + iTag, ref sprites[i].MxDP);
				ser.Sync("MxE" + iTag, ref sprites[i].MxE);
				ser.Sync("MxM" + iTag, ref sprites[i].MxM);
				ser.Sync("MxMC" + iTag, ref sprites[i].MxMC);
				ser.Sync("MxX" + iTag, ref sprites[i].MxX);
				ser.Sync("MxXE" + iTag, ref sprites[i].MxXE);
				ser.Sync("MxY" + iTag, ref sprites[i].MxY);
				ser.Sync("MxYE" + iTag, ref sprites[i].MxYE);
			}

			if (ser.IsReader)
			{
				UpdateBorder();
				UpdatePlotter();
			}
		}
	}
}