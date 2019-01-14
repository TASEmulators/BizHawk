using System.Collections.Generic;

namespace SharpCompress.Compressors.Rar.VM
{
    internal class VMPreparedProgram
    {
        internal List<VMPreparedCommand> Commands = new List<VMPreparedCommand>();
        internal List<VMPreparedCommand> AltCommands = new List<VMPreparedCommand>();

        public int CommandCount { get; set; }

        internal List<byte> GlobalData = new List<byte>();
        internal List<byte> StaticData = new List<byte>();

        // static data contained in DB operators
        internal int[] InitR = new int[7];

        internal int FilteredDataOffset { get; set; }
        internal int FilteredDataSize { get; set; }
    }
}