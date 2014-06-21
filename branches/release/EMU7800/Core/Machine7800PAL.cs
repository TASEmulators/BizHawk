namespace EMU7800.Core
{
    public sealed class Machine7800PAL : Machine7800
    {
        public override string ToString()
        {
            return GetType().Name;
        }

        public Machine7800PAL(Cart cart, Bios7800 bios, HSC7800 hsc, ILogger logger)
            : base(cart, bios, hsc, logger, 312, 34, 50, 31200 /* PAL_SAMPLES_PER_SEC */, MariaTables.PALPalette)
        {
        }

        #region Serialization Members

        public Machine7800PAL(DeserializationContext input) : base(input, MariaTables.PALPalette, 312)
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
