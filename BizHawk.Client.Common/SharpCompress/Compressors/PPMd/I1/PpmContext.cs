namespace SharpCompress.Compressors.PPMd.I1
{
    /// <summary>
    /// The PPM context structure.  This is tightly coupled with <see cref="Model"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This must be a structure rather than a class because several places in the associated code assume that
    /// <see cref="PpmContext"/> is a value type (meaning that assignment creates a completely new copy of
    /// the instance rather than just copying a reference to the same instance).
    /// </para>
    /// </remarks>
    internal partial class Model
    {
        /// <summary>
        /// The structure which represents the current PPM context.  This is 12 bytes in size.
        /// </summary>
        internal struct PpmContext
        {
            public uint _address;
            public byte[] _memory;
            public static readonly PpmContext ZERO = new PpmContext(0, null);
            public const int SIZE = 12;

            /// <summary>
            /// Initializes a new instance of the <see cref="PpmContext"/> structure.
            /// </summary>
            public PpmContext(uint address, byte[] memory)
            {
                _address = address;
                _memory = memory;
            }

            /// <summary>
            /// Gets or sets the number statistics.
            /// </summary>
            public byte NumberStatistics { get => _memory[_address]; set => _memory[_address] = value; }

            /// <summary>
            /// Gets or sets the flags.
            /// </summary>
            public byte Flags { get => _memory[_address + 1]; set => _memory[_address + 1] = value; }

            /// <summary>
            /// Gets or sets the summary frequency.
            /// </summary>
            public ushort SummaryFrequency
            {
                get => (ushort)(_memory[_address + 2] | _memory[_address + 3] << 8);
                set
                {
                    _memory[_address + 2] = (byte)value;
                    _memory[_address + 3] = (byte)(value >> 8);
                }
            }

            /// <summary>
            /// Gets or sets the statistics.
            /// </summary>
            public PpmState Statistics
            {
                get => new PpmState(
                                    _memory[_address + 4] | ((uint)_memory[_address + 5]) << 8 |
                                    ((uint)_memory[_address + 6]) << 16 | ((uint)_memory[_address + 7]) << 24, _memory);
                set
                {
                    _memory[_address + 4] = (byte)value._address;
                    _memory[_address + 5] = (byte)(value._address >> 8);
                    _memory[_address + 6] = (byte)(value._address >> 16);
                    _memory[_address + 7] = (byte)(value._address >> 24);
                }
            }

            /// <summary>
            /// Gets or sets the suffix.
            /// </summary>
            public PpmContext Suffix
            {
                get => new PpmContext(
                                      _memory[_address + 8] | ((uint)_memory[_address + 9]) << 8 |
                                      ((uint)_memory[_address + 10]) << 16 | ((uint)_memory[_address + 11]) << 24, _memory);
                set
                {
                    _memory[_address + 8] = (byte)value._address;
                    _memory[_address + 9] = (byte)(value._address >> 8);
                    _memory[_address + 10] = (byte)(value._address >> 16);
                    _memory[_address + 11] = (byte)(value._address >> 24);
                }
            }

            /// <summary>
            /// The first PPM state associated with the PPM context.
            /// </summary>
            /// <remarks>
            /// <para>
            /// The first PPM state overlaps this PPM context instance (the context.SummaryFrequency and context.Statistics members
            /// of PpmContext use 6 bytes and so can therefore fit into the space used by the Symbol, Frequency and
            /// Successor members of PpmState, since they also add up to 6 bytes).
            /// </para>
            /// <para>
            /// PpmContext (context.SummaryFrequency and context.Statistics use 6 bytes)
            ///     1 context.NumberStatistics
            ///     1 context.Flags
            ///     2 context.SummaryFrequency
            ///     4 context.Statistics (pointer to PpmState)
            ///     4 context.Suffix (pointer to PpmContext)
            /// </para>
            /// <para>
            /// PpmState (total of 6 bytes)
            ///     1 Symbol
            ///     1 Frequency
            ///     4 Successor (pointer to PpmContext)
            /// </para>
            /// </remarks>
            /// <returns></returns>
            public PpmState FirstState => new PpmState(_address + 2, _memory);

            /// <summary>
            /// Gets or sets the symbol of the first PPM state.  This is provided for convenience.  The same
            /// information can be obtained using the Symbol property on the PPM state provided by the
            /// <see cref="FirstState"/> property.
            /// </summary>
            public byte FirstStateSymbol { get => _memory[_address + 2]; set => _memory[_address + 2] = value; }

            /// <summary>
            /// Gets or sets the frequency of the first PPM state.  This is provided for convenience.  The same
            /// information can be obtained using the Frequency property on the PPM state provided by the
            ///context.FirstState property.
            /// </summary>
            public byte FirstStateFrequency { get => _memory[_address + 3]; set => _memory[_address + 3] = value; }

            /// <summary>
            /// Gets or sets the successor of the first PPM state.  This is provided for convenience.  The same
            /// information can be obtained using the Successor property on the PPM state provided by the
            /// </summary>
            public PpmContext FirstStateSuccessor
            {
                get => new PpmContext(
                                      _memory[_address + 4] | ((uint)_memory[_address + 5]) << 8 |
                                      ((uint)_memory[_address + 6]) << 16 | ((uint)_memory[_address + 7]) << 24, _memory);
                set
                {
                    _memory[_address + 4] = (byte)value._address;
                    _memory[_address + 5] = (byte)(value._address >> 8);
                    _memory[_address + 6] = (byte)(value._address >> 16);
                    _memory[_address + 7] = (byte)(value._address >> 24);
                }
            }

            /// <summary>
            /// Allow a pointer to be implicitly converted to a PPM context.
            /// </summary>
            /// <param name="pointer"></param>
            /// <returns></returns>
            public static implicit operator PpmContext(Pointer pointer)
            {
                return new PpmContext(pointer._address, pointer._memory);
            }

            /// <summary>
            /// Allow pointer-like addition on a PPM context.
            /// </summary>
            /// <param name="context"></param>
            /// <param name="offset"></param>
            /// <returns></returns>
            public static PpmContext operator +(PpmContext context, int offset)
            {
                context._address = (uint)(context._address + offset * SIZE);
                return context;
            }

            /// <summary>
            /// Allow pointer-like subtraction on a PPM context.
            /// </summary>
            /// <param name="context"></param>
            /// <param name="offset"></param>
            /// <returns></returns>
            public static PpmContext operator -(PpmContext context, int offset)
            {
                context._address = (uint)(context._address - offset * SIZE);
                return context;
            }

            /// <summary>
            /// Compare two PPM contexts.
            /// </summary>
            /// <param name="context1"></param>
            /// <param name="context2"></param>
            /// <returns></returns>
            public static bool operator <=(PpmContext context1, PpmContext context2)
            {
                return context1._address <= context2._address;
            }

            /// <summary>
            /// Compare two PPM contexts.
            /// </summary>
            /// <param name="context1"></param>
            /// <param name="context2"></param>
            /// <returns></returns>
            public static bool operator >=(PpmContext context1, PpmContext context2)
            {
                return context1._address >= context2._address;
            }

            /// <summary>
            /// Compare two PPM contexts.
            /// </summary>
            /// <param name="context1"></param>
            /// <param name="context2"></param>
            /// <returns></returns>
            public static bool operator ==(PpmContext context1, PpmContext context2)
            {
                return context1._address == context2._address;
            }

            /// <summary>
            /// Compare two PPM contexts.
            /// </summary>
            /// <param name="context1"></param>
            /// <param name="context2"></param>
            /// <returns></returns>
            public static bool operator !=(PpmContext context1, PpmContext context2)
            {
                return context1._address != context2._address;
            }

            /// <summary>
            /// Indicates whether this instance and a specified object are equal.
            /// </summary>
            /// <returns>true if obj and this instance are the same type and represent the same value; otherwise, false.</returns>
            /// <param name="obj">Another object to compare to.</param>
            public override bool Equals(object obj)
            {
                if (obj is PpmContext)
                {
                    PpmContext context = (PpmContext)obj;
                    return context._address == _address;
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

        private void EncodeBinarySymbol(int symbol, PpmContext context)
        {
            PpmState state = context.FirstState;
            int index1 = _probabilities[state.Frequency - 1];
            int index2 = _numberStatisticsToBinarySummaryIndex[context.Suffix.NumberStatistics] + _previousSuccess +
                         context.Flags + ((_runLength >> 26) & 0x20);

            if (state.Symbol == symbol)
            {
                _foundState = state;
                state.Frequency += (byte)((state.Frequency < 196) ? 1 : 0);
                _coder._lowCount = 0;
                _coder._highCount = _binarySummary[index1, index2];
                _binarySummary[index1, index2] +=
                    (ushort)(INTERVAL - Mean(_binarySummary[index1, index2], PERIOD_BIT_COUNT, 2));
                _previousSuccess = 1;
                _runLength++;
            }
            else
            {
                _coder._lowCount = _binarySummary[index1, index2];
                _binarySummary[index1, index2] -= (ushort)Mean(_binarySummary[index1, index2], PERIOD_BIT_COUNT, 2);
                _coder._highCount = BINARY_SCALE;
                _initialEscape = EXPONENTIAL_ESCAPES[_binarySummary[index1, index2] >> 10];
                _characterMask[state.Symbol] = _escapeCount;
                _previousSuccess = 0;
                _numberMasked = 0;
                _foundState = PpmState.ZERO;
            }
        }

        private void EncodeSymbol1(int symbol, PpmContext context)
        {
            uint lowCount;
            uint index = context.Statistics.Symbol;
            PpmState state = context.Statistics;
            _coder._scale = context.SummaryFrequency;
            if (index == symbol)
            {
                _coder._highCount = state.Frequency;
                _previousSuccess = (byte)((2 * _coder._highCount >= _coder._scale) ? 1 : 0);
                _foundState = state;
                _foundState.Frequency += 4;
                context.SummaryFrequency += 4;
                _runLength += _previousSuccess;
                if (state.Frequency > MAXIMUM_FREQUENCY)
                {
                    Rescale(context);
                }
                _coder._lowCount = 0;
                return;
            }

            lowCount = state.Frequency;
            index = context.NumberStatistics;
            _previousSuccess = 0;
            while ((++state).Symbol != symbol)
            {
                lowCount += state.Frequency;
                if (--index == 0)
                {
                    _coder._lowCount = lowCount;
                    _characterMask[state.Symbol] = _escapeCount;
                    _numberMasked = context.NumberStatistics;
                    index = context.NumberStatistics;
                    _foundState = PpmState.ZERO;
                    do
                    {
                        _characterMask[(--state).Symbol] = _escapeCount;
                    }
                    while (--index != 0);
                    _coder._highCount = _coder._scale;
                    return;
                }
            }
            _coder._highCount = (_coder._lowCount = lowCount) + state.Frequency;
            Update1(state, context);
        }

        private void EncodeSymbol2(int symbol, PpmContext context)
        {
            See2Context see2Context = MakeEscapeFrequency(context);
            uint currentSymbol;
            uint lowCount = 0;
            uint index = (uint)(context.NumberStatistics - _numberMasked);
            PpmState state = context.Statistics - 1;

            do
            {
                do
                {
                    currentSymbol = state[1].Symbol;
                    state++;
                }
                while (_characterMask[currentSymbol] == _escapeCount);
                _characterMask[currentSymbol] = _escapeCount;
                if (currentSymbol == symbol)
                {
                    goto SymbolFound;
                }
                lowCount += state.Frequency;
            }
            while (--index != 0);

            _coder._lowCount = lowCount;
            _coder._scale += _coder._lowCount;
            _coder._highCount = _coder._scale;
            see2Context._summary += (ushort)_coder._scale;
            _numberMasked = context.NumberStatistics;
            return;

            SymbolFound:
            _coder._lowCount = lowCount;
            lowCount += state.Frequency;
            _coder._highCount = lowCount;
            for (PpmState p1 = state; --index != 0;)
            {
                do
                {
                    currentSymbol = p1[1].Symbol;
                    p1++;
                }
                while (_characterMask[currentSymbol] == _escapeCount);
                lowCount += p1.Frequency;
            }
            _coder._scale += lowCount;
            see2Context.Update();
            Update2(state, context);
        }

        private void DecodeBinarySymbol(PpmContext context)
        {
            PpmState state = context.FirstState;
            int index1 = _probabilities[state.Frequency - 1];
            int index2 = _numberStatisticsToBinarySummaryIndex[context.Suffix.NumberStatistics] + _previousSuccess +
                         context.Flags + ((_runLength >> 26) & 0x20);

            if (_coder.RangeGetCurrentShiftCount(TOTAL_BIT_COUNT) < _binarySummary[index1, index2])
            {
                _foundState = state;
                state.Frequency += (byte)((state.Frequency < 196) ? 1 : 0);
                _coder._lowCount = 0;
                _coder._highCount = _binarySummary[index1, index2];
                _binarySummary[index1, index2] +=
                    (ushort)(INTERVAL - Mean(_binarySummary[index1, index2], PERIOD_BIT_COUNT, 2));
                _previousSuccess = 1;
                _runLength++;
            }
            else
            {
                _coder._lowCount = _binarySummary[index1, index2];
                _binarySummary[index1, index2] -= (ushort)Mean(_binarySummary[index1, index2], PERIOD_BIT_COUNT, 2);
                _coder._highCount = BINARY_SCALE;
                _initialEscape = EXPONENTIAL_ESCAPES[_binarySummary[index1, index2] >> 10];
                _characterMask[state.Symbol] = _escapeCount;
                _previousSuccess = 0;
                _numberMasked = 0;
                _foundState = PpmState.ZERO;
            }
        }

        private void DecodeSymbol1(PpmContext context)
        {
            uint index;
            uint count;
            uint highCount = context.Statistics.Frequency;
            PpmState state = context.Statistics;
            _coder._scale = context.SummaryFrequency;

            count = _coder.RangeGetCurrentCount();
            if (count < highCount)
            {
                _coder._highCount = highCount;
                _previousSuccess = (byte)((2 * _coder._highCount >= _coder._scale) ? 1 : 0);
                _foundState = state;
                highCount += 4;
                _foundState.Frequency = (byte)highCount;
                context.SummaryFrequency += 4;
                _runLength += _previousSuccess;
                if (highCount > MAXIMUM_FREQUENCY)
                {
                    Rescale(context);
                }
                _coder._lowCount = 0;
                return;
            }

            index = context.NumberStatistics;
            _previousSuccess = 0;
            while ((highCount += (++state).Frequency) <= count)
            {
                if (--index == 0)
                {
                    _coder._lowCount = highCount;
                    _characterMask[state.Symbol] = _escapeCount;
                    _numberMasked = context.NumberStatistics;
                    index = context.NumberStatistics;
                    _foundState = PpmState.ZERO;
                    do
                    {
                        _characterMask[(--state).Symbol] = _escapeCount;
                    }
                    while (--index != 0);
                    _coder._highCount = _coder._scale;
                    return;
                }
            }
            _coder._highCount = highCount;
            _coder._lowCount = _coder._highCount - state.Frequency;
            Update1(state, context);
        }

        private void DecodeSymbol2(PpmContext context)
        {
            See2Context see2Context = MakeEscapeFrequency(context);
            uint currentSymbol;
            uint count;
            uint highCount = 0;
            uint index = (uint)(context.NumberStatistics - _numberMasked);
            uint stateIndex = 0;
            PpmState state = context.Statistics - 1;

            do
            {
                do
                {
                    currentSymbol = state[1].Symbol;
                    state++;
                }
                while (_characterMask[currentSymbol] == _escapeCount);
                highCount += state.Frequency;
                _decodeStates[stateIndex++] = state;

                // note that decodeStates is a static array that is re-used on each call to this method (for performance reasons)
            }
            while (--index != 0);

            _coder._scale += highCount;
            count = _coder.RangeGetCurrentCount();
            stateIndex = 0;
            state = _decodeStates[stateIndex];
            if (count < highCount)
            {
                highCount = 0;
                while ((highCount += state.Frequency) <= count)
                {
                    state = _decodeStates[++stateIndex];
                }
                _coder._highCount = highCount;
                _coder._lowCount = _coder._highCount - state.Frequency;
                see2Context.Update();
                Update2(state, context);
            }
            else
            {
                _coder._lowCount = highCount;
                _coder._highCount = _coder._scale;
                index = (uint)(context.NumberStatistics - _numberMasked);
                _numberMasked = context.NumberStatistics;
                do
                {
                    _characterMask[_decodeStates[stateIndex].Symbol] = _escapeCount;
                    stateIndex++;
                }
                while (--index != 0);
                see2Context._summary += (ushort)_coder._scale;
            }
        }

        private void Update1(PpmState state, PpmContext context)
        {
            _foundState = state;
            _foundState.Frequency += 4;
            context.SummaryFrequency += 4;
            if (state[0].Frequency > state[-1].Frequency)
            {
                Swap(state[0], state[-1]);
                _foundState = --state;
                if (state.Frequency > MAXIMUM_FREQUENCY)
                {
                    Rescale(context);
                }
            }
        }

        private void Update2(PpmState state, PpmContext context)
        {
            _foundState = state;
            _foundState.Frequency += 4;
            context.SummaryFrequency += 4;
            if (state.Frequency > MAXIMUM_FREQUENCY)
            {
                Rescale(context);
            }
            _escapeCount++;
            _runLength = _initialRunLength;
        }

        private See2Context MakeEscapeFrequency(PpmContext context)
        {
            uint numberStatistics = (uint)2 * context.NumberStatistics;
            See2Context see2Context;

            if (context.NumberStatistics != 0xff)
            {
                // Note that context.Flags is always in the range 0 .. 28 (this ensures that the index used for the second
                // dimension of the see2Contexts array is always in the range 0 .. 31).

                numberStatistics = context.Suffix.NumberStatistics;
                int index1 = _probabilities[context.NumberStatistics + 2] - 3;
                int index2 = ((context.SummaryFrequency > 11 * (context.NumberStatistics + 1)) ? 1 : 0) +
                             ((2 * context.NumberStatistics < numberStatistics + _numberMasked) ? 2 : 0) + context.Flags;
                see2Context = _see2Contexts[index1, index2];
                _coder._scale = see2Context.Mean();
            }
            else
            {
                see2Context = _emptySee2Context;
                _coder._scale = 1;
            }

            return see2Context;
        }

        private void Rescale(PpmContext context)
        {
            uint oldUnitCount;
            int adder;
            uint escapeFrequency;
            uint index = context.NumberStatistics;

            byte localSymbol;
            byte localFrequency;
            PpmContext localSuccessor;
            PpmState p1;
            PpmState state;

            for (state = _foundState; state != context.Statistics; state--)
            {
                Swap(state[0], state[-1]);
            }

            state.Frequency += 4;
            context.SummaryFrequency += 4;
            escapeFrequency = (uint)(context.SummaryFrequency - state.Frequency);
            adder = (_orderFall != 0 || _method > ModelRestorationMethod.Freeze) ? 1 : 0;
            state.Frequency = (byte)((state.Frequency + adder) >> 1);
            context.SummaryFrequency = state.Frequency;

            do
            {
                escapeFrequency -= (++state).Frequency;
                state.Frequency = (byte)((state.Frequency + adder) >> 1);
                context.SummaryFrequency += state.Frequency;
                if (state[0].Frequency > state[-1].Frequency)
                {
                    p1 = state;
                    localSymbol = p1.Symbol;
                    localFrequency = p1.Frequency;
                    localSuccessor = p1.Successor;
                    do
                    {
                        Copy(p1[0], p1[-1]);
                    }
                    while (localFrequency > (--p1)[-1].Frequency);
                    p1.Symbol = localSymbol;
                    p1.Frequency = localFrequency;
                    p1.Successor = localSuccessor;
                }
            }
            while (--index != 0);

            if (state.Frequency == 0)
            {
                do
                {
                    index++;
                }
                while ((--state).Frequency == 0);

                escapeFrequency += index;
                oldUnitCount = (uint)((context.NumberStatistics + 2) >> 1);
                context.NumberStatistics -= (byte)index;
                if (context.NumberStatistics == 0)
                {
                    localSymbol = context.Statistics.Symbol;
                    localFrequency = context.Statistics.Frequency;
                    localSuccessor = context.Statistics.Successor;
                    localFrequency = (byte)((2 * localFrequency + escapeFrequency - 1) / escapeFrequency);
                    if (localFrequency > MAXIMUM_FREQUENCY / 3)
                    {
                        localFrequency = (byte)(MAXIMUM_FREQUENCY / 3);
                    }
                    _allocator.FreeUnits(context.Statistics, oldUnitCount);
                    context.FirstStateSymbol = localSymbol;
                    context.FirstStateFrequency = localFrequency;
                    context.FirstStateSuccessor = localSuccessor;
                    context.Flags = (byte)((context.Flags & 0x10) + ((localSymbol >= 0x40) ? 0x08 : 0x00));
                    _foundState = context.FirstState;
                    return;
                }

                context.Statistics = _allocator.ShrinkUnits(context.Statistics, oldUnitCount,
                                                           (uint)((context.NumberStatistics + 2) >> 1));
                context.Flags &= 0xf7;
                index = context.NumberStatistics;
                state = context.Statistics;
                context.Flags |= (byte)((state.Symbol >= 0x40) ? 0x08 : 0x00);
                do
                {
                    context.Flags |= (byte)(((++state).Symbol >= 0x40) ? 0x08 : 0x00);
                }
                while (--index != 0);
            }

            escapeFrequency -= (escapeFrequency >> 1);
            context.SummaryFrequency += (ushort)escapeFrequency;
            context.Flags |= 0x04;
            _foundState = context.Statistics;
        }

        private void Refresh(uint oldUnitCount, bool scale, PpmContext context)
        {
            int index = context.NumberStatistics;
            int escapeFrequency;
            int scaleValue = (scale ? 1 : 0);

            context.Statistics = _allocator.ShrinkUnits(context.Statistics, oldUnitCount, (uint)((index + 2) >> 1));
            PpmState statistics = context.Statistics;
            context.Flags =
                (byte)((context.Flags & (0x10 + (scale ? 0x04 : 0x00))) + ((statistics.Symbol >= 0x40) ? 0x08 : 0x00));
            escapeFrequency = context.SummaryFrequency - statistics.Frequency;
            statistics.Frequency = (byte)((statistics.Frequency + scaleValue) >> scaleValue);
            context.SummaryFrequency = statistics.Frequency;

            do
            {
                escapeFrequency -= (++statistics).Frequency;
                statistics.Frequency = (byte)((statistics.Frequency + scaleValue) >> scaleValue);
                context.SummaryFrequency += statistics.Frequency;
                context.Flags |= (byte)((statistics.Symbol >= 0x40) ? 0x08 : 0x00);
            }
            while (--index != 0);

            escapeFrequency = (escapeFrequency + scaleValue) >> scaleValue;
            context.SummaryFrequency += (ushort)escapeFrequency;
        }

        private PpmContext CutOff(int order, PpmContext context)
        {
            int index;
            PpmState state;

            if (context.NumberStatistics == 0)
            {
                state = context.FirstState;
                if ((Pointer)state.Successor >= _allocator._baseUnit)
                {
                    if (order < _modelOrder)
                    {
                        state.Successor = CutOff(order + 1, state.Successor);
                    }
                    else
                    {
                        state.Successor = PpmContext.ZERO;
                    }

                    if (state.Successor == PpmContext.ZERO && order > ORDER_BOUND)
                    {
                        _allocator.SpecialFreeUnits(context);
                        return PpmContext.ZERO;
                    }

                    return context;
                }
                _allocator.SpecialFreeUnits(context);
                return PpmContext.ZERO;
            }

            uint unitCount = (uint)((context.NumberStatistics + 2) >> 1);
            context.Statistics = _allocator.MoveUnitsUp(context.Statistics, unitCount);
            index = context.NumberStatistics;
            for (state = context.Statistics + index; state >= context.Statistics; state--)
            {
                if (state.Successor < _allocator._baseUnit)
                {
                    state.Successor = PpmContext.ZERO;
                    Swap(state, context.Statistics[index--]);
                }
                else if (order < _modelOrder)
                {
                    state.Successor = CutOff(order + 1, state.Successor);
                }
                else
                {
                    state.Successor = PpmContext.ZERO;
                }
            }

            if (index != context.NumberStatistics && order != 0)
            {
                context.NumberStatistics = (byte)index;
                state = context.Statistics;
                if (index < 0)
                {
                    _allocator.FreeUnits(state, unitCount);
                    _allocator.SpecialFreeUnits(context);
                    return PpmContext.ZERO;
                }
                if (index == 0)
                {
                    context.Flags = (byte)((context.Flags & 0x10) + ((state.Symbol >= 0x40) ? 0x08 : 0x00));
                    Copy(context.FirstState, state);
                    _allocator.FreeUnits(state, unitCount);
                    context.FirstStateFrequency = (byte)((context.FirstStateFrequency + 11) >> 3);
                }
                else
                {
                    Refresh(unitCount, context.SummaryFrequency > 16 * index, context);
                }
            }

            return context;
        }

        private PpmContext RemoveBinaryContexts(int order, PpmContext context)
        {
            if (context.NumberStatistics == 0)
            {
                PpmState state = context.FirstState;
                if ((Pointer)state.Successor >= _allocator._baseUnit && order < _modelOrder)
                {
                    state.Successor = RemoveBinaryContexts(order + 1, state.Successor);
                }
                else
                {
                    state.Successor = PpmContext.ZERO;
                }
                if ((state.Successor == PpmContext.ZERO) &&
                    (context.Suffix.NumberStatistics == 0 || context.Suffix.Flags == 0xff))
                {
                    _allocator.FreeUnits(context, 1);
                    return PpmContext.ZERO;
                }
                return context;
            }

            for (PpmState state = context.Statistics + context.NumberStatistics; state >= context.Statistics; state--)
            {
                if ((Pointer)state.Successor >= _allocator._baseUnit && order < _modelOrder)
                {
                    state.Successor = RemoveBinaryContexts(order + 1, state.Successor);
                }
                else
                {
                    state.Successor = PpmContext.ZERO;
                }
            }

            return context;
        }
    }
}