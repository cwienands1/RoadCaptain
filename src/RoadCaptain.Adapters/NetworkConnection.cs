﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using RoadCaptain.Adapters.Protobuf;
using RoadCaptain.Ports;

namespace RoadCaptain.Adapters
{
    internal class NetworkConnection : IZwiftGameConnection, IMessageReceiver
    {
        private static readonly TimeSpan ReceiveMessageBytesTimeout = TimeSpan.FromMilliseconds(250);
        private static readonly object SyncRoot = new();
        private readonly int _port;
        private Socket? _listeningSocket;
        private readonly CancellationTokenSource _tokenSource;
        private readonly TimeSpan _acceptTimeout;
        private readonly TimeSpan _dataTimeout;
        // TODO: Figure out what a decent size is for the receive buffer, maybe dynamically even?
        private readonly int _receiveBufferSize;
        private readonly IGameStateDispatcher _gameStateDispatcher;
        private readonly MonitoringEvents _monitoringEvents;
        private Socket? _clientSocket;
        private readonly AutoResetEvent _dataResetEvent = new(false);
        private readonly ConcurrentQueue<byte[]> _dataBuffer = new();
        private Thread? _thread;
        private readonly IZwiftCrypto _zwiftCrypto;
        private uint _commandCounter;

        public NetworkConnection(
            IGameStateDispatcher gameStateDispatcher,
            MonitoringEvents monitoringEvents, 
            IZwiftCrypto zwiftCrypto)
            : this(
                21588,
                TimeSpan.FromSeconds(5),
                TimeSpan.FromSeconds(5),
                512,
                gameStateDispatcher,
                monitoringEvents, 
                zwiftCrypto)
        {
        }

        internal NetworkConnection(int port, TimeSpan acceptTimeout, TimeSpan dataTimeout, int receiveBufferSize,
            IGameStateDispatcher gameStateDispatcher, MonitoringEvents monitoringEvents, IZwiftCrypto zwiftCrypto)
        {
            if (port < 0 || port > UInt16.MaxValue)
            {
                throw new ArgumentOutOfRangeException(nameof(port));
            }

            _port = port;
            _tokenSource = new CancellationTokenSource();
            _acceptTimeout =acceptTimeout;
            _dataTimeout = dataTimeout;
            _receiveBufferSize = receiveBufferSize;
            _gameStateDispatcher = gameStateDispatcher;
            _monitoringEvents = monitoringEvents;
            _zwiftCrypto = zwiftCrypto;
        }

        public event EventHandler? AcceptTimeoutExpired;
        public event EventHandler? DataTimeoutExpired;
        public event EventHandler? ConnectionLost;
        public event EventHandler? ConnectionAccepted;
        public event EventHandler<DataEventArgs>? Data;

        public Task StartAsync()
        {
            // Only start the thread once
            lock (SyncRoot)
            {
                if (_thread is { IsAlive: true })
                {
                    return Task.CompletedTask;
                }

                _thread = new Thread(
                    () => ConnectionLoop().GetAwaiter().GetResult())
                {
                    IsBackground = true,
                    Name = "NetworkConnection"
                };

                _thread.Start();
            }

            return Task.CompletedTask;
        }

