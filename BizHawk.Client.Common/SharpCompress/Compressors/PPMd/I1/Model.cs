#region Using

using System;
using System.IO;

#endregion

// This is a port of Dmitry Shkarin's PPMd Variant I Revision 1.
// Ported by Michael Bone (mjbone03@yahoo.com.au).

namespace SharpCompress.Compressors.PPMd.I1
{
    /// <summary>
    /// The model.
    /// </summary>
    internal partial class Model
    {
        public const uint SIGNATURE = 0x84acaf8fU;
        public const char VARIANT = 'I';
        public const int MAXIMUM_ORDER = 16; // maximum allowed model order

        private const byte UPPER_FREQUENCY = 5;
        private const byte INTERVAL_BIT_COUNT = 7;
        private const byte PERIOD_BIT_COUNT = 7;
        private const byte TOTAL_BIT_COUNT = INTERVAL_BIT_COUNT + PERIOD_BIT_COUNT;
        private const uint INTERVAL = 1 << INTERVAL_BIT_COUNT;
        private const uint BINARY_SCALE = 1 << TOTAL_BIT_COUNT;
        private const uint MAXIMUM_FREQUENCY = 124;
        private const uint ORDER_BOUND = 9;

        private readonly See2Context[,] _see2Contexts;
        private readonly See2Context _emptySee2Context;
        private PpmContext _maximumContext;
        private readonly ushort[,] _binarySummary = new ushort[25, 64]; // binary SEE-contexts
        private readonly byte[] _numberStatisticsToBinarySummaryIndex = new byte[256];
        private readonly byte[] _probabilities = new byte[260];
        private readonly byte[] _characterMask = new byte[256];
        private byte _escapeCount;
        private int _modelOrder;
        private int _orderFall;
        private int _initialEscape;
        private int _initialRunLength;
        private int _runLength;
        private byte _previousSuccess;
        private byte _numberMasked;
        private ModelRestorationMethod _method;
        private PpmState _foundState; // found next state transition

        private Allocator _allocator;
        private Coder _coder;
        private PpmContext _minimumContext;
        private byte _numberStatistics;
        private readonly PpmState[] _decodeStates = new PpmState[256];

        private static readonly ushort[] INITIAL_BINARY_ESCAPES =
        {
            0x3CDD, 0x1F3F, 0x59BF, 0x48F3, 0x64A1, 0x5ABC, 0x6632,
            0x6051
        };

        private static readonly byte[] EXPONENTIAL_ESCAPES = {25, 14, 9, 7, 5, 5, 4, 4, 4, 3, 3, 3, 2, 2, 2, 2};

        #region Public Methods

