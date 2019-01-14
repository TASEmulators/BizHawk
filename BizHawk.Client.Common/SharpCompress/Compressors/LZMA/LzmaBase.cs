namespace SharpCompress.Compressors.LZMA
{
    internal abstract class Base
    {
        public const uint K_NUM_REP_DISTANCES = 4;
        public const uint K_NUM_STATES = 12;

        // static byte []kLiteralNextStates  = {0, 0, 0, 0, 1, 2, 3, 4,  5,  6,   4, 5};
        // static byte []kMatchNextStates    = {7, 7, 7, 7, 7, 7, 7, 10, 10, 10, 10, 10};
        // static byte []kRepNextStates      = {8, 8, 8, 8, 8, 8, 8, 11, 11, 11, 11, 11};
        // static byte []kShortRepNextStates = {9, 9, 9, 9, 9, 9, 9, 11, 11, 11, 11, 11};

        public struct State
        {
            public uint _index;

            public void Init()
            {
                _index = 0;
            }

            public void UpdateChar()
            {
                if (_index < 4)
                {
                    _index = 0;
                }
                else if (_index < 10)
                {
                    _index -= 3;
                }
                else
                {
                    _index -= 6;
                }
            }

            public void UpdateMatch()
            {
                _index = (uint)(_index < 7 ? 7 : 10);
            }

            public void UpdateRep()
            {
                _index = (uint)(_index < 7 ? 8 : 11);
            }

            public void UpdateShortRep()
            {
                _index = (uint)(_index < 7 ? 9 : 11);
            }

            public bool IsCharState()
            {
                return _index < 7;
            }
        }

        public const int K_NUM_POS_SLOT_BITS = 6;
        public const int K_DIC_LOG_SIZE_MIN = 0;

        // public const int kDicLogSizeMax = 30;
        // public const uint kDistTableSizeMax = kDicLogSizeMax * 2;

        public const int K_NUM_LEN_TO_POS_STATES_BITS = 2; // it's for speed optimization
        public const uint K_NUM_LEN_TO_POS_STATES = 1 << K_NUM_LEN_TO_POS_STATES_BITS;

        public const uint K_MATCH_MIN_LEN = 2;

        public static uint GetLenToPosState(uint len)
        {
            len -= K_MATCH_MIN_LEN;
            if (len < K_NUM_LEN_TO_POS_STATES)
            {
                return len;
            }
            return K_NUM_LEN_TO_POS_STATES - 1;
        }

        public const int K_NUM_ALIGN_BITS = 4;
        public const uint K_ALIGN_TABLE_SIZE = 1 << K_NUM_ALIGN_BITS;
        public const uint K_ALIGN_MASK = (K_ALIGN_TABLE_SIZE - 1);

        public const uint K_START_POS_MODEL_INDEX = 4;
        public const uint K_END_POS_MODEL_INDEX = 14;
        public const uint K_NUM_POS_MODELS = K_END_POS_MODEL_INDEX - K_START_POS_MODEL_INDEX;

        public const uint K_NUM_FULL_DISTANCES = 1 << ((int)K_END_POS_MODEL_INDEX / 2);

        public const uint K_NUM_LIT_POS_STATES_BITS_ENCODING_MAX = 4;
        public const uint K_NUM_LIT_CONTEXT_BITS_MAX = 8;

        public const int K_NUM_POS_STATES_BITS_MAX = 4;
        public const uint K_NUM_POS_STATES_MAX = (1 << K_NUM_POS_STATES_BITS_MAX);
        public const int K_NUM_POS_STATES_BITS_ENCODING_MAX = 4;
        public const uint K_NUM_POS_STATES_ENCODING_MAX = (1 << K_NUM_POS_STATES_BITS_ENCODING_MAX);

        public const int K_NUM_LOW_LEN_BITS = 3;
        public const int K_NUM_MID_LEN_BITS = 3;
        public const int K_NUM_HIGH_LEN_BITS = 8;
        public const uint K_NUM_LOW_LEN_SYMBOLS = 1 << K_NUM_LOW_LEN_BITS;
        public const uint K_NUM_MID_LEN_SYMBOLS = 1 << K_NUM_MID_LEN_BITS;

        public const uint K_NUM_LEN_SYMBOLS = K_NUM_LOW_LEN_SYMBOLS + K_NUM_MID_LEN_SYMBOLS +
                                           (1 << K_NUM_HIGH_LEN_BITS);

        public const uint K_MATCH_MAX_LEN = K_MATCH_MIN_LEN + K_NUM_LEN_SYMBOLS - 1;
    }
}