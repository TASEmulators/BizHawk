using System;
using System.IO;
using SharpCompress.Compressors.LZMA.LZ;
using SharpCompress.Compressors.LZMA.RangeCoder;

namespace SharpCompress.Compressors.LZMA
{
    internal class Encoder : ICoder, ISetCoderProperties, IWriteCoderProperties
    {
        private enum EMatchFinderType
        {
            Bt2,
            Bt4
        }

        private const UInt32 K_IFINITY_PRICE = 0xFFFFFFF;

        private static readonly Byte[] G_FAST_POS = new Byte[1 << 11];

        static Encoder()
        {
            const Byte kFastSlots = 22;
            int c = 2;
            G_FAST_POS[0] = 0;
            G_FAST_POS[1] = 1;
            for (Byte slotFast = 2; slotFast < kFastSlots; slotFast++)
            {
                UInt32 k = ((UInt32)1 << ((slotFast >> 1) - 1));
                for (UInt32 j = 0; j < k; j++, c++)
                {
                    G_FAST_POS[c] = slotFast;
                }
            }
        }

        private static UInt32 GetPosSlot(UInt32 pos)
        {
            if (pos < (1 << 11))
            {
                return G_FAST_POS[pos];
            }
            if (pos < (1 << 21))
            {
                return (UInt32)(G_FAST_POS[pos >> 10] + 20);
            }
            return (UInt32)(G_FAST_POS[pos >> 20] + 40);
        }

        private static UInt32 GetPosSlot2(UInt32 pos)
        {
            if (pos < (1 << 17))
            {
                return (UInt32)(G_FAST_POS[pos >> 6] + 12);
            }
            if (pos < (1 << 27))
            {
                return (UInt32)(G_FAST_POS[pos >> 16] + 32);
            }
            return (UInt32)(G_FAST_POS[pos >> 26] + 52);
        }

        private Base.State _state = new Base.State();
        private Byte _previousByte;
        private readonly UInt32[] _repDistances = new UInt32[Base.K_NUM_REP_DISTANCES];

        private void BaseInit()
        {
            _state.Init();
            _previousByte = 0;
            for (UInt32 i = 0; i < Base.K_NUM_REP_DISTANCES; i++)
            {
                _repDistances[i] = 0;
            }
        }

        private const int K_DEFAULT_DICTIONARY_LOG_SIZE = 22;
        private const UInt32 K_NUM_FAST_BYTES_DEFAULT = 0x20;

        private class LiteralEncoder
        {
            public struct Encoder2
            {
                private BitEncoder[] _encoders;

                public void Create()
                {
                    _encoders = new BitEncoder[0x300];
                }

                public void Init()
                {
                    for (int i = 0; i < 0x300; i++)
                    {
                        _encoders[i].Init();
                    }
                }

                public void Encode(RangeCoder.Encoder rangeEncoder, byte symbol)
                {
                    uint context = 1;
                    for (int i = 7; i >= 0; i--)
                    {
                        uint bit = (uint)((symbol >> i) & 1);
                        _encoders[context].Encode(rangeEncoder, bit);
                        context = (context << 1) | bit;
                    }
                }

                public void EncodeMatched(RangeCoder.Encoder rangeEncoder, byte matchByte, byte symbol)
                {
                    uint context = 1;
                    bool same = true;
                    for (int i = 7; i >= 0; i--)
                    {
                        uint bit = (uint)((symbol >> i) & 1);
                        uint state = context;
                        if (same)
                        {
                            uint matchBit = (uint)((matchByte >> i) & 1);
                            state += ((1 + matchBit) << 8);
                            same = (matchBit == bit);
                        }
                        _encoders[state].Encode(rangeEncoder, bit);
                        context = (context << 1) | bit;
                    }
                }

                public uint GetPrice(bool matchMode, byte matchByte, byte symbol)
                {
                    uint price = 0;
                    uint context = 1;
                    int i = 7;
                    if (matchMode)
                    {
                        for (; i >= 0; i--)
                        {
                            uint matchBit = (uint)(matchByte >> i) & 1;
                            uint bit = (uint)(symbol >> i) & 1;
                            price += _encoders[((1 + matchBit) << 8) + context].GetPrice(bit);
                            context = (context << 1) | bit;
                            if (matchBit != bit)
                            {
                                i--;
                                break;
                            }
                        }
                    }
                    for (; i >= 0; i--)
                    {
                        uint bit = (uint)(symbol >> i) & 1;
                        price += _encoders[context].GetPrice(bit);
                        context = (context << 1) | bit;
                    }
                    return price;
                }
            }

            private Encoder2[] _coders;
            private int _numPrevBits;
            private int _numPosBits;
            private uint _posMask;

            public void Create(int numPosBits, int numPrevBits)
            {
                if (_coders != null && _numPrevBits == numPrevBits && _numPosBits == numPosBits)
                {
                    return;
                }
                _numPosBits = numPosBits;
                _posMask = ((uint)1 << numPosBits) - 1;
                _numPrevBits = numPrevBits;
                uint numStates = (uint)1 << (_numPrevBits + _numPosBits);
                _coders = new Encoder2[numStates];
                for (uint i = 0; i < numStates; i++)
                {
                    _coders[i].Create();
                }
            }

            public void Init()
            {
                uint numStates = (uint)1 << (_numPrevBits + _numPosBits);
                for (uint i = 0; i < numStates; i++)
                {
                    _coders[i].Init();
                }
            }

            public Encoder2 GetSubCoder(UInt32 pos, Byte prevByte)
            {
                return _coders[((pos & _posMask) << _numPrevBits) + (uint)(prevByte >> (8 - _numPrevBits))];
            }
        }

        private class LenEncoder
        {
            private BitEncoder _choice = new BitEncoder();
            private BitEncoder _choice2 = new BitEncoder();
            private readonly BitTreeEncoder[] _lowCoder = new BitTreeEncoder[Base.K_NUM_POS_STATES_ENCODING_MAX];
            private readonly BitTreeEncoder[] _midCoder = new BitTreeEncoder[Base.K_NUM_POS_STATES_ENCODING_MAX];
            private BitTreeEncoder _highCoder = new BitTreeEncoder(Base.K_NUM_HIGH_LEN_BITS);

            public LenEncoder()
            {
                for (UInt32 posState = 0; posState < Base.K_NUM_POS_STATES_ENCODING_MAX; posState++)
                {
                    _lowCoder[posState] = new BitTreeEncoder(Base.K_NUM_LOW_LEN_BITS);
                    _midCoder[posState] = new BitTreeEncoder(Base.K_NUM_MID_LEN_BITS);
                }
            }

            public void Init(UInt32 numPosStates)
            {
                _choice.Init();
                _choice2.Init();
                for (UInt32 posState = 0; posState < numPosStates; posState++)
                {
                    _lowCoder[posState].Init();
                    _midCoder[posState].Init();
                }
                _highCoder.Init();
            }

            public void Encode(RangeCoder.Encoder rangeEncoder, UInt32 symbol, UInt32 posState)
            {
                if (symbol < Base.K_NUM_LOW_LEN_SYMBOLS)
                {
                    _choice.Encode(rangeEncoder, 0);
                    _lowCoder[posState].Encode(rangeEncoder, symbol);
                }
                else
                {
                    symbol -= Base.K_NUM_LOW_LEN_SYMBOLS;
                    _choice.Encode(rangeEncoder, 1);
                    if (symbol < Base.K_NUM_MID_LEN_SYMBOLS)
                    {
                        _choice2.Encode(rangeEncoder, 0);
                        _midCoder[posState].Encode(rangeEncoder, symbol);
                    }
                    else
                    {
                        _choice2.Encode(rangeEncoder, 1);
                        _highCoder.Encode(rangeEncoder, symbol - Base.K_NUM_MID_LEN_SYMBOLS);
                    }
                }
            }

