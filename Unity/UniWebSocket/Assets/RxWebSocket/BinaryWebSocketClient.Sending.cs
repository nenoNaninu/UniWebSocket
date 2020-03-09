﻿using System;
using System.Net.WebSockets;
using System.Threading.Tasks;
using RxWebSocket.Exceptions;
using RxWebSocket.Validations;

namespace RxWebSocket
{
    public partial class BinaryWebSocketClient
    {
        /// <summary>
        /// Send text message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Text message to be sent</param>
        public void Send(string message)
        {
            throw new NotImplementedException("BinaryWebSocketClient cannot send string.");
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        public void Send(byte[] message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                _sendMessageQueue.Add(new ArraySegment<byte>(message));
            }
            else
            {
                throw new WebSocketBadInputException($"Input message (byte[]) of the Send function is null or 0 Length. Please correct it.");
            }
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        public void Send(ArraySegment<byte> message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                _sendMessageQueue.Add(message);
            }
            else
            {
                throw new WebSocketBadInputException($"Input message (byte[]) of the Send function is 0 Count. Please correct it.");
            }
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        /// <param name="messageType"></param>
        public void Send(byte[] message, WebSocketMessageType messageType)
        {
            if (!ValidationUtils.ValidateInput(message))
            {
                throw new WebSocketBadInputException($"Input message (byte[]) of the Send function is null or 0 Length. Please correct it.");
            }

            if (messageType != WebSocketMessageType.Binary)
            {
                throw new WebSocketBadInputException($"In BinaryWebSocketClient, the message type must be binary.");                
            }

            _sendMessageQueue.Add(new ArraySegment<byte>(message));
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// The message is inserted into the queue, and the actual sending takes place in background thread.
        /// </summary>
        /// <param name="message">Binary message to be sent</param>
        /// <param name="messageType"></param>
        public void Send(ArraySegment<byte> message, WebSocketMessageType messageType)
        {
            if (!ValidationUtils.ValidateInput(message))
            {
                throw new WebSocketBadInputException($"Input message (byte[]) of the Send function is null or 0 Length. Please correct it.");
            }

            if (messageType != WebSocketMessageType.Binary)
            {
                throw new WebSocketBadInputException($"In BinaryWebSocketClient, the message type must be binary.");                
            }

            _sendMessageQueue.Add(message);
        }

        /// <summary>
        /// Send text message to the websocket channel. 
        /// It doesn't use a queue.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public Task SendInstant(string message)
        {
            throw new NotImplementedException("BinaryWebSocketClient cannot send string.");
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// It doesn't use a queue.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public Task SendInstant(byte[] message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return SendInternalSynchronized(new ArraySegment<byte>(message));
            }

            throw new WebSocketBadInputException($"Input message (byte[]) of the SendInstant function is null or 0 Length. Please correct it.");
        }

        public Task SendInstant(byte[] message, WebSocketMessageType messageType)
        {
            if (!ValidationUtils.ValidateInput(message))
            {
                throw new WebSocketBadInputException($"Input message (byte[]) of the Send function is null or 0 Length. Please correct it.");
            }

            if (messageType != WebSocketMessageType.Binary)
            {
                throw new WebSocketBadInputException($"In BinaryWebSocketClient, the message type must be binary.");                
            }
      
            return SendInternalSynchronized(new ArraySegment<byte>(message));
        }

        /// <summary>
        /// Send binary message to the websocket channel. 
        /// It doesn't use a queue.
        /// </summary>
        /// <param name="message">Message to be sent</param>
        public Task SendInstant(ArraySegment<byte> message)
        {
            if (ValidationUtils.ValidateInput(message))
            {
                return SendInternalSynchronized(message);
            }

            throw new WebSocketBadInputException($"Input message (ArraySegment<byte>) of the SendInstant function is 0 Count. Please correct it.");
        }

        public Task SendInstant(ArraySegment<byte> message, WebSocketMessageType messageType)
        {
            if (!ValidationUtils.ValidateInput(message))
            {
                throw new WebSocketBadInputException($"Input message (ArraySegment<byte>) of the SendInstant function is 0 Count. Please correct it.");
            }

            if (messageType != WebSocketMessageType.Binary)
            {
                throw new WebSocketBadInputException($"In BinaryWebSocketClient, the message type must be binary.");
            }

            return SendInternalSynchronized(message);
        }

        private async Task SendMessageFromQueue()
        {
            try
            {
                foreach (var message in _sendMessageQueue.GetConsumingEnumerable(_cancellationAllJobs.Token))
                {
                    try
                    {
                        await SendInternalSynchronized(message).ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        _logger?.Error(e, FormatLogMessage($"Failed to send binary message: '{message}'. Error: {e.Message}"));
                        _exceptionSubject.OnNext(new WebSocketExceptionDetail(e, ErrorType.SendBinary));
                    }
                }
            }
            catch (TaskCanceledException)
            {
                // task was canceled, ignore
            }
            catch (OperationCanceledException)
            {
                // operation was canceled, ignore
            }
            catch (Exception e)
            {
                if (_cancellationAllJobs.IsCancellationRequested || IsDisposed)
                {
                    // disposing/canceling, do nothing and exit
                    return;
                }

                _logger?.Error(e, FormatLogMessage($"Sending binary thread failed, error: {e.Message}."));
                _exceptionSubject.OnNext(new WebSocketExceptionDetail(e, ErrorType.BinaryQueue));
            }
        }

        private void StartBackgroundThreadForSendingMessage()
        {
#pragma warning disable 4014
            Task.Factory.StartNew(_ => SendMessageFromQueue(), TaskCreationOptions.LongRunning, _cancellationAllJobs.Token);
#pragma warning restore 4014
        }

        private async Task SendInternalSynchronized(ArraySegment<byte> message)
        {
            using (await _sendLocker.LockAsync().ConfigureAwait(false))
            {
                await SendInternal(message).ConfigureAwait(false);
            }
        }

        private async Task SendInternal(ArraySegment<byte> message)
        {
            if (!IsOpen)
            {
                _logger?.Warn(FormatLogMessage($"Client is not connected to server, cannot send binary, length: {message.Count}"));
                return;
            }

            _logger?.Log(FormatLogMessage($"Sending binary, length: {message.Count}"));

            await _socket
                .SendAsync(message, WebSocketMessageType.Binary, true, _cancellationCurrentJobs.Token)
                .ConfigureAwait(false);
        }
    }
}