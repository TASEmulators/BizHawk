namespace EMU7800.Core
{
    public sealed class Machine2600NTSC : Machine2600
    {
        public override string ToString()
        {
            return GetType().Name;
        }

        public Machine2600NTSC(Cart cart, ILogger logger)
            : base(cart, logger, 262, 16, 60, 31440 /* NTSC_SAMPLES_PER_SEC */, TIATables.NTSCPalette)
        {
        }

        #region Serialization Members

        public Machine2600NTSC(DeserializationContext input) : base(input, TIATables.NTSCPalette)
        {
            input.CheckVersion(1);
        }

        public override void GetObjectData(SerializationContext output)
        {
            base.GetObjectData(output);
            output.WriteVersion(1);
        }

        #endregion
    }
}
