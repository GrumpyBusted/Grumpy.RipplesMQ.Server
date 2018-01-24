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
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.RipplesMQ.Core.Dto;
using Grumpy.RipplesMQ.Core.Entities;
using Grumpy.RipplesMQ.Core.Enum;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Core.Interfaces;
using Grumpy.RipplesMQ.Core.Messages;
using Grumpy.RipplesMQ.Shared.Exceptions;
using Grumpy.RipplesMQ.Shared.Messages;
using Newtonsoft.Json;

namespace Grumpy.RipplesMQ.Core
{
    public class MessageBroker : IMessageBroker
    {
        private readonly IRepositoriesFactory _repositoriesFactory;
        private readonly IQueueFactory _queueFactory;
        private readonly IQueueHandler _localeQueueHandler;
        private readonly IQueueHandler _remoteQueueHandler;
        private readonly MessageBrokerServiceInformation _messageBrokerServiceInformation;
        private CancellationToken _cancellationToken;
        private ITimerTask _handshakeTask;
        private ITimerTask _repositoryCleanupTask;
        private bool _disposed;

        public List<Dto.MessageBrokerService> MessageBrokerServices { get; }
        public List<Dto.SubscribeHandler> SubscribeHandlers { get; }
        public List<Dto.RequestHandler> RequestHandlers { get; }

        public MessageBroker(MessageBrokerConfig messageBrokerConfig, IRepositoriesFactory repositoriesFactory, IQueueHandlerFactory queueHandlerFactory, IQueueFactory queueFactory, IProcessInformation processInformation)
        {
            _repositoriesFactory = repositoriesFactory;
            _queueFactory = queueFactory;
            _localeQueueHandler = queueHandlerFactory.Create();
            _remoteQueueHandler = queueHandlerFactory.Create();

            _messageBrokerServiceInformation = new MessageBrokerServiceInformation
            {
                Id = UniqueKeyUtility.Generate(),
                ServerName = processInformation.MachineName,
                ServiceName = messageBrokerConfig.ServiceName,
                InstanceName = messageBrokerConfig.InstanceName,
                LocaleQueueName = Shared.Config.MessageBrokerConfig.LocaleQueueName,
                RemoteQueueName =  messageBrokerConfig.RemoteQueueName
            };

            Console.WriteLine(_messageBrokerServiceInformation.SerializeToJson());

            MessageBrokerServices = new List<Dto.MessageBrokerService>();
            SubscribeHandlers = new List<Dto.SubscribeHandler>();
            RequestHandlers = new List<Dto.RequestHandler>();
        }

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

            _localeQueueHandler.Start(_messageBrokerServiceInformation.LocaleQueueName, true, LocaleQueueMode.DurableCreate, true, Handler, null, null, 1000, true, false, _cancellationToken);
            _remoteQueueHandler.Start(_messageBrokerServiceInformation.RemoteQueueName, true, LocaleQueueMode.DurableCreate, true, Handler, null, null, 1000, true, false, _cancellationToken);

            _handshakeTask = new TimerTask();
            _handshakeTask.Start(SendMessageBrokerHandshakes, 30000, _cancellationToken);

            _repositoryCleanupTask = new TimerTask();
            _repositoryCleanupTask.Start(SendRepositoryCleanupMessage, 3600000, _cancellationToken);
        }

