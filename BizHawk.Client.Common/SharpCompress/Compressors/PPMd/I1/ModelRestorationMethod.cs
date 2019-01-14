#region Using



#endregion

namespace SharpCompress.Compressors.PPMd.I1
{
    /// <summary>
    /// The method used to adjust the model when the memory limit is reached.
    /// </summary>
    internal enum ModelRestorationMethod
    {
        /// <summary>
        /// Restart the model from scratch (this is the default).
        /// </summary>
        Restart = 0,

        /// <summary>
        /// Cut off the model (nearly twice as slow).
        /// </summary>
        CutOff = 1,

        /// <summary>
        /// Freeze the context tree (in some cases may result in poor compression).
        /// </summary>
        Freeze = 2
    }
}