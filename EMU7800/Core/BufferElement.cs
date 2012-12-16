namespace EMU7800.Core
{
	/*
	 * why this sucks:
	 * A read costs 3 shifts and an or.  A write costs 2 shifts.  Additional shifts are
	 * needed elsewhere to figure out which item in a BufferElement[] to access.  Because
	 * the encapsulation is for a BufferElement and not a whole array of them, code elsewhere
	 * is gunked up with 'BufferElement.SIZE' shifts.  If the 32 bit "alias" was actually used,
	 * there might be some purpose to this code:  but it's only used for a ZeroMemory()
	 * replacement.  Every use of BufferElement in the code is a BufferElement[] used as a gunked
	 * up replacement for a byte[].
	 * 
	 * A small speed increase was observed hacking this out; but my motivation was more about cleaness
	 * and stomping out bad ideas.
	 */

	/*
    /// <summary>
    /// Frames are composed of <see cref="BufferElement"/>s,
    /// that group bytes into machine words for efficient array processing.
    /// Bytes are packed in logical little endian order.
    /// </summary>
    public struct BufferElement
    {
        /// <summary>
        /// The number of bytes contained within a <see cref="BufferElement"/>.
        /// </summary>
        public const int SIZE  = 4;       // 2^SHIFT

        /// <summary>
        /// The mask value applied against a byte array index to access the individual bytes within a <see cref="BufferElement"/>.
        /// </summary>
        public const int MASK  = 3;       // SIZE - 1

        /// <summary>
        /// The left shift value to convert a byte array index to a <see cref="BufferElement"/> array index.
        /// </summary>
        public const int SHIFT = 2;

        uint _data;

        /// <summary>
        /// A convenience accessor for reading/writing individual bytes within this <see cref="BufferElement"/>.
        /// </summary>
        /// <param name="offset"></param>
        public byte this[int offset]
        {
            get
            {
                var i = (offset & MASK) << 3;
                return (byte)(_data >> i);
            }
            set
            {
                var i  = (offset & MASK) << 3;
                var d  = (uint)value << i;
                var di = (uint)0xff  << i;
                _data = _data & ~di | d;
            }
        }

        /// <summary>
        /// Zeros out all bytes of this <see cref="BufferElement"/>.
        /// </summary>
        public void ClearAll()
        {
            _data = 0;
        }
    }
	*/
}
