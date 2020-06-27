using System.Threading.Tasks;

namespace DataSharingLibrary
{
    public class PipeResponse
    {
        public bool IsSuccess { get; set; } = true;
        public string ErrorMessage { get; set; } = string.Empty;
    }

    public class PipeResponse<T> : PipeResponse
    {
        public T Data { get; set; }

        public static PipeResponse<T> SetFail(string fail)
        {
            return new PipeResponse<T>()
            {
                IsSuccess = false,
                ErrorMessage = fail
            };
        }
    }
}
