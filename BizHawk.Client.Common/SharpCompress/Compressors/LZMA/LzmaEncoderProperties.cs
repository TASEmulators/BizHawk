namespace SharpCompress.Compressors.LZMA
{
    public class LzmaEncoderProperties
    {
        internal CoderPropId[] _propIDs;
        internal object[] _properties;

        public LzmaEncoderProperties()
            : this(false)
        {
        }

        public LzmaEncoderProperties(bool eos)
            : this(eos, 1 << 20)
        {
        }

        public LzmaEncoderProperties(bool eos, int dictionary)
            : this(eos, dictionary, 32)
        {
        }

        public LzmaEncoderProperties(bool eos, int dictionary, int numFastBytes)
        {
            int posStateBits = 2;
            int litContextBits = 3;
            int litPosBits = 0;
            int algorithm = 2;
            string mf = "bt4";

            _propIDs = new[]
                      {
                          CoderPropId.DictionarySize,
                          CoderPropId.PosStateBits,
                          CoderPropId.LitContextBits,
                          CoderPropId.LitPosBits,
                          CoderPropId.Algorithm,
                          CoderPropId.NumFastBytes,
                          CoderPropId.MatchFinder,
                          CoderPropId.EndMarker
                      };
            _properties = new object[]
                         {
                             dictionary,
                             posStateBits,
                             litContextBits,
                             litPosBits,
                             algorithm,
                             numFastBytes,
                             mf,
                             eos
                         };
        }
    }
}