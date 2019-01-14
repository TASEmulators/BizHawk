namespace SharpCompress.Common
{
    public class MultipartStreamRequiredException : ExtractionException
    {
        public MultipartStreamRequiredException(string message)
            : base(message)
        {
        }
    }
}