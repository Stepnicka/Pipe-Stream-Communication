namespace DataSharingLibrary
{
    public class PipeRequest
    {
        public string RequestName { get; set; }
    }

    public class PipeRequest<U> : PipeRequest
    {
        public U Parameter { get; set; }
    }
}
