namespace SharpCompress.Compressors.PPMd.H
{
    internal abstract class Pointer
    {
        /// <summary> Initialize the object with the array (may be null)</summary>
        /// <param name="mem">the byte array
        /// </param>
        internal Pointer(byte[] mem)
        {
            Memory = mem;
        }

        internal byte[] Memory { get; private set; }

        internal virtual int Address { get; set; }

        protected T Initialize<T>(byte[] mem)
            where T : Pointer
        {
            Memory = mem;
            Address = 0;
            return this as T;
        }
    }
}