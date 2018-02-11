using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grumpy.Common;
using Grumpy.Common.Extensions;
using Grumpy.Common.Interfaces;
using Grumpy.Common.Threading;
using Grumpy.Json;
using Grumpy.Logging;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.MessageQueue.Msmq.Exceptions;
using Grumpy.RipplesMQ.Core.Dto;
using Grumpy.RipplesMQ.Core.Enum;
using Grumpy.RipplesMQ.Core.Exceptions;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Core.Interfaces;
using Grumpy.RipplesMQ.Core.Messages;
using Grumpy.RipplesMQ.Entity;
using Grumpy.RipplesMQ.Shared.Messages;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Grumpy.RipplesMQ.Core
{
    /// <inheritdoc />
    public class MessageBroker : IMessageBroker
    {
        private readonly IRepositoriesFactory _repositoriesFactory;
        private readonly IQueueFactory _queueFactory;
        private readonly ILogger _logger;
        private readonly IQueueHandler _localeQueueHandler;
        private readonly IQueueHandler _remoteQueueHandler;
        private readonly MessageBrokerServiceInformation _messageBrokerServiceInformation;
        private readonly ITimerTask _handshakeTask;
        private readonly ITimerTask _repositoryCleanupTask;
        private CancellationToken _cancellationToken;
        private bool _disposed;

        /// <inheritdoc />
        public List<Dto.MessageBrokerService> MessageBrokerServices { get; }

        /// <inheritdoc />
        public List<Dto.SubscribeHandler> SubscribeHandlers { get; }

        /// <inheritdoc />
        public List<Dto.RequestHandler> RequestHandlers { get; }

        /// <inheritdoc />
        public MessageBroker(ILogger logger, MessageBrokerConfig messageBrokerConfig, IRepositoriesFactory repositoriesFactory, IQueueHandlerFactory queueHandlerFactory, IQueueFactory queueFactory, IProcessInformation processInformation)
        {
            _repositoriesFactory = repositoriesFactory;
            _queueFactory = queueFactory;
            _logger = logger;
            _localeQueueHandler = queueHandlerFactory.Create();
            _remoteQueueHandler = queueHandlerFactory.Create();
            _handshakeTask = new TimerTask();
            _repositoryCleanupTask = new TimerTask();

            _messageBrokerServiceInformation = new MessageBrokerServiceInformation
            {
                Id = UniqueKeyUtility.Generate(),
                ServerName = processInformation.MachineName,
                ServiceName = messageBrokerConfig.ServiceName,
                LocaleQueueName = Shared.Config.MessageBrokerConfig.LocaleQueueName,
                RemoteQueueName = messageBrokerConfig.RemoteQueueName
            };

            MessageBrokerServices = new List<Dto.MessageBrokerService>();
            SubscribeHandlers = new List<Dto.SubscribeHandler>();
            RequestHandlers = new List<Dto.RequestHandler>();

            _logger.Information("RipplesMQ Message Broker Server Created {@Information}", _messageBrokerServiceInformation);
        }

        /// <inheritdoc />
        public void Start(CancellationToken cancellationToken)
        {
            _cancellationToken = cancellationToken;

            using (var repositories = _repositoriesFactory.Create())
            {
                UpdateMessageBrokerServiceRepository(repositories);
                LoadMessageBrokers(repositories);
                LoadLocaleSubscribers(repositories, _messageBrokerServiceInformation.ServerName);
            }

            UpdateMessageBrokerService(_messageBrokerServiceInformation.ServerName, _messageBrokerServiceInformation.RemoteQueueName, _messageBrokerServiceInformation.Id);

            _localeQueueHandler.Start(_messageBrokerServiceInformation.LocaleQueueName, true, LocaleQueueMode.DurableCreate, true, LocaleHandler, (o, exception) => ErrorHandler(o, exception), null, 1000, true, false, _cancellationToken);
            _remoteQueueHandler.Start(_messageBrokerServiceInformation.RemoteQueueName, true, LocaleQueueMode.DurableCreate, true, RemoteHandler, (o, exception) => ErrorHandler(o, exception), null, 1000, true, false, _cancellationToken);

            _handshakeTask.Start(SendMessageBrokerHandshakes, 30000, _cancellationToken);
            _repositoryCleanupTask.Start(SendRepositoryCleanupMessage, 3600000, _cancellationToken);

            _logger.Information("RipplesMQ Message Broker Server Started");
        }

        /// <inheritdoc />
        public void Stop()
        {
            _localeQueueHandler.Stop();
            _remoteQueueHandler.Stop();
            _handshakeTask.Stop();
            _repositoryCleanupTask.Stop();

            _logger.Information("RipplesMQ Message Broker Server Stopped");
        }

        /// <inheritdoc />
        public void Handler(object message)
        {
            _logger.Information("Message received {MessageType} {@Message}", message.GetType().Name, message);

            switch (message)
            {
                case MessageBusServiceHandshakeMessage messageBusServiceHandshakeMessage:
                    Handler(messageBusServiceHandshakeMessage);
                    break;
                case PublishMessage publishMessage:
                    Handler(publishMessage);
                    break;
                case PublishSubscriberMessage publishSubscriberMessage:
                    Handler(publishSubscriberMessage);
                    break;
                case SubscribeHandlerCompleteMessage subscribeHandlerCompleteMessage:
                    Handler(subscribeHandlerCompleteMessage);
                    break;
                case SubscribeHandlerErrorMessage subscribeHandlerErrorMessage:
                    Handler(subscribeHandlerErrorMessage);
                    break;
                case RequestMessage requestMessage:
                    Handler(requestMessage);
                    break;
                case ResponseMessage responseMessage:
                    Handler(responseMessage);
                    break;
                case ResponseErrorMessage responseErrorMessage:
                    Handler(responseErrorMessage);
                    break;
                case SendMessageBrokerHandshakeMessage sendMessageBrokerHandshakeMessage:
                    Handler(sendMessageBrokerHandshakeMessage);
                    break;
                case MessageBrokerHandshakeMessage messageBrokerHandshakeMessage:
                    Handler(messageBrokerHandshakeMessage);
                    break;
                case RepositoryCleanupMessage repositoryCleanupMessage:
                    Handler(repositoryCleanupMessage);
                    break;
                case CleanOldServicesMessage cleanOldServicesMessage:
                    Handler(cleanOldServicesMessage);
                    break;
            }
        }

        private void LocaleHandler(object message, CancellationToken cancellationToken)
        {
            Handler(message);
        }

        private void RemoteHandler(object message, CancellationToken cancellationToken)
        {
            Handler(message);
        }

        /// <inheritdoc />
        public void ErrorHandler(object message, Exception exception)
        {
            _logger.Error(exception, "Error handling message {@Message}", message);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2213:DisposableFieldsShouldBeDisposed")]
        private void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                _disposed = true;

                if (disposing)
                {
                    _localeQueueHandler.Dispose();
                    _remoteQueueHandler.Dispose();

                    lock (MessageBrokerServices)
                    {
                        Parallel.ForEach(MessageBrokerServices, s => s.Queue?.Dispose());
                    }

                    lock (SubscribeHandlers)
                    {
                        Parallel.ForEach(SubscribeHandlers, s => s.Queue?.Dispose());
                    }

                    lock (RequestHandlers)
                    {
                        Parallel.ForEach(RequestHandlers, s => s.Queue?.Dispose());
                    }

                    _handshakeTask?.Dispose();
                    _repositoryCleanupTask?.Dispose();
                }
            }
        }

        private void SendMessageBrokerHandshakes()
        {
            _logger.Debug("Message Broker State {@MessageBrokerServices} {@SubscriberHandlers} {@RequestHandlers}", MessageBrokerServices, SubscribeHandlers, RequestHandlers);

            Handler((object) new SendMessageBrokerHandshakeMessage());
        }

        private void SendRepositoryCleanupMessage()
        {
            string max;

            lock (MessageBrokerServices)
            {
                max = MessageBrokerServices.Where(b => !b.Id.NullOrEmpty()).Select(m => m.Id).Max();
            }

            if (_messageBrokerServiceInformation.Id == max)
            {
                _logger.Debug("This Message Broker is mediating the repository cleanup {MyId}", _messageBrokerServiceInformation.Id);

                Handler((object) new RepositoryCleanupMessage());

                foreach (var serverName in MessageBrokerServices.Select(s => s.ServerName).Distinct())
                {
                    SendCleanOldServicesMessage(serverName);
                }
            }
            else
                _logger.Debug("Another Message Broker is mediating the repository cleanup {MyId} {MediatorId}", _messageBrokerServiceInformation.Id, max);
        }

        private void Handler(MessageBusServiceHandshakeMessage message)
        {
            var changed = false;
            var save = false;

            using (var repositories = _repositoriesFactory.Create())
            {
                foreach (var subscribeHandler in message.SubscribeHandlers ?? Enumerable.Empty<Shared.Messages.SubscribeHandler>())
                {
                    changed |= UpdateSubscribeHandler(message.ServerName, subscribeHandler.Topic, subscribeHandler.MessageType, subscribeHandler.Name, message.ServiceName, subscribeHandler.QueueName, subscribeHandler.Durable, DateTimeOffset.Now);

                    if (subscribeHandler.Durable)
                    {
                        save = true;

                        SaveSubscriber(repositories, message.ServerName, message.ServiceName, subscribeHandler);
                    }
                }

                if (save)
                    repositories.Save();
            }

            lock (SubscribeHandlers)
            {
                changed |= SubscribeHandlers.RemoveAll(e => e.ServerName == message.ServerName && e.ServiceName == message.ServiceName && !message.SubscribeHandlers.Select(r => r.QueueName).Contains(e.QueueName)) > 0;
            }

            changed = (message.RequestHandlers ?? Enumerable.Empty<Shared.Messages.RequestHandler>()).Aggregate(changed, (current, requestHandler) => current | UpdateRequestHandler(message.ServerName, message.ServiceName, requestHandler.Name, requestHandler.RequestType, requestHandler.ResponseType, requestHandler.QueueName, DateTimeOffset.Now));

            lock (RequestHandlers)
            {
                changed |= RequestHandlers.RemoveAll(e => e.ServerName == message.ServerName && e.ServiceName == message.ServiceName && !message.RequestHandlers.Select(r => r.QueueName).Contains(e.QueueName)) > 0;
            }

            SendReply(message.ReplyQueue, CreateMessageBusServiceHandshakeReplyMessage(message), false);

            if (changed)
                SendMessageBrokerHandshakes();
        }

        private MessageBusServiceHandshakeReplyMessage CreateMessageBusServiceHandshakeReplyMessage(MessageBusServiceHandshakeMessage message)
        {
            return new MessageBusServiceHandshakeReplyMessage
            {
                MessageBrokerServerName = _messageBrokerServiceInformation.ServerName,
                MessageBrokerServiceName = _messageBrokerServiceInformation.ServiceName,
                SendDateTime = message.SendDateTime,
                ReplyDateTime = DateTimeOffset.Now,
                CompletedDateTime = null
            };
        }

        private void Handler(PublishMessage message)
        {
            var subscribeNames = GetSubscriberNames(message.Topic, message.MessageType);

            if (message.Persistent)
                SavePersistentMessage(message, subscribeNames);

            if (!subscribeNames.Any())
                _logger.Error("No Subscribers found for Topic {Topic} {@Message}", message.Topic, message);
            else
            {
                if (message.Persistent)
                {
                    using (var repositories = _repositoriesFactory.Create())
                    {
                        repositories.BeginTransaction();

                        SaveMessageStates(repositories.MessageStateRepository(), subscribeNames, message);

                        repositories.Save();

                        SendPublishSubscribeMessage(message, subscribeNames);

                        repositories.CommitTransaction();
                    }
                }
                else
                    SendPublishSubscribeMessage(message, subscribeNames);
            }

            if (!message.ReplyQueue.NullOrWhiteSpace())
                SendReply(message.ReplyQueue, CreatePublishReplyMessage(message), false);
        }

        private static void SaveMessageStates(IMessageStateRepository messageStateRepository, IEnumerable<string> subscribeNames, PublishMessage message)
        {
            foreach (var subscribeName in subscribeNames)
            {
                SaveMessageState(messageStateRepository, message.MessageId, subscribeName, SubscribeHandlerState.Distributed, message.ErrorCount);
            }
        }

        private void SendPublishSubscribeMessage(PublishMessage message, IEnumerable<string> subscribeNames)
        {
            using (var queue = _queueFactory.CreateLocale(_messageBrokerServiceInformation.LocaleQueueName, true, LocaleQueueMode.Durable, true, AccessMode.Send))
            {
                foreach (var subscribeName in subscribeNames)
                {
                    queue.Send(CreatePublishSubscriberMessage(subscribeName, message));
                }
            }
        }

        private List<string> GetSubscriberNames(string topic, string messageType)
        {
            lock (SubscribeHandlers)
            {
                return SubscribeHandlers.Where(s => s.Topic == topic && s.MessageType == messageType).Select(n => n.Name).Distinct().ToList();
            }
        }

        private void SavePersistentMessage(PublishMessage message, IList<string> subscriberNames)
        {
            using (var repositories = _repositoriesFactory.Create())
            {
                try
                {
                    SavePersistentMessage(message, subscriberNames, repositories);
                }
                catch (Exception exception)
                {
                    _logger.Warning(exception, "Error saving persistent message, retrying once {@Message} {@SubscriberNames}", message, subscriberNames);

                    repositories.MessageRepository().Delete(message.MessageId);

                    repositories.Save();

                    SavePersistentMessage(message, subscriberNames, repositories);
                }
            }
        }

        private static void SavePersistentMessage(PublishMessage message, IEnumerable<string> subscriberNames, IRepositories repositories)
        {
            SaveMessage(repositories, message);

            var messageStateRepository = repositories.MessageStateRepository();

            foreach (var subscriberName in subscriberNames)
            {
                SaveMessageState(messageStateRepository, message.MessageId, subscriberName, SubscribeHandlerState.Published, message.ErrorCount);
            }

            repositories.Save();
        }

        private PublishReplyMessage CreatePublishReplyMessage(PublishMessage message)
        {
            return new PublishReplyMessage
            {
                MessageBrokerServerName = _messageBrokerServiceInformation.ServerName,
                MessageBrokerServiceName = _messageBrokerServiceInformation.ServiceName,
                MessageId = message.MessageId,
                Topic = message.Topic,
                PublishDateTime = message.PublishDateTime,
                ReplyDateTime = DateTimeOffset.Now,
                CompletedDateTime = null
            };
        }

        private static PublishSubscriberMessage CreatePublishSubscriberMessage(string subscribeName, PublishMessage message)
        {
            return new PublishSubscriberMessage
            {
                SubscriberName = subscribeName,
                Message = message
            };
        }

        private void Handler(PublishSubscriberMessage message)
        {
            try
            {
                var subscribeHandler = SubscribeHandler(message.SubscriberName, message.Message);

                if (IsLocale(subscribeHandler.ServerName))
                    SendPublishMessageToSubscribeHandler(subscribeHandler, message.Message);
                else
                    SendPublishSubscriberMessageToRemoteMessageBroker(subscribeHandler.ServerName, message);
            }
            catch (Exception exception)
            {
                _logger.Error(exception, "Unable to find Subscriber {@Message}", message);

                if (message.Message.Persistent)
                    SaveMessageState(message.SubscriberName, message.Message.MessageId, SubscribeHandlerState.Error, message.Message.ErrorCount);

                using (var queue = _queueFactory.CreateLocale(Shared.Config.MessageBrokerConfig.LocaleQueueName, true, LocaleQueueMode.Durable, true, AccessMode.Send))
                {
                    queue.Send(CreateSubscribeHandlerErrorMessage(message));
                }
            }
        }

        private static SubscribeHandlerErrorMessage CreateSubscribeHandlerErrorMessage(PublishSubscriberMessage message)
        {
            return new SubscribeHandlerErrorMessage
            {
                MessageId = message.Message.MessageId,
                Message = message.Message,
                Durable = true,
                Exception = null,
                Name = message.SubscriberName,
                PublisherServerName = message.Message.ServerName,
                PublisherServiceName = message.Message.ServiceName,
                PublishDateTime = message.Message.PublishDateTime
            };
        }

        private void SendPublishMessageToSubscribeHandler(Dto.SubscribeHandler subscribeHandler, PublishMessage message)
        {
            try
            {
                if (message.Persistent)
                {
                    using (var repositories = _repositoriesFactory.Create())
                    {
                        repositories.BeginTransaction();
                        SaveMessageState(repositories.MessageStateRepository(), message.MessageId, subscribeHandler.Name, SubscribeHandlerState.SendToSubscriber, message.ErrorCount);
                        repositories.Save();

                        SendToSubscribeHandler(subscribeHandler, message);

                        repositories.CommitTransaction();
                    }
                }
                else
                    SendToSubscribeHandler(subscribeHandler, message);
            }
            catch (Exception exception)
            {
                _logger.Warning(exception, "Error sending publish message {@SubscribeHandler} {@Message}", subscribeHandler, message);

                if (message.Persistent)
                    SaveMessageState(message.MessageId, subscribeHandler.Name, SubscribeHandlerState.Error, message.ErrorCount);
            }
        }

        private void SendPublishSubscriberMessageToRemoteMessageBroker(string serverName, PublishSubscriberMessage message)
        {
            try
            {
                if (message.Message.Persistent)
                {
                    using (var repositories = _repositoriesFactory.Create())
                    {
                        repositories.BeginTransaction();
                        SaveMessageState(repositories.MessageStateRepository(), message.Message.MessageId, message.SubscriberName, SubscribeHandlerState.SendToServer, message.Message.ErrorCount);
                        repositories.Save();

                        SendToRemoteMessageBroker(serverName, message);

                        repositories.CommitTransaction();
                    }
                }
                else
                    SendToRemoteMessageBroker(serverName, message);
            }
            catch (Exception exception)
            {
                _logger.Warning(exception, "Error sending Publish message to Remote Message Broker {ServerName} {@Message}", serverName, message);

                if (message.Message.Persistent)
                    SaveMessageState(message.Message.MessageId, message.SubscriberName, SubscribeHandlerState.Error, message.Message.ErrorCount);
            }
        }

        private void Handler(SubscribeHandlerCompleteMessage message)
        {
            if (message.Persistent)
                SaveMessageState(message.Name, message.MessageId, SubscribeHandlerState.Completed);
        }

        private void Handler(SubscribeHandlerErrorMessage message)
        {
            ++message.Message.ErrorCount;

            if (message.Message.Persistent)
                SaveMessageState(message.Name, message.Message.MessageId, SubscribeHandlerState.Error, message.Message.ErrorCount);

            if (message.Message.ErrorCount > 2)
                throw new PublishMessageException("Error sending Message to Subscriber", message);

            var subscribeHandler = SubscribeHandler(message.Name, message.Message, false);

            if (subscribeHandler != null)
            {
                if (IsLocale(subscribeHandler.ServerName))
                    SendPublishMessageToSubscribeHandler(subscribeHandler, message.Message);
                else
                    SendPublishSubscriberMessageToRemoteMessageBroker(subscribeHandler.ServerName, CreatePublishSubscriberMessage(message.Name, message.Message));
            }

            if (subscribeHandler == null)
                throw new PublishMessageException("No subscriber handler found", message);
        }

        private void Handler(RequestMessage message)
        {
            try
            {
                var requestHandler = RequestHandler(message);

                if (IsLocale(requestHandler.ServerName))
                    SendToRequestHandler(requestHandler, message);
                else
                    SendToRemoteMessageBroker(requestHandler.ServerName, message);
            }
            catch (Exception exception)
            {
                SendReply(message.ReplyQueue, CreateResponseErrorMessage(message, exception), true);
            }
        }

        private void SendToSubscribeHandler(Dto.SubscribeHandler subscribeHandler, PublishMessage message)
        {
            if (subscribeHandler.Queue == null)
                subscribeHandler.Queue = _queueFactory.CreateLocale(subscribeHandler.QueueName, true, subscribeHandler.Durable ? LocaleQueueMode.Durable : LocaleQueueMode.TemporarySlave, true, AccessMode.Send);

            subscribeHandler.Queue.Send(message);
        }

        private void SendToRequestHandler(Dto.RequestHandler requestHandler, RequestMessage message)
        {
            if (requestHandler.Queue == null)
                requestHandler.Queue = _queueFactory.CreateLocale(requestHandler.QueueName, true, LocaleQueueMode.Durable, true, AccessMode.Send);

            requestHandler.Queue.Send(message);
        }

        private Dto.SubscribeHandler SubscribeHandler(string subscriberName, PublishMessage message, bool localeFirst = true)
        {
            var handler = SubscribeHandlers.Where(s => IsLocale(s.ServerName) == localeFirst && s.Name == subscriberName && s.MessageType == message.MessageType && s.Topic == message.Topic && s.HandshakeDateTime != null).ToList();

            if (!handler.Any())
                handler = SubscribeHandlers.Where(s => IsLocale(s.ServerName) != localeFirst && s.Name == subscriberName && s.MessageType == message.MessageType && s.Topic == message.Topic && s.HandshakeDateTime != null).ToList();

            if (!handler.Any())
                throw new SubscribeHandlerNotFoundException(subscriberName, message);

            return handler.OrderByDescending(r => r.HandshakeDateTime).FirstOrDefault();
        }

        private Dto.RequestHandler RequestHandler(RequestMessage message, bool localeFirst = true)
        {
            var handler = RequestHandlers.Where(r => IsLocale(r.ServerName) == localeFirst && r.Name == message.Name && r.RequestType == message.RequestType && r.ResponseType == message.ResponseType && r.HandshakeDateTime != null).ToList();

            if (!handler.Any())
                handler = RequestHandlers.Where(r => IsLocale(r.ServerName) != localeFirst && r.Name == message.Name && r.RequestType == message.RequestType && r.ResponseType == message.ResponseType && r.HandshakeDateTime != null).ToList();

            if (!handler.Any())
                throw new RequestHandlerNotFoundException(message);

            return handler.OrderByDescending(r => r.HandshakeDateTime).FirstOrDefault();
        }

        private ResponseErrorMessage CreateResponseErrorMessage(RequestMessage message, Exception exception)
        {
            return new ResponseErrorMessage
            {
                RequesterServerName = message.RequesterServerName,
                RequesterServiceName = message.RequesterServiceName,
                ResponderServerName = _messageBrokerServiceInformation.ServerName,
                ResponderServiceName = _messageBrokerServiceInformation.ServiceName,
                RequestId = message.RequestId,
                ReplyQueue = message.ReplyQueue,
                RequestMessage = message,
                Exception = exception,
                RequestDateTime = message.RequestDateTime,
                ResponseDateTime = DateTimeOffset.Now,
                CompletedDateTime = null
            };
        }

        private void Handler(ResponseMessage message)
        {
            SendResponse(message.RequesterServerName, message.ReplyQueue, message);
        }

        private void Handler(ResponseErrorMessage message)
        {
            SendResponse(message.RequesterServerName, message.ReplyQueue, message);
        }

        private void SendResponse<T>(string serverName, string replyQueueName, T message)
        {
            if (IsLocale(serverName))
                SendReply(replyQueueName, message, true);
            else
                SendToRemoteMessageBroker(serverName, message);
        }

        // ReSharper disable once UnusedParameter.Local
        private void Handler(SendMessageBrokerHandshakeMessage message)
        {
            lock (MessageBrokerServices)
            {
                MessageBrokerServices.RemoveAll(s => s.ErrorCount >= 3);
            }

            RemoveDeadHandlers();

            var messageBrokerHandshakeMessage = CreateMessageBrokerHandshakeMessage();

            var messageBrokerServices = GetMessageBrokerServices();

            foreach (var messageBrokerService in messageBrokerServices)
            {
                SendMessageBrokerHandshakeMessage(messageBrokerService, messageBrokerHandshakeMessage);
            }

            UpdateMessageBrokerServicePulseDateTime();
        }

        private void RemoveDeadHandlers()
        {
            var time = DateTimeOffset.Now.AddMinutes(-10);

            lock (SubscribeHandlers)
            {
                foreach (var subscribeHandler in SubscribeHandlers.Where(e => e.Queue != null && e.HandshakeDateTime != null && e.HandshakeDateTime < time && !e.Durable))
                {
                    subscribeHandler.Queue.Dispose();
                }

                SubscribeHandlers.RemoveAll(e => e.HandshakeDateTime != null && e.HandshakeDateTime < time && !e.Durable);
            }

            lock (RequestHandlers)
            {
                foreach (var requestHandler in RequestHandlers.Where(e => e.Queue != null && e.HandshakeDateTime != null && e.HandshakeDateTime < time))
                {
                    requestHandler.Queue.Dispose();
                }

                RequestHandlers.RemoveAll(e => e.HandshakeDateTime != null && e.HandshakeDateTime < time);
            }
        }

        private MessageBrokerHandshakeMessage CreateMessageBrokerHandshakeMessage()
        {
            var messageBrokerHandshakeMessage = new MessageBrokerHandshakeMessage
            {
                MessageBrokerId = _messageBrokerServiceInformation.Id,
                ServerName = _messageBrokerServiceInformation.ServerName,
                QueueName = _messageBrokerServiceInformation.RemoteQueueName
            };

            lock (RequestHandlers)
            {
                messageBrokerHandshakeMessage.LocaleRequestHandlers = RequestHandlers.Where(r => IsLocale(r.ServerName) && r.HandshakeDateTime != null).Select(s => new LocaleRequestHandler { Name = s.Name, ServiceName = s.ServiceName, RequestType = s.RequestType, ResponseType = s.ResponseType, QueueName = s.QueueName, HandshakeDateTime = s.HandshakeDateTime ?? DateTimeOffset.Now }).ToList();
            }

            lock (SubscribeHandlers)
            {
                messageBrokerHandshakeMessage.LocaleSubscribeHandlers = SubscribeHandlers.Where(r => IsLocale(r.ServerName) && r.HandshakeDateTime != null).Select(s => new LocaleSubscribeHandler { Name = s.Name, ServiceName = s.ServiceName, QueueName = s.QueueName, Topic = s.Topic, Durable = s.Durable, MessageType = s.MessageType, HandshakeDateTime = s.HandshakeDateTime ?? DateTimeOffset.Now }).ToList();
            }

            return messageBrokerHandshakeMessage;
        }

        private IEnumerable<Dto.MessageBrokerService> GetMessageBrokerServices()
        {
            ICollection<Dto.MessageBrokerService> messageBrokerServices;

            lock (MessageBrokerServices)
            {
                messageBrokerServices = MessageBrokerServices.Where(s => s.ServerName != _messageBrokerServiceInformation.ServerName || s.RemoteQueueName != _messageBrokerServiceInformation.RemoteQueueName).ToList();
            }

            return messageBrokerServices;
        }

        private void SendMessageBrokerHandshakeMessage(Dto.MessageBrokerService messageBrokerService, MessageBrokerHandshakeMessage messageBrokerHandshakeMessage)
        {
            try
            {
                SendToRemoteMessageBroker(messageBrokerService, messageBrokerHandshakeMessage);

                messageBrokerService.ErrorCount = 0;
            }
            catch (QueueMissingException exception)
            {
                _logger.Warning(exception, "Remote queue not found {@Service}", messageBrokerService);

                ++messageBrokerService.ErrorCount;
            }
        }

        private void SendCleanOldServicesMessage(string serverName)
        {
            try
            {
                SendToRemoteMessageBroker(serverName, new CleanOldServicesMessage());
            }
            catch (MessageBrokerQueueException exception)
            {
                _logger.Warning(exception, "Unable to send cleanup task to another message broker {ServerName}", serverName);
            }
        }

        private void Handler(MessageBrokerHandshakeMessage message)
        {
            UpdateMessageBrokerService(message);

            foreach (var remoteSubscribeHandler in message.LocaleSubscribeHandlers ?? Enumerable.Empty<LocaleSubscribeHandler>())
            {
                UpdateSubscribeHandler(message.ServerName, remoteSubscribeHandler.Topic, remoteSubscribeHandler.MessageType, remoteSubscribeHandler.Name, remoteSubscribeHandler.ServiceName, remoteSubscribeHandler.QueueName, remoteSubscribeHandler.Durable, remoteSubscribeHandler.HandshakeDateTime);
            }

            foreach (var remoteRequestHandler in message.LocaleRequestHandlers ?? Enumerable.Empty<LocaleRequestHandler>())
            {
                UpdateRequestHandler(message.ServerName, remoteRequestHandler.ServiceName, remoteRequestHandler.Name, remoteRequestHandler.RequestType, remoteRequestHandler.ResponseType, remoteRequestHandler.QueueName, remoteRequestHandler.HandshakeDateTime);
            }
        }

        // ReSharper disable once UnusedParameter.Local
        private void Handler(RepositoryCleanupMessage message)
        {
            RemoveMessagesForDeadSubscribers();
            RemoveCompletedMessages();
            RemoveMessageStateForDeadMessages();
        }

        // ReSharper disable once UnusedParameter.Local
        private void Handler(CleanOldServicesMessage cleanOldServicesMessage)
        {
            using (var repositories = _repositoriesFactory.Create())
            {
                var changes = false;

                var messageBrokerServiceRepository = repositories.MessageBrokerServiceRepository();

                foreach (var messageBrokerService in messageBrokerServiceRepository.GetAll().Where(s => s.ServerName == _messageBrokerServiceInformation.ServerName && s.PulseDateTime < DateTimeOffset.Now.AddDays(-7)))
                {
                    DeleteQueue(messageBrokerService.RemoteQueueName);

                    changes = true;

                    messageBrokerServiceRepository.Delete(messageBrokerService.ServerName, messageBrokerService.ServiceName);
                }

                var subscriberRepository = repositories.SubscriberRepository();

                foreach (var subscriber in subscriberRepository.GetAll().Where(s => s.ServerName == _messageBrokerServiceInformation.ServerName && s.PulseDateTime < DateTimeOffset.Now.AddDays(-7)))
                {
                    DeleteQueue(subscriber.QueueName);

                    changes = true;

                    subscriberRepository.Delete(subscriber.ServerName, subscriber.QueueName);
                }

                if (changes)
                    repositories.Save();
            }
        }

        private void DeleteQueue(string queueName)
        {
            try
            {
                using (var queue = _queueFactory.CreateLocale(queueName, true, LocaleQueueMode.Durable, true, AccessMode.SendAndReceive))
                {
                    queue.Delete();
                }
            }
            catch (Exception exception)
            {
                _logger.Warning(exception, "Unable to delete queue {QueueName}", queueName);
            }
        }

        private void UpdateMessageBrokerServiceRepository(IRepositories repositories)
        {
            var messageBrokerServiceRepository = repositories.MessageBrokerServiceRepository();

            var messageBrokerService = messageBrokerServiceRepository.Get(_messageBrokerServiceInformation.ServerName, _messageBrokerServiceInformation.ServiceName);

            if (messageBrokerService == null)
            {
                messageBrokerService = new Entity.MessageBrokerService
                {
                    ServerName = _messageBrokerServiceInformation.ServerName,
                    ServiceName = _messageBrokerServiceInformation.ServiceName,
                    LocaleQueueName = _messageBrokerServiceInformation.LocaleQueueName,
                    RemoteQueueName = _messageBrokerServiceInformation.RemoteQueueName,
                    StartDateTime = DateTimeOffset.Now,
                    PulseDateTime = DateTimeOffset.Now
                };

                messageBrokerServiceRepository.Insert(messageBrokerService);
            }
            else
            {
                messageBrokerService.LocaleQueueName = _messageBrokerServiceInformation.LocaleQueueName;
                messageBrokerService.RemoteQueueName = _messageBrokerServiceInformation.RemoteQueueName;
                messageBrokerService.StartDateTime = DateTimeOffset.Now;
                messageBrokerService.PulseDateTime = DateTimeOffset.Now;
            }

            repositories.Save();
        }

        private void UpdateMessageBrokerServicePulseDateTime()
        {
            using (var repositories = _repositoriesFactory.Create())
            {
                var messageBrokerServiceRepository = repositories.MessageBrokerServiceRepository();

                var messageBrokerService = messageBrokerServiceRepository.Get(_messageBrokerServiceInformation.ServerName, _messageBrokerServiceInformation.ServiceName);

                if (messageBrokerService != null)
                {
                    messageBrokerService.PulseDateTime = DateTimeOffset.Now;

                    repositories.Save();
                }
            }
        }

        private void LoadMessageBrokers(IRepositories repositories)
        {
            var messageBrokerServices = repositories.MessageBrokerServiceRepository().GetAll().ToList();

            lock (MessageBrokerServices)
            {
                foreach (var messageBrokerService in messageBrokerServices)
                {
                    UpdateMessageBrokerService(messageBrokerService);
                }
            }
        }

        private void UpdateMessageBrokerService(Entity.MessageBrokerService messageBrokerService)
        {
            UpdateMessageBrokerService(messageBrokerService.ServerName, messageBrokerService.RemoteQueueName);
        }

        private void UpdateMessageBrokerService(MessageBrokerHandshakeMessage message)
        {
            UpdateMessageBrokerService(message.ServerName, message.QueueName, message.MessageBrokerId, DateTimeOffset.Now);
        }

        private void UpdateMessageBrokerService(string serverName, string remoteQueueName, string id = null, DateTimeOffset? handshakeDateTime = null)
        {
            lock (MessageBrokerServices)
            {
                var messageBrokerService = MessageBrokerServices.FirstOrDefault(m => m.ServerName == serverName && m.RemoteQueueName == remoteQueueName);

                if (messageBrokerService == null)
                {
                    MessageBrokerServices.Add(new Dto.MessageBrokerService
                    {
                        Id = id,
                        ServerName = serverName,
                        RemoteQueueName = remoteQueueName,
                        HandshakeDateTime = handshakeDateTime
                    });
                }
                else
                {
                    messageBrokerService.Id = id ?? messageBrokerService.Id;
                    messageBrokerService.HandshakeDateTime = handshakeDateTime ?? messageBrokerService.HandshakeDateTime;
                }
            }
        }

        private void UpdateSubscribeHandler(Subscriber subscriber)
        {
            UpdateSubscribeHandler(subscriber.ServerName, subscriber.Topic, subscriber.MessageType, subscriber.Name, subscriber.ServiceName, subscriber.QueueName, true, null);
        }

        private bool UpdateSubscribeHandler(string serverName, string topic, string messageType, string name, string serviceName, string queueName, bool durable, DateTimeOffset? handshakeDateTime)
        {
            lock (SubscribeHandlers)
            {
                var subscribeHandler = SubscribeHandlers.FirstOrDefault(r => r.ServerName == serverName && r.QueueName == queueName);

                if (subscribeHandler == null)
                {
                    subscribeHandler = new Dto.SubscribeHandler
                    {
                        ServerName = serverName,
                        ServiceName = serviceName,
                        Topic = topic,
                        MessageType = messageType,
                        Name = name,
                        QueueName = queueName,
                        Durable = durable,
                        HandshakeDateTime = handshakeDateTime,
                        Queue = null
                    };

                    SubscribeHandlers.Add(subscribeHandler);

                    return true;
                }

                var before = subscribeHandler.SerializeToJson();

                subscribeHandler.Topic = topic ?? subscribeHandler.Topic;
                subscribeHandler.Name = name ?? subscribeHandler.Name;
                subscribeHandler.Durable = durable;
                subscribeHandler.MessageType = messageType;

                var after = subscribeHandler.SerializeToJson();

                subscribeHandler.HandshakeDateTime = handshakeDateTime ?? subscribeHandler.HandshakeDateTime;

                return before != after;
            }
        }

        private bool UpdateRequestHandler(string serverName, string serviceName, string name, string requestType, string responseType, string queueName, DateTimeOffset? handshakeDateTime)
        {
            lock (RequestHandlers)
            {
                var requestHandler = RequestHandlers.FirstOrDefault(r => r.ServerName == serverName && r.QueueName == queueName);

                if (requestHandler == null)
                {
                    requestHandler = new Dto.RequestHandler
                    {
                        ServerName = serverName,
                        ServiceName = serviceName,
                        Name = name,
                        RequestType = requestType,
                        ResponseType = responseType,
                        QueueName = queueName,
                        HandshakeDateTime = handshakeDateTime,
                        Queue = null
                    };

                    RequestHandlers.Add(requestHandler);

                    return true;
                }

                var before = requestHandler.SerializeToJson();

                requestHandler.Name = name ?? requestHandler.Name;
                requestHandler.RequestType = requestType ?? requestHandler.RequestType;
                requestHandler.ResponseType = responseType ?? requestHandler.ResponseType;

                var after = requestHandler.SerializeToJson();

                requestHandler.HandshakeDateTime = handshakeDateTime ?? requestHandler.HandshakeDateTime;

                return before != after;
            }
        }

        private void SendReply<T>(string replyQueueName, T message, bool transactional)
        {
            try
            {
                using (var replyQueue = _queueFactory.CreateLocale(replyQueueName, true, LocaleQueueMode.TemporarySlave, transactional, AccessMode.Send))
                {
                    replyQueue.Send(message);
                }
            }
            catch (QueueMissingException exception)
            {
                _logger.Warning(exception, "Unable to send reply to {QueueName} {Transactional} {@Message}", replyQueueName, transactional, message);
            }
        }

        private void LoadLocaleSubscribers(IRepositories repositories, string serverName)
        {
            var subscribers = repositories.SubscriberRepository().GetAll().Where(s => s.ServerName == serverName);

            foreach (var subscriber in subscribers)
            {
                UpdateSubscribeHandler(subscriber);
            }
        }

        private void RemoveMessagesForDeadSubscribers()
        {
            using (var repositories = _repositoriesFactory.Create())
            {
                var messageStateRepository = repositories.MessageStateRepository();
                var subscriberRepository = repositories.SubscriberRepository();

                var subscriberNames = messageStateRepository.GetAll().Select(m => m.SubscriberName).Distinct().Except(subscriberRepository.GetAll().Select(s => s.Name).Distinct()).ToList();

                if (subscriberNames.Any())
                {
                    var processCount = 0;

                    foreach (var subscriberName in subscriberNames)
                    {
                        if (SaveAndHasBeenCanceled(repositories, ++processCount))
                            return;

                        messageStateRepository.DeleteBySubscriber(subscriberName);
                    }

                    repositories.Save();
                }
            }
        }

        private void RemoveCompletedMessages()
        {
            using (var repositories = _repositoriesFactory.Create())
            {
                var messageStateRepository = repositories.MessageStateRepository();
                var messageRepository = repositories.MessageRepository();

                var messageIds = messageRepository.GetAll().Where(m => m.PublishDateTime < DateTimeOffset.Now.AddDays(-7)).Select(m => m.Id).Distinct().Except(messageStateRepository.GetAll().Where(s => s.State != "Completed").Select(s => s.MessageId).Distinct()).ToList();

                if (messageIds.Any())
                {
                    var processCount = 0;

                    foreach (var messageId in messageIds)
                    {
                        if (SaveAndHasBeenCanceled(repositories, ++processCount))
                            return;

                        messageRepository.Delete(messageId);
                    }

                    repositories.Save();
                }
            }
        }

        private void RemoveMessageStateForDeadMessages()
        {
            using (var repositories = _repositoriesFactory.Create())
            {
                var messageStateRepository = repositories.MessageStateRepository();
                var messageRepository = repositories.MessageRepository();

                var messageIds = messageStateRepository.GetAll().Select(s => s.MessageId).Distinct().Except(messageRepository.GetAll().Select(m => m.Id).Distinct()).ToList();

                if (messageIds.Any())
                {
                    var processCount = 0;

                    foreach (var messageId in messageIds)
                    {
                        if (SaveAndHasBeenCanceled(repositories, ++processCount))
                            return;

                        messageStateRepository.DeleteByMessageId(messageId);
                    }

                    repositories.Save();
                }
            }
        }

        private static void SaveSubscriber(IRepositories repositories, string serverName, string serviceName, Shared.Messages.SubscribeHandler message)
        {
            var subscriberRepository = repositories.SubscriberRepository();

            var subscriber = subscriberRepository.Get(serverName, message.QueueName);

            if (subscriber == null)
            {
                subscriber = new Subscriber
                {
                    ServerName = serverName,
                    ServiceName = serviceName,
                    Name = message.Name,
                    MessageType = message.MessageType,
                    Topic = message.Topic,
                    QueueName = message.QueueName,
                    RegisterDateTime = DateTimeOffset.Now,
                    PulseDateTime = DateTimeOffset.Now
                };

                subscriberRepository.Insert(subscriber);
            }
            else
            {
                subscriber.ServiceName = serviceName;
                subscriber.Name = message.Name;
                subscriber.Topic = message.Topic;
                subscriber.PulseDateTime = DateTimeOffset.Now;
            }
        }

        private static void SaveMessage(IRepositories repositories, PublishMessage message)
        {
            repositories.MessageRepository().Insert(new Message
            {
                Id = message.MessageId,
                Topic = message.Topic,
                Body = message.SerializeToJson(new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All }),
                PublishDateTime = message.PublishDateTime,
                Type = message.GetType().ToString()
            });
        }

        private void SaveMessageState(string subscriberName, string messageId, SubscribeHandlerState subscribeHandlerState, int errorCount = 0)
        {
            using (var repositories = _repositoriesFactory.Create())
            {
                SaveMessageState(repositories.MessageStateRepository(), messageId, subscriberName, subscribeHandlerState, errorCount);

                repositories.Save();
            }
        }

        private static void SaveMessageState(IMessageStateRepository messageStateRepository, string messageId, string subscriberName, SubscribeHandlerState subscribeHandlerState, int errorCount = 0)
        {
            messageStateRepository.Insert(new MessageState
            {
                MessageId = messageId,
                SubscriberName = subscriberName,
                State = subscribeHandlerState.ToString(),
                ErrorCount = errorCount
            });
        }

        private void SendToRemoteMessageBroker<T>(string serverName, T message)
        {
            var queue = RemoteMessageBrokerQueue(serverName);

            SendToRemoteMessageBroker(queue, message);
        }

        private IRemoteQueue RemoteMessageBrokerQueue(string serverName)
        {
            var messageBrokerService = MessageBrokerServices.Where(b => b.ServerName == serverName && b.HandshakeDateTime != null).OrderByDescending(m => m.HandshakeDateTime).FirstOrDefault();

            if (messageBrokerService == null)
                throw new MessageBrokerException(serverName);

            return RemoteMessageBrokerQueue(messageBrokerService);
        }
        
        private void SendToRemoteMessageBroker<T>(Dto.MessageBrokerService messageBrokerService, T message)
        {
            var queue = RemoteMessageBrokerQueue(messageBrokerService);

            SendToRemoteMessageBroker(queue, message);
        }

        private IRemoteQueue RemoteMessageBrokerQueue(Dto.MessageBrokerService messageBrokerService)
        {
            if (messageBrokerService.Queue == null)
                messageBrokerService.Queue = _queueFactory.CreateRemote(messageBrokerService.ServerName, messageBrokerService.RemoteQueueName, true, RemoteQueueMode.Durable, true, AccessMode.Send);

            if (messageBrokerService.Queue == null)
                throw new MessageBrokerQueueException(messageBrokerService);

            return messageBrokerService.Queue;
        }

        private static void SendToRemoteMessageBroker<T>(IQueue queue, T message)
        {
            queue.Send(message);
        }

        private bool SaveAndHasBeenCanceled(IRepositories repositories, int count)
        {
            if (count % 100 == 0)
            {
                repositories.Save();

                if (_cancellationToken.IsCancellationRequested)
                    return true;
            }

            return false;
        }

        private bool IsLocale(string serverName)
        {
            return string.Equals(serverName, _messageBrokerServiceInformation.ServerName, StringComparison.InvariantCultureIgnoreCase);
        }
    }
}