            public void SetPrices(UInt32 posState, UInt32 numSymbols, UInt32[] prices, UInt32 st)
            {
                UInt32 a0 = _choice.GetPrice0();
                UInt32 a1 = _choice.GetPrice1();
                UInt32 b0 = a1 + _choice2.GetPrice0();
                UInt32 b1 = a1 + _choice2.GetPrice1();
                UInt32 i = 0;
                for (i = 0; i < Base.K_NUM_LOW_LEN_SYMBOLS; i++)
                {
                    if (i >= numSymbols)
                    {
                        return;
                    }
                    prices[st + i] = a0 + _lowCoder[posState].GetPrice(i);
                }
                for (; i < Base.K_NUM_LOW_LEN_SYMBOLS + Base.K_NUM_MID_LEN_SYMBOLS; i++)
                {
                    if (i >= numSymbols)
                    {
                        return;
                    }
                    prices[st + i] = b0 + _midCoder[posState].GetPrice(i - Base.K_NUM_LOW_LEN_SYMBOLS);
                }
                for (; i < numSymbols; i++)
                {
                    prices[st + i] = b1 + _highCoder.GetPrice(i - Base.K_NUM_LOW_LEN_SYMBOLS - Base.K_NUM_MID_LEN_SYMBOLS);
                }
            }
        }

        private const UInt32 K_NUM_LEN_SPEC_SYMBOLS = Base.K_NUM_LOW_LEN_SYMBOLS + Base.K_NUM_MID_LEN_SYMBOLS;

        private class LenPriceTableEncoder : LenEncoder
        {
            private readonly UInt32[] _prices = new UInt32[Base.K_NUM_LEN_SYMBOLS << Base.K_NUM_POS_STATES_BITS_ENCODING_MAX];
            private UInt32 _tableSize;
            private readonly UInt32[] _counters = new UInt32[Base.K_NUM_POS_STATES_ENCODING_MAX];

            public void SetTableSize(UInt32 tableSize)
            {
                _tableSize = tableSize;
            }

            public UInt32 GetPrice(UInt32 symbol, UInt32 posState)
            {
                return _prices[posState * Base.K_NUM_LEN_SYMBOLS + symbol];
            }

            private void UpdateTable(UInt32 posState)
            {
                SetPrices(posState, _tableSize, _prices, posState * Base.K_NUM_LEN_SYMBOLS);
                _counters[posState] = _tableSize;
            }

            public void UpdateTables(UInt32 numPosStates)
            {
                for (UInt32 posState = 0; posState < numPosStates; posState++)
                {
                    UpdateTable(posState);
                }
            }

            public new void Encode(RangeCoder.Encoder rangeEncoder, UInt32 symbol, UInt32 posState)
            {
                base.Encode(rangeEncoder, symbol, posState);
                if (--_counters[posState] == 0)
                {
                    UpdateTable(posState);
                }
            }
        }

        private const UInt32 K_NUM_OPTS = 1 << 12;

        private class Optimal
        {
            public Base.State _state;

            public bool _prev1IsChar;
            public bool _prev2;

            public UInt32 _posPrev2;
            public UInt32 _backPrev2;

            public UInt32 _price;
            public UInt32 _posPrev;
            public UInt32 _backPrev;

            public UInt32 _backs0;
            public UInt32 _backs1;
            public UInt32 _backs2;
            public UInt32 _backs3;

            public void MakeAsChar()
            {
                _backPrev = 0xFFFFFFFF;
                _prev1IsChar = false;
            }

            public void MakeAsShortRep()
            {
                _backPrev = 0;
                ;
                _prev1IsChar = false;
            }

            public bool IsShortRep()
            {
                return (_backPrev == 0);
            }
        }

        private readonly Optimal[] _optimum = new Optimal[K_NUM_OPTS];
        private BinTree _matchFinder;
        private readonly RangeCoder.Encoder _rangeEncoder = new RangeCoder.Encoder();

        private readonly BitEncoder[] _isMatch =
            new BitEncoder[Base.K_NUM_STATES << Base.K_NUM_POS_STATES_BITS_MAX];

        private readonly BitEncoder[] _isRep = new BitEncoder[Base.K_NUM_STATES];
        private readonly BitEncoder[] _isRepG0 = new BitEncoder[Base.K_NUM_STATES];
        private readonly BitEncoder[] _isRepG1 = new BitEncoder[Base.K_NUM_STATES];
        private readonly BitEncoder[] _isRepG2 = new BitEncoder[Base.K_NUM_STATES];

        private readonly BitEncoder[] _isRep0Long =
            new BitEncoder[Base.K_NUM_STATES << Base.K_NUM_POS_STATES_BITS_MAX];

        private readonly BitTreeEncoder[] _posSlotEncoder = new BitTreeEncoder[Base.K_NUM_LEN_TO_POS_STATES];

        private readonly BitEncoder[] _posEncoders =
            new BitEncoder[Base.K_NUM_FULL_DISTANCES - Base.K_END_POS_MODEL_INDEX];

        private BitTreeEncoder _posAlignEncoder = new BitTreeEncoder(Base.K_NUM_ALIGN_BITS);

        private readonly LenPriceTableEncoder _lenEncoder = new LenPriceTableEncoder();
        private readonly LenPriceTableEncoder _repMatchLenEncoder = new LenPriceTableEncoder();

        private readonly LiteralEncoder _literalEncoder = new LiteralEncoder();

        private readonly UInt32[] _matchDistances = new UInt32[Base.K_MATCH_MAX_LEN * 2 + 2];

        private UInt32 _numFastBytes = K_NUM_FAST_BYTES_DEFAULT;
        private UInt32 _longestMatchLength;
        private UInt32 _numDistancePairs;

        private UInt32 _additionalOffset;

        private UInt32 _optimumEndIndex;
        private UInt32 _optimumCurrentIndex;

        private bool _longestMatchWasFound;

        private readonly UInt32[] _posSlotPrices = new UInt32[1 << (Base.K_NUM_POS_SLOT_BITS + Base.K_NUM_LEN_TO_POS_STATES_BITS)];
        private readonly UInt32[] _distancesPrices = new UInt32[Base.K_NUM_FULL_DISTANCES << Base.K_NUM_LEN_TO_POS_STATES_BITS];
        private readonly UInt32[] _alignPrices = new UInt32[Base.K_ALIGN_TABLE_SIZE];
        private UInt32 _alignPriceCount;

        private UInt32 _distTableSize = (K_DEFAULT_DICTIONARY_LOG_SIZE * 2);

        private int _posStateBits = 2;
        private UInt32 _posStateMask = (4 - 1);
        private int _numLiteralPosStateBits;
        private int _numLiteralContextBits = 3;

        private UInt32 _dictionarySize = (1 << K_DEFAULT_DICTIONARY_LOG_SIZE);
        private UInt32 _dictionarySizePrev = 0xFFFFFFFF;
        private UInt32 _numFastBytesPrev = 0xFFFFFFFF;

        private Int64 _nowPos64;
        private bool _finished;
        private Stream _inStream;

        private EMatchFinderType _matchFinderType = EMatchFinderType.Bt4;
        private bool _writeEndMark;

        private bool _needReleaseMfStream;
        private bool _processingMode;

        private void Create()
        {
            if (_matchFinder == null)
            {
                BinTree bt = new BinTree();
                int numHashBytes = 4;
                if (_matchFinderType == EMatchFinderType.Bt2)
                {
                    numHashBytes = 2;
                }
                bt.SetType(numHashBytes);
                _matchFinder = bt;
            }
            _literalEncoder.Create(_numLiteralPosStateBits, _numLiteralContextBits);

            if (_dictionarySize == _dictionarySizePrev && _numFastBytesPrev == _numFastBytes)
            {
                return;
            }
            _matchFinder.Create(_dictionarySize, K_NUM_OPTS, _numFastBytes, Base.K_MATCH_MAX_LEN + 1 + K_NUM_OPTS);
            _dictionarySizePrev = _dictionarySize;
            _numFastBytesPrev = _numFastBytes;
        }

