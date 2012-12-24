namespace EMU7800.Core
{
    public sealed class Machine7800NTSC : Machine7800
    {
        public override string ToString()
        {
            return GetType().Name;
        }

        public Machine7800NTSC(Cart cart, Bios7800 bios, HSC7800 hsc, ILogger logger)
            : base(cart, bios, hsc, logger, 262, 16, 60, 31440 /* NTSC_SAMPLES_PER_SEC */, MariaTables.NTSCPalette)
        {
        }

        #region Serialization Members

        public Machine7800NTSC(DeserializationContext input) : base(input, MariaTables.NTSCPalette, 262)
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
