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
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Core.Interfaces;
using Grumpy.RipplesMQ.Core.Messages;
using Grumpy.RipplesMQ.Entity;
using Grumpy.RipplesMQ.Shared.Exceptions;
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

            _logger.LogInformation("RipplesMQ Message Broker Server Created {@Information}", _messageBrokerServiceInformation);
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

            _localeQueueHandler.Start(_messageBrokerServiceInformation.LocaleQueueName, true, LocaleQueueMode.DurableCreate, true, Handler, ErrorHandler, null, 1000, true, false, _cancellationToken);
            _remoteQueueHandler.Start(_messageBrokerServiceInformation.RemoteQueueName, true, LocaleQueueMode.DurableCreate, true, Handler, ErrorHandler, null, 1000, true, false, _cancellationToken);

            _handshakeTask.Start(SendMessageBrokerHandshakes, 30000, _cancellationToken);
            _repositoryCleanupTask.Start(SendRepositoryCleanupMessage, 3600000, _cancellationToken);

            _logger.LogInformation("RipplesMQ Message Broker Server Started");
        }

        /// <inheritdoc />
        public void Stop()
        {
            _localeQueueHandler.Stop();
            _remoteQueueHandler.Stop();
            _handshakeTask.Stop();
            _repositoryCleanupTask.Stop();

            _logger.LogInformation("RipplesMQ Message Broker Server Stopped");
        }

        /// <inheritdoc />
        public void Handler(object message, CancellationToken cancellationToken)
        {
            _logger.LogDebug("Message received {MessageType} {@Message}", message.GetType().Name, message);

            switch (message)
            {
                case MessageBusServiceRegisterMessage messageBusServiceRegisterMessage:
                    Handler(messageBusServiceRegisterMessage);
                    break;
                case SubscribeHandlerRegisterMessage subscribeHandlerRegisterMessage:
                    Handler(subscribeHandlerRegisterMessage);
                    break;
                case RequestHandlerRegisterMessage requestHandlerRegisterMessage:
                    Handler(requestHandlerRegisterMessage);
                    break;
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
            Handler(new SendMessageBrokerHandshakeMessage(), _cancellationToken);
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

                Handler(new RepositoryCleanupMessage(), _cancellationToken);

                foreach (var serverName in MessageBrokerServices.Select(s => s.ServerName).Distinct())
                {
                    SendCleanOldServicesMessage(serverName);
                }
            }
            else
                _logger.Debug("Another Message Broker is mediating the repository cleanup {MyId} {MediatorId}", _messageBrokerServiceInformation.Id, max);
        }

        private void Handler(MessageBusServiceRegisterMessage message)
        {
            SendReply(message.ReplyQueue, CreateMessageBusServiceRegisterReplyMessage(message.RegisterDateTime), false);
        }

        private MessageBusServiceRegisterReplyMessage CreateMessageBusServiceRegisterReplyMessage(DateTimeOffset registerDateTime)
        {
            return new MessageBusServiceRegisterReplyMessage
            {
                MessageBrokerServerName = _messageBrokerServiceInformation.ServerName,
                MessageBrokerServiceName = _messageBrokerServiceInformation.ServiceName,
                RegisterDateTime = registerDateTime,
                ReplyDateTime = DateTimeOffset.Now,
                CompletedDateTime = null
            };
        }

        private void Handler(SubscribeHandlerRegisterMessage message)
        {
            if (message.Durable)
                SaveSubscriber(message);

            SendReply(message.ReplyQueue, CreateSubscribeHandlerRegisterReplyMessage(message.RegisterDateTime), false);

            UpdateSubscribeHandler(message);
        }

        private SubscribeHandlerRegisterReplyMessage CreateSubscribeHandlerRegisterReplyMessage(DateTimeOffset registerDateTime)
        {
            return new SubscribeHandlerRegisterReplyMessage
            {
                MessageBrokerServerName = _messageBrokerServiceInformation.ServerName,
                MessageBrokerServiceName = _messageBrokerServiceInformation.ServiceName,
                RegisterDateTime = registerDateTime,
                ReplyDateTime = DateTimeOffset.Now,
                CompletedDateTime = null
            };
        }

        private void Handler(RequestHandlerRegisterMessage message)
        {
            SendReply(message.ReplyQueue, CreateRequestHandlerRegisterReplyMessage(message.RegisterDateTime), false);

            UpdateRequestHandler(message);
        }

        private RequestHandlerRegisterReplyMessage CreateRequestHandlerRegisterReplyMessage(DateTimeOffset registerDateTime)
        {
            return new RequestHandlerRegisterReplyMessage
            {
                MessageBrokerServerName = _messageBrokerServiceInformation.ServerName,
                MessageBrokerServiceName = _messageBrokerServiceInformation.ServiceName,
                RegisterDateTime = registerDateTime,
                ReplyDateTime = DateTimeOffset.Now,
                CompletedDateTime = null
            };
        }

        private void Handler(MessageBusServiceHandshakeMessage message)
        {
            foreach (var subscribeHandler in message.SubscribeHandlers ?? Enumerable.Empty<Shared.Messages.SubscribeHandler>())
            {
                UpdateSubscribeHandler(message.ServerName, subscribeHandler.Topic, subscribeHandler.Name, subscribeHandler.QueueName, subscribeHandler.Durable, DateTimeOffset.Now);

                if (subscribeHandler.Durable)
                    UpdateSubscribePulse(message.ServerName, subscribeHandler.QueueName);
            }

            foreach (var requestHandler in message.RequestHandlers ?? Enumerable.Empty<Shared.Messages.RequestHandler>())
            {
                UpdateRequestHandler(message.ServerName, requestHandler.Name, requestHandler.QueueName, DateTimeOffset.Now);
            }
        }

        private void Handler(PublishMessage message)
        {
            var subscribeNames = GetSubscriberNames(message.Topic);

            if (!subscribeNames.Any())
                _logger.Information("No Subscribers found for Topic {@Message}", message);

            if (message.Persistent)
                SavePersistentMessage(message, subscribeNames);

            using (var queue = _queueFactory.CreateLocale(Shared.Config.MessageBrokerConfig.LocaleQueueName, true, LocaleQueueMode.Durable, true))
            {
                if (message.Persistent)
                {
                    using (var repositories = _repositoriesFactory.Create())
                    {
                        var messageStateRepository = repositories.MessageStateRepository();

                        foreach (var subscribeName in subscribeNames)
                        {
                            queue.Send(CreatePublishSubscriberMessage(subscribeName, message));

                            SaveMessageState(messageStateRepository, message.MessageId, subscribeName, SubscribeHandlerState.Distributed, message.ErrorCount);
                        }

                        repositories.Save();
                    }
                }
                else
                {
                    foreach (var subscribeName in subscribeNames)
                    {
                        queue.Send(CreatePublishSubscriberMessage(subscribeName, message));
                    }
                }
            }

            if (!message.ReplyQueue.NullOrWhiteSpace())
                SendReply(message.ReplyQueue, CreatePublishReplyMessage(message), false);
        }

        private List<string> GetSubscriberNames(string topic)
        {
            lock (SubscribeHandlers)
            {
                return SubscribeHandlers.Where(s => s.Topic == topic).Select(n => n.Name).Distinct().ToList();
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
                SaveMessageState(messageStateRepository, message.MessageId, subscriberName, SubscribeHandlerState.Published,
                    message.ErrorCount, message.PublishDateTime);
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
            var subscribeHandler = FindSubscribeHandler(message.SubscriberName, message.Message.Topic, true);

            var subscribeHandlerState = SubscribeHandlerState.Error;

            if (subscribeHandler != null)
                subscribeHandlerState = IsLocale(subscribeHandler.ServerName) ? SendPublishMessage(subscribeHandler, message.Message) : SendPublishSubscriberMessageToRemoteMessageBroker(subscribeHandler.ServerName, message);

            if (message.Message.Persistent)
                SaveMessageState(message.SubscriberName, message.Message.MessageId, subscribeHandlerState, message.Message.ErrorCount);

            if (subscribeHandlerState == SubscribeHandlerState.Error)
            {
                _logger.Error("Error sending Publish message to Subscriber {@SubscribeHandler} {@Message}", subscribeHandler, message);

                using (var queue = _queueFactory.CreateLocale(Shared.Config.MessageBrokerConfig.LocaleQueueName, true, LocaleQueueMode.Durable, true))
                {
                    var subscribeHandlerErrorMessage = new SubscribeHandlerErrorMessage
                    {
                        MessageId = message.Message.MessageId,
                        Message = message.Message,
                        Durable = subscribeHandler?.Durable ?? true,
                        Exception = null,
                        Name = message.SubscriberName,
                        PublisherServerName = message.Message.ServerName,
                        PublisherServiceName = message.Message.ServiceName,
                        PublishDateTime = message.Message.PublishDateTime
                    };

                    queue.Send(subscribeHandlerErrorMessage);
                }
            }
        }

        private Dto.SubscribeHandler FindSubscribeHandler(string name, string topic, bool localeFirst)
        {
            var subscribeHandler = SubscribeHandlers.Where(s => IsLocale(s.ServerName) == localeFirst && s.Name == name && s.Topic == topic && s.HandshakeDateTime != null).OrderByDescending(o => o.HandshakeDateTime).FirstOrDefault();

            return subscribeHandler ?? SubscribeHandlers.Where(s => IsLocale(s.ServerName) != localeFirst && s.Name == name && s.Topic == topic && s.HandshakeDateTime != null).OrderByDescending(o => o.HandshakeDateTime).FirstOrDefault();
        }

        private SubscribeHandlerState SendPublishMessage(Dto.SubscribeHandler subscribeHandler, PublishMessage message)
        {
            try
            {
                if (subscribeHandler.Queue == null)
                    subscribeHandler.Queue = _queueFactory.CreateLocale(subscribeHandler.QueueName, true, subscribeHandler.Durable ? LocaleQueueMode.Durable : LocaleQueueMode.TemporarySlave, true);

                subscribeHandler.Queue.Send(message);
            }
            catch (Exception exception)
            {
                _logger.Warning(exception, "Error sending message to Subscriber {@SubscribeHandler} {@Message}", subscribeHandler, message);

                return SubscribeHandlerState.Error;
            }

            return SubscribeHandlerState.SendToSubscriber;
        }

        private SubscribeHandlerState SendPublishSubscriberMessageToRemoteMessageBroker(string serverName, PublishSubscriberMessage message)
        {
            try
            {
                var queue = RemoteMessageBrokerQueue(serverName);

                if (queue != null)
                {
                    queue.Send(message);

                    return SubscribeHandlerState.SendToServer;
                }
            }
            catch (Exception exception)
            {
                _logger.Warning(exception, "Error sending Publish message to Remote Message Broker {ServerName} {@Message}", serverName, message);
            }

            return SubscribeHandlerState.Error;
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

            if (message.Message.ErrorCount == 1)
            {
                var subscribeHandler = FindSubscribeHandler(message.Name, message.Message.Topic, false);

                if (subscribeHandler != null)
                {
                    var subscribeHandlerState = IsLocale(subscribeHandler.ServerName) ? SendPublishMessage(subscribeHandler, message.Message) : SendPublishSubscriberMessageToRemoteMessageBroker(subscribeHandler.ServerName, CreatePublishSubscriberMessage(message.Name, message.Message));

                    if (message.Message.Persistent)
                        SaveMessageState(message.Name, message.Message.MessageId, subscribeHandlerState, message.Message.ErrorCount);
                }
            }
        }

        private void Handler(RequestMessage message)
        {
            var requestHandler = RequestHandlers.Where(r => r.Name == message.Name && IsLocale(r.ServerName) && r.HandshakeDateTime != null).OrderByDescending(r => r.HandshakeDateTime).FirstOrDefault();

            IQueue queue = null;

            if (requestHandler != null)
            {
                if (requestHandler.Queue == null)
                    requestHandler.Queue = _queueFactory.CreateLocale(requestHandler.QueueName, true, LocaleQueueMode.Durable, true);

                queue = requestHandler.Queue;
            }
            else
            {
                requestHandler = RequestHandlers.Where(r => r.Name == message.Name && r.ServerName != _messageBrokerServiceInformation.ServerName && r.HandshakeDateTime != null).OrderByDescending(r => r.HandshakeDateTime).FirstOrDefault();

                if (requestHandler != null)
                    queue = RemoteMessageBrokerQueue(requestHandler.ServerName);
            }

            if (queue != null)
                queue.Send(message);
            else
                SendResponse(message.RequesterServerName, message.ReplyQueue, CreateResponseErrorMessage(message, new RequestHandlerNotFoundException(message.Name)));
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
                RemoteMessageBrokerQueue(serverName)?.Send(message);
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
                messageBrokerHandshakeMessage.LocaleRequestHandlers = RequestHandlers.Where(r => IsLocale(r.ServerName)).Select(s => new LocaleRequestHandler { Name = s.Name, QueueName = s.QueueName }).ToList();
            }

            lock (SubscribeHandlers)
            {
                messageBrokerHandshakeMessage.LocaleSubscribeHandlers = SubscribeHandlers.Where(r => IsLocale(r.ServerName)).Select(s => new LocaleSubscribeHandler { Name = s.Name, QueueName = s.QueueName, Topic = s.Topic, Durable = s.Durable }).ToList();
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
                using (var queue = _queueFactory.CreateRemote(messageBrokerService.ServerName, messageBrokerService.RemoteQueueName, true, RemoteQueueMode.Durable, true))
                {
                    if (queue == null)
                        throw new QueueMissingException(messageBrokerService.RemoteQueueName);

                    queue.Send(messageBrokerHandshakeMessage);

                    messageBrokerService.ErrorCount = 0;
                }
            }
            catch (QueueMissingException exception)
            {
                _logger.Warning(exception, "Remote queue not found {@Service}", messageBrokerService);

                ++messageBrokerService.ErrorCount;
            }
        }

        private void SendCleanOldServicesMessage(string serverName)
        {
            var messageBrokerService = MessageBrokerServices.Where(s => s.ServerName == serverName).OrderByDescending(m => m.HandshakeDateTime).FirstOrDefault();

            try
            {
                if (messageBrokerService != null)
                {
                    using (var queue = _queueFactory.CreateRemote(messageBrokerService.ServerName, messageBrokerService.RemoteQueueName, true, RemoteQueueMode.Durable, true))
                    {
                        queue?.Send(new CleanOldServicesMessage());
                    }
                }
            }
            catch (QueueMissingException exception)
            {
                _logger.Warning(exception, "Remote queue not found on {ServerName} {@Service}", serverName, messageBrokerService);
            }
        }

        private void Handler(MessageBrokerHandshakeMessage message)
        {
            UpdateMessageBrokerService(message);

            foreach (var remoteSubscribeHandler in message.LocaleSubscribeHandlers ?? Enumerable.Empty<LocaleSubscribeHandler>())
            {
                UpdateSubscribeHandler(message.ServerName, remoteSubscribeHandler.Topic, remoteSubscribeHandler.Name, remoteSubscribeHandler.QueueName, remoteSubscribeHandler.Durable, DateTimeOffset.Now);
            }

            foreach (var remoteRequestHandler in message.LocaleRequestHandlers ?? Enumerable.Empty<LocaleRequestHandler>())
            {
                UpdateRequestHandler(message.ServerName, remoteRequestHandler.Name, remoteRequestHandler.QueueName, DateTimeOffset.Now);
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
                using (var queue = _queueFactory.CreateLocale(queueName, true, LocaleQueueMode.Durable, true))
                {
                    queue.Delete();
                }
            }
            catch (Exception exception)
            {
                _logger.Warning(exception, $"Unable to delete queue {queueName}");
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

        private void UpdateSubscribePulse(string serverName, string queueName)
        {
            using (var repositories = _repositoriesFactory.Create())
            {
                var subscriberRepository = repositories.SubscriberRepository();

                var subscriber = subscriberRepository.Get(serverName, queueName);

                if (subscriber != null)
                {
                    subscriber.PulseDateTime = DateTimeOffset.Now;

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

        private void UpdateMessageBrokerService(string serverName, string remoteQueueName, string id = null, DateTimeOffset? handshakeDateTime = null, IQueue queue = null)
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
                        HandshakeDateTime = handshakeDateTime,
                        Queue = queue
                    });
                }
                else
                {
                    messageBrokerService.Id = id ?? messageBrokerService.Id;
                    messageBrokerService.HandshakeDateTime = handshakeDateTime ?? messageBrokerService.HandshakeDateTime;
                    messageBrokerService.Queue = queue ?? messageBrokerService.Queue;
                }
            }
        }

        private void UpdateSubscribeHandler(Subscriber subscriber)
        {
            UpdateSubscribeHandler(subscriber.ServerName, subscriber.Topic, subscriber.Name, subscriber.QueueName, true, DateTimeOffset.Now);
        }

        private void UpdateSubscribeHandler(SubscribeHandlerRegisterMessage message)
        {
            UpdateSubscribeHandler(message.ServerName, message.Topic, message.Name, message.QueueName, message.Durable, DateTimeOffset.Now);
        }

        private void UpdateSubscribeHandler(string serverName, string topic, string name, string queueName, bool durable, DateTimeOffset? handshakeDateTime = null, IQueue queue = null)
        {
            lock (SubscribeHandlers)
            {
                var subscribeHandler = SubscribeHandlers.FirstOrDefault(r => r.ServerName == serverName && r.QueueName == queueName);

                if (subscribeHandler == null)
                {
                    SubscribeHandlers.Add(new Dto.SubscribeHandler
                    {
                        ServerName = serverName,
                        Topic = topic,
                        Name = name,
                        QueueName = queueName,
                        Durable = durable,
                        HandshakeDateTime = handshakeDateTime,
                        Queue = queue
                    });
                }
                else
                {
                    subscribeHandler.Topic = topic ?? subscribeHandler.Topic;
                    subscribeHandler.Name = name ?? subscribeHandler.Name;
                    subscribeHandler.HandshakeDateTime = handshakeDateTime ?? subscribeHandler.HandshakeDateTime;
                    subscribeHandler.Queue = queue ?? subscribeHandler.Queue;
                }
            }
        }

        private void UpdateRequestHandler(RequestHandlerRegisterMessage message)
        {
            UpdateRequestHandler(message.ServerName, message.Name, message.QueueName, DateTimeOffset.Now);
        }

        private void UpdateRequestHandler(string serverName, string name, string queueName, DateTimeOffset? handshakeDateTime = null, IQueue queue = null)
        {
            lock (RequestHandlers)
            {
                var requestHandler = RequestHandlers.FirstOrDefault(r => r.ServerName == serverName && r.QueueName == queueName);

                if (requestHandler == null)
                {
                    RequestHandlers.Add(new Dto.RequestHandler
                    {
                        ServerName = serverName,
                        Name = name,
                        QueueName = queueName,
                        HandshakeDateTime = handshakeDateTime,
                        Queue = queue
                    });
                }
                else
                {
                    requestHandler.Name = name ?? requestHandler.Name;
                    requestHandler.HandshakeDateTime = handshakeDateTime ?? requestHandler.HandshakeDateTime;
                    requestHandler.Queue = queue ?? requestHandler.Queue;
                }
            }
        }

        private void SendReply<T>(string replyQueueName, T message, bool transactional)
        {
            try
            {
                using (var replyQueue = _queueFactory.CreateLocale(replyQueueName, true, LocaleQueueMode.TemporarySlave, transactional))
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

        private void SaveSubscriber(SubscribeHandlerRegisterMessage message)
        {
            using (var repositories = _repositoriesFactory.Create())
            {
                var subscriberRepository = repositories.SubscriberRepository();

                var subscriber = subscriberRepository.Get(message.ServerName, message.QueueName);

                if (subscriber == null)
                {
                    subscriber = new Subscriber
                    {
                        ServerName = message.ServerName,
                        ServiceName = message.ServiceName,
                        Name = message.Name,
                        Topic = message.Topic,
                        QueueName = message.QueueName,
                        RegisterDateTime = DateTimeOffset.Now,
                        PulseDateTime = DateTimeOffset.Now
                    };

                    subscriberRepository.Insert(subscriber);
                }
                else
                {
                    subscriber.ServiceName = message.ServiceName;
                    subscriber.Name = message.Name;
                    subscriber.Topic = message.Topic;
                    subscriber.RegisterDateTime = DateTimeOffset.Now;
                    subscriber.PulseDateTime = DateTimeOffset.Now;
                }

                repositories.Save();
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

        private static void SaveMessageState(IMessageStateRepository messageStateRepository, string messageId, string subscriberName, SubscribeHandlerState subscribeHandlerState, int errorCount = 0, DateTimeOffset? updateDateTime = null)
        {
            messageStateRepository.Insert(new MessageState
            {
                MessageId = messageId,
                SubscriberName = subscriberName,
                State = subscribeHandlerState.ToString(),
                ErrorCount = errorCount,
                UpdateDateTime = updateDateTime ?? DateTimeOffset.Now
            });
        }

        private IQueue RemoteMessageBrokerQueue(string serverName)
        {
            var messageBrokerService = MessageBrokerServices.Where(b => b.ServerName == serverName && b.HandshakeDateTime != null).OrderByDescending(m => m.HandshakeDateTime).FirstOrDefault();

            if (messageBrokerService != null && messageBrokerService.Queue == null)
                messageBrokerService.Queue = _queueFactory.CreateRemote(messageBrokerService.ServerName, messageBrokerService.RemoteQueueName, true, RemoteQueueMode.Durable, true);

            return messageBrokerService?.Queue;
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