        public Encoder()
        {
            for (int i = 0; i < K_NUM_OPTS; i++)
            {
                _optimum[i] = new Optimal();
            }
            for (int i = 0; i < Base.K_NUM_LEN_TO_POS_STATES; i++)
            {
                _posSlotEncoder[i] = new BitTreeEncoder(Base.K_NUM_POS_SLOT_BITS);
            }
        }

        private void SetWriteEndMarkerMode(bool writeEndMarker)
        {
            _writeEndMark = writeEndMarker;
        }

        private void Init()
        {
            BaseInit();
            _rangeEncoder.Init();

            uint i;
            for (i = 0; i < Base.K_NUM_STATES; i++)
            {
                for (uint j = 0; j <= _posStateMask; j++)
                {
                    uint complexState = (i << Base.K_NUM_POS_STATES_BITS_MAX) + j;
                    _isMatch[complexState].Init();
                    _isRep0Long[complexState].Init();
                }
                _isRep[i].Init();
                _isRepG0[i].Init();
                _isRepG1[i].Init();
                _isRepG2[i].Init();
            }
            _literalEncoder.Init();
            for (i = 0; i < Base.K_NUM_LEN_TO_POS_STATES; i++)
            {
                _posSlotEncoder[i].Init();
            }
            for (i = 0; i < Base.K_NUM_FULL_DISTANCES - Base.K_END_POS_MODEL_INDEX; i++)
            {
                _posEncoders[i].Init();
            }

            _lenEncoder.Init((UInt32)1 << _posStateBits);
            _repMatchLenEncoder.Init((UInt32)1 << _posStateBits);

            _posAlignEncoder.Init();

            _longestMatchWasFound = false;
            _optimumEndIndex = 0;
            _optimumCurrentIndex = 0;
            _additionalOffset = 0;
        }

        private void ReadMatchDistances(out UInt32 lenRes, out UInt32 numDistancePairs)
        {
            lenRes = 0;
            numDistancePairs = _matchFinder.GetMatches(_matchDistances);
            if (numDistancePairs > 0)
            {
                lenRes = _matchDistances[numDistancePairs - 2];
                if (lenRes == _numFastBytes)
                {
                    lenRes += _matchFinder.GetMatchLen((int)lenRes - 1, _matchDistances[numDistancePairs - 1],
                                                       Base.K_MATCH_MAX_LEN - lenRes);
                }
            }
            _additionalOffset++;
        }

        private void MovePos(UInt32 num)
        {
            if (num > 0)
            {
                _matchFinder.Skip(num);
                _additionalOffset += num;
            }
        }

        private UInt32 GetRepLen1Price(Base.State state, UInt32 posState)
        {
            return _isRepG0[state._index].GetPrice0() +
                   _isRep0Long[(state._index << Base.K_NUM_POS_STATES_BITS_MAX) + posState].GetPrice0();
        }

        private UInt32 GetPureRepPrice(UInt32 repIndex, Base.State state, UInt32 posState)
        {
            UInt32 price;
            if (repIndex == 0)
            {
                price = _isRepG0[state._index].GetPrice0();
                price += _isRep0Long[(state._index << Base.K_NUM_POS_STATES_BITS_MAX) + posState].GetPrice1();
            }
            else
            {
                price = _isRepG0[state._index].GetPrice1();
                if (repIndex == 1)
                {
                    price += _isRepG1[state._index].GetPrice0();
                }
                else
                {
                    price += _isRepG1[state._index].GetPrice1();
                    price += _isRepG2[state._index].GetPrice(repIndex - 2);
                }
            }
            return price;
        }

        private UInt32 GetRepPrice(UInt32 repIndex, UInt32 len, Base.State state, UInt32 posState)
        {
            UInt32 price = _repMatchLenEncoder.GetPrice(len - Base.K_MATCH_MIN_LEN, posState);
            return price + GetPureRepPrice(repIndex, state, posState);
        }

        private UInt32 GetPosLenPrice(UInt32 pos, UInt32 len, UInt32 posState)
        {
            UInt32 price;
            UInt32 lenToPosState = Base.GetLenToPosState(len);
            if (pos < Base.K_NUM_FULL_DISTANCES)
            {
                price = _distancesPrices[(lenToPosState * Base.K_NUM_FULL_DISTANCES) + pos];
            }
            else
            {
                price = _posSlotPrices[(lenToPosState << Base.K_NUM_POS_SLOT_BITS) + GetPosSlot2(pos)] +
                        _alignPrices[pos & Base.K_ALIGN_MASK];
            }
            return price + _lenEncoder.GetPrice(len - Base.K_MATCH_MIN_LEN, posState);
        }

        private UInt32 Backward(out UInt32 backRes, UInt32 cur)
        {
            _optimumEndIndex = cur;
            UInt32 posMem = _optimum[cur]._posPrev;
            UInt32 backMem = _optimum[cur]._backPrev;
            do
            {
                if (_optimum[cur]._prev1IsChar)
                {
                    _optimum[posMem].MakeAsChar();
                    _optimum[posMem]._posPrev = posMem - 1;
                    if (_optimum[cur]._prev2)
                    {
                        _optimum[posMem - 1]._prev1IsChar = false;
                        _optimum[posMem - 1]._posPrev = _optimum[cur]._posPrev2;
                        _optimum[posMem - 1]._backPrev = _optimum[cur]._backPrev2;
                    }
                }
                UInt32 posPrev = posMem;
                UInt32 backCur = backMem;

                backMem = _optimum[posPrev]._backPrev;
                posMem = _optimum[posPrev]._posPrev;

                _optimum[posPrev]._backPrev = backCur;
                _optimum[posPrev]._posPrev = cur;
                cur = posPrev;
            }
            while (cur > 0);
            backRes = _optimum[0]._backPrev;
            _optimumCurrentIndex = _optimum[0]._posPrev;
            return _optimumCurrentIndex;
        }

        private readonly UInt32[] _reps = new UInt32[Base.K_NUM_REP_DISTANCES];
        private readonly UInt32[] _repLens = new UInt32[Base.K_NUM_REP_DISTANCES];