        private async Task ConnectionLoop()
        {
            _listeningSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
            {
                NoDelay = true
            };

            _listeningSocket.Bind(new IPEndPoint(IPAddress.Loopback, _port));

            _listeningSocket.Listen();

            while (!_tokenSource.IsCancellationRequested)
            {
                _monitoringEvents.WaitingForConnection();
                _gameStateDispatcher.WaitingForConnection();

                var acceptTask = _listeningSocket.AcceptAsync(_tokenSource.Token).AsTask();

                while (!acceptTask.IsCompleted)
                {
                    var timeoutTask = Task.Delay(_acceptTimeout);

                    var completedTask = await Task.WhenAny(timeoutTask, acceptTask);

                    if (completedTask == timeoutTask)
                    {
                        AcceptTimeoutExpired?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        // When the network connection is stopped then
                        // the acceptTask potentially completes before
                        // the timeout task but it will be in a failed
                        // state (cancelled).
                        if (_tokenSource.IsCancellationRequested)
                        {
                            return;
                        }

                        _clientSocket = acceptTask.Result;

                        _monitoringEvents.AcceptedConnection(_clientSocket.RemoteEndPoint as IPEndPoint);
                        _gameStateDispatcher.Connected();

                        ConnectionAccepted?.Invoke(this, EventArgs.Empty);

                        break;
                    }
                }

                var dataReadTask = Task.Factory.StartNew(ReadDataFromClientSocket);

                while (!_tokenSource.IsCancellationRequested)
                {
                    var timeoutTask = Task.Delay(_dataTimeout);

                    var completedTask = await Task.WhenAny(timeoutTask, dataReadTask);

                    if (completedTask == timeoutTask)
                    {
                        DataTimeoutExpired?.Invoke(this, EventArgs.Empty);
                    }
                    else
                    {
                        // When the network connection is stopped then
                        // the acceptTask potentially completes before
                        // the timeout task but it will be in a failed
                        // state (cancelled).
                        if (_tokenSource.IsCancellationRequested)
                        {
                            return;
                        }

                        if (dataReadTask.Result is { Length: > 0 })
                        {
                            _dataBuffer.Enqueue(dataReadTask.Result);
                            _dataResetEvent.Set();
                            Data?.Invoke(this, new DataEventArgs(dataReadTask.Result));
                        }

                        if (_clientSocket == null || !_clientSocket.Connected)
                        {
                            _clientSocket = null;

                            // Drop back to accepting a new connection
                            break;
                        }

                        if (_clientSocket.Connected)
                        {
                            dataReadTask = Task.Factory.StartNew(ReadDataFromClientSocket);
                        }
                    }
                }
            }
        }

        private byte[]? ReadDataFromClientSocket()
        {
            List<byte> result = new();

            // I suspect this buffer could even be allocated just once
            // as this class is thread-safe(ish).
            // TODO: Investigate if this can be allocated once instead of every call to this method
            var buffer = new byte[_receiveBufferSize];

            while (!_tokenSource.IsCancellationRequested)
            {
                int read;

                try
                {
                    read = _clientSocket!.Receive(buffer, 0, buffer.Length, SocketFlags.None, out var socketError);

                    // If a client closes the socket then the number of bytes read
                    // becomes zero. That's a bit unfortunate but alas nothing we
                    // can do anything about. Therefore if either there is a socket
                    // error _or_ the number of bytes received is zero we will
                    // consider the client connection to be closed.
                    if (socketError != SocketError.Success || read == 0)
                    {
                        CloseAndCleanupClientSocket();

                        return null;
                    }
                }
                catch (SocketException)
                {
                    CloseAndCleanupClientSocket();
                    return null;
                }
                catch (ObjectDisposedException)
                {
                    CloseAndCleanupClientSocket();
                    return null;
                }

                if (read > 0)
                {
                    // This is annoying because it allocates these bytes again
                    // TODO: Optimize allocations when reading data from socket
                    result.AddRange(buffer.Take(read));

                    if (read < buffer.Length - 1)
                    {
                        // At this point there isn't any more data
                        // in the socket receive buffer and we can
                        // stop.
                        break;
                    }
                }
            }

            if (result.Any())
            {
                return result.ToArray();
            }

            return null;
        }

        private void CloseAndCleanupClientSocket()
        {
            try
            {
                _clientSocket?.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException)
            {
                // Don't care
            }
            catch (ObjectDisposedException)
            {
                _clientSocket = null;
            }

            try
            {
                _clientSocket?.Close();
            }
            catch (SocketException)
            {
                // Don't care
            }
            catch (ObjectDisposedException)
            {
                // Don't care
            }
            finally
            {
                // Clear this so that the next call to ReceiveMessageBytes() will block
                // on accepting a new connection.
                _clientSocket = null;
            }
            
            ConnectionLost?.Invoke(this, EventArgs.Empty);
        }

        public void Shutdown()
        {
            _tokenSource.Cancel();

            // Also close the client socket so that the counter party
            // is informed we're shutting down.
            CloseAndCleanupClientSocket();

            try
            {
                _listeningSocket?.Shutdown(SocketShutdown.Both);
            }
            catch (SocketException)
            {
                // Nop
            }

            try
            {
                _listeningSocket?.Close();
            }
            catch (SocketException)
            {
                // Nop
            }

            try
            {
                _listeningSocket?.Dispose();
            }
            catch
            {
                // Nop
            }
        }

