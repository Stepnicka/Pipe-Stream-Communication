using DataSharingLibrary.Interfaces;
using System;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Buffers;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DataSharingLibrary.Client
{
    /// <summary>
    ///     Client for remote network process communication
    /// </summary>
    public class PipeClient : ICommunicationClient
    {
        private readonly NamedPipeClientStream _pipeClient;

        private readonly IPipeLogger _logger;
        private readonly IMemoryOwner<byte> _memoryOwner = MemoryPool<byte>.Shared.Rent(-1);

        private const int tryConnectTimeout = 1 * 60 * 100; // 0.5 minute

        /// <summary> Create a client to connect to a remote server </summary>
        /// <param name="serverId"> The name of the server instance to which we are connecting. </param>
        /// <param name="loggers"> Logger iplementing IPipeLogger interface </param>
        /// <param name="serverName"> The name of the remote station, in case of local use leave the default value. (např: "sql1.tmt.local" ) </param>
        public PipeClient(string serverId, IPipeLogger logger, string serverName = ".")
        {
            _logger = logger;
            _pipeClient = new NamedPipeClientStream(serverName, serverId, PipeDirection.InOut, PipeOptions.Asynchronous | PipeOptions.WriteThrough);
        }

        /// <summary>
        ///     Startup the client and connect to the server
        /// </summary>
        public Task Start(CancellationToken cancellationToken, int connectionTimout = tryConnectTimeout)
        {
            try
            {
                return _pipeClient.ConnectAsync(connectionTimout, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                return Task.FromException(ex);
            }
        }

        /// <summary>
        ///     Stop client and drain the pipe
        /// </summary>
        public void Stop()
        {
            if (_pipeClient != null)
            {
                if (_pipeClient.IsConnected)
                    _pipeClient.Close();

                _pipeClient.Dispose();
            }
        }

        /// <summary>
        ///     Send message to server and return the response
        /// </summary>
        public async Task<PipeResponse<T>> SendMessage<T, U>(PipeRequest<U> request, CancellationToken cancellationToken)
        {
            var state = new PipeTaskState<T>();

            await WriteAsync<T,U>(state, request ,cancellationToken);

            WaitForPipeDrain<T>(state);

            await ReadAsync<T>(state, cancellationToken);

            ConvertResponse<T>(state);

            return state.Response;
        }

        /// <summary>
        ///     Begin writing to a network stream
        /// </summary>
        private Task WriteAsync<T,U>(PipeTaskState<T> state, PipeRequest<U> request ,CancellationToken cancellationToken)
        {
            try
            {
                if (cancellationToken.IsCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();

                if (!_pipeClient.IsConnected)
                    throw new IOException("Unable to send message, no connection established.");

                if (_pipeClient.CanWrite)
                    return _pipeClient.WriteAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(request)), cancellationToken).AsTask();
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(WriteAsync)}-{ex.Message}");
                state.Response = PipeResponse<T>.SetFail(ex.Message);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Begin reading from a network stream
        /// </summary>
        private Task ReadAsync<T>(PipeTaskState<T> state, CancellationToken cancellationToken)
        {
            if (state.Response != null && state.Response.IsSuccess == false)
                return Task.CompletedTask;

            try
            {
                if (cancellationToken.IsCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();

                if (!_pipeClient.IsConnected)
                    throw new IOException("Unable to recieve message, no connection established.");

                if (_pipeClient.CanRead)
                    return _pipeClient.ReadAsync(_memoryOwner.Memory, cancellationToken).AsTask();
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(ReadAsync)}-{ex.Message}");
                state.Response = PipeResponse<T>.SetFail(ex.Message);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Wait for a pipe to drain before another write/read
        /// </summary>
        private void WaitForPipeDrain<T>(PipeTaskState<T> state)
        {
            try
            {
                _pipeClient.WaitForPipeDrain();
            }
            catch (Exception ex)
            {
                _logger.Error(ex.Message);
                state.Response = PipeResponse<T>.SetFail(ex.Message);
            }
        }

        /// <summary>
        ///     Convert response from 
        /// </summary>
        private void ConvertResponse<T>(PipeTaskState<T> state)
        {
            if (state.Response != null && state.Response.IsSuccess == false)
                return;

            try
            {
                var json = Encoding.UTF8.GetString(_memoryOwner.Memory.Span).TrimEnd('\0');

                if (string.IsNullOrWhiteSpace(json))
                    throw new IOException("Response from server is empty.");

                state.Response = JsonConvert.DeserializeObject<PipeResponse<T>>(json);
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(ReadAsync)}-{ex.Message}");
                state.Response = PipeResponse<T>.SetFail(ex.Message);
            }
        }
    }
}
