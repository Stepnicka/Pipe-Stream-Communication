using System;
using System.Threading;
using System.Threading.Tasks;

namespace DataSharingLibrary.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface ICommunicationServer 
    {
        event EventHandler<ClientConnectedEventArgs> ClientConnectedEvent;
        event EventHandler<ClientDisconnectedEventArgs> ClientDisconnectedEvent;

        string ServerId { get; }

        /// <summary>
        /// 
        /// </summary>
        Task Start(CancellationToken cancellationToken);

        /// <summary>
        /// 
        /// </summary>
        void Stop();
    }

    public class ClientConnectedEventArgs : EventArgs
    {
        public string ClientId { get; set; }
        public CancellationToken CancellationToken { get; set; }
    }

    public class ClientDisconnectedEventArgs : EventArgs
    {
        public string ClientId { get; set; }
    }
}
