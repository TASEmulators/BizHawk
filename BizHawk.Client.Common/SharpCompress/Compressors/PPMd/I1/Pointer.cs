#region Using

using System;

#endregion

namespace SharpCompress.Compressors.PPMd.I1
{
    /// <summary>
    /// A structure containing a single address representing a position in the <see cref="_memory"/> array.  This
    /// is intended to mimic the behaviour of a pointer in C/C++.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This must be a structure rather than a class because several places in the associated code assume that
    /// <see cref="Pointer"/> is a value type (meaning that assignment creates a completely new copy of the
    /// instance rather than just copying a reference to the same instance).
    /// </para>
    /// <para>
    /// Note that <see cref="_address"/> is a field rather than a property for performance reasons.
    /// </para>
    /// </remarks>
    internal struct Pointer
    {
        public uint _address;
        public byte[] _memory;
        public static readonly Pointer ZERO = new Pointer(0, null);
        public const int SIZE = 1;

        /// <summary>
        /// Initializes a new instance of the <see cref="Pointer"/> structure.
        /// </summary>
        public Pointer(uint address, byte[] memory)
        {
            _address = address;
            _memory = memory;
        }

        /// <summary>
        /// Gets or sets the byte at the given <paramref name="offset"/>.
        /// </summary>
        /// <param name="offset"></param>
        /// <returns></returns>
        public byte this[int offset]
        {
            get
            {
#if DEBUG
                if (_address == 0)
                {
                    throw new InvalidOperationException("The pointer being indexed is a null pointer.");
                }
#endif
                return _memory[_address + offset];
            }
            set
            {
#if DEBUG
                if (_address == 0)
                {
                    throw new InvalidOperationException("The pointer being indexed is a null pointer.");
                }
#endif
                _memory[_address + offset] = value;
            }
        }

        /// <summary>
        /// Allow a <see cref="MemoryNode"/> to be implicitly converted to a <see cref="Pointer"/>.
        /// </summary>
        /// <param name="memoryNode"></param>
        /// <returns></returns>
        public static implicit operator Pointer(MemoryNode memoryNode)
        {
            return new Pointer(memoryNode._address, memoryNode._memory);
        }

        /// <summary>
        /// Allow a <see cref="Model.PpmContext"/> to be implicitly converted to a <see cref="Pointer"/>.
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static implicit operator Pointer(Model.PpmContext context)
        {
            return new Pointer(context._address, context._memory);
        }

        /// <summary>
        /// Allow a <see cref="PpmState"/> to be implicitly converted to a <see cref="Pointer"/>.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        public static implicit operator Pointer(PpmState state)
        {
            return new Pointer(state._address, state._memory);
        }

        /// <summary>
        /// Increase the address of a pointer by the given number of bytes.
        /// </summary>
        /// <param name="pointer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static Pointer operator +(Pointer pointer, int offset)
        {
#if DEBUG
            if (pointer._address == 0)
            {
                throw new InvalidOperationException("The pointer is a null pointer.");
            }
#endif
            pointer._address = (uint)(pointer._address + offset);
            return pointer;
        }

        /// <summary>
        /// Increase the address of a pointer by the given number of bytes.
        /// </summary>
        /// <param name="pointer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static Pointer operator +(Pointer pointer, uint offset)
        {
#if DEBUG
            if (pointer._address == 0)
            {
                throw new InvalidOperationException("The pointer is a null pointer.");
            }
#endif
            pointer._address += offset;
            return pointer;
        }

        /// <summary>
        /// Increment the address of a pointer.
        /// </summary>
        /// <param name="pointer"></param>
        /// <returns></returns>
        public static Pointer operator ++(Pointer pointer)
        {
#if DEBUG
            if (pointer._address == 0)
            {
                throw new InvalidOperationException("The pointer being incremented is a null pointer.");
            }
#endif
            pointer._address++;
            return pointer;
        }

        /// <summary>
        /// Decrease the address of a pointer by the given number of bytes.
        /// </summary>
        /// <param name="pointer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static Pointer operator -(Pointer pointer, int offset)
        {
#if DEBUG
            if (pointer._address == 0)
            {
                throw new InvalidOperationException("The pointer is a null pointer.");
            }
#endif
            pointer._address = (uint)(pointer._address - offset);
            return pointer;
        }

        /// <summary>
        /// Decrease the address of a pointer by the given number of bytes.
        /// </summary>
        /// <param name="pointer"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static Pointer operator -(Pointer pointer, uint offset)
        {
#if DEBUG
            if (pointer._address == 0)
            {
                throw new InvalidOperationException("The pointer is a null pointer.");
            }
#endif
            pointer._address -= offset;
            return pointer;
        }