        public Model()
        {
            // Construct the conversion table for number statistics.  Initially it will contain the following values.
            //
            // 0 2 4 4 4 4 4 4 4 4 4 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6
            // 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6
            // 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6
            // 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6
            // 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6
            // 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6
            // 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6
            // 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6 6

            _numberStatisticsToBinarySummaryIndex[0] = 2 * 0;
            _numberStatisticsToBinarySummaryIndex[1] = 2 * 1;
            for (int index = 2; index < 11; index++)
            {
                _numberStatisticsToBinarySummaryIndex[index] = 2 * 2;
            }
            for (int index = 11; index < 256; index++)
            {
                _numberStatisticsToBinarySummaryIndex[index] = 2 * 3;
            }

            // Construct the probability table.  Initially it will contain the following values (depending on the value of
            // the upper frequency).
            //
            // 00 01 02 03 04 05 06 06 07 07 07 08 08 08 08 09 09 09 09 09 10 10 10 10 10 10 11 11 11 11 11 11
            // 11 12 12 12 12 12 12 12 12 13 13 13 13 13 13 13 13 13 14 14 14 14 14 14 14 14 14 14 15 15 15 15
            // 15 15 15 15 15 15 15 16 16 16 16 16 16 16 16 16 16 16 16 17 17 17 17 17 17 17 17 17 17 17 17 17
            // 18 18 18 18 18 18 18 18 18 18 18 18 18 18 19 19 19 19 19 19 19 19 19 19 19 19 19 19 19 20 20 20
            // 20 20 20 20 20 20 20 20 20 20 20 20 20 21 21 21 21 21 21 21 21 21 21 21 21 21 21 21 21 21 22 22
            // 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 22 23 23 23 23 23 23 23 23 23 23 23 23 23 23 23 23
            // 23 23 23 24 24 24 24 24 24 24 24 24 24 24 24 24 24 24 24 24 24 24 24 25 25 25 25 25 25 25 25 25
            // 25 25 25 25 25 25 25 25 25 25 25 25 26 26 26 26 26 26 26 26 26 26 26 26 26 26 26 26 26 26 26 26
            // 26 26 27 27

            uint count = 1;
            uint step = 1;
            uint probability = UPPER_FREQUENCY;

            for (int index = 0; index < UPPER_FREQUENCY; index++)
            {
                _probabilities[index] = (byte)index;
            }

            for (int index = UPPER_FREQUENCY; index < 260; index++)
            {
                _probabilities[index] = (byte)probability;
                count--;
                if (count == 0)
                {
                    step++;
                    count = step;
                    probability++;
                }
            }

            // Create the context array.

            _see2Contexts = new See2Context[24, 32];
            for (int index1 = 0; index1 < 24; index1++)
            {
                for (int index2 = 0; index2 < 32; index2++)
                {
                    _see2Contexts[index1, index2] = new See2Context();
                }
            }

            // Set the signature (identifying the algorithm).

            _emptySee2Context = new See2Context();
            _emptySee2Context._summary = (ushort)(SIGNATURE & 0x0000ffff);
            _emptySee2Context._shift = (byte)((SIGNATURE >> 16) & 0x000000ff);
            _emptySee2Context._count = (byte)(SIGNATURE >> 24);
        }

        /// <summary>
        /// Encode (ie. compress) a given source stream, writing the encoded result to the target stream.
        /// </summary>
        public void Encode(Stream target, Stream source, PpmdProperties properties)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            EncodeStart(properties);
            EncodeBlock(target, source, true);
        }

        internal Coder EncodeStart(PpmdProperties properties)
        {
            _allocator = properties._allocator;
            _coder = new Coder();
            _coder.RangeEncoderInitialize();
            StartModel(properties.ModelOrder, properties.RestorationMethod);
            return _coder;
        }

        internal void EncodeBlock(Stream target, Stream source, bool final)
        {
            while (true)
            {
                _minimumContext = _maximumContext;
                _numberStatistics = _minimumContext.NumberStatistics;

                int c = source.ReadByte();
                if (c < 0 && !final)
                {
                    return;
                }

                if (_numberStatistics != 0)
                {
                    EncodeSymbol1(c, _minimumContext);
                    _coder.RangeEncodeSymbol();
                }
                else
                {
                    EncodeBinarySymbol(c, _minimumContext);
                    _coder.RangeShiftEncodeSymbol(TOTAL_BIT_COUNT);
                }

                while (_foundState == PpmState.ZERO)
                {
                    _coder.RangeEncoderNormalize(target);
                    do
                    {
                        _orderFall++;
                        _minimumContext = _minimumContext.Suffix;
                        if (_minimumContext == PpmContext.ZERO)
                        {
                            goto StopEncoding;
                        }
                    }
                    while (_minimumContext.NumberStatistics == _numberMasked);
                    EncodeSymbol2(c, _minimumContext);
                    _coder.RangeEncodeSymbol();
                }

                if (_orderFall == 0 && (Pointer)_foundState.Successor >= _allocator._baseUnit)
                {
                    _maximumContext = _foundState.Successor;
                }
                else
                {
                    UpdateModel(_minimumContext);
                    if (_escapeCount == 0)
                    {
                        ClearMask();
                    }
                }

                _coder.RangeEncoderNormalize(target);
            }

            StopEncoding:
            _coder.RangeEncoderFlush(target);
        }

