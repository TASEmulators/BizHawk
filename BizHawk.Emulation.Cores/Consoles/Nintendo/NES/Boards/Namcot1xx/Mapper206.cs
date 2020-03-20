namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//various japanese Namcot108 boards plus DEROM
	[NesBoardImplPriority]
	internal sealed class Mapper206 : Namcot108Board_Base
	{
		public override bool Configure(EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.BoardType)
			{
				case "MAPPER206VS":
					NES.VS_dips[0] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_1 ? 1 : 0);
					NES.VS_dips[1] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_2 ? 1 : 0);
					NES.VS_dips[2] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_3 ? 1 : 0);
					NES.VS_dips[3] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_4 ? 1 : 0);
					NES.VS_dips[4] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_5 ? 1 : 0);
					NES.VS_dips[5] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_6 ? 1 : 0);
					NES.VS_dips[6] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_7 ? 1 : 0);
					NES.VS_dips[7] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_8 ? 1 : 0);
					NES._isVS = true;
					break;

				case "NES-DEROM": //R.B.I. baseball (U)
					AssertPrg(64); AssertChr(32,64); AssertVram(0); AssertWram(0);
					break;
				case "NAMCOT-3401": //babel no tou (J)
					AssertPrg(32); AssertChr(32); AssertVram(0); AssertWram(0);
					break;
				case "NAMCOT-3405": //side pocket (J)
					AssertPrg(128); AssertChr(32); AssertVram(0); AssertWram(0);
					break;
				case "NAMCOT-3406": //karnov (J)
					AssertPrg(128); AssertChr(64); AssertVram(0); AssertWram(0);
					break;
				case "NAMCOT-3407": //family jockey (J)
					AssertPrg(32); AssertChr(32, 16); AssertVram(0); AssertWram(0);
					break;
				case "NAMCOT-3413": //pro yakyuu family stadium (J)
					AssertPrg(64); AssertChr(32); AssertVram(0); AssertWram(0);
					break;
				case "NAMCOT-3414": //family boxing (J)
					AssertPrg(64); AssertChr(64); AssertVram(0); AssertWram(0);
					break;
				case "NAMCOT-3415": //mappy-land (J)
					AssertPrg(128); AssertChr(32); AssertVram(0); AssertWram(0);
					break;
				case "NAMCOT-3416": //dragon slayer IV (J) (aka legacy of the wizard)
					AssertPrg(128); AssertChr(64); AssertVram(0); AssertWram(0);
					break;
				case "NAMCOT-3417": //spy kid (J), R.B.I. Baseball 2.7z|Pro Yakyuu - Family Stadium '87 (J) [!]
					//apparently this shows up as namcot 108 as well but perhaps there is no difference
					//(is this game older than the other namcot 109 games?)
					AssertPrg(32); AssertChr(32); AssertVram(0); AssertWram(0);
					break;
				case "TENGEN-800030": // Pac-Mania (U), etc
					AssertPrg(64, 128); AssertChr(32, 64); AssertVram(0); AssertWram(0);
					break;
				case "TENGEN-800002":
					AssertPrg(64); AssertChr(32, 64); AssertVram(0); AssertWram(0);
					break;
				case "NES-DE1ROM":
					AssertPrg(128); AssertChr(64); AssertVram(0); AssertWram(0);
					break;
				case "NAMCOT-3451":
					AssertPrg(32); AssertChr(32); AssertVram(0); AssertWram(0);
					break;
				case "MAPPER206":
					AssertVram(0);
					break;
				default:
					return false;
			}

			BaseSetup();
			SetMirrorType(Cart.PadH, Cart.PadV);

			return true;
		}
	}
}
