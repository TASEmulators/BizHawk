namespace SharpCompress.Compressors.Rar.VM
{
    internal class VMPreparedCommand
    {
        internal VMPreparedCommand()
        {
            Op1 = new VMPreparedOperand();
            Op2 = new VMPreparedOperand();
        }

        internal VMCommands OpCode { get; set; }
        internal bool IsByteMode { get; set; }
        internal VMPreparedOperand Op1 { get; }

        internal VMPreparedOperand Op2 { get; }
    }
}