        /// <summary>
        /// Decrement the address of a pointer.
        /// </summary>
        /// <param name="pointer"></param>
        /// <returns></returns>
        public static Pointer operator --(Pointer pointer)
        {
#if DEBUG
            if (pointer._address == 0)
            {
                throw new InvalidOperationException("The pointer being decremented is a null pointer.");
            }
#endif
            pointer._address--;
            return pointer;
        }

        /// <summary>
        /// Subtract two pointers.
        /// </summary>
        /// <param name="pointer1"></param>
        /// <param name="pointer2"></param>
        /// <returns>The number of bytes between the two pointers.</returns>
        public static uint operator -(Pointer pointer1, Pointer pointer2)
        {
#if DEBUG
            if (pointer1._address == 0)
            {
                throw new InvalidOperationException(
                                                    "The pointer to the left of the subtraction operator is a null pointer.");
            }
            if (pointer2._address == 0)
            {
                throw new InvalidOperationException(
                                                    "The pointer to the right of the subtraction operator is a null pointer.");
            }
#endif
            return pointer1._address - pointer2._address;
        }

        /// <summary>
        /// Compare pointers.
        /// </summary>
        /// <param name="pointer1"></param>
        /// <param name="pointer2"></param>
        /// <returns></returns>
        public static bool operator <(Pointer pointer1, Pointer pointer2)
        {
#if DEBUG
            if (pointer1._address == 0)
            {
                throw new InvalidOperationException(
                                                    "The pointer to the left of the less than operator is a null pointer.");
            }
            if (pointer2._address == 0)
            {
                throw new InvalidOperationException(
                                                    "The pointer to the right of the less than operator is a null pointer.");
            }
#endif
            return pointer1._address < pointer2._address;
        }

        /// <summary>
        /// Compare two pointers.
        /// </summary>
        /// <param name="pointer1"></param>
        /// <param name="pointer2"></param>
        /// <returns></returns>
        public static bool operator <=(Pointer pointer1, Pointer pointer2)
        {
#if DEBUG
            if (pointer1._address == 0)
            {
                throw new InvalidOperationException(
                                                    "The pointer to the left of the less than or equal to operator is a null pointer.");
            }
            if (pointer2._address == 0)
            {
                throw new InvalidOperationException(
                                                    "The pointer to the right of the less than or equal to operator is a null pointer.");
            }
#endif
            return pointer1._address <= pointer2._address;
        }

        /// <summary>
        /// Compare two pointers.
        /// </summary>
        /// <param name="pointer1"></param>
        /// <param name="pointer2"></param>
        /// <returns></returns>
        public static bool operator >(Pointer pointer1, Pointer pointer2)
        {
#if DEBUG
            if (pointer1._address == 0)
            {
                throw new InvalidOperationException(
                                                    "The pointer to the left of the greater than operator is a null pointer.");
            }
            if (pointer2._address == 0)
            {
                throw new InvalidOperationException(
                                                    "The pointer to the right of the greater than operator is a null pointer.");
            }
#endif
            return pointer1._address > pointer2._address;
        }

        /// <summary>
        /// Compare two pointers.
        /// </summary>
        /// <param name="pointer1"></param>
        /// <param name="pointer2"></param>
        /// <returns></returns>
        public static bool operator >=(Pointer pointer1, Pointer pointer2)
        {
#if DEBUG
            if (pointer1._address == 0)
            {
                throw new InvalidOperationException(
                                                    "The pointer to the left of the greater than or equal to operator is a null pointer.");
            }
            if (pointer2._address == 0)
            {
                throw new InvalidOperationException(
                                                    "The pointer to the right of the greater than or equal to operator is a null pointer.");
            }
#endif
            return pointer1._address >= pointer2._address;
        }

        /// <summary>
        /// Compare two pointers.
        /// </summary>
        /// <param name="pointer1"></param>
        /// <param name="pointer2"></param>
        /// <returns></returns>
        public static bool operator ==(Pointer pointer1, Pointer pointer2)
        {
            return pointer1._address == pointer2._address;
        }

        /// <summary>
        /// Compare two pointers.
        /// </summary>
        /// <param name="pointer1"></param>
        /// <param name="pointer2"></param>
        /// <returns></returns>
        public static bool operator !=(Pointer pointer1, Pointer pointer2)
        {
            return pointer1._address != pointer2._address;
        }

        /// <summary>
        /// Indicates whether this instance and a specified object are equal.
        /// </summary>
        /// <returns>true if obj and this instance are the same type and represent the same value; otherwise, false.</returns>
        /// <param name="obj">Another object to compare to.</param>
        public override bool Equals(object obj)
        {
            if (obj is Pointer)
            {
                Pointer pointer = (Pointer)obj;
                return pointer._address == _address;
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