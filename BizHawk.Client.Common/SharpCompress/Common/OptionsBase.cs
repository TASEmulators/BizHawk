
namespace SharpCompress.Common
{
    public class OptionsBase
    {
        /// <summary>
        /// SharpCompress will keep the supplied streams open.  Default is true.
        /// </summary>
        public bool LeaveStreamOpen { get; set; } = true;

        public ArchiveEncoding ArchiveEncoding { get; set; } = new ArchiveEncoding();
    }
}