        private UInt32 GetOptimum(UInt32 position, out UInt32 backRes)
        {
            if (_optimumEndIndex != _optimumCurrentIndex)
            {
                UInt32 lenRes = _optimum[_optimumCurrentIndex]._posPrev - _optimumCurrentIndex;
                backRes = _optimum[_optimumCurrentIndex]._backPrev;
                _optimumCurrentIndex = _optimum[_optimumCurrentIndex]._posPrev;
                return lenRes;
            }
            _optimumCurrentIndex = _optimumEndIndex = 0;

            UInt32 lenMain, numDistancePairs;
            if (!_longestMatchWasFound)
            {
                ReadMatchDistances(out lenMain, out numDistancePairs);
            }
            else
            {
                lenMain = _longestMatchLength;
                numDistancePairs = _numDistancePairs;
                _longestMatchWasFound = false;
            }

            UInt32 numAvailableBytes = _matchFinder.GetNumAvailableBytes() + 1;
            if (numAvailableBytes < 2)
            {
                backRes = 0xFFFFFFFF;
                return 1;
            }
            if (numAvailableBytes > Base.K_MATCH_MAX_LEN)
            {
                numAvailableBytes = Base.K_MATCH_MAX_LEN;
            }

            UInt32 repMaxIndex = 0;
            UInt32 i;
            for (i = 0; i < Base.K_NUM_REP_DISTANCES; i++)
            {
                _reps[i] = _repDistances[i];
                _repLens[i] = _matchFinder.GetMatchLen(0 - 1, _reps[i], Base.K_MATCH_MAX_LEN);
                if (_repLens[i] > _repLens[repMaxIndex])
                {
                    repMaxIndex = i;
                }
            }
            if (_repLens[repMaxIndex] >= _numFastBytes)
            {
                backRes = repMaxIndex;
                UInt32 lenRes = _repLens[repMaxIndex];
                MovePos(lenRes - 1);
                return lenRes;
            }

            if (lenMain >= _numFastBytes)
            {
                backRes = _matchDistances[numDistancePairs - 1] + Base.K_NUM_REP_DISTANCES;
                MovePos(lenMain - 1);
                return lenMain;
            }

            Byte currentByte = _matchFinder.GetIndexByte(0 - 1);
            Byte matchByte = _matchFinder.GetIndexByte((Int32)(0 - _repDistances[0] - 1 - 1));

            if (lenMain < 2 && currentByte != matchByte && _repLens[repMaxIndex] < 2)
            {
                backRes = 0xFFFFFFFF;
                return 1;
            }

            _optimum[0]._state = _state;

            UInt32 posState = (position & _posStateMask);

            _optimum[1]._price = _isMatch[(_state._index << Base.K_NUM_POS_STATES_BITS_MAX) + posState].GetPrice0() +
                                _literalEncoder.GetSubCoder(position, _previousByte)
                                               .GetPrice(!_state.IsCharState(), matchByte, currentByte);
            _optimum[1].MakeAsChar();

            UInt32 matchPrice = _isMatch[(_state._index << Base.K_NUM_POS_STATES_BITS_MAX) + posState].GetPrice1();
            UInt32 repMatchPrice = matchPrice + _isRep[_state._index].GetPrice1();

            if (matchByte == currentByte)
            {
                UInt32 shortRepPrice = repMatchPrice + GetRepLen1Price(_state, posState);
                if (shortRepPrice < _optimum[1]._price)
                {
                    _optimum[1]._price = shortRepPrice;
                    _optimum[1].MakeAsShortRep();
                }
            }

            UInt32 lenEnd = ((lenMain >= _repLens[repMaxIndex]) ? lenMain : _repLens[repMaxIndex]);

            if (lenEnd < 2)
            {
                backRes = _optimum[1]._backPrev;
                return 1;
            }

            _optimum[1]._posPrev = 0;

            _optimum[0]._backs0 = _reps[0];
            _optimum[0]._backs1 = _reps[1];
            _optimum[0]._backs2 = _reps[2];
            _optimum[0]._backs3 = _reps[3];

            UInt32 len = lenEnd;
            do
            {
                _optimum[len--]._price = K_IFINITY_PRICE;
            }
            while (len >= 2);

            for (i = 0; i < Base.K_NUM_REP_DISTANCES; i++)
            {
                UInt32 repLen = _repLens[i];
                if (repLen < 2)
                {
                    continue;
                }
                UInt32 price = repMatchPrice + GetPureRepPrice(i, _state, posState);
                do
                {
                    UInt32 curAndLenPrice = price + _repMatchLenEncoder.GetPrice(repLen - 2, posState);
                    Optimal optimum = _optimum[repLen];
                    if (curAndLenPrice < optimum._price)
                    {
                        optimum._price = curAndLenPrice;
                        optimum._posPrev = 0;
                        optimum._backPrev = i;
                        optimum._prev1IsChar = false;
                    }
                }
                while (--repLen >= 2);
            }

            UInt32 normalMatchPrice = matchPrice + _isRep[_state._index].GetPrice0();

            len = ((_repLens[0] >= 2) ? _repLens[0] + 1 : 2);
            if (len <= lenMain)
            {
                UInt32 offs = 0;
                while (len > _matchDistances[offs])
                {
                    offs += 2;
                }
                for (;; len++)
                {
                    UInt32 distance = _matchDistances[offs + 1];
                    UInt32 curAndLenPrice = normalMatchPrice + GetPosLenPrice(distance, len, posState);
                    Optimal optimum = _optimum[len];
                    if (curAndLenPrice < optimum._price)
                    {
                        optimum._price = curAndLenPrice;
                        optimum._posPrev = 0;
                        optimum._backPrev = distance + Base.K_NUM_REP_DISTANCES;
                        optimum._prev1IsChar = false;
                    }
                    if (len == _matchDistances[offs])
                    {
                        offs += 2;
                        if (offs == numDistancePairs)
                        {
                            break;
                        }
                    }
                }
            }

            UInt32 cur = 0;

            while (true)
            {
                cur++;
                if (cur == lenEnd)
                {
                    return Backward(out backRes, cur);
                }
                UInt32 newLen;
                ReadMatchDistances(out newLen, out numDistancePairs);
                if (newLen >= _numFastBytes)
                {
                    _numDistancePairs = numDistancePairs;
                    _longestMatchLength = newLen;
                    _longestMatchWasFound = true;
                    return Backward(out backRes, cur);
                }
                position++;
                UInt32 posPrev = _optimum[cur]._posPrev;
                Base.State state;
                if (_optimum[cur]._prev1IsChar)
                {
                    posPrev--;
                    if (_optimum[cur]._prev2)
                    {
                        state = _optimum[_optimum[cur]._posPrev2]._state;
                        if (_optimum[cur]._backPrev2 < Base.K_NUM_REP_DISTANCES)
                        {
                            state.UpdateRep();
                        }
                        else
                        {
                            state.UpdateMatch();
                        }
                    }
                    else
                    {
                        state = _optimum[posPrev]._state;
                    }
                    state.UpdateChar();
                }
                else
                {
                    state = _optimum[posPrev]._state;
                }
                if (posPrev == cur - 1)
                {
                    if (_optimum[cur].IsShortRep())
                    {
                        state.UpdateShortRep();
                    }
                    else
                    {
                        state.UpdateChar();
                    }
                }
                else
                {
                    UInt32 pos;
                    if (_optimum[cur]._prev1IsChar && _optimum[cur]._prev2)
                    {
                        posPrev = _optimum[cur]._posPrev2;
                        pos = _optimum[cur]._backPrev2;
                        state.UpdateRep();
                    }
                    else
                    {
                        pos = _optimum[cur]._backPrev;
                        if (pos < Base.K_NUM_REP_DISTANCES)
                        {
                            state.UpdateRep();
                        }
                        else
                        {
                            state.UpdateMatch();
                        }
                    }
                    Optimal opt = _optimum[posPrev];
                    if (pos < Base.K_NUM_REP_DISTANCES)
                    {
                        if (pos == 0)
                        {
                            _reps[0] = opt._backs0;
                            _reps[1] = opt._backs1;
                            _reps[2] = opt._backs2;
                            _reps[3] = opt._backs3;
                        }
                        else if (pos == 1)
                        {
                            _reps[0] = opt._backs1;
                            _reps[1] = opt._backs0;
                            _reps[2] = opt._backs2;
                            _reps[3] = opt._backs3;
                        }
                        else if (pos == 2)
                        {
                            _reps[0] = opt._backs2;
                            _reps[1] = opt._backs0;
                            _reps[2] = opt._backs1;
                            _reps[3] = opt._backs3;
                        }
                        else
                        {
                            _reps[0] = opt._backs3;
                            _reps[1] = opt._backs0;
                            _reps[2] = opt._backs1;
                            _reps[3] = opt._backs2;
                        }
                    }
                    else
                    {
                        _reps[0] = (pos - Base.K_NUM_REP_DISTANCES);
                        _reps[1] = opt._backs0;
                        _reps[2] = opt._backs1;
                        _reps[3] = opt._backs2;
                    }
                }
                _optimum[cur]._state = state;
                _optimum[cur]._backs0 = _reps[0];
                _optimum[cur]._backs1 = _reps[1];
                _optimum[cur]._backs2 = _reps[2];
                _optimum[cur]._backs3 = _reps[3];
                UInt32 curPrice = _optimum[cur]._price;

                currentByte = _matchFinder.GetIndexByte(0 - 1);
                matchByte = _matchFinder.GetIndexByte((Int32)(0 - _reps[0] - 1 - 1));

                posState = (position & _posStateMask);

                UInt32 curAnd1Price = curPrice +
                                      _isMatch[(state._index << Base.K_NUM_POS_STATES_BITS_MAX) + posState].GetPrice0() +
                                      _literalEncoder.GetSubCoder(position, _matchFinder.GetIndexByte(0 - 2)).
                                                     GetPrice(!state.IsCharState(), matchByte, currentByte);

                Optimal nextOptimum = _optimum[cur + 1];

                bool nextIsChar = false;
                if (curAnd1Price < nextOptimum._price)
                {
                    nextOptimum._price = curAnd1Price;
                    nextOptimum._posPrev = cur;
                    nextOptimum.MakeAsChar();
                    nextIsChar = true;
                }

                matchPrice = curPrice + _isMatch[(state._index << Base.K_NUM_POS_STATES_BITS_MAX) + posState].GetPrice1();
                repMatchPrice = matchPrice + _isRep[state._index].GetPrice1();

                if (matchByte == currentByte &&
                    !(nextOptimum._posPrev < cur && nextOptimum._backPrev == 0))
                {
                    UInt32 shortRepPrice = repMatchPrice + GetRepLen1Price(state, posState);
                    if (shortRepPrice <= nextOptimum._price)
                    {
                        nextOptimum._price = shortRepPrice;
                        nextOptimum._posPrev = cur;
                        nextOptimum.MakeAsShortRep();
                        nextIsChar = true;
                    }
                }

                UInt32 numAvailableBytesFull = _matchFinder.GetNumAvailableBytes() + 1;
                numAvailableBytesFull = Math.Min(K_NUM_OPTS - 1 - cur, numAvailableBytesFull);
                numAvailableBytes = numAvailableBytesFull;

                if (numAvailableBytes < 2)
                {
                    continue;
                }
                if (numAvailableBytes > _numFastBytes)
                {
                    numAvailableBytes = _numFastBytes;
                }
                if (!nextIsChar && matchByte != currentByte)
                {
                    // try Literal + rep0
                    UInt32 t = Math.Min(numAvailableBytesFull - 1, _numFastBytes);
                    UInt32 lenTest2 = _matchFinder.GetMatchLen(0, _reps[0], t);
                    if (lenTest2 >= 2)
                    {
                        Base.State state2 = state;
                        state2.UpdateChar();
                        UInt32 posStateNext = (position + 1) & _posStateMask;
                        UInt32 nextRepMatchPrice = curAnd1Price +
                                                   _isMatch[(state2._index << Base.K_NUM_POS_STATES_BITS_MAX) + posStateNext]
                                                       .GetPrice1() +
                                                   _isRep[state2._index].GetPrice1();
                        {
                            UInt32 offset = cur + 1 + lenTest2;
                            while (lenEnd < offset)
                            {
                                _optimum[++lenEnd]._price = K_IFINITY_PRICE;
                            }
                            UInt32 curAndLenPrice = nextRepMatchPrice + GetRepPrice(
                                                                                    0, lenTest2, state2, posStateNext);
                            Optimal optimum = _optimum[offset];
                            if (curAndLenPrice < optimum._price)
                            {
                                optimum._price = curAndLenPrice;
                                optimum._posPrev = cur + 1;
                                optimum._backPrev = 0;
                                optimum._prev1IsChar = true;
                                optimum._prev2 = false;
                            }
                        }
                    }
                }

                UInt32 startLen = 2; // speed optimization 

                for (UInt32 repIndex = 0; repIndex < Base.K_NUM_REP_DISTANCES; repIndex++)
                {
                    UInt32 lenTest = _matchFinder.GetMatchLen(0 - 1, _reps[repIndex], numAvailableBytes);
                    if (lenTest < 2)
                    {
                        continue;
                    }
                    UInt32 lenTestTemp = lenTest;
                    do
                    {
                        while (lenEnd < cur + lenTest)
                        {
                            _optimum[++lenEnd]._price = K_IFINITY_PRICE;
                        }
                        UInt32 curAndLenPrice = repMatchPrice + GetRepPrice(repIndex, lenTest, state, posState);
                        Optimal optimum = _optimum[cur + lenTest];
                        if (curAndLenPrice < optimum._price)
                        {
                            optimum._price = curAndLenPrice;
                            optimum._posPrev = cur;
                            optimum._backPrev = repIndex;
                            optimum._prev1IsChar = false;
                        }
                    }
                    while (--lenTest >= 2);
                    lenTest = lenTestTemp;

                    if (repIndex == 0)
                    {
                        startLen = lenTest + 1;
                    }

                    // if (_maxMode)
                    if (lenTest < numAvailableBytesFull)
                    {
                        UInt32 t = Math.Min(numAvailableBytesFull - 1 - lenTest, _numFastBytes);
                        UInt32 lenTest2 = _matchFinder.GetMatchLen((Int32)lenTest, _reps[repIndex], t);
                        if (lenTest2 >= 2)
                        {
                            Base.State state2 = state;
                            state2.UpdateRep();
                            UInt32 posStateNext = (position + lenTest) & _posStateMask;
                            UInt32 curAndLenCharPrice =
                                repMatchPrice + GetRepPrice(repIndex, lenTest, state, posState) +
                                _isMatch[(state2._index << Base.K_NUM_POS_STATES_BITS_MAX) + posStateNext].GetPrice0() +
                                _literalEncoder.GetSubCoder(position + lenTest,
                                                            _matchFinder.GetIndexByte((Int32)lenTest - 1 - 1))
                                               .GetPrice(true,
                                                         _matchFinder.GetIndexByte(
                                                                                   (Int32)lenTest - 1 - (Int32)(_reps[repIndex] + 1)),
                                                         _matchFinder.GetIndexByte((Int32)lenTest - 1));
                            state2.UpdateChar();
                            posStateNext = (position + lenTest + 1) & _posStateMask;
                            UInt32 nextMatchPrice = curAndLenCharPrice +
                                                    _isMatch[(state2._index << Base.K_NUM_POS_STATES_BITS_MAX) + posStateNext]
                                                        .GetPrice1();
                            UInt32 nextRepMatchPrice = nextMatchPrice + _isRep[state2._index].GetPrice1();

                            // for(; lenTest2 >= 2; lenTest2--)
                            {
                                UInt32 offset = lenTest + 1 + lenTest2;
                                while (lenEnd < cur + offset)
                                {
                                    _optimum[++lenEnd]._price = K_IFINITY_PRICE;
                                }
                                UInt32 curAndLenPrice = nextRepMatchPrice +
                                                        GetRepPrice(0, lenTest2, state2, posStateNext);
                                Optimal optimum = _optimum[cur + offset];
                                if (curAndLenPrice < optimum._price)
                                {
                                    optimum._price = curAndLenPrice;
                                    optimum._posPrev = cur + lenTest + 1;
                                    optimum._backPrev = 0;
                                    optimum._prev1IsChar = true;
                                    optimum._prev2 = true;
                                    optimum._posPrev2 = cur;
                                    optimum._backPrev2 = repIndex;
                                }
                            }
                        }
                    }
                }

                if (newLen > numAvailableBytes)
                {
                    newLen = numAvailableBytes;
                    for (numDistancePairs = 0; newLen > _matchDistances[numDistancePairs]; numDistancePairs += 2)
                    {
                        ;
                    }
                    _matchDistances[numDistancePairs] = newLen;
                    numDistancePairs += 2;
                }
                if (newLen >= startLen)
                {
                    normalMatchPrice = matchPrice + _isRep[state._index].GetPrice0();
                    while (lenEnd < cur + newLen)
                    {
                        _optimum[++lenEnd]._price = K_IFINITY_PRICE;
                    }

                    UInt32 offs = 0;
                    while (startLen > _matchDistances[offs])
                    {
                        offs += 2;
                    }

                    for (UInt32 lenTest = startLen;; lenTest++)
                    {
                        UInt32 curBack = _matchDistances[offs + 1];
                        UInt32 curAndLenPrice = normalMatchPrice + GetPosLenPrice(curBack, lenTest, posState);
                        Optimal optimum = _optimum[cur + lenTest];
                        if (curAndLenPrice < optimum._price)
                        {
                            optimum._price = curAndLenPrice;
                            optimum._posPrev = cur;
                            optimum._backPrev = curBack + Base.K_NUM_REP_DISTANCES;
                            optimum._prev1IsChar = false;
                        }

                        if (lenTest == _matchDistances[offs])
                        {
                            if (lenTest < numAvailableBytesFull)
                            {
                                UInt32 t = Math.Min(numAvailableBytesFull - 1 - lenTest, _numFastBytes);
                                UInt32 lenTest2 = _matchFinder.GetMatchLen((Int32)lenTest, curBack, t);
                                if (lenTest2 >= 2)
                                {
                                    Base.State state2 = state;
                                    state2.UpdateMatch();
                                    UInt32 posStateNext = (position + lenTest) & _posStateMask;
                                    UInt32 curAndLenCharPrice = curAndLenPrice +
                                                                _isMatch[
                                                                         (state2._index << Base.K_NUM_POS_STATES_BITS_MAX) +
                                                                         posStateNext].GetPrice0() +
                                                                _literalEncoder.GetSubCoder(position + lenTest,
                                                                                            _matchFinder.GetIndexByte(
                                                                                                                      (Int32)lenTest - 1 - 1))
                                                                               .
                                                                               GetPrice(true,
                                                                                        _matchFinder.GetIndexByte(
                                                                                                                  (Int32)lenTest -
                                                                                                                  (Int32)(curBack + 1) - 1),
                                                                                        _matchFinder.GetIndexByte(
                                                                                                                  (Int32)lenTest - 1));
                                    state2.UpdateChar();
                                    posStateNext = (position + lenTest + 1) & _posStateMask;
                                    UInt32 nextMatchPrice = curAndLenCharPrice +
                                                            _isMatch[
                                                                     (state2._index << Base.K_NUM_POS_STATES_BITS_MAX) +
                                                                     posStateNext].GetPrice1();
                                    UInt32 nextRepMatchPrice = nextMatchPrice + _isRep[state2._index].GetPrice1();

                                    UInt32 offset = lenTest + 1 + lenTest2;
                                    while (lenEnd < cur + offset)
                                    {
                                        _optimum[++lenEnd]._price = K_IFINITY_PRICE;
                                    }
                                    curAndLenPrice = nextRepMatchPrice + GetRepPrice(0, lenTest2, state2, posStateNext);
                                    optimum = _optimum[cur + offset];
                                    if (curAndLenPrice < optimum._price)
                                    {
                                        optimum._price = curAndLenPrice;
                                        optimum._posPrev = cur + lenTest + 1;
                                        optimum._backPrev = 0;
                                        optimum._prev1IsChar = true;
                                        optimum._prev2 = true;
                                        optimum._posPrev2 = cur;
                                        optimum._backPrev2 = curBack + Base.K_NUM_REP_DISTANCES;
                                    }
                                }
                            }
                            offs += 2;
                            if (offs == numDistancePairs)
                            {
                                break;
                            }
                        }
                    }
                }
            }
        }

