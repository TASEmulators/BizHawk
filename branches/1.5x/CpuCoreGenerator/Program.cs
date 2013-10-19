class Program
{
    static void Main()
    {
        var x = new M6502.CoreGenerator();
        x.InitOpcodeTable();
        x.GenerateDisassembler("../../../BizHawk.Emulation/CPUs/MOS 6502/Disassembler.cs");
        x.GenerateExecutor("../../../BizHawk.Emulation/CPUs/MOS 6502/Execute.cs");

        var y = new HuC6280.CoreGenerator();
        y.InitOpcodeTable();
        y.GenerateDisassembler("../../../BizHawk.Emulation/CPUs/HuC6280/Disassembler.cs");
        y.GenerateExecutor("../../../BizHawk.Emulation/CPUs/HuC6280/Execute.cs");
    }
}
