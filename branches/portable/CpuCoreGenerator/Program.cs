class Program
{
    static void Main()
    {
        //We don't currently use this anymore
        //var x = new M6502.CoreGenerator();
        //x.InitOpcodeTable();
        //x.GenerateDisassembler("../../../BizHawk.Emulation.Common/CPUs/MOS 6502X/Disassembler.cs");
        //x.GenerateExecutor("../../../BizHawk.Emulation.Common/CPUs/MOS 6502X/Execute.cs");

        var y = new HuC6280.CoreGenerator();
        y.InitOpcodeTable();
        y.GenerateDisassembler("../../../BizHawk.Emulation.Cores/CPUs/HuC6280/Disassembler.cs");
        y.GenerateExecutor("../../../BizHawk.Emulation.Cores/CPUs/HuC6280/Execute.cs");
		y.GenerateCDL("../../../BizHawk.Emulation.Cores/CPUs/HuC6280/CDLOpcodes.cs");
    }
}
