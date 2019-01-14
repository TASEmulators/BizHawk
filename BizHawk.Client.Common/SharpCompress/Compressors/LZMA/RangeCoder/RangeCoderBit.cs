using System;

namespace SharpCompress.Compressors.LZMA.RangeCoder
{
    internal struct BitEncoder
    {
        public const int K_NUM_BIT_MODEL_TOTAL_BITS = 11;
        public const uint K_BIT_MODEL_TOTAL = (1 << K_NUM_BIT_MODEL_TOTAL_BITS);
        private const int K_NUM_MOVE_BITS = 5;
        private const int K_NUM_MOVE_REDUCING_BITS = 2;
        public const int K_NUM_BIT_PRICE_SHIFT_BITS = 6;

        private uint _prob;

        public void Init()
        {
            _prob = K_BIT_MODEL_TOTAL >> 1;
        }

        public void UpdateModel(uint symbol)
        {
            if (symbol == 0)
            {
                _prob += (K_BIT_MODEL_TOTAL - _prob) >> K_NUM_MOVE_BITS;
            }
            else
            {
                _prob -= (_prob) >> K_NUM_MOVE_BITS;
            }
        }

        public void Encode(Encoder encoder, uint symbol)
        {
            // encoder.EncodeBit(Prob, kNumBitModelTotalBits, symbol);
            // UpdateModel(symbol);
            uint newBound = (encoder._range >> K_NUM_BIT_MODEL_TOTAL_BITS) * _prob;
            if (symbol == 0)
            {
                encoder._range = newBound;
                _prob += (K_BIT_MODEL_TOTAL - _prob) >> K_NUM_MOVE_BITS;
            }
            else
            {
                encoder._low += newBound;
                encoder._range -= newBound;
                _prob -= (_prob) >> K_NUM_MOVE_BITS;
            }
            if (encoder._range < Encoder.K_TOP_VALUE)
            {
                encoder._range <<= 8;
                encoder.ShiftLow();
            }
        }

        private static readonly UInt32[] PROB_PRICES = new UInt32[K_BIT_MODEL_TOTAL >> K_NUM_MOVE_REDUCING_BITS];

        static BitEncoder()
        {
            const int kNumBits = (K_NUM_BIT_MODEL_TOTAL_BITS - K_NUM_MOVE_REDUCING_BITS);
            for (int i = kNumBits - 1; i >= 0; i--)
            {
                UInt32 start = (UInt32)1 << (kNumBits - i - 1);
                UInt32 end = (UInt32)1 << (kNumBits - i);
                for (UInt32 j = start; j < end; j++)
                {
                    PROB_PRICES[j] = ((UInt32)i << K_NUM_BIT_PRICE_SHIFT_BITS) +
                                    (((end - j) << K_NUM_BIT_PRICE_SHIFT_BITS) >> (kNumBits - i - 1));
                }
            }
        }

        public uint GetPrice(uint symbol)
        {
            return PROB_PRICES[(((_prob - symbol) ^ ((-(int)symbol))) & (K_BIT_MODEL_TOTAL - 1)) >> K_NUM_MOVE_REDUCING_BITS];
        }

        public uint GetPrice0()
        {
            return PROB_PRICES[_prob >> K_NUM_MOVE_REDUCING_BITS];
        }

        public uint GetPrice1()
        {
            return PROB_PRICES[(K_BIT_MODEL_TOTAL - _prob) >> K_NUM_MOVE_REDUCING_BITS];
        }
    }

    internal struct BitDecoder
    {
        public const int K_NUM_BIT_MODEL_TOTAL_BITS = 11;
        public const uint K_BIT_MODEL_TOTAL = (1 << K_NUM_BIT_MODEL_TOTAL_BITS);
        private const int K_NUM_MOVE_BITS = 5;

        private uint _prob;

        public void UpdateModel(int numMoveBits, uint symbol)
        {
            if (symbol == 0)
            {
                _prob += (K_BIT_MODEL_TOTAL - _prob) >> numMoveBits;
            }
            else
            {
                _prob -= (_prob) >> numMoveBits;
            }
        }

        public void Init()
        {
            _prob = K_BIT_MODEL_TOTAL >> 1;
        }

        public uint Decode(Decoder rangeDecoder)
        {
            uint newBound = (rangeDecoder._range >> K_NUM_BIT_MODEL_TOTAL_BITS) * _prob;
            if (rangeDecoder._code < newBound)
            {
                rangeDecoder._range = newBound;
                _prob += (K_BIT_MODEL_TOTAL - _prob) >> K_NUM_MOVE_BITS;
                if (rangeDecoder._range < Decoder.K_TOP_VALUE)
                {
                    rangeDecoder._code = (rangeDecoder._code << 8) | (byte)rangeDecoder._stream.ReadByte();
                    rangeDecoder._range <<= 8;
                    rangeDecoder._total++;
                }
                return 0;
            }
            rangeDecoder._range -= newBound;
            rangeDecoder._code -= newBound;
            _prob -= (_prob) >> K_NUM_MOVE_BITS;
            if (rangeDecoder._range < Decoder.K_TOP_VALUE)
            {
                rangeDecoder._code = (rangeDecoder._code << 8) | (byte)rangeDecoder._stream.ReadByte();
                rangeDecoder._range <<= 8;
                rangeDecoder._total++;
            }
            return 1;
        }
    }
}