        private bool ChangePair(UInt32 smallDist, UInt32 bigDist)
        {
            const int kDif = 7;
            return (smallDist < ((UInt32)(1) << (32 - kDif)) && bigDist >= (smallDist << kDif));
        }

        private void WriteEndMarker(UInt32 posState)
        {
            if (!_writeEndMark)
            {
                return;
            }

            _isMatch[(_state._index << Base.K_NUM_POS_STATES_BITS_MAX) + posState].Encode(_rangeEncoder, 1);
            _isRep[_state._index].Encode(_rangeEncoder, 0);
            _state.UpdateMatch();
            UInt32 len = Base.K_MATCH_MIN_LEN;
            _lenEncoder.Encode(_rangeEncoder, len - Base.K_MATCH_MIN_LEN, posState);
            UInt32 posSlot = (1 << Base.K_NUM_POS_SLOT_BITS) - 1;
            UInt32 lenToPosState = Base.GetLenToPosState(len);
            _posSlotEncoder[lenToPosState].Encode(_rangeEncoder, posSlot);
            int footerBits = 30;
            UInt32 posReduced = (((UInt32)1) << footerBits) - 1;
            _rangeEncoder.EncodeDirectBits(posReduced >> Base.K_NUM_ALIGN_BITS, footerBits - Base.K_NUM_ALIGN_BITS);
            _posAlignEncoder.ReverseEncode(_rangeEncoder, posReduced & Base.K_ALIGN_MASK);
        }

