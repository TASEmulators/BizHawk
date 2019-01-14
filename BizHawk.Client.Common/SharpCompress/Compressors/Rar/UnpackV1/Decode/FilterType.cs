namespace SharpCompress.Compressors.Rar.UnpackV1.Decode
{
    internal enum FilterType : byte
    {
          // These values must not be changed, because we use them directly
          // in RAR5 compression and decompression code.
          FILTER_DELTA=0, FILTER_E8, FILTER_E8E9, FILTER_ARM,
          FILTER_AUDIO, FILTER_RGB, FILTER_ITANIUM, FILTER_PPM, FILTER_NONE
    }
}