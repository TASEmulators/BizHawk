using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class VicIINew : IVideoProvider
	{
		public StateParameters State
		{
			get
			{
				StateParameters result = new StateParameters();

				// internal
				result.Save("RC", RC);
				result.Save("VC", VC);
				result.Save("VCBASE", VCBASE);
				result.Save("VMLI", VMLI);

				// external
				result.Save("BMM", BMM);
				result.Save("BxC", BxC, BxC.Length);
				result.Save("CB", CB);
				result.Save("CSEL", CSEL);
				result.Save("DEN", DEN);
				result.Save("EC", EC);
				result.Save("ECM", ECM);
				result.Save("ELP", ELP);
				result.Save("EMBC", EMBC);
				result.Save("EMMC", EMMC);
				result.Save("ERST", ERST);
				result.Save("ILP", ILP);
				result.Save("IMBC", IMBC);
				result.Save("IMMC", IMMC);
				result.Save("IRQ", IRQ);
				result.Save("IRST", IRST);
				result.Save("LPX", LPX);
				result.Save("LPY", LPY);
				result.Save("MCM", MCM);
				result.Save("MMx", MMx, MMx.Length);
				result.Save("RASTER", RASTER);
				result.Save("RES", RES);
				result.Save("RSEL", RSEL);
				result.Save("VM", VM);
				result.Save("XSCROLL", XSCROLL);
				result.Save("YSCROLL", YSCROLL);
				
				// state
				result.Save("BADLINE", badline);
				result.Save("BITMAPCOLUMN", bitmapColumn);
				result.Save("BITMAPDATA", bitmapData);
				result.Save("BORDERONMAIN", borderOnMain);
				result.Save("BORDERONVERTICAL", borderOnVertical);
				result.Save("CENTERENABLED", centerEnabled);
				result.Save("CHARACTERDATA", characterData);
				result.Save("CHARACTERDATABUS", characterDataBus);
				result.Save("CHARMEM", characterMemory, characterMemory.Length);
				result.Save("COLORDATA", colorData);
				result.Save("COLORDATABUS", colorDataBus);
				result.Save("COLORMEM", colorMemory, colorMemory.Length);
				result.Save("DISPLAYENABLED", displayEnabled);
				result.Save("IDLE", idle);
				result.Save("PLOTTERBUFFERINDEX", plotterBufferIndex);
				result.Save("PLOTTERDATA", plotterData);
				result.Save("PLOTTERDATABUFFER", plotterDataBuffer, plotterDataBuffer.Length);
				result.Save("PLOTTERDELAY", plotterDelay);
				result.Save("PLOTTERPIXEL", plotterPixel);
				result.Save("PLOTTERPIXELBUFFER", plotterPixelBuffer, plotterPixelBuffer.Length);
				result.Save("RASTERINTERRUPTLINE", rasterInterruptLine);
				result.Save("RASTERX", rasterX);
				result.Save("REFRESHADDRESS", refreshAddress);

				// pipeline
				result.Save("CYCLE", cycle);
				result.Save("PIPELINEGACCESS", pipelineGAccess);
				result.Save("PIPELINEMEMORYBUSY", pipelineMemoryBusy);

				// sprites
				for (int i = 0; i < 8; i++)
				{
					string iTag = i.ToString();
					result.Save("MC" + iTag, sprites[i].MC);
					result.Save("MCBASE" + iTag, sprites[i].MCBASE);
					result.Save("MD" + iTag, sprites[i].MD);
					result.Save("MDMA" + iTag, sprites[i].MDMA);
					result.Save("MPTR" + iTag, sprites[i].MPTR);
					result.Save("MSR" + iTag, sprites[i].MSR);
					result.Save("MxXEToggle" + iTag, sprites[i].MxXEToggle);
					result.Save("MxYEToggle" + iTag, sprites[i].MxYEToggle);

					result.Save("MxC" + iTag, sprites[i].MxC);
					result.Save("MxD" + iTag, sprites[i].MxD);
					result.Save("MxDP" + iTag, sprites[i].MxDP);
					result.Save("MxE" + iTag, sprites[i].MxE);
					result.Save("MxM" + iTag, sprites[i].MxM);
					result.Save("MxMC" + iTag, sprites[i].MxMC);
					result.Save("MxX" + iTag, sprites[i].MxX);
					result.Save("MxXE" + iTag, sprites[i].MxXE);
					result.Save("MxY" + iTag, sprites[i].MxY);
					result.Save("MxYE" + iTag, sprites[i].MxYE);
				}

				return result;
			}
			set
			{
				StateParameters result = value;

				// internal
				result.Load("RC", out RC);
				result.Load("VC", out VC);
				result.Load("VCBASE", out VCBASE);
				result.Load("VMLI", out VMLI);

				// external
				result.Load("BMM", out BMM);
				result.Load("BxC", out BxC, BxC.Length);
				result.Load("CB", out CB);
				result.Load("CSEL", out CSEL);
				result.Load("DEN", out DEN);
				result.Load("EC", out EC);
				result.Load("ECM", out ECM);
				result.Load("ELP", out ELP);
				result.Load("EMBC", out EMBC);
				result.Load("EMMC", out EMMC);
				result.Load("ERST", out ERST);
				result.Load("ILP", out ILP);
				result.Load("IMBC", out IMBC);
				result.Load("IMMC", out IMMC);
				result.Load("IRQ", out IRQ);
				result.Load("IRST", out IRST);
				result.Load("LPX", out LPX);
				result.Load("LPY", out LPY);
				result.Load("MCM", out MCM);
				result.Load("MMx", out MMx, MMx.Length);
				result.Load("RASTER", out RASTER);
				result.Load("RES", out RES);
				result.Load("RSEL", out RSEL);
				result.Load("VM", out VM);
				result.Load("XSCROLL", out XSCROLL);
				result.Load("YSCROLL", out YSCROLL);

				// state
				result.Load("BADLINE", out badline);
				result.Load("BITMAPCOLUMN", out bitmapColumn);
				result.Load("BITMAPDATA", out bitmapData);
				result.Load("BORDERONMAIN", out borderOnMain);
				result.Load("BORDERONVERTICAL", out borderOnVertical);
				result.Load("CENTERENABLED", out centerEnabled);
				result.Load("CHARACTERDATA", out characterData);
				result.Load("CHARACTERDATABUS", out characterDataBus);
				result.Load("CHARMEM", out characterMemory, characterMemory.Length);
				result.Load("COLORDATA", out colorData);
				result.Load("COLORDATABUS", out colorDataBus);
				result.Load("COLORMEM", out colorMemory, colorMemory.Length);
				result.Load("DISPLAYENABLED", out displayEnabled);
				result.Load("IDLE", out idle);
				result.Load("PLOTTERBUFFERINDEX", out plotterBufferIndex);
				result.Load("PLOTTERDATA", out plotterData);
				result.Load("PLOTTERDATABUFFER", out plotterDataBuffer, plotterDataBuffer.Length);
				result.Load("PLOTTERDELAY", out plotterDelay);
				result.Load("PLOTTERPIXEL", out plotterPixel);
				result.Load("PLOTTERPIXELBUFFER", out plotterPixelBuffer, plotterPixelBuffer.Length);
				result.Load("RASTERINTERRUPTLINE", out rasterInterruptLine);
				result.Load("RASTERX", out rasterX);
				result.Load("REFRESHADDRESS", out refreshAddress);

				// pipeline
				result.Load("CYCLE", out cycle);
				result.Load("PIPELINEGACCESS", out pipelineGAccess);
				result.Load("PIPELINEMEMORYBUSY", out pipelineMemoryBusy);

				// sprites
				for (int i = 0; i < 8; i++)
				{
					string iTag = i.ToString();
					result.Load("MC" + iTag, out sprites[i].MC);
					result.Load("MCBASE" + iTag, out sprites[i].MCBASE);
					result.Load("MD" + iTag, out sprites[i].MD);
					result.Load("MDMA" + iTag, out sprites[i].MDMA);
					result.Load("MPTR" + iTag, out sprites[i].MPTR);
					result.Load("MSR" + iTag, out sprites[i].MSR);
					result.Load("MxXEToggle" + iTag, out sprites[i].MxXEToggle);
					result.Load("MxYEToggle" + iTag, out sprites[i].MxYEToggle);

					result.Load("MxC" + iTag, out sprites[i].MxC);
					result.Load("MxD" + iTag, out sprites[i].MxD);
					result.Load("MxDP" + iTag, out sprites[i].MxDP);
					result.Load("MxE" + iTag, out sprites[i].MxE);
					result.Load("MxM" + iTag, out sprites[i].MxM);
					result.Load("MxMC" + iTag, out sprites[i].MxMC);
					result.Load("MxX" + iTag, out sprites[i].MxX);
					result.Load("MxXE" + iTag, out sprites[i].MxXE);
					result.Load("MxY" + iTag, out sprites[i].MxY);
					result.Load("MxYE" + iTag, out sprites[i].MxYE);
				}

				UpdateInterrupts();
				UpdateBorder();
				UpdatePlotter();
			}
		}
	}
}
