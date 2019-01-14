#region Using



#endregion

namespace SharpCompress.Compressors.PPMd.I1
{
    /// <summary>
    /// SEE2 (secondary escape estimation) contexts for PPM contexts with masked symbols.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This must be a class rather than a structure because MakeEscapeFrequency returns a See2Context
    /// instance from the see2Contexts array.  The caller (for example, EncodeSymbol2) then updates the
    /// returned See2Context instance and expects the updates to be reflected in the see2Contexts array.
    /// This would not happen if this were a structure.
    /// </para>
    /// <remarks>
    /// Note that in most cases fields are used rather than properties for performance reasons (for example,
    /// <see cref="_shift"/> is a field rather than a property).
    /// </remarks>
    /// </remarks>
    internal class See2Context
    {
        private const byte PERIOD_BIT_COUNT = 7;

        public ushort _summary;
        public byte _shift;
        public byte _count;

        public void Initialize(uint initialValue)
        {
            _shift = PERIOD_BIT_COUNT - 4;
            _summary = (ushort)(initialValue << _shift);
            _count = 7;
        }

        public uint Mean()
        {
            uint value = (uint)(_summary >> _shift);
            _summary = (ushort)(_summary - value);
            return (uint)(value + ((value == 0) ? 1 : 0));
        }

        public void Update()
        {
            if (_shift < PERIOD_BIT_COUNT && --_count == 0)
            {
                _summary += _summary;
                _count = (byte)(3 << _shift++);
            }
        }
    }
}