        public void Handler(object message, CancellationToken cancellationToken)
        {
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
                case PublishSubscriberMessage remotePublishMessage:
                    Handler(remotePublishMessage);
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
            }
        }

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
                Handler(new RepositoryCleanupMessage(), _cancellationToken);
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
                MessageBrokerInstanceName = _messageBrokerServiceInformation.InstanceName,
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
                MessageBrokerInstanceName = _messageBrokerServiceInformation.InstanceName,
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
                MessageBrokerInstanceName = _messageBrokerServiceInformation.InstanceName,
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
            }

            foreach (var requestHandler in message.RequestHandlers ?? Enumerable.Empty<Shared.Messages.RequestHandler>())
            {
                UpdateRequestHandler(message.ServerName, requestHandler.Name, requestHandler.QueueName, DateTimeOffset.Now);
            }
        }

        private void Handler(PublishMessage message)
        {
            var subscribeNames = GetSubscriberNames(message.Topic);

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

        private void SavePersistentMessage(PublishMessage message, IEnumerable<string> subscriberNames)
        {
            using (var repositories = _repositoriesFactory.Create())
            {
                SaveMessage(repositories, message);

                var messageStateRepository = repositories.MessageStateRepository();

                foreach (var subscriberName in subscriberNames)
                {
                    SaveMessageState(messageStateRepository, message.MessageId, subscriberName, SubscribeHandlerState.Published, message.ErrorCount, message.PublishDateTime);
                }

                repositories.Save();
            }
        }

        private PublishReplyMessage CreatePublishReplyMessage(PublishMessage message)
        {
            return new PublishReplyMessage
            {
                MessageBrokerServerName = _messageBrokerServiceInformation.ServerName,
                MessageBrokerServiceName = _messageBrokerServiceInformation.ServiceName,
                MessageBrokerInstanceName = _messageBrokerServiceInformation.InstanceName,
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
        }

        private Dto.SubscribeHandler FindSubscribeHandler(string name, string topic, bool localeFirst)
        {
            var subscribeHandler = SubscribeHandlers.Where(s => IsLocale(s.ServerName) == localeFirst && s.Name == name && s.Topic == topic && s.LastHandshakeDateTime != null).OrderByDescending(o => o.LastHandshakeDateTime).FirstOrDefault();

            return subscribeHandler ?? SubscribeHandlers.Where(s => IsLocale(s.ServerName) != localeFirst && s.Name == name && s.Topic == topic && s.LastHandshakeDateTime != null).OrderByDescending(o => o.LastHandshakeDateTime).FirstOrDefault();
        }

        private SubscribeHandlerState SendPublishMessage(Dto.SubscribeHandler subscribeHandler, PublishMessage message)
        {
            if (subscribeHandler.Queue == null)
                subscribeHandler.Queue = _queueFactory.CreateLocale(subscribeHandler.QueueName, true, subscribeHandler.Durable ? LocaleQueueMode.Durable : LocaleQueueMode.TemporarySlave, true);

            subscribeHandler.Queue.Send(message);

            return SubscribeHandlerState.SendToSubscriber;
        }

        private SubscribeHandlerState SendPublishSubscriberMessageToRemoteMessageBroker(string serverName, PublishSubscriberMessage message)
        {
            var queue = RemoteMessageBrokerQueue(serverName);

            if (queue != null)
            {
                queue.Send(message);

                return SubscribeHandlerState.SendToServer;
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
            var requestHandler = RequestHandlers.Where(r => r.Name == message.Name && IsLocale(r.ServerName) && r.LastHandshakeDateTime != null).OrderByDescending(r => r.LastHandshakeDateTime).FirstOrDefault();

            IQueue queue = null;

            if (requestHandler != null)
            {
                if (requestHandler.Queue == null)
                    requestHandler.Queue = _queueFactory.CreateLocale(requestHandler.QueueName, true, LocaleQueueMode.Durable, true);

                queue = requestHandler.Queue;
            }
            else
            {
                requestHandler = RequestHandlers.Where(r => r.Name == message.Name && r.ServerName != _messageBrokerServiceInformation.ServerName && r.LastHandshakeDateTime != null).OrderByDescending(r => r.LastHandshakeDateTime).FirstOrDefault();

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
                RequesterInstanceName = message.RequesterInstanceName,
                ResponderServerName = _messageBrokerServiceInformation.ServerName,
                ResponderServiceName = _messageBrokerServiceInformation.ServiceName,
                ResponderInstanceName = _messageBrokerServiceInformation.InstanceName,
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
            RemoveDeadHandlers();

            var messageBrokerHandshakeMessage = CreateMessageBrokerHandshakeMessage();

            var messageBrokerServices = GetMessageBrokerServices();

            foreach (var messageBrokerService in messageBrokerServices)
            {
                SendMessageBrokerHandshakeMessage(messageBrokerService, messageBrokerHandshakeMessage);
            }
        }

        private void RemoveDeadHandlers()
        {
            var time = DateTimeOffset.Now.AddMinutes(-10);

            lock (SubscribeHandlers)
            {
                foreach (var subscribeHandler in SubscribeHandlers.Where(e => e.Queue != null && e.LastHandshakeDateTime != null && e.LastHandshakeDateTime < time && !e.Durable))
                {
                    subscribeHandler.Queue.Dispose();
                }

                SubscribeHandlers.RemoveAll(e => e.LastHandshakeDateTime != null && e.LastHandshakeDateTime < time && !e.Durable);
            }

            lock (RequestHandlers)
            {
                foreach (var requestHandler in RequestHandlers.Where(e => e.Queue != null && e.LastHandshakeDateTime != null && e.LastHandshakeDateTime < time))
                {
                    requestHandler.Queue.Dispose();
                }

                RequestHandlers.RemoveAll(e => e.LastHandshakeDateTime != null && e.LastHandshakeDateTime < time);
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
            using (var queue = _queueFactory.CreateRemote(messageBrokerService.ServerName, messageBrokerService.RemoteQueueName, true, RemoteQueueMode.Durable, true))
            {
                queue.Send(messageBrokerHandshakeMessage);
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

        private void UpdateMessageBrokerServiceRepository(IRepositories repositories)
        {
            var messageBrokerServiceRepository = repositories.MessageBrokerServiceRepository();

            var messageBrokerService = messageBrokerServiceRepository.Get(_messageBrokerServiceInformation.ServerName, _messageBrokerServiceInformation.ServiceName, _messageBrokerServiceInformation.InstanceName);

            if (messageBrokerService == null)
            {
                messageBrokerService = new Entities.MessageBrokerService
                {
                    ServerName = _messageBrokerServiceInformation.ServerName,
                    ServiceName = _messageBrokerServiceInformation.ServiceName,
                    InstanceName = _messageBrokerServiceInformation.InstanceName,
                    LocaleQueueName = _messageBrokerServiceInformation.LocaleQueueName,
                    RemoteQueueName = _messageBrokerServiceInformation.RemoteQueueName,
                    LastStartDateTime = DateTimeOffset.Now
                };

                messageBrokerServiceRepository.Insert(messageBrokerService);
            }
            else
            {
                messageBrokerService.LocaleQueueName = _messageBrokerServiceInformation.LocaleQueueName;
                messageBrokerService.RemoteQueueName = _messageBrokerServiceInformation.RemoteQueueName;
                messageBrokerService.LastStartDateTime = DateTimeOffset.Now;

                messageBrokerServiceRepository.Update(messageBrokerService);
            }

            repositories.Save();
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

        private void UpdateMessageBrokerService(Entities.MessageBrokerService messageBrokerService)
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
                        LastHandshakeDateTime = handshakeDateTime,
                        Queue = queue
                    });
                }
                else
                {
                    messageBrokerService.Id = id ?? messageBrokerService.Id;
                    messageBrokerService.LastHandshakeDateTime = handshakeDateTime ?? messageBrokerService.LastHandshakeDateTime;
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
                        LastHandshakeDateTime = handshakeDateTime,
                        Queue = queue
                    });
                }
                else
                {
                    subscribeHandler.Topic = topic ?? subscribeHandler.Topic;
                    subscribeHandler.Name = name ?? subscribeHandler.Name;
                    subscribeHandler.LastHandshakeDateTime = handshakeDateTime ?? subscribeHandler.LastHandshakeDateTime;
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
                        LastHandshakeDateTime = handshakeDateTime,
                        Queue = queue
                    });
                }
                else
                {
                    requestHandler.Name = name ?? requestHandler.Name;
                    requestHandler.LastHandshakeDateTime = handshakeDateTime ?? requestHandler.LastHandshakeDateTime;
                    requestHandler.Queue = queue ?? requestHandler.Queue;
                }
            }
        }

        private void SendReply<T>(string replyQueueName, T message, bool transactional)
        {
            using (var replyQueue = _queueFactory.CreateLocale(replyQueueName, true, LocaleQueueMode.TemporarySlave, transactional))
            {
                replyQueue.Send(message);
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

                var messageIds = messageRepository.GetAll().Where(m => m.PublishDateTime < DateTimeOffset.Now.AddDays(-1)).Select(m => m.Id).Distinct().Except(messageStateRepository.GetAll().Where(s => s.State != "Completed").Select(s => s.MessageId).Distinct()).ToList();

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
                        InstanceName = message.InstanceName,
                        Name = message.Name,
                        Topic = message.Topic,
                        QueueName = message.QueueName,
                        LastRegisterDateTime = DateTimeOffset.Now
                    };

                    subscriberRepository.Insert(subscriber);
                }
                else
                {
                    subscriber.ServiceName = message.ServiceName;
                    subscriber.InstanceName = message.InstanceName;
                    subscriber.Name = message.Name;
                    subscriber.Topic = message.Topic;
                    subscriber.LastRegisterDateTime = DateTimeOffset.Now;

                    subscriberRepository.Update(subscriber);
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
            var messageBrokerService = MessageBrokerServices.Where(b => b.ServerName == serverName && b.LastHandshakeDateTime != null).OrderByDescending(m => m.LastHandshakeDateTime).FirstOrDefault();

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