using System.IO.Pipes;
using System.Text;
using System;
using DataSharingLibrary.Interfaces;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;

namespace DataSharingLibrary.Server
{
    /// <summary>
    ///     Server client pro příjmání zpráv
    /// </summary>
    /// <remarks> Štěpnička Luboš, 2020-04-02 </remarks>
    internal class InternalPipeServer : ICommunicationServer
    {
        public string ServerId { get => _serverId; }
        private readonly string _serverId;

        public event EventHandler<ClientConnectedEventArgs> ClientConnectedEvent;
        public event EventHandler<ClientDisconnectedEventArgs> ClientDisconnectedEvent;

        private readonly NamedPipeServerStream _pipeServer;

        private readonly MethodStack _methods;

        private readonly IPipeLogger _logger;

        private readonly IMemoryOwner<byte> _memoryOwner = MemoryPool<byte>.Shared.Rent(-1);

        private bool _isStopping;
        private readonly object _lockingObject = new object();

        /// <param name="pipeName"> Název kanálu, který má být vytvořen. Klient se musí s tímto jménem seznámit, aby se mohl připojit k serveru. </param>
        /// <param name="maxNumberOfServerInstances"> Maximální počet instancí serveru, které sdílejí stejný název. I/O výjimka bude vyvolána při vytváření NamedPipeServerStream, pokud její vytvoření dosáhne maximálního čísla. </param>
        public InternalPipeServer(string pipeName, int maxNumberOfServerInstances, IPipeLogger logger, MethodStack methods)
        {
            _methods = methods;
            _logger = logger;
            _pipeServer = new NamedPipeServerStream(pipeName, PipeDirection.InOut, maxNumberOfServerInstances, PipeTransmissionMode.Message, PipeOptions.Asynchronous, 512, 512);

            _serverId = Guid.NewGuid().ToString();
        }

        /// <summary>
        ///     StartUp server instance and wait for client connection
        /// </summary>
        public async Task Start(CancellationToken cancellationToken)
        {
            _logger.Info($"Starting pipeStreamServer instace and waiting for connections..");

            try
            {
                await _pipeServer.WaitForConnectionAsync(cancellationToken);

                OnConnected(cancellationToken);

                await Communicate(new PipeTaskState<object>(), cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
        }

        /// <summary>
        ///     CloseUp server instance and dispose everything
        /// </summary>
        public void Stop()
        {
            if (_isStopping)
                return;

            _logger.Info($"Stopping pipeStreamServer instace..");

            try
            {
                _isStopping = true;

                if (_pipeServer.IsConnected)
                    _pipeServer.Disconnect();
            }
            catch (Exception ex)
            {
                _logger.Error(ex);
            }
            finally
            {
                _pipeServer.Close();
                _pipeServer.Dispose();
                _memoryOwner.Dispose();
            }
        }

        /// <summary>
        ///     Start communication with client
        /// </summary>
        public async Task Communicate(PipeTaskState<object> state, CancellationToken cancellationToken)
        {
            await ReadAsync(state, cancellationToken);

            WaitForPipeDrain(state);

            await WriteAsync(state, cancellationToken);

            WaitForPipeDrain(state);

            if (!_isStopping)
            {
                lock (_lockingObject)
                {
                    if (!_isStopping)
                    {
                        OnDisconnected();
                        Stop();
                    }
                }
            }
        }

        /// <summary>
        ///     Begin reading from a network stream
        /// </summary>
        public Task ReadAsync(PipeTaskState<object> state, CancellationToken cancellationToken)
        {
            if (_pipeServer.IsConnected == false) /*Check if client is connected*/
                return Task.CompletedTask;

            try
            {
                if (cancellationToken.IsCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();

                return _pipeServer.ReadAsync(_memoryOwner.Memory, cancellationToken).AsTask();
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(ReadAsync)} - {ex.Message}");
                state.Response = PipeResponse<object>.SetFail(ex.Message);
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Begin writing to a network stream
        /// </summary>
        public Task WriteAsync(PipeTaskState<object> state, CancellationToken cancellationToken)
        {
            if (_pipeServer.IsConnected == false) /*Check if client is connected*/
                return Task.CompletedTask;

            try
            {
                if (cancellationToken.IsCancellationRequested)
                    cancellationToken.ThrowIfCancellationRequested();

                state.Response = CallMethod();

                return _pipeServer.WriteAsync(Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(state.Response)), cancellationToken).AsTask();
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(WriteAsync)} - {ex.Message}");
            }

            return Task.CompletedTask;
        }

        /// <summary>
        ///     Wait for a pipe to drain before another write/read
        /// </summary>
        private void WaitForPipeDrain(PipeTaskState<object> state)
        {
            try
            {
                _pipeServer.WaitForPipeDrain();
            }
            catch (Exception ex)
            {
                _logger.Error($"{nameof(WaitForPipeDrain)} - {ex.Message}");
                state.Response = PipeResponse<object>.SetFail(ex.Message);
            }
        }

        /// <summary>
        ///     Call registered method
        /// </summary>
        private PipeResponse<object> CallMethod()
        {
            try
            {
                string jsonRequest = Encoding.UTF8.GetString(_memoryOwner.Memory.Span);

                if (string.IsNullOrWhiteSpace(jsonRequest))
                {
                    return PipeResponse<object>.SetFail("Recieved message was empty.");
                }

                PipeRequest request = JsonConvert.DeserializeObject<PipeRequest>(jsonRequest);
                var RequestName = request.RequestName;

                if (!_methods.TryGetMethodParameterType(RequestName, out Type requestParameterType))
                {
                    return PipeResponse<object>.SetFail("Unable to find method on server.");
                }

                dynamic fullRequest = JsonConvert.DeserializeObject(jsonRequest, requestParameterType);
                object parameter = fullRequest.Parameter;

                if (_methods.TryRunMethod(RequestName, parameter, out object response))
                {
                    return new PipeResponse<object>() { IsSuccess = true, ErrorMessage = string.Empty, Data = response };
                }
                else
                {
                    return PipeResponse<object>.SetFail("Unable to execute method on server.");
                }
            }
            catch (Exception ex)
            {
                return PipeResponse<object>.SetFail(ex.Message);
            }
        }

        /// <summary>
        ///     Invoke event when client connects
        /// </summary>
        protected virtual void OnConnected(CancellationToken cancellationToken)
        {
            _logger.Info($"Client {_serverId} connected.");
            ClientConnectedEvent?.Invoke(this, new ClientConnectedEventArgs { ClientId = _serverId, CancellationToken = cancellationToken });
        }

        /// <summary>
        ///     Invoke event when client disconnects
        /// </summary>
        protected virtual void OnDisconnected()
        {
            _logger.Info($"Client {_serverId} disconnected.");
            ClientDisconnectedEvent?.Invoke(this, new ClientDisconnectedEventArgs { ClientId = _serverId });
        }
    }
}
