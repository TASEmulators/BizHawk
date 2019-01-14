namespace SharpCompress.Compressors.Rar.VM
{
    internal class VMCmdFlags
    {
        public const byte VMCF_OP0 = 0;
        public const byte VMCF_OP1 = 1;
        public const byte VMCF_OP2 = 2;
        public const byte VMCF_OPMASK = 3;
        public const byte VMCF_BYTEMODE = 4;
        public const byte VMCF_JUMP = 8;
        public const byte VMCF_PROC = 16;
        public const byte VMCF_USEFLAGS = 32;
        public const byte VMCF_CHFLAGS = 64;

        public static byte[] VM_CmdFlags =
        {
            VMCF_OP2 | VMCF_BYTEMODE, VMCF_OP2 | VMCF_BYTEMODE | VMCF_CHFLAGS,
            VMCF_OP2 | VMCF_BYTEMODE | VMCF_CHFLAGS,
            VMCF_OP2 | VMCF_BYTEMODE | VMCF_CHFLAGS,
            VMCF_OP1 | VMCF_JUMP | VMCF_USEFLAGS,
            VMCF_OP1 | VMCF_JUMP | VMCF_USEFLAGS,
            VMCF_OP1 | VMCF_BYTEMODE | VMCF_CHFLAGS,
            VMCF_OP1 | VMCF_BYTEMODE | VMCF_CHFLAGS, VMCF_OP1 | VMCF_JUMP,
            VMCF_OP2 | VMCF_BYTEMODE | VMCF_CHFLAGS,
            VMCF_OP2 | VMCF_BYTEMODE | VMCF_CHFLAGS,
            VMCF_OP2 | VMCF_BYTEMODE | VMCF_CHFLAGS,
            VMCF_OP2 | VMCF_BYTEMODE | VMCF_CHFLAGS,
            VMCF_OP1 | VMCF_JUMP | VMCF_USEFLAGS,
            VMCF_OP1 | VMCF_JUMP | VMCF_USEFLAGS,
            VMCF_OP1 | VMCF_JUMP | VMCF_USEFLAGS,
            VMCF_OP1 | VMCF_JUMP | VMCF_USEFLAGS,
            VMCF_OP1 | VMCF_JUMP | VMCF_USEFLAGS,
            VMCF_OP1 | VMCF_JUMP | VMCF_USEFLAGS, VMCF_OP1, VMCF_OP1,
            VMCF_OP1 | VMCF_PROC, VMCF_OP0 | VMCF_PROC, VMCF_OP1 | VMCF_BYTEMODE,
            VMCF_OP2 | VMCF_BYTEMODE | VMCF_CHFLAGS,
            VMCF_OP2 | VMCF_BYTEMODE | VMCF_CHFLAGS,
            VMCF_OP2 | VMCF_BYTEMODE | VMCF_CHFLAGS,
            VMCF_OP1 | VMCF_BYTEMODE | VMCF_CHFLAGS, VMCF_OP0, VMCF_OP0,
            VMCF_OP0 | VMCF_USEFLAGS, VMCF_OP0 | VMCF_CHFLAGS, VMCF_OP2, VMCF_OP2,
            VMCF_OP2 | VMCF_BYTEMODE, VMCF_OP2 | VMCF_BYTEMODE,
            VMCF_OP2 | VMCF_BYTEMODE,
            VMCF_OP2 | VMCF_BYTEMODE | VMCF_USEFLAGS | VMCF_CHFLAGS,
            VMCF_OP2 | VMCF_BYTEMODE | VMCF_USEFLAGS | VMCF_CHFLAGS, VMCF_OP0
        };
    }
}