        public byte[]? ReceiveMessageBytes()
        {
            while (!_tokenSource.IsCancellationRequested)
            {
                if (_dataBuffer.TryDequeue(out var dataBuffer))
                {
                    return dataBuffer;
                }

                _dataResetEvent.WaitOne(ReceiveMessageBytesTimeout);
            }

            return null;
        }

        private void SendMessageBytes(byte[] payload)
        {
            if (_clientSocket == null)
            {
                _monitoringEvents.Error("Tried to send data to Zwift but there is no active connection");
                return;
            }

            // Note: For messages to the Zwift app we need to have a 4-byte length prefix instead
            // of the 2-byte one we see on incoming messages...
            var payloadToSend = WrapWithLength(_zwiftCrypto.Encrypt(payload));

            var offset = 0;

            while (offset < payloadToSend.Length)
            {
                var sent = _clientSocket.Send(payloadToSend, offset, payloadToSend.Length - offset, SocketFlags.None);
                
                _monitoringEvents.Debug("Sent {Count} bytes, {Sent} sent so far of {Total} total payload size", sent, offset + sent, payloadToSend.Length);

                offset += sent;
            }
        }

        public void SendInitialPairingMessage(uint riderId, uint sequenceNumber)
        {
            var message = new ZwiftCompanionToAppRiderMessage
            {
                MyId = riderId,
                Details = new ZwiftCompanionToAppRiderMessage.Types.RiderMessage
                {
                    RiderId = riderId,
                    Tag1 = _commandCounter++,
                    Type = (uint)PhoneToGameCommandType.PairingAs
                },
                Sequence = sequenceNumber
            };
            
            SendMessageBytes(message.ToByteArray());
        }

        public void SendTurnCommand(TurnDirection direction, ulong sequenceNumber, uint riderId)
        {
            var message = new ZwiftCompanionToAppRiderMessage
            {
                MyId = riderId,
                Details = new ZwiftCompanionToAppRiderMessage.Types.RiderMessage
                {
                    CommandType = (uint)GetCommandTypeForTurnDirection(direction),
                    Tag1 = _commandCounter++, // This is a sequence of the number of commands we've sent to the game
                    Type = (uint)PhoneToGameCommandType.CustomAction, // Tag2
                    Tag3 = 0,
                    Tag5 = 0,
                    Tag7 = 0
                },
                Sequence = (uint)sequenceNumber // This value is provided via the SomethingEmpty synchronization command
            };

            SendMessageBytes(message.ToByteArray());
        }

        public void EndActivity(ulong sequenceNumber, string activityName, uint riderId)
        {
            var message = new ZwiftCompanionToAppRiderMessage
            {
                MyId = riderId,
                Details = new ZwiftCompanionToAppRiderMessage.Types.RiderMessage
                {
                    Tag1 = _commandCounter++, // This is a sequence of the number of commands we've sent to the game
                    Type = (uint)PhoneToGameCommandType.DoneRiding, // Tag2
                    Tag3 = 0,
                    Tag5 = 0,
                    Tag7 = 0,
                    Data = new ZwiftCompanionToAppRiderMessage.Types.RiderMessage.Types.RiderMessageData
                    {
                        Tag1 = 15,
                        SubData = new ZwiftCompanionToAppRiderMessage.Types.RiderMessage.Types.RiderMessageData.Types.RiderMessageSubData
                        {
                            Tag1 = 3,
                            WorldName = activityName,
                            Tag4 = 0
                        }
                    }
                },
                Sequence = (uint)sequenceNumber // This value is provided via the SomethingEmpty synchronization command
            };

            SendMessageBytes(message.ToByteArray());
        }

        private static CommandType GetCommandTypeForTurnDirection(TurnDirection direction)
        {
            switch (direction)
            {
                case TurnDirection.Left:
                    return CommandType.TurnLeft;
                case TurnDirection.GoStraight:
                    return CommandType.GoStraight;
                case TurnDirection.Right:
                    return CommandType.TurnRight;
                default:
                    return CommandType.Unknown;
            }
        }

        private static byte[] WrapWithLength(byte[] payload)
        {
            var prefix = BitConverter.GetBytes(payload.Length);

            if (BitConverter.IsLittleEndian)
            {
                prefix = prefix.Reverse().ToArray();
            }

            return prefix
                .Concat(payload)
                .ToArray();
        }
    }
}