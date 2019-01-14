#region Using

using System.IO;

#endregion

namespace SharpCompress.Compressors.PPMd.I1
{
    /// <summary>
    /// A simple range coder.
    /// </summary>
    /// <remarks>
    /// Note that in most cases fields are used rather than properties for performance reasons (for example,
    /// <see cref="_scale"/> is a field rather than a property).
    /// </remarks>
    internal class Coder
    {
        private const uint RANGE_TOP = 1 << 24;
        private const uint RANGE_BOTTOM = 1 << 15;
        private uint _low;
        private uint _code;
        private uint _range;

        public uint _lowCount;
        public uint _highCount;
        public uint _scale;

        public void RangeEncoderInitialize()
        {
            _low = 0;
            _range = uint.MaxValue;
        }

        public void RangeEncoderNormalize(Stream stream)
        {
            while ((_low ^ (_low + _range)) < RANGE_TOP ||
                   _range < RANGE_BOTTOM && ((_range = (uint)-_low & (RANGE_BOTTOM - 1)) != 0 || true))
            {
                stream.WriteByte((byte)(_low >> 24));
                _range <<= 8;
                _low <<= 8;
            }
        }

        public void RangeEncodeSymbol()
        {
            _low += _lowCount * (_range /= _scale);
            _range *= _highCount - _lowCount;
        }

        public void RangeShiftEncodeSymbol(int rangeShift)
        {
            _low += _lowCount * (_range >>= rangeShift);
            _range *= _highCount - _lowCount;
        }

        public void RangeEncoderFlush(Stream stream)
        {
            for (uint index = 0; index < 4; index++)
            {
                stream.WriteByte((byte)(_low >> 24));
                _low <<= 8;
            }
        }

        public void RangeDecoderInitialize(Stream stream)
        {
            _low = 0;
            _code = 0;
            _range = uint.MaxValue;
            for (uint index = 0; index < 4; index++)
            {
                _code = (_code << 8) | (byte)stream.ReadByte();
            }
        }

        public void RangeDecoderNormalize(Stream stream)
        {
            while ((_low ^ (_low + _range)) < RANGE_TOP ||
                   _range < RANGE_BOTTOM && ((_range = (uint)-_low & (RANGE_BOTTOM - 1)) != 0 || true))
            {
                _code = (_code << 8) | (byte)stream.ReadByte();
                _range <<= 8;
                _low <<= 8;
            }
        }

        public uint RangeGetCurrentCount()
        {
            return (_code - _low) / (_range /= _scale);
        }

        public uint RangeGetCurrentShiftCount(int rangeShift)
        {
            return (_code - _low) / (_range >>= rangeShift);
        }

        public void RangeRemoveSubrange()
        {
            _low += _range * _lowCount;
            _range *= _highCount - _lowCount;
        }
    }
}