        private void Flush(UInt32 nowPos)
        {
            ReleaseMfStream();
            WriteEndMarker(nowPos & _posStateMask);
            _rangeEncoder.FlushData();
            _rangeEncoder.FlushStream();
        }

        public void CodeOneBlock(out Int64 inSize, out Int64 outSize, out bool finished)
        {
            inSize = 0;
            outSize = 0;
            finished = true;

            if (_inStream != null)
            {
                _matchFinder.SetStream(_inStream);
                _needReleaseMfStream = true;
                _inStream = null;
            }

            if (_finished)
            {
                return;
            }
            _finished = true;

            Int64 progressPosValuePrev = _nowPos64;
            if (_nowPos64 == 0)
            {
                if (_trainSize > 0)
                {
                    for (; _trainSize > 0 && (!_processingMode || !_matchFinder.IsDataStarved); _trainSize--)
                    {
                        _matchFinder.Skip(1);
                    }
                    if (_trainSize == 0)
                    {
                        _previousByte = _matchFinder.GetIndexByte(-1);
                    }
                }
                if (_processingMode && _matchFinder.IsDataStarved)
                {
                    _finished = false;
                    return;
                }
                if (_matchFinder.GetNumAvailableBytes() == 0)
                {
                    Flush((UInt32)_nowPos64);
                    return;
                }
                UInt32 len, numDistancePairs; // it's not used
                ReadMatchDistances(out len, out numDistancePairs);
                UInt32 posState = (UInt32)(_nowPos64) & _posStateMask;
                _isMatch[(_state._index << Base.K_NUM_POS_STATES_BITS_MAX) + posState].Encode(_rangeEncoder, 0);
                _state.UpdateChar();
                Byte curByte = _matchFinder.GetIndexByte((Int32)(0 - _additionalOffset));
                _literalEncoder.GetSubCoder((UInt32)(_nowPos64), _previousByte).Encode(_rangeEncoder, curByte);
                _previousByte = curByte;
                _additionalOffset--;
                _nowPos64++;
            }
            if (_processingMode && _matchFinder.IsDataStarved)
            {
                _finished = false;
                return;
            }
            if (_matchFinder.GetNumAvailableBytes() == 0)
            {
                Flush((UInt32)_nowPos64);
                return;
            }
            while (true)
            {
                if (_processingMode && _matchFinder.IsDataStarved)
                {
                    _finished = false;
                    return;
                }

                UInt32 pos;
                UInt32 len = GetOptimum((UInt32)_nowPos64, out pos);

                UInt32 posState = ((UInt32)_nowPos64) & _posStateMask;
                UInt32 complexState = (_state._index << Base.K_NUM_POS_STATES_BITS_MAX) + posState;
                if (len == 1 && pos == 0xFFFFFFFF)
                {
                    _isMatch[complexState].Encode(_rangeEncoder, 0);
                    Byte curByte = _matchFinder.GetIndexByte((Int32)(0 - _additionalOffset));
                    LiteralEncoder.Encoder2 subCoder = _literalEncoder.GetSubCoder((UInt32)_nowPos64, _previousByte);
                    if (!_state.IsCharState())
                    {
                        Byte matchByte =
                            _matchFinder.GetIndexByte((Int32)(0 - _repDistances[0] - 1 - _additionalOffset));
                        subCoder.EncodeMatched(_rangeEncoder, matchByte, curByte);
                    }
                    else
                    {
                        subCoder.Encode(_rangeEncoder, curByte);
                    }
                    _previousByte = curByte;
                    _state.UpdateChar();
                }
                else
                {
                    _isMatch[complexState].Encode(_rangeEncoder, 1);
                    if (pos < Base.K_NUM_REP_DISTANCES)
                    {
                        _isRep[_state._index].Encode(_rangeEncoder, 1);
                        if (pos == 0)
                        {
                            _isRepG0[_state._index].Encode(_rangeEncoder, 0);
                            if (len == 1)
                            {
                                _isRep0Long[complexState].Encode(_rangeEncoder, 0);
                            }
                            else
                            {
                                _isRep0Long[complexState].Encode(_rangeEncoder, 1);
                            }
                        }
                        else
                        {
                            _isRepG0[_state._index].Encode(_rangeEncoder, 1);
                            if (pos == 1)
                            {
                                _isRepG1[_state._index].Encode(_rangeEncoder, 0);
                            }
                            else
                            {
                                _isRepG1[_state._index].Encode(_rangeEncoder, 1);
                                _isRepG2[_state._index].Encode(_rangeEncoder, pos - 2);
                            }
                        }
                        if (len == 1)
                        {
                            _state.UpdateShortRep();
                        }
                        else
                        {
                            _repMatchLenEncoder.Encode(_rangeEncoder, len - Base.K_MATCH_MIN_LEN, posState);
                            _state.UpdateRep();
                        }
                        UInt32 distance = _repDistances[pos];
                        if (pos != 0)
                        {
                            for (UInt32 i = pos; i >= 1; i--)
                            {
                                _repDistances[i] = _repDistances[i - 1];
                            }
                            _repDistances[0] = distance;
                        }
                    }
                    else
                    {
                        _isRep[_state._index].Encode(_rangeEncoder, 0);
                        _state.UpdateMatch();
                        _lenEncoder.Encode(_rangeEncoder, len - Base.K_MATCH_MIN_LEN, posState);
                        pos -= Base.K_NUM_REP_DISTANCES;
                        UInt32 posSlot = GetPosSlot(pos);
                        UInt32 lenToPosState = Base.GetLenToPosState(len);
                        _posSlotEncoder[lenToPosState].Encode(_rangeEncoder, posSlot);

                        if (posSlot >= Base.K_START_POS_MODEL_INDEX)
                        {
                            int footerBits = (int)((posSlot >> 1) - 1);
                            UInt32 baseVal = ((2 | (posSlot & 1)) << footerBits);
                            UInt32 posReduced = pos - baseVal;

                            if (posSlot < Base.K_END_POS_MODEL_INDEX)
                            {
                                BitTreeEncoder.ReverseEncode(_posEncoders,
                                                             baseVal - posSlot - 1, _rangeEncoder, footerBits,
                                                             posReduced);
                            }
                            else
                            {
                                _rangeEncoder.EncodeDirectBits(posReduced >> Base.K_NUM_ALIGN_BITS,
                                                               footerBits - Base.K_NUM_ALIGN_BITS);
                                _posAlignEncoder.ReverseEncode(_rangeEncoder, posReduced & Base.K_ALIGN_MASK);
                                _alignPriceCount++;
                            }
                        }
                        UInt32 distance = pos;
                        for (UInt32 i = Base.K_NUM_REP_DISTANCES - 1; i >= 1; i--)
                        {
                            _repDistances[i] = _repDistances[i - 1];
                        }
                        _repDistances[0] = distance;
                        _matchPriceCount++;
                    }
                    _previousByte = _matchFinder.GetIndexByte((Int32)(len - 1 - _additionalOffset));
                }
                _additionalOffset -= len;
                _nowPos64 += len;
                if (_additionalOffset == 0)
                {
                    // if (!_fastMode)
                    if (_matchPriceCount >= (1 << 7))
                    {
                        FillDistancesPrices();
                    }
                    if (_alignPriceCount >= Base.K_ALIGN_TABLE_SIZE)
                    {
                        FillAlignPrices();
                    }
                    inSize = _nowPos64;
                    outSize = _rangeEncoder.GetProcessedSizeAdd();
                    if (_processingMode && _matchFinder.IsDataStarved)
                    {
                        _finished = false;
                        return;
                    }
                    if (_matchFinder.GetNumAvailableBytes() == 0)
                    {
                        Flush((UInt32)_nowPos64);
                        return;
                    }

                    if (_nowPos64 - progressPosValuePrev >= (1 << 12))
                    {
                        _finished = false;
                        finished = false;
                        return;
                    }
                }
            }
        }

