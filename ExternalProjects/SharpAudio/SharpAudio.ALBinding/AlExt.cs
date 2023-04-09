#pragma warning disable CS1591

namespace SharpAudio.ALBinding
{
    public static unsafe partial class AlNative
    {
        public const int AL_FORMAT_MONO_FLOAT32 = 0x10010;
        public const int AL_FORMAT_STEREO_FLOAT32 = 0x10011;

        public const int AL_LOOP_POINTS_SOFT = 0x2015;

        public const int AL_UNPACK_BLOCK_ALIGNMENT_SOFT = 0x200C;
        public const int AL_PACK_BLOCK_ALIGNMENT_SOFT = 0x200D;

        public const int AL_FORMAT_MONO_MSADPCM_SOFT = 0x1302;
        public const int AL_FORMAT_STEREO_MSADPCM_SOFT = 0x1303;

        public const int AL_BYTE_SOFT = 0x1400;
        public const int AL_SHORT_SOFT = 0x1402;
        public const int AL_FLOAT_SOFT = 0x1406;

        public const int AL_MONO_SOFT = 0x1500;
        public const int AL_STEREO_SOFT = 0x1501;

        public const int AL_GAIN_LIMIT_SOFT = 0x200E;
    }
}
