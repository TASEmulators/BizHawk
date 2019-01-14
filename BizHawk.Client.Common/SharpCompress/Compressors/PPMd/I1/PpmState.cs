#region Using



#endregion

namespace SharpCompress.Compressors.PPMd.I1
{
    /// <summary>
    /// PPM state.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This must be a structure rather than a class because several places in the associated code assume that
    /// <see cref="PpmState"/> is a value type (meaning that assignment creates a completely new copy of the
    /// instance rather than just copying a reference to the same instance).
    /// </para>
    /// <para>
    /// Note that <see cref="_address"/> is a field rather than a property for performance reasons.
    /// </para>
    /// </remarks>
    internal struct PpmState
    {
        public uint _address;
        public byte[] _memory;
        public static readonly PpmState ZERO = new PpmState(0, null);
        public const int SIZE = 6;

        /// <summary>
        /// Initializes a new instance of the <see cref="PpmState"/> structure.
        /// </summary>
        public PpmState(uint address, byte[] memory)
        {
            _address = address;
            _memory = memory;
        }

        /// <summary>
        /// Gets or sets the symbol.
        /// </summary>
        public byte Symbol { get => _memory[_address]; set => _memory[_address] = value; }

        /// <summary>
        /// Gets or sets the frequency.
        /// </summary>
        public byte Frequency { get => _memory[_address + 1]; set => _memory[_address + 1] = value; }

        /// <summary>
        /// Gets or sets the successor.
        /// </summary>
        public Model.PpmContext Successor
        {
            get => new Model.PpmContext(
                                        _memory[_address + 2] | ((uint)_memory[_address + 3]) << 8 |
                                        ((uint)_memory[_address + 4]) << 16 | ((uint)_memory[_address + 5]) << 24, _memory);
            set
            {
                _memory[_address + 2] = (byte)value._address;
                _memory[_address + 3] = (byte)(value._address >> 8);
                _memory[_address + 4] = (byte)(value._address >> 16);
                _memory[_address + 5] = (byte)(value._address >> 24);
            }
        }

        /// <summary>
        /// Gets the <see cref="PpmState"/> at the <paramref name="offset"/> relative to this
        /// <see cref="PpmState"/>.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public PpmState this[int offset] => new PpmState((uint)(_address + offset * SIZE), _memory);

        /// <summary>
        /// Allow a pointer to be implicitly converted to a PPM state.
        /// </summary>
        /// <param name="pointer"></param>
        /// <returns></returns>
        public static implicit operator PpmState(Pointer pointer)
        {
            return new PpmState(pointer._address, pointer._memory);
        }

        /// <summary>
        /// Allow pointer-like addition on a PPM state.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static PpmState operator +(PpmState state, int offset)
        {
            state._address = (uint)(state._address + offset * SIZE);
            return state;
        }

        /// <summary>
        /// Allow pointer-like incrementing on a PPM state.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static PpmState operator ++(PpmState state)
        {
            state._address += SIZE;
            return state;
        }

        /// <summary>
        /// Allow pointer-like subtraction on a PPM state.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static PpmState operator -(PpmState state, int offset)
        {
            state._address = (uint)(state._address - offset * SIZE);
            return state;
        }

        /// <summary>
        /// Allow pointer-like decrementing on a PPM state.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static PpmState operator --(PpmState state)
        {
            state._address -= SIZE;
            return state;
        }

        /// <summary>
        /// Compare two PPM states.
        /// </summary>
        /// <param name="state1"></param>
        /// <param name="state2"></param>
        /// <returns></returns>
        public static bool operator <=(PpmState state1, PpmState state2)
        {
            return state1._address <= state2._address;
        }

        /// <summary>
        /// Compare two PPM states.
        /// </summary>
        /// <param name="state1"></param>
        /// <param name="state2"></param>
        /// <returns></returns>
        public static bool operator >=(PpmState state1, PpmState state2)
        {
            return state1._address >= state2._address;
        }

        /// <summary>
        /// Compare two PPM states.
        /// </summary>
        /// <param name="state1"></param>
        /// <param name="state2"></param>
        /// <returns></returns>
        public static bool operator ==(PpmState state1, PpmState state2)
        {
            return state1._address == state2._address;
        }

        /// <summary>
        /// Compare two PPM states.
        /// </summary>
        /// <param name="state1"></param>
        /// <param name="state2"></param>
        /// <returns></returns>
        public static bool operator !=(PpmState state1, PpmState state2)
        {
            return state1._address != state2._address;
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <returns>true if obj and this instance are the same type and represent the same value; otherwise, false.</returns>
        /// <param name="obj">Another object to compare to.</param>
        public override bool Equals(object obj)
        {
            if (obj is PpmState)
            {
                PpmState state = (PpmState)obj;
                return state._address == _address;
            }
            return base.Equals(obj);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer that is the hash code for this instance.</returns>
        public override int GetHashCode()
        {
            return _address.GetHashCode();
        }
    }
}