        private void ReleaseMfStream()
        {
            if (_matchFinder != null && _needReleaseMfStream)
            {
                _matchFinder.ReleaseStream();
                _needReleaseMfStream = false;
            }
        }

        private void SetOutStream(Stream outStream)
        {
            _rangeEncoder.SetStream(outStream);
        }

        private void ReleaseOutStream()
        {
            _rangeEncoder.ReleaseStream();
        }

        private void ReleaseStreams()
        {
            ReleaseMfStream();
            ReleaseOutStream();
        }

        public void SetStreams(Stream inStream, Stream outStream,
                               Int64 inSize, Int64 outSize)
        {
            _inStream = inStream;
            _finished = false;
            Create();
            SetOutStream(outStream);
            Init();
            _matchFinder.Init();

            // if (!_fastMode)
            {
                FillDistancesPrices();
                FillAlignPrices();
            }

            _lenEncoder.SetTableSize(_numFastBytes + 1 - Base.K_MATCH_MIN_LEN);
            _lenEncoder.UpdateTables((UInt32)1 << _posStateBits);
            _repMatchLenEncoder.SetTableSize(_numFastBytes + 1 - Base.K_MATCH_MIN_LEN);
            _repMatchLenEncoder.UpdateTables((UInt32)1 << _posStateBits);

            _nowPos64 = 0;
        }

        public void Code(Stream inStream, Stream outStream,
                         Int64 inSize, Int64 outSize, ICodeProgress progress)
        {
            _needReleaseMfStream = false;
            _processingMode = false;
            try
            {
                SetStreams(inStream, outStream, inSize, outSize);
                while (true)
                {
                    Int64 processedInSize;
                    Int64 processedOutSize;
                    bool finished;
                    CodeOneBlock(out processedInSize, out processedOutSize, out finished);
                    if (finished)
                    {
                        return;
                    }
                    if (progress != null)
                    {
                        progress.SetProgress(processedInSize, processedOutSize);
                    }
                }
            }
            finally
            {
                ReleaseStreams();
            }
        }

        public long Code(Stream inStream, bool final)
        {
            _matchFinder.SetStream(inStream);
            _processingMode = !final;
            try
            {
                while (true)
                {
                    Int64 processedInSize;
                    Int64 processedOutSize;
                    bool finished;
                    CodeOneBlock(out processedInSize, out processedOutSize, out finished);
                    if (finished)
                    {
                        return processedInSize;
                    }
                }
            }
            finally
            {
                _matchFinder.ReleaseStream();
                if (final)
                {
                    ReleaseStreams();
                }
            }
        }

        public void Train(Stream trainStream)
        {
            if (_nowPos64 > 0)
            {
                throw new InvalidOperationException();
            }
            _trainSize = (uint)trainStream.Length;
            if (_trainSize > 0)
            {
                _matchFinder.SetStream(trainStream);
                for (; _trainSize > 0 && !_matchFinder.IsDataStarved; _trainSize--)
                {
                    _matchFinder.Skip(1);
                }
                if (_trainSize == 0)
                {
                    _previousByte = _matchFinder.GetIndexByte(-1);
                }
                _matchFinder.ReleaseStream();
            }
        }

