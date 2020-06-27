using System.Threading;
using System.Threading.Tasks;

namespace DataSharingLibrary.Interfaces
{
    /// <summary>
    ///     Client communicating with server
    /// </summary>
    public interface ICommunicationClient 
    {
        /// <summary>
        ///     Start up client and connect to server
        /// </summary>
        Task Start(CancellationToken cancellationToken, int connectionTimout);

        /// <summary>
        ///     Send message to server and return response
        /// </summary>
        Task<PipeResponse<T>> SendMessage<T, U>(PipeRequest<U> request, CancellationToken cancellationToken);

        /// <summary>
        ///     Stop communication and dispose of everything
        /// </summary>
        void Stop();
    }
}