        /// <summary>
        /// Dencode (ie. decompress) a given source stream, writing the decoded result to the target stream.
        /// </summary>
        public void Decode(Stream target, Stream source, PpmdProperties properties)
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            DecodeStart(source, properties);
            byte[] buffer = new byte[65536];
            int read;
            while ((read = DecodeBlock(source, buffer, 0, buffer.Length)) != 0)
            {
                target.Write(buffer, 0, read);
            }
        }

        internal Coder DecodeStart(Stream source, PpmdProperties properties)
        {
            _allocator = properties._allocator;
            _coder = new Coder();
            _coder.RangeDecoderInitialize(source);
            StartModel(properties.ModelOrder, properties.RestorationMethod);
            _minimumContext = _maximumContext;
            _numberStatistics = _minimumContext.NumberStatistics;
            return _coder;
        }

        internal int DecodeBlock(Stream source, byte[] buffer, int offset, int count)
        {
            if (_minimumContext == PpmContext.ZERO)
            {
                return 0;
            }

            int total = 0;
            while (total < count)
            {
                if (_numberStatistics != 0)
                {
                    DecodeSymbol1(_minimumContext);
                }
                else
                {
                    DecodeBinarySymbol(_minimumContext);
                }

                _coder.RangeRemoveSubrange();

                while (_foundState == PpmState.ZERO)
                {
                    _coder.RangeDecoderNormalize(source);
                    do
                    {
                        _orderFall++;
                        _minimumContext = _minimumContext.Suffix;
                        if (_minimumContext == PpmContext.ZERO)
                        {
                            goto StopDecoding;
                        }
                    }
                    while (_minimumContext.NumberStatistics == _numberMasked);
                    DecodeSymbol2(_minimumContext);
                    _coder.RangeRemoveSubrange();
                }

                buffer[offset] = _foundState.Symbol;
                offset++;
                total++;

                if (_orderFall == 0 && (Pointer)_foundState.Successor >= _allocator._baseUnit)
                {
                    _maximumContext = _foundState.Successor;
                }
                else
                {
                    UpdateModel(_minimumContext);
                    if (_escapeCount == 0)
                    {
                        ClearMask();
                    }
                }

                _minimumContext = _maximumContext;
                _numberStatistics = _minimumContext.NumberStatistics;
                _coder.RangeDecoderNormalize(source);
            }

            StopDecoding:
            return total;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Initialise the model (unless the model order is set to 1 in which case the model should be cleared so that
        /// the statistics are carried over, allowing "solid" mode compression).
        /// </summary>
        private void StartModel(int modelOrder, ModelRestorationMethod modelRestorationMethod)
        {
            Array.Clear(_characterMask, 0, _characterMask.Length);
            _escapeCount = 1;

            // Compress in "solid" mode if the model order value is set to 1 (this will examine the current PPM context
            // structures to determine the value of orderFall).

            if (modelOrder < 2)
            {
                _orderFall = _modelOrder;
                for (PpmContext context = _maximumContext; context.Suffix != PpmContext.ZERO; context = context.Suffix)
                {
                    _orderFall--;
                }
                return;
            }

            _modelOrder = modelOrder;
            _orderFall = modelOrder;
            _method = modelRestorationMethod;
            _allocator.Initialize();
            _initialRunLength = -((modelOrder < 12) ? modelOrder : 12) - 1;
            _runLength = _initialRunLength;

            // Allocate the context structure.

            _maximumContext = _allocator.AllocateContext();
            _maximumContext.Suffix = PpmContext.ZERO;
            _maximumContext.NumberStatistics = 255;
            _maximumContext.SummaryFrequency = (ushort)(_maximumContext.NumberStatistics + 2);
            _maximumContext.Statistics = _allocator.AllocateUnits(256 / 2);

            // allocates enough space for 256 PPM states (each is 6 bytes)

            _previousSuccess = 0;
            for (int index = 0; index < 256; index++)
            {
                PpmState state = _maximumContext.Statistics[index];
                state.Symbol = (byte)index;
                state.Frequency = 1;
                state.Successor = PpmContext.ZERO;
            }

            uint probability = 0;
            for (int index1 = 0; probability < 25; probability++)
            {
                while (_probabilities[index1] == probability)
                {
                    index1++;
                }
                for (int index2 = 0; index2 < 8; index2++)
                {
                    _binarySummary[probability, index2] =
                        (ushort)(BINARY_SCALE - INITIAL_BINARY_ESCAPES[index2] / (index1 + 1));
                }
                for (int index2 = 8; index2 < 64; index2 += 8)
                {
                    for (int index3 = 0; index3 < 8; index3++)
                    {
                        _binarySummary[probability, index2 + index3] = _binarySummary[probability, index3];
                    }
                }
            }

            probability = 0;
            for (uint index1 = 0; probability < 24; probability++)
            {
                while (_probabilities[index1 + 3] == probability + 3)
                {
                    index1++;
                }
                for (int index2 = 0; index2 < 32; index2++)
                {
                    _see2Contexts[probability, index2].Initialize(2 * index1 + 5);
                }
            }
        }

        private void UpdateModel(PpmContext minimumContext)
        {
            PpmState state = PpmState.ZERO;
            PpmContext successor;
            PpmContext currentContext = _maximumContext;
            uint numberStatistics;
            uint ns1;
            uint cf;
            uint sf;
            uint s0;
            uint foundStateFrequency = _foundState.Frequency;
            byte foundStateSymbol = _foundState.Symbol;
            byte symbol;
            byte flag;

            PpmContext foundStateSuccessor = _foundState.Successor;
            PpmContext context = minimumContext.Suffix;

            if ((foundStateFrequency < MAXIMUM_FREQUENCY / 4) && (context != PpmContext.ZERO))
            {
                if (context.NumberStatistics != 0)
                {
                    state = context.Statistics;
                    if (state.Symbol != foundStateSymbol)
                    {
                        do
                        {
                            symbol = state[1].Symbol;
                            state++;
                        }
                        while (symbol != foundStateSymbol);
                        if (state[0].Frequency >= state[-1].Frequency)
                        {
                            Swap(state[0], state[-1]);
                            state--;
                        }
                    }
                    cf = (uint)((state.Frequency < MAXIMUM_FREQUENCY - 9) ? 2 : 0);
                    state.Frequency += (byte)cf;
                    context.SummaryFrequency += (byte)cf;
                }
                else
                {
                    state = context.FirstState;
                    state.Frequency += (byte)((state.Frequency < 32) ? 1 : 0);
                }
            }

            if (_orderFall == 0 && foundStateSuccessor != PpmContext.ZERO)
            {
                _foundState.Successor = CreateSuccessors(true, state, minimumContext);
                if (_foundState.Successor == PpmContext.ZERO)
                {
                    goto RestartModel;
                }
                _maximumContext = _foundState.Successor;
                return;
            }

            _allocator._text[0] = foundStateSymbol;
            _allocator._text++;
            successor = _allocator._text;

            if (_allocator._text >= _allocator._baseUnit)
            {
                goto RestartModel;
            }

            if (foundStateSuccessor != PpmContext.ZERO)
            {
                if (foundStateSuccessor < _allocator._baseUnit)
                {
                    foundStateSuccessor = CreateSuccessors(false, state, minimumContext);
                }
            }
            else
            {
                foundStateSuccessor = ReduceOrder(state, minimumContext);
            }

            if (foundStateSuccessor == PpmContext.ZERO)
            {
                goto RestartModel;
            }

            if (--_orderFall == 0)
            {
                successor = foundStateSuccessor;
                _allocator._text -= (_maximumContext != minimumContext) ? 1 : 0;
            }
            else if (_method > ModelRestorationMethod.Freeze)
            {
                successor = foundStateSuccessor;
                _allocator._text = _allocator._heap;
                _orderFall = 0;
            }

            numberStatistics = minimumContext.NumberStatistics;
            s0 = minimumContext.SummaryFrequency - numberStatistics - foundStateFrequency;
            flag = (byte)((foundStateSymbol >= 0x40) ? 0x08 : 0x00);
            for (; currentContext != minimumContext; currentContext = currentContext.Suffix)
            {
                ns1 = currentContext.NumberStatistics;
                if (ns1 != 0)
                {
                    if ((ns1 & 1) != 0)
                    {
                        state = _allocator.ExpandUnits(currentContext.Statistics, (ns1 + 1) >> 1);
                        if (state == PpmState.ZERO)
                        {
                            goto RestartModel;
                        }
                        currentContext.Statistics = state;
                    }
                    currentContext.SummaryFrequency += (ushort)((3 * ns1 + 1 < numberStatistics) ? 1 : 0);
                }
                else
                {
                    state = _allocator.AllocateUnits(1);
                    if (state == PpmState.ZERO)
                    {
                        goto RestartModel;
                    }
                    Copy(state, currentContext.FirstState);
                    currentContext.Statistics = state;
                    if (state.Frequency < MAXIMUM_FREQUENCY / 4 - 1)
                    {
                        state.Frequency += state.Frequency;
                    }
                    else
                    {
                        state.Frequency = (byte)(MAXIMUM_FREQUENCY - 4);
                    }
                    currentContext.SummaryFrequency =
                        (ushort)(state.Frequency + _initialEscape + ((numberStatistics > 2) ? 1 : 0));
                }

                cf = (uint)(2 * foundStateFrequency * (currentContext.SummaryFrequency + 6));
                sf = s0 + currentContext.SummaryFrequency;

                if (cf < 6 * sf)
                {
                    cf = (uint)(1 + ((cf > sf) ? 1 : 0) + ((cf >= 4 * sf) ? 1 : 0));
                    currentContext.SummaryFrequency += 4;
                }
                else
                {
                    cf = (uint)(4 + ((cf > 9 * sf) ? 1 : 0) + ((cf > 12 * sf) ? 1 : 0) + ((cf > 15 * sf) ? 1 : 0));
                    currentContext.SummaryFrequency += (ushort)cf;
                }

                state = currentContext.Statistics + (++currentContext.NumberStatistics);
                state.Successor = successor;
                state.Symbol = foundStateSymbol;
                state.Frequency = (byte)cf;
                currentContext.Flags |= flag;
            }

            _maximumContext = foundStateSuccessor;
            return;

            RestartModel:
            RestoreModel(currentContext, minimumContext, foundStateSuccessor);
        }

        private PpmContext CreateSuccessors(bool skip, PpmState state, PpmContext context)
        {
            PpmContext upBranch = _foundState.Successor;
            PpmState[] states = new PpmState[MAXIMUM_ORDER];
            uint stateIndex = 0;
            byte symbol = _foundState.Symbol;

            if (!skip)
            {
                states[stateIndex++] = _foundState;
                if (context.Suffix == PpmContext.ZERO)
                {
                    goto NoLoop;
                }
            }

            bool gotoLoopEntry = false;
            if (state != PpmState.ZERO)
            {
                context = context.Suffix;
                gotoLoopEntry = true;
            }

            do
            {
                if (gotoLoopEntry)
                {
                    gotoLoopEntry = false;
                    goto LoopEntry;
                }

                context = context.Suffix;
                if (context.NumberStatistics != 0)
                {
                    byte temporary;
                    state = context.Statistics;
                    if (state.Symbol != symbol)
                    {
                        do
                        {
                            temporary = state[1].Symbol;
                            state++;
                        }
                        while (temporary != symbol);
                    }
                    temporary = (byte)((state.Frequency < MAXIMUM_FREQUENCY - 9) ? 1 : 0);
                    state.Frequency += temporary;
                    context.SummaryFrequency += temporary;
                }
                else
                {
                    state = context.FirstState;
                    state.Frequency +=
                        (byte)(((context.Suffix.NumberStatistics == 0) ? 1 : 0) & ((state.Frequency < 24) ? 1 : 0));
                }

                LoopEntry:
                if (state.Successor != upBranch)
                {
                    context = state.Successor;
                    break;
                }
                states[stateIndex++] = state;
            }
            while (context.Suffix != PpmContext.ZERO);

            NoLoop:
            if (stateIndex == 0)
            {
                return context;
            }

            byte localNumberStatistics = 0;
            byte localFlags = (byte)((symbol >= 0x40) ? 0x10 : 0x00);
            symbol = upBranch.NumberStatistics;
            byte localSymbol = symbol;
            byte localFrequency;
            PpmContext localSuccessor = ((Pointer)upBranch) + 1;
            localFlags |= (byte)((symbol >= 0x40) ? 0x08 : 0x00);

            if (context.NumberStatistics != 0)
            {
                state = context.Statistics;
                if (state.Symbol != symbol)
                {
                    byte temporary;
                    do
                    {
                        temporary = state[1].Symbol;
                        state++;
                    }
                    while (temporary != symbol);
                }
                uint cf = (uint)(state.Frequency - 1);
                uint s0 = (uint)(context.SummaryFrequency - context.NumberStatistics - cf);
                localFrequency = (byte)(1 + ((2 * cf <= s0) ? (uint)((5 * cf > s0) ? 1 : 0) : ((cf + 2 * s0 - 3) / s0)));
            }
            else
            {
                localFrequency = context.FirstStateFrequency;
            }

            do
            {
                PpmContext currentContext = _allocator.AllocateContext();
                if (currentContext == PpmContext.ZERO)
                {
                    return PpmContext.ZERO;
                }
                currentContext.NumberStatistics = localNumberStatistics;
                currentContext.Flags = localFlags;
                currentContext.FirstStateSymbol = localSymbol;
                currentContext.FirstStateFrequency = localFrequency;
                currentContext.FirstStateSuccessor = localSuccessor;
                currentContext.Suffix = context;
                context = currentContext;
                states[--stateIndex].Successor = context;
            }
            while (stateIndex != 0);

            return context;
        }

        private PpmContext ReduceOrder(PpmState state, PpmContext context)
        {
            PpmState currentState;
            PpmState[] states = new PpmState[MAXIMUM_ORDER];
            uint stateIndex = 0;
            PpmContext currentContext = context;
            PpmContext upBranch = _allocator._text;
            byte temporary;
            byte symbol = _foundState.Symbol;

            states[stateIndex++] = _foundState;
            _foundState.Successor = upBranch;
            _orderFall++;

            bool gotoLoopEntry = false;
            if (state != PpmState.ZERO)
            {
                context = context.Suffix;
                gotoLoopEntry = true;
            }

            while (true)
            {
                if (gotoLoopEntry)
                {
                    gotoLoopEntry = false;
                    goto LoopEntry;
                }

                if (context.Suffix == PpmContext.ZERO)
                {
                    if (_method > ModelRestorationMethod.Freeze)
                    {
                        do
                        {
                            states[--stateIndex].Successor = context;
                        }
                        while (stateIndex != 0);
                        _allocator._text = _allocator._heap + 1;
                        _orderFall = 1;
                    }
                    return context;
                }

                context = context.Suffix;
                if (context.NumberStatistics != 0)
                {
                    state = context.Statistics;
                    if (state.Symbol != symbol)
                    {
                        do
                        {
                            temporary = state[1].Symbol;
                            state++;
                        }
                        while (temporary != symbol);
                    }
                    temporary = (byte)((state.Frequency < MAXIMUM_FREQUENCY - 9) ? 2 : 0);
                    state.Frequency += temporary;
                    context.SummaryFrequency += temporary;
                }
                else
                {
                    state = context.FirstState;
                    state.Frequency += (byte)((state.Frequency < 32) ? 1 : 0);
                }

                LoopEntry:
                if (state.Successor != PpmContext.ZERO)
                {
                    break;
                }
                states[stateIndex++] = state;
                state.Successor = upBranch;
                _orderFall++;
            }

            if (_method > ModelRestorationMethod.Freeze)
            {
                context = state.Successor;
                do
                {
                    states[--stateIndex].Successor = context;
                }
                while (stateIndex != 0);
                _allocator._text = _allocator._heap + 1;
                _orderFall = 1;
                return context;
            }
            if (state.Successor <= upBranch)
            {
                currentState = _foundState;
                _foundState = state;
                state.Successor = CreateSuccessors(false, PpmState.ZERO, context);
                _foundState = currentState;
            }

            if (_orderFall == 1 && currentContext == _maximumContext)
            {
                _foundState.Successor = state.Successor;
                _allocator._text--;
            }

            return state.Successor;
        }

        private void RestoreModel(PpmContext context, PpmContext minimumContext, PpmContext foundStateSuccessor)
        {
            PpmContext currentContext;

            _allocator._text = _allocator._heap;
            for (currentContext = _maximumContext; currentContext != context; currentContext = currentContext.Suffix)
            {
                if (--currentContext.NumberStatistics == 0)
                {
                    currentContext.Flags =
                        (byte)
                        ((currentContext.Flags & 0x10) + ((currentContext.Statistics.Symbol >= 0x40) ? 0x08 : 0x00));
                    PpmState state = currentContext.Statistics;
                    Copy(currentContext.FirstState, state);
                    _allocator.SpecialFreeUnits(state);
                    currentContext.FirstStateFrequency = (byte)((currentContext.FirstStateFrequency + 11) >> 3);
                }
                else
                {
                    Refresh((uint)((currentContext.NumberStatistics + 3) >> 1), false, currentContext);
                }
            }

            for (; currentContext != minimumContext; currentContext = currentContext.Suffix)
            {
                if (currentContext.NumberStatistics == 0)
                {
                    currentContext.FirstStateFrequency -= (byte)(currentContext.FirstStateFrequency >> 1);
                }
                else if ((currentContext.SummaryFrequency += 4) > 128 + 4 * currentContext.NumberStatistics)
                {
                    Refresh((uint)((currentContext.NumberStatistics + 2) >> 1), true, currentContext);
                }
            }

            if (_method > ModelRestorationMethod.Freeze)
            {
                _maximumContext = foundStateSuccessor;
                _allocator._glueCount += (uint)(((_allocator._memoryNodes[1].Stamp & 1) == 0) ? 1 : 0);
            }
            else if (_method == ModelRestorationMethod.Freeze)
            {
                while (_maximumContext.Suffix != PpmContext.ZERO)
                {
                    _maximumContext = _maximumContext.Suffix;
                }

                RemoveBinaryContexts(0, _maximumContext);
                _method = _method + 1;
                _allocator._glueCount = 0;
                _orderFall = _modelOrder;
            }
            else if (_method == ModelRestorationMethod.Restart ||
                     _allocator.GetMemoryUsed() < (_allocator._allocatorSize >> 1))
            {
                StartModel(_modelOrder, _method);
                _escapeCount = 0;
            }
            else
            {
                while (_maximumContext.Suffix != PpmContext.ZERO)
                {
                    _maximumContext = _maximumContext.Suffix;
                }

                do
                {
                    CutOff(0, _maximumContext);
                    _allocator.ExpandText();
                }
                while (_allocator.GetMemoryUsed() > 3 * (_allocator._allocatorSize >> 2));

                _allocator._glueCount = 0;
                _orderFall = _modelOrder;
            }
        }

        private static void Swap(PpmState state1, PpmState state2)
        {
            byte swapSymbol = state1.Symbol;
            byte swapFrequency = state1.Frequency;
            PpmContext swapSuccessor = state1.Successor;

            state1.Symbol = state2.Symbol;
            state1.Frequency = state2.Frequency;
            state1.Successor = state2.Successor;

            state2.Symbol = swapSymbol;
            state2.Frequency = swapFrequency;
            state2.Successor = swapSuccessor;
        }

        private static void Copy(PpmState state1, PpmState state2)
        {
            state1.Symbol = state2.Symbol;
            state1.Frequency = state2.Frequency;
            state1.Successor = state2.Successor;
        }

        private static int Mean(int sum, int shift, int round)
        {
            return (sum + (1 << (shift - round))) >> shift;
        }

        private void ClearMask()
        {
            _escapeCount = 1;
            Array.Clear(_characterMask, 0, _characterMask.Length);
        }

        #endregion
    }
}