        private const int K_PROP_SIZE = 5;
        private readonly Byte[] _properties = new Byte[K_PROP_SIZE];

        public void WriteCoderProperties(Stream outStream)
        {
            _properties[0] = (Byte)((_posStateBits * 5 + _numLiteralPosStateBits) * 9 + _numLiteralContextBits);
            for (int i = 0; i < 4; i++)
            {
                _properties[1 + i] = (Byte)((_dictionarySize >> (8 * i)) & 0xFF);
            }
            outStream.Write(_properties, 0, K_PROP_SIZE);
        }

        private readonly UInt32[] _tempPrices = new UInt32[Base.K_NUM_FULL_DISTANCES];
        private UInt32 _matchPriceCount;

        private void FillDistancesPrices()
        {
            for (UInt32 i = Base.K_START_POS_MODEL_INDEX; i < Base.K_NUM_FULL_DISTANCES; i++)
            {
                UInt32 posSlot = GetPosSlot(i);
                int footerBits = (int)((posSlot >> 1) - 1);
                UInt32 baseVal = ((2 | (posSlot & 1)) << footerBits);
                _tempPrices[i] = BitTreeEncoder.ReverseGetPrice(_posEncoders,
                                                               baseVal - posSlot - 1, footerBits, i - baseVal);
            }

            for (UInt32 lenToPosState = 0; lenToPosState < Base.K_NUM_LEN_TO_POS_STATES; lenToPosState++)
            {
                UInt32 posSlot;
                BitTreeEncoder encoder = _posSlotEncoder[lenToPosState];

                UInt32 st = (lenToPosState << Base.K_NUM_POS_SLOT_BITS);
                for (posSlot = 0; posSlot < _distTableSize; posSlot++)
                {
                    _posSlotPrices[st + posSlot] = encoder.GetPrice(posSlot);
                }
                for (posSlot = Base.K_END_POS_MODEL_INDEX; posSlot < _distTableSize; posSlot++)
                {
                    _posSlotPrices[st + posSlot] += ((((posSlot >> 1) - 1) - Base.K_NUM_ALIGN_BITS) <<
                                                     BitEncoder.K_NUM_BIT_PRICE_SHIFT_BITS);
                }

                UInt32 st2 = lenToPosState * Base.K_NUM_FULL_DISTANCES;
                UInt32 i;
                for (i = 0; i < Base.K_START_POS_MODEL_INDEX; i++)
                {
                    _distancesPrices[st2 + i] = _posSlotPrices[st + i];
                }
                for (; i < Base.K_NUM_FULL_DISTANCES; i++)
                {
                    _distancesPrices[st2 + i] = _posSlotPrices[st + GetPosSlot(i)] + _tempPrices[i];
                }
            }
            _matchPriceCount = 0;
        }

        private void FillAlignPrices()
        {
            for (UInt32 i = 0; i < Base.K_ALIGN_TABLE_SIZE; i++)
            {
                _alignPrices[i] = _posAlignEncoder.ReverseGetPrice(i);
            }
            _alignPriceCount = 0;
        }

        private static readonly string[] K_MATCH_FINDER_I_DS =
        {
            "BT2",
            "BT4"
        };

        private static int FindMatchFinder(string s)
        {
            for (int m = 0; m < K_MATCH_FINDER_I_DS.Length; m++)
            {
                if (s == K_MATCH_FINDER_I_DS[m])
                {
                    return m;
                }
            }
            return -1;
        }

        public void SetCoderProperties(CoderPropId[] propIDs, object[] properties)
        {
            for (UInt32 i = 0; i < properties.Length; i++)
            {
                object prop = properties[i];
                switch (propIDs[i])
                {
                    case CoderPropId.NumFastBytes:
                    {
                        if (!(prop is Int32))
                        {
                            throw new InvalidParamException();
                        }
                        Int32 numFastBytes = (Int32)prop;
                        if (numFastBytes < 5 || numFastBytes > Base.K_MATCH_MAX_LEN)
                        {
                            throw new InvalidParamException();
                        }
                        _numFastBytes = (UInt32)numFastBytes;
                        break;
                    }
                    case CoderPropId.Algorithm:
                    {
                        /*
                        if (!(prop is Int32))
                            throw new InvalidParamException();
                        Int32 maximize = (Int32)prop;
                        _fastMode = (maximize == 0);
                        _maxMode = (maximize >= 2);
                        */
                        break;
                    }
                    case CoderPropId.MatchFinder:
                    {
                        if (!(prop is String))
                        {
                            throw new InvalidParamException();
                        }
                        EMatchFinderType matchFinderIndexPrev = _matchFinderType;
                        int m = FindMatchFinder(((string)prop).ToUpper());
                        if (m < 0)
                        {
                            throw new InvalidParamException();
                        }
                        _matchFinderType = (EMatchFinderType)m;
                        if (_matchFinder != null && matchFinderIndexPrev != _matchFinderType)
                        {
                            _dictionarySizePrev = 0xFFFFFFFF;
                            _matchFinder = null;
                        }
                        break;
                    }
                    case CoderPropId.DictionarySize:
                    {
                        const int kDicLogSizeMaxCompress = 30;
                        if (!(prop is Int32))
                        {
                            throw new InvalidParamException();
                        }
                        ;
                        Int32 dictionarySize = (Int32)prop;
                        if (dictionarySize < (UInt32)(1 << Base.K_DIC_LOG_SIZE_MIN) ||
                            dictionarySize > (UInt32)(1 << kDicLogSizeMaxCompress))
                        {
                            throw new InvalidParamException();
                        }
                        _dictionarySize = (UInt32)dictionarySize;
                        int dicLogSize;
                        for (dicLogSize = 0; dicLogSize < (UInt32)kDicLogSizeMaxCompress; dicLogSize++)
                        {
                            if (dictionarySize <= ((UInt32)(1) << dicLogSize))
                            {
                                break;
                            }
                        }
                        _distTableSize = (UInt32)dicLogSize * 2;
                        break;
                    }
                    case CoderPropId.PosStateBits:
                    {
                        if (!(prop is Int32))
                        {
                            throw new InvalidParamException();
                        }
                        Int32 v = (Int32)prop;
                        if (v < 0 || v > (UInt32)Base.K_NUM_POS_STATES_BITS_ENCODING_MAX)
                        {
                            throw new InvalidParamException();
                        }
                        _posStateBits = v;
                        _posStateMask = (((UInt32)1) << _posStateBits) - 1;
                        break;
                    }
                    case CoderPropId.LitPosBits:
                    {
                        if (!(prop is Int32))
                        {
                            throw new InvalidParamException();
                        }
                        Int32 v = (Int32)prop;
                        if (v < 0 || v > Base.K_NUM_LIT_POS_STATES_BITS_ENCODING_MAX)
                        {
                            throw new InvalidParamException();
                        }
                        _numLiteralPosStateBits = v;
                        break;
                    }
                    case CoderPropId.LitContextBits:
                    {
                        if (!(prop is Int32))
                        {
                            throw new InvalidParamException();
                        }
                        Int32 v = (Int32)prop;
                        if (v < 0 || v > Base.K_NUM_LIT_CONTEXT_BITS_MAX)
                        {
                            throw new InvalidParamException();
                        }
                        ;
                        _numLiteralContextBits = v;
                        break;
                    }
                    case CoderPropId.EndMarker:
                    {
                        if (!(prop is Boolean))
                        {
                            throw new InvalidParamException();
                        }
                        SetWriteEndMarkerMode((Boolean)prop);
                        break;
                    }
                    default:
                        throw new InvalidParamException();
                }
            }
        }

        private uint _trainSize;

        public void SetTrainSize(uint trainSize)
        {
            _trainSize = trainSize;
        }
    }
}