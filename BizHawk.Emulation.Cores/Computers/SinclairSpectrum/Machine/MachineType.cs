
namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    /// <summary>
    /// The various spectrum models ZXHawk emulates
    /// </summary>
    public enum MachineType
    {
        /// <summary>
        /// Original Sinclair Spectrum 16K model
        /// </summary>
        ZXSpectrum16,

        /// <summary>
        /// Sinclair Spectrum 48K model
        /// </summary>
        ZXSpectrum48,

        /// <summary>
        /// Sinclair Spectrum 128K model
        /// </summary>
        ZXSpectrum128,

        /// <summary>
        /// Sinclair Spectrum 128 +2 model
        /// </summary>
        ZXSpectrum128Plus2,

        /// <summary>
        /// Sinclair Spectrum 128 +2a model (same as the +3 just without disk drive)
        /// </summary>
        ZXSpectrum128Plus2a,

        /// <summary>
        /// Sinclair Spectrum 128 +3 model
        /// </summary>
        ZXSpectrum128Plus3,

		/// <summary>
		/// Russian 128k pentagon clone
		/// </summary>
		Pentagon128,
    }
}
