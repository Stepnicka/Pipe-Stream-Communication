using DataSharingLibrary.Interfaces;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Threading;
using System;
using System.Threading.Tasks;

namespace DataSharingLibrary.Server
{
    public class PipeServer
    {
        private readonly string _pipeName;
        private readonly IDictionary<string, ICommunicationServer> _servers;
        private const int MaxNumberOfServerInstances = 10;

        private readonly MethodStack _methods;
        private readonly IPipeLogger _logger;

        public PipeServer(IPipeLogger logger, string pipeName, MethodStack methods)
        {
            _pipeName = pipeName;
            _methods = methods;
            _logger = logger;

            _servers = new ConcurrentDictionary<string, ICommunicationServer>();
        }

        public async void Start(CancellationToken cancellationToken)
        {
            await Task.Run(() => StartNamedPipeServer(cancellationToken));
        }

        public void Stop()
        {
            foreach (var server in _servers.Values)
            {
                try
                {
                    UnregisterFromServerEvents(server);
                    server.Stop();
                }
                catch (Exception)
                {
                    _logger.Error("Nepovedlo se ukončit server.");
                }
            }

            _servers.Clear();
        }

        private Task StartNamedPipeServer(CancellationToken cancellationToken)
        {
            var server = new InternalPipeServer(_pipeName, MaxNumberOfServerInstances, _logger, _methods);
            _servers[server.ServerId] = server;

            server.ClientConnectedEvent += ClientConnectedHandler;
            server.ClientDisconnectedEvent += ClientDisconnectedHandler;

            return server.Start(cancellationToken);
        }

        private void StopNamedPipeServer(string id)
        {
            UnregisterFromServerEvents(_servers[id]);
            _servers[id].Stop();
            _servers.Remove(id);
        }

        /// <summary>
        ///     Cancel event registration for servers
        /// </summary>
        private void UnregisterFromServerEvents(ICommunicationServer server)
        {
            server.ClientConnectedEvent -= ClientConnectedHandler;
            server.ClientDisconnectedEvent -= ClientDisconnectedHandler;
        }

        /// <summary>
        ///     Handles client connection. When client connects start a new instance so we can listen for new connection
        /// </summary>
        private async void ClientConnectedHandler(object sender, ClientConnectedEventArgs eventArgs)
        {
            await Task.Run(() => StartNamedPipeServer(eventArgs.CancellationToken));
        }

        /// <summary>
        ///     Handles client disconnections. Removes the server from the pool
        /// </summary>
        private void ClientDisconnectedHandler(object sender, ClientDisconnectedEventArgs eventArgs)
        {
            StopNamedPipeServer(eventArgs.ClientId);
        }
    }
}
