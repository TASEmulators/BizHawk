namespace EMU7800.Core
{
    public sealed class Machine2600PAL : Machine2600
    {
        public override string ToString()
        {
            return GetType().Name;
        }

        public Machine2600PAL(Cart cart, ILogger logger)
            : base(cart, logger, 312, 32, 50, 31200 /* PAL_SAMPLES_PER_SEC */, TIATables.PALPalette)
        {
        }

        #region Serialization Members

        public Machine2600PAL(DeserializationContext input) : base(input, TIATables.PALPalette)
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
