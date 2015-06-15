using System;

namespace Jellyfish.Virtu
{
    public partial class Memory
    {
        private const int BankCount = 2;

        private const int BankMain = 0;
        private const int BankAux = 1;

        private const int RegionCount = 12;

        private const int Region0001 = 0;
        private const int Region02BF = 1;
        private const int Region0407 = 2;
        private const int Region080B = 3;
        private const int Region203F = 4;
        private const int Region405F = 5;
        private const int RegionC0C0 = 6;
        private const int RegionC1C7 = 7;
        private const int RegionC3C3 = 8;
        private const int RegionC8CF = 9;
        private const int RegionD0DF = 10;
        private const int RegionE0FF = 11;

        private static readonly int[] RegionBaseAddress = new int[RegionCount]
        {
            0x0000, 0x0200, 0x0200, 0x0200, 0x0200, 0x0200, 0xC000, 0xC100, 0xC100, 0xC100, 0xD000, 0xE000
        };

        private const int PageCount = 256;

        private static readonly int[] PageRegion = new int[PageCount]
        {
            Region0001, Region0001, Region02BF, Region02BF, Region0407, Region0407, Region0407, Region0407, 
            Region080B, Region080B, Region080B, Region080B, Region02BF, Region02BF, Region02BF, Region02BF, 
            Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, 
            Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, 
            Region203F, Region203F, Region203F, Region203F, Region203F, Region203F, Region203F, Region203F, 
            Region203F, Region203F, Region203F, Region203F, Region203F, Region203F, Region203F, Region203F, 
            Region203F, Region203F, Region203F, Region203F, Region203F, Region203F, Region203F, Region203F, 
            Region203F, Region203F, Region203F, Region203F, Region203F, Region203F, Region203F, Region203F, 
            Region405F, Region405F, Region405F, Region405F, Region405F, Region405F, Region405F, Region405F, 
            Region405F, Region405F, Region405F, Region405F, Region405F, Region405F, Region405F, Region405F, 
            Region405F, Region405F, Region405F, Region405F, Region405F, Region405F, Region405F, Region405F, 
            Region405F, Region405F, Region405F, Region405F, Region405F, Region405F, Region405F, Region405F, 
            Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, 
            Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, 
            Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, 
            Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, 
            Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, 
            Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, 
            Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, 
            Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, 
            Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, 
            Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, 
            Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, 
            Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, Region02BF, 
            RegionC0C0, RegionC1C7, RegionC1C7, RegionC3C3, RegionC1C7, RegionC1C7, RegionC1C7, RegionC1C7, 
            RegionC8CF, RegionC8CF, RegionC8CF, RegionC8CF, RegionC8CF, RegionC8CF, RegionC8CF, RegionC8CF, 
            RegionD0DF, RegionD0DF, RegionD0DF, RegionD0DF, RegionD0DF, RegionD0DF, RegionD0DF, RegionD0DF, 
            RegionD0DF, RegionD0DF, RegionD0DF, RegionD0DF, RegionD0DF, RegionD0DF, RegionD0DF, RegionD0DF, 
            RegionE0FF, RegionE0FF, RegionE0FF, RegionE0FF, RegionE0FF, RegionE0FF, RegionE0FF, RegionE0FF, 
            RegionE0FF, RegionE0FF, RegionE0FF, RegionE0FF, RegionE0FF, RegionE0FF, RegionE0FF, RegionE0FF, 
            RegionE0FF, RegionE0FF, RegionE0FF, RegionE0FF, RegionE0FF, RegionE0FF, RegionE0FF, RegionE0FF, 
            RegionE0FF, RegionE0FF, RegionE0FF, RegionE0FF, RegionE0FF, RegionE0FF, RegionE0FF, RegionE0FF
        };

        private const int State80Col = 0x000001;
        private const int StateText = 0x000002;
        private const int StateMixed = 0x000004;
        private const int StateHires = 0x000008;
        private const int StateDRes = 0x000010;
        private const int State80Store = 0x000020;
        private const int StateAltChrSet = 0x000040;
        private const int StateAltZP = 0x000080;
        private const int StateBank1 = 0x000100;
        private const int StateHRamRd = 0x000200;
        private const int StateHRamPreWrt = 0x000400;
        private const int StateHRamWrt = 0x000800;
        private const int StatePage2 = 0x001000;
        private const int StateRamRd = 0x002000;
        private const int StateRamWrt = 0x004000;
        private const int StateSlotC3Rom = 0x008000;
        private const int StateIntC8Rom = 0x010000; // [5-28]
        private const int StateIntCXRom = 0x020000;
        private const int StateAn0 = 0x040000;
        private const int StateAn1 = 0x080000;
        private const int StateAn2 = 0x100000;
        private const int StateAn3 = 0x200000;
        private const int StateVideo = State80Col | StateText | StateMixed | StateHires | StateDRes;

        private const int StateVideoModeCount = 32;

        private static readonly int[] StateVideoMode = new int[StateVideoModeCount]
        {
            Video.Mode0, Video.Mode0, Video.Mode1, Video.Mode2, Video.Mode3, Video.Mode4, Video.Mode1, Video.Mode2, 
            Video.Mode5, Video.Mode5, Video.Mode1, Video.Mode2, Video.Mode6, Video.Mode7, Video.Mode1, Video.Mode2, 
            Video.Mode8, Video.Mode9, Video.Mode1, Video.Mode2, Video.ModeA, Video.ModeB, Video.Mode1, Video.Mode2, 
            Video.ModeC, Video.ModeD, Video.Mode1, Video.Mode2, Video.ModeE, Video.ModeF, Video.Mode1, Video.Mode2
        };

        private Action<int, byte>[][][] WriteRamModeBankRegion;
    }
}
