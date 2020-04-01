namespace PeNet.Structures
{
    /// <summary>
    ///     Abstract class for a Windows structure.
    /// </summary>
    public abstract class AbstractStructure
    {
        /// <summary>
        ///     A PE file as a binary buffer.
        /// </summary>
        internal readonly byte[] Buff;

        /// <summary>
        ///     The offset to the structure in the buffer.
        /// </summary>
        internal readonly uint Offset;


        /// <summary>
        ///     Creates a new AbstractStructure which holds fields
        ///     that all structures have in common.
        /// </summary>
        /// <param name="buff">A PE file as a binary buffer.</param>
        /// <param name="offset">The offset to the structure in the buffer.</param>
        protected AbstractStructure(byte[] buff, uint offset)
        {
            Buff = buff;
            Offset = offset;
        }
    }
}