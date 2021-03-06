﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using FluentAssertions;
using Grumpy.Common;
using Grumpy.Common.Interfaces;
using Grumpy.Json;
using Grumpy.MessageQueue.Enum;
using Grumpy.MessageQueue.Interfaces;
using Grumpy.MessageQueue.Msmq.Exceptions;
using Grumpy.RipplesMQ.Core.Exceptions;
using Grumpy.RipplesMQ.Core.Infrastructure;
using Grumpy.RipplesMQ.Core.Interfaces;
using Grumpy.RipplesMQ.Core.Messages;
using Grumpy.RipplesMQ.Entity;
using Grumpy.RipplesMQ.Shared.Messages;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Grumpy.RipplesMQ.Core.UnitTests
{
    public class MessageBrokerTests
    {
        private readonly MessageBrokerConfig _messageBrokerConfig;
        private readonly IRepositoryContextFactory _repositoryContextFactory;
        private readonly IQueueHandlerFactory _queueHandlerFactory;
        private readonly IQueueFactory _queueFactory;
        private readonly IProcessInformation _processInformation;
        private readonly CancellationToken _cancellationToken;
        private readonly IRepositoryContext _repositoryContext;
        private readonly IMessageBrokerServiceRepository _messageBrokerServiceRepository;
        private readonly ISubscriberRepository _subscriberRepository;
        private readonly IMessageRepository _messageRepository;
        private readonly IMessageStateRepository _messageStateRepository;
        private readonly ILocaleQueue _messageBrokerQueue;

        public MessageBrokerTests()
        {
            _messageBrokerConfig = new MessageBrokerConfig
            {
                ServiceName = "UnitTest",
                RemoteQueueName = "MyRemoteQueueName"
            };

            _queueHandlerFactory = Substitute.For<IQueueHandlerFactory>();
            _processInformation = Substitute.For<IProcessInformation>();
            _processInformation.MachineName.Returns("MyTestServer");
            _cancellationToken = new CancellationToken();
            _messageBrokerServiceRepository = Substitute.For<IMessageBrokerServiceRepository>();
            _subscriberRepository = Substitute.For<ISubscriberRepository>();
            _messageRepository = Substitute.For<IMessageRepository>();
            _messageStateRepository = Substitute.For<IMessageStateRepository>();
            _repositoryContext = Substitute.For<IRepositoryContext>();
            _repositoryContext.MessageBrokerServiceRepository.Returns(_messageBrokerServiceRepository);
            _repositoryContext.SubscriberRepository.Returns(_subscriberRepository);
            _repositoryContext.MessageRepository.Returns(_messageRepository);
            _repositoryContext.MessageStateRepository.Returns(_messageStateRepository);
            _repositoryContextFactory = Substitute.For<IRepositoryContextFactory>();
            _repositoryContextFactory.Get().Returns(_repositoryContext);
            _queueFactory = Substitute.For<IQueueFactory>();
            _messageBrokerQueue = Substitute.For<ILocaleQueue>();
            _queueFactory.CreateLocale(Shared.Config.MessageBrokerConfig.LocaleQueueName, Arg.Any<bool>(), Arg.Any<LocaleQueueMode>(), Arg.Any<bool>(), Arg.Any<AccessMode>()).Returns(_messageBrokerQueue);
        }

        [Fact]
        public void CreateMessageBrokerShouldCreateObject()
        {
            using (var cut = CreateMessageBroker())
            {
                cut.Should().NotBeNull();
            }
        }

        [Fact]
        public void CreateMessageBrokerShouldSaveMessageBrokerService()
        {
            _messageBrokerServiceRepository.Get(Arg.Any<string>(), Arg.Any<string>()).Returns((MessageBrokerService)null);

            using (var cut = CreateMessageBroker())
            {
                cut.Start(_cancellationToken);
            }

            _messageBrokerServiceRepository.Received(1).Insert(Arg.Any<MessageBrokerService>());
            _repositoryContext.Received(1).Save();
        }

        [Fact]
        public void CreateMessageBrokerShouldUpdateMessageBrokerService()
        {
            _messageBrokerServiceRepository.Get(Arg.Any<string>(), Arg.Any<string>()).Returns(new MessageBrokerService());

            using (var cut = CreateMessageBroker())
            {
                cut.Start(_cancellationToken);
            }

            _messageBrokerServiceRepository.Received(0).Insert(Arg.Any<MessageBrokerService>());
        }

        [Fact]
        public void CreateMessageBrokerShouldLoadMessageBrokers()
        {
            _messageBrokerServiceRepository.GetAll().Returns(new List<MessageBrokerService>
            {
                new MessageBrokerService { ServerName = "MyTestServer", RemoteQueueName = "MyRemoteQueueName" },
                new MessageBrokerService { ServerName = "MyTestServer", RemoteQueueName = "AnotherQueueName" },
                new MessageBrokerService { ServerName = "OtherServer", RemoteQueueName = "MyRemoteQueueName" },
                new MessageBrokerService { ServerName = "OtherServer", RemoteQueueName = "AnotherQueueName" }
            }.AsQueryable());

            using (var cut = CreateMessageBroker())
            {
                cut.Start(_cancellationToken);
                cut.MessageBrokerServices.Count.Should().Be(4);
            }
        }

        [Fact]
        public void CreateMessageBrokerShouldLoadLocaleSubscribers()
        {
            _subscriberRepository.GetAll().Returns(new List<Subscriber>
            {
                new Subscriber { ServerName = "MyTestServer", QueueName = "MyQueueName" },
                new Subscriber { ServerName = "MyTestServer", QueueName = "OtherQueueName" },
                new Subscriber { ServerName = "OtherServer", QueueName = "MyQueueName" }
            }.AsQueryable());

            using (var cut = CreateMessageBroker())
            {
                cut.Start(_cancellationToken);
                Thread.Sleep(1000);
                cut.SubscribeHandlers.Count.Should().Be(2);
            }
        }

        [Fact]
        public void SendMessageBrokerHandshakesToAllOtherMessageBrokers()
        {
            _messageBrokerServiceRepository.GetAll().Returns(new List<MessageBrokerService>
            {
                new MessageBrokerService { ServerName = "MyTestServer", RemoteQueueName = "MyRemoteQueueName" },
                new MessageBrokerService { ServerName = "MyTestServer", RemoteQueueName = "AnotherQueueName" },
                new MessageBrokerService { ServerName = "OtherServer", RemoteQueueName = "MyRemoteQueueName" },
                new MessageBrokerService { ServerName = "OtherServer", RemoteQueueName = "AnotherQueueName" }
            }.AsQueryable());

            using (var cut = CreateMessageBroker())
            {
                cut.Start(_cancellationToken);
                Thread.Sleep(1000);
            }

            _queueFactory.Received(3).CreateRemote(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<RemoteQueueMode>(), Arg.Any<bool>(), Arg.Any<AccessMode>());
        }

        [Fact]
        public void SendingHandshakeShouldIncludeSubscriber()
        {
            var localeQueue = Substitute.For<ILocaleQueue>();
            _queueFactory.CreateLocale("AnotherQueueName", Arg.Any<bool>(), Arg.Any<LocaleQueueMode>(), Arg.Any<bool>(), Arg.Any<AccessMode>()).Returns(localeQueue);
            var remoteQueue = Substitute.For<IRemoteQueue>();
            _queueFactory.CreateRemote("MyTestServer", "AnotherQueueName", Arg.Any<bool>(), Arg.Any<RemoteQueueMode>(), Arg.Any<bool>(), Arg.Any<AccessMode>()).Returns(remoteQueue);

            _messageBrokerServiceRepository.GetAll().Returns(new List<MessageBrokerService>
            {
                new MessageBrokerService { ServerName = "MyTestServer", RemoteQueueName = "MyRemoteQueueName" },
                new MessageBrokerService { ServerName = "MyTestServer", RemoteQueueName = "AnotherQueueName" }
            }.AsQueryable());

            using (var cut = CreateMessageBroker())
            {
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { ServerName = "MyTestServer", QueueName = "MyQueueName", Topic = "MyTopic", HandshakeDateTime = DateTimeOffset.Now });
                cut.Start(_cancellationToken);
                Thread.Sleep(1000);
            }

            localeQueue.Send(Arg.Is<MessageBrokerHandshakeMessage>(m => m.ServerName == "MyServerName"));
            remoteQueue.Received(1).Send(Arg.Is<MessageBrokerHandshakeMessage>(m => m.LocaleSubscribeHandlers.Count(s => s.Topic == "MyTopic") == 1));
        }

        [Fact]
        public void CanCallErrorHandler()
        {
            using (var cut = CreateMessageBroker())
            {
                cut.ErrorHandler("Error", new Exception());
            }
        }

        [Fact]
        public void RemoveDeadSubscriberBeforeSendingHandshake()
        {
            var queue = Substitute.For<ILocaleQueue>();

            using (var cut = CreateMessageBroker())
            {
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { ServerName = "MyTestServer", QueueName = "MyQueueNameA", HandshakeDateTime = DateTimeOffset.Now.AddMinutes(-30), Queue = queue });
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { ServerName = "MyTestServer", QueueName = "MyQueueNameB", HandshakeDateTime = DateTimeOffset.Now });

                HandleMessage(cut, new SendMessageBrokerHandshakeMessage());

                cut.SubscribeHandlers.Count(s => s.QueueName == "MyQueueNameA").Should().Be(0);
                cut.SubscribeHandlers.Count(s => s.QueueName == "MyQueueNameB").Should().Be(1);
            }

            queue.Received(1).Dispose();
        }

        [Fact]
        public void RemoveDeadRequesterBeforeSendingHandshake()
        {
            var queue = Substitute.For<ILocaleQueue>();

            using (var cut = CreateMessageBroker())
            {
                cut.RequestHandlers.Add(new Dto.RequestHandler { ServerName = "MyTestServer", QueueName = "MyQueueNameA", HandshakeDateTime = DateTimeOffset.Now.AddMinutes(-30), Queue = queue });
                cut.RequestHandlers.Add(new Dto.RequestHandler { ServerName = "MyTestServer", QueueName = "MyQueueNameB", HandshakeDateTime = DateTimeOffset.Now });

                HandleMessage(cut, new SendMessageBrokerHandshakeMessage());

                cut.RequestHandlers.Count(s => s.QueueName == "MyQueueNameA").Should().Be(0);
                cut.RequestHandlers.Count(s => s.QueueName == "MyQueueNameB").Should().Be(1);
            }

            queue.Received(1).Dispose();
        }

        [Fact]
        public void MessageBrokerHandshakeWhenRemoteQueueNotExistsShouldDeleteMessageBrokerService()
        {
            _queueFactory.CreateRemote(Arg.Any<string>(), "NonExistingRemoteQueue", Arg.Any<bool>(), Arg.Any<RemoteQueueMode>(), Arg.Any<bool>(), Arg.Any<AccessMode>()).Returns(e => throw new QueueMissingException("NonExistingRemoteQueue"));

            using (var cut = CreateMessageBroker())
            {
                cut.MessageBrokerServices.Add(new Dto.MessageBrokerService { RemoteQueueName = "NonExistingRemoteQueue" });

                HandleMessage(cut, new SendMessageBrokerHandshakeMessage());

                cut.MessageBrokerServices.Single(m => m.RemoteQueueName == "NonExistingRemoteQueue").ErrorCount.Should().Be(1);
            }
        }

        [Fact]
        public void MessageBrokerHandshakeShouldUpdatePulse()
        {
            var messageBrokerService = new MessageBrokerService { ServerName = "MyTestServer", RemoteQueueName = "MyRemoteQueue" };
            _messageBrokerServiceRepository.Get(Arg.Any<string>(), Arg.Any<string>()).Returns(messageBrokerService);

            using (var cut = CreateMessageBroker())
            {
                cut.MessageBrokerServices.Add(new Dto.MessageBrokerService { RemoteQueueName = "MyRemoteQueue" });

                HandleMessage(cut, new SendMessageBrokerHandshakeMessage());

                messageBrokerService.PulseDateTime.Should().BeAfter(DateTimeOffset.Now.AddHours(-1));
            }

            _repositoryContext.Received(1).Save();
        }

        [Fact]
        public void MessageBrokerHandshakeShouldRemoveMessageService()
        {
            using (var cut = CreateMessageBroker())
            {
                cut.MessageBrokerServices.Add(new Dto.MessageBrokerService { RemoteQueueName = "MyRemoteQueueA", ErrorCount = 4 });
                cut.MessageBrokerServices.Add(new Dto.MessageBrokerService { RemoteQueueName = "MyRemoteQueueB", ErrorCount = 2 });

                HandleMessage(cut, new SendMessageBrokerHandshakeMessage());

                cut.MessageBrokerServices.Count.Should().Be(1);
            }
        }

        [Fact]
        public void ShouldNotCleanupAfterStartWhenMissingIdFromOtherServer()
        {
            _messageBrokerServiceRepository.GetAll().Returns(new List<MessageBrokerService>
            {
                new MessageBrokerService { ServerName = "MyTestServer", RemoteQueueName = "MyRemoteQueueName" },
                new MessageBrokerService { ServerName = "MyTestServer", RemoteQueueName = "AnotherQueueName" }
            }.AsQueryable());

            _messageRepository.GetAll().Returns(new List<Message>
            {
                new Message { Id = "MessageId1", PublishDateTime = DateTimeOffset.Now.AddDays(-10) }
            }.AsQueryable());

            using (var cut = CreateMessageBroker())
            {
                cut.Start(_cancellationToken);
            }

            _messageRepository.Received(0).Delete("MessageId1");
        }

        [Fact]
        public void ShouldCleanupAfterStartWhenOnlyOne()
        {
            _messageBrokerServiceRepository.GetAll().Returns(new List<MessageBrokerService>
            {
                new MessageBrokerService { ServerName = "MyTestServer", RemoteQueueName = "MyRemoteQueueName" }
            }.AsQueryable());

            _messageRepository.GetAll().Returns(new List<Message>
            {
                new Message { Id = "MessageId1", PublishDateTime = DateTimeOffset.Now.AddDays(-10) }
            }.AsQueryable());

            using (var cut = CreateMessageBroker())
            {
                cut.Start(_cancellationToken);
                Thread.Sleep(1000);
            }

            _messageRepository.Received(1).Delete("MessageId1");
        }

        [Fact]
        public void ShouldSendCleanupServiceMessage()
        {
            var remoteQueue = Substitute.For<IRemoteQueue>();

            _queueFactory.CreateRemote("MyTestServer", "MyRemoteQueueName", Arg.Any<bool>(), Arg.Any<RemoteQueueMode>(), Arg.Any<bool>(), Arg.Any<AccessMode>()).Returns(remoteQueue);

            using (var cut = CreateMessageBroker())
            {
                cut.MessageBrokerServices.Add(new Dto.MessageBrokerService { ServerName = "MyTestServer", RemoteQueueName = "MyRemoteQueueName", HandshakeDateTime = DateTimeOffset.Now });
                cut.Start(_cancellationToken);
                Thread.Sleep(1000);
            }

            remoteQueue.Received(1).Send(Arg.Any<CleanOldServicesMessage>());
        }

        [Fact]
        public void RemoveMessagesForDeadSubscribersShouldDeleteDeadMessageState()
        {
            _messageStateRepository.GetAll().Returns(new List<MessageState>
            {
                new MessageState { MessageId = "MessageId1", SubscriberName = "SubscriberA" },
                new MessageState { MessageId = "MessageId1", SubscriberName = "SubscriberB" },
                new MessageState { MessageId = "MessageId2", SubscriberName = "SubscriberA" },
                new MessageState { MessageId = "MessageId3", SubscriberName = "SubscriberC" }
            }.AsQueryable());

            _subscriberRepository.GetAll().Returns(new List<Subscriber>
            {
                new Subscriber { Name = "SubscriberB" },
                new Subscriber { Name = "SubscriberC" }
            }.AsQueryable());

            HandleMessage(new RepositoryCleanupMessage());

            _messageStateRepository.Received(1).DeleteBySubscriber("SubscriberA");
            _repositoryContext.Received(2).Save();
        }

        [Fact]
        public void RemoveMessagesForDeadSubscribersWhenNoDeadSubscribersFoundShouldNotSave()
        {
            _messageStateRepository.GetAll().Returns(new List<MessageState>
            {
                new MessageState { MessageId = "MessageId1", SubscriberName = "SubscriberA" },
                new MessageState { MessageId = "MessageId2", SubscriberName = "SubscriberB" }
            }.AsQueryable());

            _subscriberRepository.GetAll().Returns(new List<Subscriber>
            {
                new Subscriber { Name = "SubscriberA" },
                new Subscriber { Name = "SubscriberB" }
            }.AsQueryable());

            HandleMessage(new RepositoryCleanupMessage());

            _repositoryContext.Received(1).Save();
        }

        [Fact]
        public void RemoveMessagesWhenCompletedShouldDeleteMessage()
        {
            _messageRepository.GetAll().Returns(new List<Message>
            {
                new Message { Id = "MessageId1", PublishDateTime = DateTimeOffset.Now.AddDays(-10) },
                new Message { Id = "MessageId2", PublishDateTime = DateTimeOffset.Now.AddDays(-10) }
            }.AsQueryable());

            _messageStateRepository.GetAll().Returns(new List<MessageState>
            {
                new MessageState { MessageId = "MessageId1", SubscriberName = "SubscriberA", State = "Completed" },
                new MessageState { MessageId = "MessageId1", SubscriberName = "SubscriberB", State = "Completed" },
                new MessageState { MessageId = "MessageId2", SubscriberName = "SubscriberA", State = "Published" }
            }.AsQueryable());

            HandleMessage(new RepositoryCleanupMessage());

            _messageRepository.Received(1).Delete("MessageId1");
            _messageRepository.Received(1).Delete(Arg.Any<string>());
            _repositoryContext.Received(2).Save();
        }

        [Fact]
        public void RemoveMessagesShouldNotDeleteMessageWhenNotAllAreCompleted()
        {
            _messageRepository.GetAll().Returns(new List<Message>
            {
                new Message { Id = "MessageId1", PublishDateTime = DateTimeOffset.Now.AddDays(-10) }
            }.AsQueryable());

            _messageStateRepository.GetAll().Returns(new List<MessageState>
            {
                new MessageState { MessageId = "MessageId1", SubscriberName = "SubscriberA", State = "Completed" },
                new MessageState { MessageId = "MessageId1", SubscriberName = "SubscriberB", State = "Published" }
            }.AsQueryable());

            HandleMessage(new RepositoryCleanupMessage());

            _messageRepository.Received(0).Delete("MessageId1");
            _repositoryContext.Received(1).Save();
        }

        [Fact]
        public void RemoveMessagesShouldDeleteMessageWhenMessageStateNotFound()
        {
            _messageRepository.GetAll().Returns(new List<Message>
            {
                new Message { Id = "MessageId1", PublishDateTime = DateTimeOffset.Now.AddDays(-10) }
            }.AsQueryable());

            _messageStateRepository.GetAll().Returns(new List<MessageState>
            {
                new MessageState { MessageId = "MessageId2", SubscriberName = "SubscriberA", State = "Completed" }
            }.AsQueryable());

            HandleMessage(new RepositoryCleanupMessage());

            _messageRepository.Received(1).Delete("MessageId1");
            _repositoryContext.Received(3).Save();
        }

        [Fact]
        public void RemoveMessagesShouldSaveOncePer100MessageToDelete()
        {
            var messages = new List<Message>();

            for (var i = 1; i < 101; ++i)
                messages.Add(new Message { Id = UniqueKeyUtility.Generate(), PublishDateTime = DateTimeOffset.Now.AddDays(-10) });

            _messageRepository.GetAll().Returns(messages.AsQueryable());

            _messageStateRepository.GetAll().Returns(Enumerable.Empty<MessageState>().AsQueryable());

            HandleMessage(new RepositoryCleanupMessage());

            _repositoryContext.Received(2).Save();
        }

        [Fact]
        public void CleanOldServicesMessageShouldDeleteOldMessageBrokerService()
        {
            _messageBrokerServiceRepository.GetAll().Returns(new List<MessageBrokerService>
            {
                new MessageBrokerService { ServerName = "MyTestServer", ServiceName = "SomeService", RemoteQueueName = "OldRemoteQueueName", PulseDateTime = DateTimeOffset.Now.AddDays(-10) },
                new MessageBrokerService { ServerName = "MyTestServer", ServiceName = "RunningService", RemoteQueueName = "NewRemoteQueueName", PulseDateTime = DateTimeOffset.Now.AddDays(-2) }
            });

            HandleMessage(new CleanOldServicesMessage());

            _messageBrokerServiceRepository.Received(1).Delete("MyTestServer", "SomeService");
            _messageBrokerServiceRepository.Received(1).Delete(Arg.Any<string>(), Arg.Any<string>());
            _repositoryContext.Received(1).Save();
        }

        [Fact]
        public void CleanOldServicesMessageShouldDeleteRemoteQueue()
        {
            var queue = Substitute.For<ILocaleQueue>();
            _queueFactory.CreateLocale("OldRemoteQueueName", Arg.Any<bool>(), Arg.Any<LocaleQueueMode>(), Arg.Any<bool>(), Arg.Any<AccessMode>()).Returns(queue);

            _messageBrokerServiceRepository.GetAll().Returns(new List<MessageBrokerService>
            {
                new MessageBrokerService { ServerName = "MyTestServer", ServiceName = "SomeService", RemoteQueueName = "OldRemoteQueueName", PulseDateTime = DateTimeOffset.Now.AddDays(-10) },
                new MessageBrokerService { ServerName = "MyTestServer", ServiceName = "RunningService", RemoteQueueName = "NewRemoteQueueName", PulseDateTime = DateTimeOffset.Now.AddDays(-2) }
            });

            HandleMessage(new CleanOldServicesMessage());

            queue.Received(1).Delete();
        }

        [Fact]
        public void CleanOldServicesMessageShouldDeleteOldSubscriber()
        {
            _subscriberRepository.GetAll().Returns(new List<Subscriber>
            {
                new Subscriber { ServerName = "MyTestServer", QueueName = "OldQueueName", PulseDateTime = DateTimeOffset.Now.AddDays(-10) },
                new Subscriber { ServerName = "MyTestServer", QueueName = "NewQueueName", PulseDateTime = DateTimeOffset.Now.AddDays(-2) }
            });

            HandleMessage(new CleanOldServicesMessage());

            _subscriberRepository.Received(1).Delete("MyTestServer", "OldQueueName");
            _subscriberRepository.Received(1).Delete(Arg.Any<string>(), Arg.Any<string>());
            _repositoryContext.Received(1).Save();
        }

        [Fact]
        public void CleanOldServicesMessageShouldDeleteSubscriberQueue()
        {
            var queue = Substitute.For<ILocaleQueue>();
            _queueFactory.CreateLocale("OldQueueName", Arg.Any<bool>(), Arg.Any<LocaleQueueMode>(), Arg.Any<bool>(), Arg.Any<AccessMode>()).Returns(queue);

            _subscriberRepository.GetAll().Returns(new List<Subscriber>
            {
                new Subscriber { ServerName = "MyTestServer", QueueName = "OldQueueName", PulseDateTime = DateTimeOffset.Now.AddDays(-10) },
                new Subscriber { ServerName = "MyTestServer", QueueName = "NewQueueName", PulseDateTime = DateTimeOffset.Now.AddDays(-2) }
            });

            HandleMessage(new CleanOldServicesMessage());

            queue.Received(1).Delete();
        }

        [Fact]
        public void RemoveMessageStateForDeadMessagesShouldDeleteMessageState()
        {
            _messageRepository.GetAll().Returns(new List<Message>
            {
                new Message { Id = "MessageId2", PublishDateTime = DateTimeOffset.Now.AddDays(-10) }
            }.AsQueryable());

            _messageStateRepository.GetAll().Returns(new List<MessageState>
            {
                new MessageState { MessageId = "MessageId1", SubscriberName = "SubscriberA", State = "Completed" },
                new MessageState { MessageId = "MessageId1", SubscriberName = "SubscriberB", State = "Published" },
                new MessageState { MessageId = "MessageId2", SubscriberName = "SubscriberB", State = "Published" }
            }.AsQueryable());

            HandleMessage(new RepositoryCleanupMessage());

            _messageStateRepository.Received(1).DeleteByMessageId("MessageId1");
            _messageStateRepository.Received(0).DeleteByMessageId("MessageId2");
            _repositoryContext.Received(2).Save();
        }

        [Fact]
        public void HandlingMessageBusServiceHandshakeMessageShouldUpdateSubscribeHandlerHandshakeDateTime()
        {
            using (var cut = CreateMessageBroker())
            {
                HandleMessage(cut, new MessageBusServiceHandshakeMessage
                {
                    SubscribeHandlers = new List<SubscribeHandler>
                    {
                        new SubscribeHandler { Name = "MySubscriber", QueueName = "MySubscribeQueue", Topic = "MyTopic" }
                    }
                });

                cut.SubscribeHandlers.ElementAt(0).QueueName.Should().Be("MySubscribeQueue");
                cut.SubscribeHandlers.ElementAt(0).HandshakeDateTime.Should().NotBeNull();
            }
        }

        [Fact]
        public void HandlingMessageBusServiceHandshakeMessageShouldSendReply()
        {
            var replyQueue = GetLocaleQueue("MyReplyQueueName");

            HandleMessage(new MessageBusServiceHandshakeMessage { ReplyQueue = "MyReplyQueueName" });

            replyQueue.Received(1).Send(Arg.Any<MessageBusServiceHandshakeReplyMessage>());
        }

        [Fact]
        public void HandlingMessageBusServiceHandshakeMessageShouldAddSubscriber()
        {
            using (var cut = CreateMessageBroker())
            {
                HandleMessage(cut, new MessageBusServiceHandshakeMessage
                {
                    SubscribeHandlers = new List<SubscribeHandler>
                    {
                        new SubscribeHandler
                        {
                            QueueName = "MySubscribeQueue",
                            Topic = "MyTopic"
                        }
                    }
                });

                cut.SubscribeHandlers.Count.Should().Be(1);
            }
        }

        [Fact]
        public void HandlingMessageBusServiceHandshakeMessageShouldSaveServiceInRepository()
        {
            _subscriberRepository.Get(Arg.Any<string>(), Arg.Any<string>()).Returns((Subscriber)null);

            HandleMessage(new MessageBusServiceHandshakeMessage
            {
                SubscribeHandlers = new List<SubscribeHandler>
                {
                    new SubscribeHandler
                    {
                        QueueName = "MySubscribeQueue",
                        Topic = "MyTopic",
                        Durable = true
                    }
                }
            });

            _subscriberRepository.Received(1).Insert(Arg.Any<Subscriber>());
            _repositoryContext.Received(1).Save();
        }


        [Fact]
        public void HandlingMessageBusServiceHandshakeMessageOnExistingServiceShouldUpdateServiceInRepository()
        {
            _subscriberRepository.Get(Arg.Any<string>(), Arg.Any<string>()).Returns(new Subscriber());

            HandleMessage(new MessageBusServiceHandshakeMessage
            {
                SubscribeHandlers = new List<SubscribeHandler>
                {
                    new SubscribeHandler
                    {
                        QueueName = "MySubscribeQueue",
                        Topic = "MyTopic",
                        Durable = true
                    }
                }
            });

            _subscriberRepository.Received(0).Insert(Arg.Any<Subscriber>());
            _repositoryContext.Received(1).Save();
        }

        [Fact]
        public void HandlingMessageBusServiceHandshakeMessageNonDurableShouldNotSaveSubscriberInRepository()
        {
            _subscriberRepository.Get(Arg.Any<string>(), Arg.Any<string>()).Returns(new Subscriber());

            HandleMessage(new MessageBusServiceHandshakeMessage
            {
                SubscribeHandlers = new List<SubscribeHandler>
                {
                    new SubscribeHandler
                    {
                        QueueName = "MySubscribeQueue",
                        Topic = "MyTopic",
                        Durable = false
                    }
                }
            });

            _subscriberRepository.Received(0).Insert(Arg.Any<Subscriber>());
            _repositoryContext.Received(0).Save();
        }

        [Fact]
        public void HandlingMessageBusServiceHandshakeMessageShouldGetRequesterQueue()
        {
            using (var cut = CreateMessageBroker())
            {
                HandleMessage(cut, new MessageBusServiceHandshakeMessage
                {
                    RequestHandlers = new List<RequestHandler>
                    {
                        new RequestHandler
                        {
                            QueueName = "MyRequesterQueue",
                            Name = "MyRequest"
                        }
                    }
                });

                cut.RequestHandlers.Count.Should().Be(1);
            }
        }

        [Fact]
        public void HandlingMessageBusServiceHandshakeMessageShouldUpdateRequestHandlerHandshakeDateTime()
        {
            using (var cut = CreateMessageBroker())
            {
                HandleMessage(cut, new MessageBusServiceHandshakeMessage
                {
                    RequestHandlers = new List<RequestHandler> {
                    new RequestHandler { Name = "MySubscriber", QueueName = "MyRequestQueue" }
                }
                });

                cut.RequestHandlers.ElementAt(0).QueueName.Should().Be("MyRequestQueue");
                cut.RequestHandlers.ElementAt(0).HandshakeDateTime.Should().NotBeNull();
            }
        }

        [Fact]
        public void HandlingMessageBusServiceHandshakeMessageShouldUpdateSubscriberPulseTimestamp()
        {
            var subscriber = new Subscriber { ServerName = "MyTestServer", QueueName = "MyQueueName" };
            _subscriberRepository.Get(Arg.Any<string>(), Arg.Any<string>()).Returns(subscriber);

            using (var cut = CreateMessageBroker())
            {
                HandleMessage(cut, new MessageBusServiceHandshakeMessage
                {
                    SubscribeHandlers = new List<SubscribeHandler>
                    {
                        new SubscribeHandler { QueueName = "MySubscribeQueue", Durable = true }
                    }
                });
            }

            subscriber.PulseDateTime.Should().BeAfter(DateTimeOffset.Now.AddHours(-1));
            _repositoryContext.Received(1).Save();
        }

        [Fact]
        public void HandlingMessageBusServiceHandshakeMessageWithChangesInSubscriberShouldSendMessageBrokerHandshake()
        {
            var remoteMessageBrokerQueue = GetRemoteQueue("AnotherServer", "RemoteMessageBrokerQueue");

            using (var cut = CreateMessageBroker())
            {
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { ServerName = "MyTestServer", QueueName = "MyQueueName", Durable = true });
                cut.MessageBrokerServices.Add(new Dto.MessageBrokerService { ServerName = "AnotherServer", RemoteQueueName = "RemoteMessageBrokerQueue", HandshakeDateTime = DateTimeOffset.Now });

                HandleMessage(cut, new MessageBusServiceHandshakeMessage
                {
                    ServerName = "MyTestServer",
                    SubscribeHandlers = new List<SubscribeHandler>
                    {
                        new SubscribeHandler { QueueName = "MyQueueName", Durable = false }
                    }
                });
            }

            remoteMessageBrokerQueue.Received(1).Send(Arg.Any<MessageBrokerHandshakeMessage>());
        }

        [Fact]
        public void HandlingMessageBusServiceHandshakeMessageWithoutChangesInSubscriberShouldNotSendMessageBrokerHandshake()
        {
            var remoteMessageBrokerQueue = GetRemoteQueue("AnotherServer", "RemoteMessageBrokerQueue");

            using (var cut = CreateMessageBroker())
            {
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { ServerName = "MyTestServer", QueueName = "MyQueueName", Durable = false });
                cut.MessageBrokerServices.Add(new Dto.MessageBrokerService { ServerName = "AnotherServer", RemoteQueueName = "RemoteMessageBrokerQueue", HandshakeDateTime = DateTimeOffset.Now });

                HandleMessage(cut, new MessageBusServiceHandshakeMessage
                {
                    ServerName = "MyTestServer",
                    SubscribeHandlers = new List<SubscribeHandler>
                    {
                        new SubscribeHandler { QueueName = "MyQueueName", Durable = false }
                    }
                });
            }

            remoteMessageBrokerQueue.Received(0).Send(Arg.Any<MessageBrokerHandshakeMessage>());
        }

        [Fact]
        public void HandlingMessageBusServiceHandshakeMessageWithChangesInRequesterShouldSendMessageBrokerHandshake()
        {
            var remoteMessageBrokerQueue = GetRemoteQueue("AnotherServer", "RemoteMessageBrokerQueue");

            using (var cut = CreateMessageBroker())
            {
                cut.RequestHandlers.Add(new Dto.RequestHandler { ServerName = "MyTestServer", QueueName = "MyQueueName", Name = "MyRequestHandler" });
                cut.MessageBrokerServices.Add(new Dto.MessageBrokerService { ServerName = "AnotherServer", RemoteQueueName = "RemoteMessageBrokerQueue", HandshakeDateTime = DateTimeOffset.Now });

                HandleMessage(cut, new MessageBusServiceHandshakeMessage
                {
                    ServerName = "MyTestServer",
                    RequestHandlers = new List<RequestHandler>
                    {
                        new RequestHandler { QueueName = "MyQueueName", Name = "MyOtherRequestHandler" }
                    }
                });
            }

            remoteMessageBrokerQueue.Received(1).Send(Arg.Any<MessageBrokerHandshakeMessage>());
        }

        [Fact]
        public void HandlingMessageBusServiceHandshakeMessageWithoutChangesInRequesterShouldNotSendMessageBrokerHandshake()
        {
            var remoteMessageBrokerQueue = GetRemoteQueue("AnotherServer", "RemoteMessageBrokerQueue");

            using (var cut = CreateMessageBroker())
            {
                cut.RequestHandlers.Add(new Dto.RequestHandler { ServerName = "MyTestServer", QueueName = "MyQueueName", Name = "MyRequestHandler" });
                cut.MessageBrokerServices.Add(new Dto.MessageBrokerService { ServerName = "AnotherServer", RemoteQueueName = "RemoteMessageBrokerQueue", HandshakeDateTime = DateTimeOffset.Now });

                HandleMessage(cut, new MessageBusServiceHandshakeMessage
                {
                    ServerName = "MyTestServer",
                    RequestHandlers = new List<RequestHandler>
                    {
                        new RequestHandler { QueueName = "MyQueueName", Name = "MyRequestHandler" }
                    }
                });
            }

            remoteMessageBrokerQueue.Received(0).Send(Arg.Any<MessageBrokerHandshakeMessage>());
        }

        [Fact]
        public void HandlingMessageBusServiceHandshakeMessageRequesterHandlerMissingShouldRemoveFromState()
        {
            using (var cut = CreateMessageBroker())
            {
                cut.RequestHandlers.Add(new Dto.RequestHandler { ServerName = "MyTestServer", QueueName = "MyQueueName", Name = "MyRequestHandler" });

                HandleMessage(cut, new MessageBusServiceHandshakeMessage
                {
                    ServerName = "MyTestServer",
                    RequestHandlers = new List<RequestHandler>
                    {
                        new RequestHandler { QueueName = "MyQueueName2", Name = "MyRequestHandler" }
                    }
                });

                cut.RequestHandlers.Count.Should().Be(1);
                cut.RequestHandlers.Count(e => e.QueueName == "MyQueueName2").Should().Be(1);
            }
        }

        [Fact]
        public void HandlingMessageBusServiceHandshakeSubscriberMessageHandlerMissingShouldRemoveFromState()
        {
            using (var cut = CreateMessageBroker())
            {
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { ServerName = "MyTestServer", QueueName = "MyQueueName", Name = "MyRequestHandler" });

                HandleMessage(cut, new MessageBusServiceHandshakeMessage
                {
                    ServerName = "MyTestServer",
                    SubscribeHandlers = new List<SubscribeHandler>
                    {
                        new SubscribeHandler { QueueName = "MyQueueName2", Name = "MyRequestHandler" }
                    }
                });

                cut.SubscribeHandlers.Count.Should().Be(1);
                cut.SubscribeHandlers.Count(e => e.QueueName == "MyQueueName2").Should().Be(1);
            }
        }

        [Fact]
        public void HandlingMessageBusServiceHandshakeSubscriberMessageHandlerMissingDurableShouldNotRemoveFromState()
        {
            using (var cut = CreateMessageBroker())
            {
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { ServerName = "MyTestServer", QueueName = "MyQueueName1", Name = "MyRequestHandler", Durable = true, HandshakeDateTime = DateTimeOffset.Now });

                HandleMessage(cut, new MessageBusServiceHandshakeMessage
                {
                    ServerName = "MyTestServer",
                    SubscribeHandlers = new List<SubscribeHandler>
                    {
                        new SubscribeHandler { QueueName = "MyQueueName2", Name = "MyRequestHandler" }
                    }
                });

                cut.SubscribeHandlers.Count.Should().Be(2);
                cut.SubscribeHandlers.Count(e => e.QueueName == "MyQueueName1" && e.HandshakeDateTime == null).Should().Be(1);
            }
        }

        [Fact]
        public void HandlingPublishPersistentMessageShouldSaveMessageToRepository()
        {
            HandleMessage(CreatePublishMessage("MyReplyQueue", "MyTopic", "Message"));

            _messageRepository.Received(1).Insert(Arg.Any<Message>());
            _repositoryContext.Received(1).Save();
        }

        private static PublishMessage CreatePublishMessage<T>(string replyQueue, string topic, T message, bool persistent = true)
        {
            return new PublishMessage
            {
                ReplyQueue = replyQueue,
                Topic = topic,
                Persistent = persistent,
                MessageBody = message.SerializeToJson(),
                MessageType = message.GetType().FullName
            };
        }

        [Fact]
        public void HandlingPublishNonPersistentMessageShouldNotSaveMessageToRepository()
        {
            HandleMessage(CreatePublishMessage("MyReplyQueue", "MyTopic", "Message", false));

            _messageRepository.Received(0).Insert(Arg.Any<Message>());
            _repositoryContext.Received(0).Save();
        }

        [Fact]
        public void HandlingPublishPersistentMessageShouldSaveSubscriberListToRepository()
        {
            using (var cut = CreateMessageBroker())
            {
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { Topic = "MyTopic", Name = "SubscriberA", HandshakeDateTime = DateTimeOffset.Now, MessageType = typeof(string).FullName });
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { Topic = "MyTopic", Name = "SubscriberA", HandshakeDateTime = DateTimeOffset.Now, MessageType = typeof(string).FullName });
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { Topic = "TheirTopic", Name = "SubscriberB", HandshakeDateTime = DateTimeOffset.Now, MessageType = typeof(string).FullName });
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { Topic = "MyTopic", Name = "SubscriberC", HandshakeDateTime = DateTimeOffset.Now, MessageType = typeof(string).FullName });

                HandleMessage(cut, CreatePublishMessage("MyReplyQueue", "MyTopic", "Message"));
            }

            _messageStateRepository.Received(2).Insert(Arg.Is<MessageState>(m => m.SubscriberName == "SubscriberA"));
            _messageStateRepository.Received(2).Insert(Arg.Is<MessageState>(m => m.SubscriberName == "SubscriberC"));
            _messageStateRepository.Received(4).Insert(Arg.Any<MessageState>());
            _repositoryContext.Received(2).Save();
        }

        [Fact]
        public void HandlingPublishMessageWithoutReplyQueueShouldNotSendReply()
        {
            var replyQueue = GetLocaleQueue("MyReplyQueue");

            HandleMessage(new PublishMessage { ReplyQueue = null, Topic = "MyTopic" });

            replyQueue.Received(0).Send(Arg.Any<PublishReplyMessage>());
        }

        [Fact]
        public void HandlingPublishMessageWithReplyQueueShouldSendReply()
        {
            var replyQueue = GetLocaleQueue("MyReplyQueue");

            HandleMessage(new PublishMessage { ReplyQueue = "MyReplyQueue", Topic = "MyTopic" });

            replyQueue.Received(1).Send(Arg.Any<PublishReplyMessage>());
        }

        [Fact]
        public void HandlingPersistentPublishMessageShouldSaveSendTimeInRepository()
        {
            using (var cut = CreateMessageBroker())
            {
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { Topic = "MyTopic", Name = "Subscriber", ServerName = "MyTestServer", HandshakeDateTime = DateTimeOffset.Now, MessageType = typeof(string).FullName });

                HandleMessage(cut, CreatePublishMessage(null, "MyTopic", "Message"));
            }

            _messageStateRepository.Received(1).Insert(Arg.Is<MessageState>(m => m.State == "Distributed"));
            _repositoryContext.Received(2).Save(); // One for saving the message and one for saving the send timestamp
        }

        [Fact]
        public void HandlingPublishMessageWithTwoSubscribersShouldSendToBothSubscriberQueues()
        {
            using (var cut = CreateMessageBroker())
            {
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { Topic = "MyTopic", Name = "MySubscriber1" });
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { Topic = "MyTopic", Name = "MySubscriber2" });

                HandleMessage(cut, new PublishMessage { Topic = "MyTopic" });
            }

            _messageBrokerQueue.Received(1).Send(Arg.Is<PublishSubscriberMessage>(e => e.SubscriberName == "MySubscriber1"));
            _messageBrokerQueue.Received(1).Send(Arg.Is<PublishSubscriberMessage>(e => e.SubscriberName == "MySubscriber2"));
        }

        [Fact]
        public void HandlingPublishMessageWithTwoOfSameSubscriberShouldSendToOneSubscriberQueues()
        {
            using (var cut = CreateMessageBroker())
            {
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { Topic = "MyTopic", Name = "MySubscriber" });
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { Topic = "MyTopic", Name = "MySubscriber" });

                HandleMessage(cut, new PublishMessage { Topic = "MyTopic" });
            }

            _messageBrokerQueue.Received(1).Send(Arg.Any<PublishSubscriberMessage>());
        }

        [Fact]
        public void HandlingPublishSubscriberMessageLocaleSubscriberExistsShouldSendToSubscribeQueue()
        {
            var subscriberQueue = GetLocaleQueue("SubscriberQueueName");

            using (var cut = CreateMessageBroker())
            {
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { Topic = "MyTopic", ServerName = "MyTestServer", Name = "MySubscriber", HandshakeDateTime = DateTimeOffset.Now, QueueName = "SubscriberQueueName" });

                HandleMessage(cut, new PublishSubscriberMessage { SubscriberName = "MySubscriber", Message = new PublishMessage { Topic = "MyTopic" } });
            }

            subscriberQueue.Received(1).Send(Arg.Any<PublishMessage>());
        }

        [Fact]
        public void HandlingPublishMessageStateNotConfirmedShouldUpdateError()
        {
            _messageStateRepository.Get(Arg.Any<string>(), Arg.Any<string>()).Returns(new MessageState());
            var subscriberQueue = GetLocaleQueue("SubscriberQueueName");

            using (var cut = CreateMessageBroker())
            {
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { Topic = "MyTopic", Name = "MySubscriber", ServerName = "MyTestServer", HandshakeDateTime = null, QueueName = "SubscriberQueueName" });

                HandleMessage(cut, new PublishSubscriberMessage { SubscriberName = "MySubscriber", Message = new PublishMessage { Topic = "MyTopic", Persistent = true } });
            }

            subscriberQueue.Received(0).Send(Arg.Any<PublishMessage>());
            _messageStateRepository.Received(1).Insert(Arg.Is<MessageState>(m => m.State == "Error"));
        }

        [Fact]
        public void HandlingPublishMessageToRemoteHandlerShouldSendToRemoteMessageBroker()
        {
            using (var cut = CreateMessageBroker())
            {
                cut.MessageBrokerServices.Add(new Dto.MessageBrokerService { ServerName = "AnotherServer", RemoteQueueName = "RemoteMessageBrokerQueue", HandshakeDateTime = DateTimeOffset.Now });
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { Topic = "MyTopic", Name = "MySubscriber", ServerName = "AnotherServer", HandshakeDateTime = DateTimeOffset.Now, QueueName = "AnotherSubscribeQueue" });

                HandleMessage(cut, new PublishMessage { Topic = "MyTopic" });
            }

            _messageBrokerQueue.Received(1).Send(Arg.Any<PublishSubscriberMessage>());
        }

        [Fact]
        public void HandlingPublishSubscriberMessageToRemoteHandlerShouldSendToRemoteMessageBroker()
        {
            var remoteMessageBrokerQueue = GetRemoteQueue("AnotherServer", "RemoteMessageBrokerQueue");

            using (var cut = CreateMessageBroker())
            {
                cut.MessageBrokerServices.Add(new Dto.MessageBrokerService { ServerName = "AnotherServer", RemoteQueueName = "RemoteMessageBrokerQueue", HandshakeDateTime = DateTimeOffset.Now });
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { Topic = "MyTopic", Name = "MySubscriber", ServerName = "AnotherServer", HandshakeDateTime = DateTimeOffset.Now, QueueName = "AnotherSubscribeQueue" });

                HandleMessage(cut, new PublishSubscriberMessage { SubscriberName = "MySubscriber", Message = new PublishMessage { Topic = "MyTopic" } });
            }

            remoteMessageBrokerQueue.Received(1).Send(Arg.Any<PublishSubscriberMessage>());
        }

        [Fact]
        public void HandlingSubscribeHandlerCompleteMessageOnPersistentMessageShouldUpdateCompletedTimeInRepository()
        {
            HandleMessage(new SubscribeHandlerCompleteMessage { Name = "SubscriberA", Persistent = true, MessageId = "MessageId1" });

            _messageStateRepository.Received(1).Insert(Arg.Is<MessageState>(m => m.State == "Completed" && m.SubscriberName == "SubscriberA"));
            _repositoryContext.Received(1).Save();
        }

        [Fact]
        public void HandlingSubscribeHandlerCompleteMessageOnNonePersistentMessageShouldUpdateCompletedTimeInRepository()
        {
            HandleMessage(new SubscribeHandlerCompleteMessage { Name = "SubscriberA", Persistent = false, MessageId = "MessageId1" });

            _messageStateRepository.Received(0).Get(Arg.Any<string>(), Arg.Any<string>());
            _repositoryContext.Received(0).Save();
        }

        [Fact]
        public void HandlingSubscribeHandlerErrorMessageOnPersistentMessageShouldUpdateErrorCountInRepository()
        {
            using (var cut = CreateMessageBroker())
            {
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { Topic = "MyTopic", Name = "SubscriberA", ServerName = "MyTestServer", HandshakeDateTime = DateTimeOffset.Now, QueueName = "MySubscribeQueue" });

                HandleMessage(cut, new SubscribeHandlerErrorMessage { Name = "SubscriberA", Message = new PublishMessage { Persistent = true, MessageId = "MessageId1", Topic = "MyTopic" } });
            }

            _messageStateRepository.Received(2).Insert(Arg.Is<MessageState>(m => m.ErrorCount == 1 && m.SubscriberName == "SubscriberA"));
            _repositoryContext.Received(2).Save();
        }

        [Fact]
        public void HandlingSubscribeHandlerErrorMessageOnPersistentMessageSecondTimeShouldUpdateErrorCountInRepository()
        {
            using (var cut = CreateMessageBroker())
            {
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { Topic = "MyTopic", Name = "SubscriberA", ServerName = "MyTestServer", HandshakeDateTime = DateTimeOffset.Now, QueueName = "MySubscribeQueue" });

                Assert.Throws<PublishMessageException>(() => HandleMessage(cut, new SubscribeHandlerErrorMessage { Name = "SubscriberA", Message = new PublishMessage { Persistent = true, MessageId = "MessageId1", ErrorCount = 2, Topic = "MyTopic" } }));
            }

            _messageStateRepository.Received(1).Insert(Arg.Is<MessageState>(m => m.ErrorCount == 3 && m.SubscriberName == "SubscriberA"));
            _repositoryContext.Received(1).Save();
        }

        [Fact]
        public void HandlingSubscribeHandlerErrorMessageOnNonePersistentMessageShouldUpdateCompletedTimeInRepository()
        {
            using (var cut = CreateMessageBroker())
            {
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { Topic = "MyTopic", Name = "SubscriberA", ServerName = "MyTestServer", HandshakeDateTime = DateTimeOffset.Now, QueueName = "MySubscribeQueue" });

                HandleMessage(cut, new SubscribeHandlerErrorMessage { Name = "SubscriberA", Message = new PublishMessage { Persistent = false, MessageId = "MessageId1", Topic = "MyTopic" } });
            }

            _messageStateRepository.Received(0).Get(Arg.Any<string>(), Arg.Any<string>());
            _repositoryContext.Received(0).Save();
        }

        [Fact]
        public void HandlingSubscribeHandlerErrorMessageFirstTimeShouldTryResendToOtherMessageBroker()
        {
            var remoteMessageBrokerQueue = GetRemoteQueue("AnotherServer", "RemoteMessageBrokerQueue");

            using (var cut = CreateMessageBroker())
            {
                cut.MessageBrokerServices.Add(new Dto.MessageBrokerService { ServerName = "MyTestServer", RemoteQueueName = "LocaleMessageBrokerQueue", HandshakeDateTime = DateTimeOffset.Now });
                cut.MessageBrokerServices.Add(new Dto.MessageBrokerService { ServerName = "AnotherServer", RemoteQueueName = "RemoteMessageBrokerQueue", HandshakeDateTime = DateTimeOffset.Now });
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { Topic = "MyTopic", Name = "MySubscriber", ServerName = "MyTestServer", HandshakeDateTime = DateTimeOffset.Now, QueueName = "MySubscribeQueue" });
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { Topic = "MyTopic", Name = "MySubscriber", ServerName = "AnotherServer", HandshakeDateTime = DateTimeOffset.Now, QueueName = "AnotherSubscribeQueue" });

                HandleMessage(cut, new SubscribeHandlerErrorMessage { Name = "MySubscriber", Message = new PublishMessage { Topic = "MyTopic", Persistent = false, MessageId = "MessageId1", ErrorCount = 0 } });
            }

            remoteMessageBrokerQueue.Received(1).Send(Arg.Is<PublishSubscriberMessage>(m => m.Message.ErrorCount == 1));
        }

        [Fact]
        public void HandlingSubscribeHandlerErrorMessageFirstTimeShouldTryResendToLocaleSubscriberIfRemoteIsNotFound()
        {
            var localeQueue = GetLocaleQueue("MySubscribeQueue");

            using (var cut = CreateMessageBroker())
            {
                cut.MessageBrokerServices.Add(new Dto.MessageBrokerService { ServerName = "MyTestServer", RemoteQueueName = "LocaleMessageBrokerQueue", HandshakeDateTime = DateTimeOffset.Now });
                cut.SubscribeHandlers.Add(new Dto.SubscribeHandler { Topic = "MyTopic", Name = "MySubscriber", ServerName = "MyTestServer", HandshakeDateTime = DateTimeOffset.Now, QueueName = "MySubscribeQueue" });

                HandleMessage(cut, new SubscribeHandlerErrorMessage { Name = "MySubscriber", Message = new PublishMessage { Topic = "MyTopic", Persistent = false, MessageId = "MessageId1", ErrorCount = 0 } });
            }

            localeQueue.Received(1).Send(Arg.Is<PublishMessage>(m => m.ErrorCount == 1));
        }

        [Fact]
        public void HandlingRequestMessageUnableToFindRequesterShouldSendResponseErrorWithException()
        {
            var replyQueue = GetLocaleQueue("MyReplyQueue");

            HandleMessage(new RequestMessage { Name = "Request", ReplyQueue = "MyReplyQueue", RequesterServerName = "MyTestServer" });

            replyQueue.Received(1).Send(Arg.Any<ResponseErrorMessage>());
        }

        [Fact]
        public void HandlingRequestMessageLocaleRequestHandlerFoundShouldSendRequestToRequestHandlerQueue()
        {
            var requestQueue = GetLocaleQueue("RequestQueue");

            using (var cut = CreateMessageBroker())
            {
                cut.RequestHandlers.Add(new Dto.RequestHandler { Name = "Request", QueueName = "RequestQueue", ServerName = "MyTestServer", HandshakeDateTime = DateTimeOffset.Now });

                HandleMessage(cut, new RequestMessage { Name = "Request", RequesterServerName = "MyTestServer" });
            }

            requestQueue.Received(1).Send(Arg.Any<RequestMessage>());
        }

        [Fact]
        public void HandlingRequestMessageRemoteRequestHandlerFoundShouldSendRequestToMessageBrokerOnServerName()
        {
            var messageBrokerQueue = GetRemoteQueue("AnotherServer", "RemoteMessageBrokerQueue");

            using (var cut = CreateMessageBroker())
            {
                cut.MessageBrokerServices.Add(new Dto.MessageBrokerService { ServerName = "AnotherServer", RemoteQueueName = "RemoteMessageBrokerQueue", HandshakeDateTime = DateTimeOffset.Now });
                cut.RequestHandlers.Add(new Dto.RequestHandler { Name = "Request", QueueName = "RequestQueue", ServerName = "AnotherServer", HandshakeDateTime = DateTimeOffset.Now });

                HandleMessage(cut, new RequestMessage { Name = "Request", RequesterServerName = "MyTestServer" });
            }

            messageBrokerQueue.Received(1).Send(Arg.Any<RequestMessage>());
        }

        [Fact]
        public void HandlingRequestMessageMultipleRequestHandlerShouldUseNewest()
        {
            var requestQueue = GetLocaleQueue("RequestQueueB");

            using (var cut = CreateMessageBroker())
            {
                cut.RequestHandlers.Add(new Dto.RequestHandler { Name = "Request", QueueName = "RequestQueueA", ServerName = "MyTestServer", HandshakeDateTime = DateTimeOffset.Now.AddMinutes(-5) });
                cut.RequestHandlers.Add(new Dto.RequestHandler { Name = "Request", QueueName = "RequestQueueB", ServerName = "MyTestServer", HandshakeDateTime = DateTimeOffset.Now });

                HandleMessage(cut, new RequestMessage { Name = "Request", RequesterServerName = "MyTestServer" });
            }

            requestQueue.Received(1).Send(Arg.Any<RequestMessage>());
        }

        [Fact]
        public void HandlingLocaleResponseMessageShouldSendToRequestReplyQueue()
        {
            var replyQueue = GetLocaleQueue("MyReplyQueue");

            HandleMessage(CreateResponseMessage("MyTestServer", "MyResponse"));

            replyQueue.Received(1).Send(Arg.Any<ResponseMessage>());
        }

        private static ResponseMessage CreateResponseMessage<T>(string requesterServerName, T response)
        {
            return new ResponseMessage
            {
                RequesterServerName = requesterServerName,
                ReplyQueue = "MyReplyQueue",
                MessageBody = response.SerializeToJson(),
                MessageType = response.GetType().FullName
            };
        }

        [Fact]
        public void HandlingRemoteResponseMessageShouldSendToRemoteMessageBrokerQueue()
        {
            var replyQueue = GetRemoteQueue("AnotherServer", "AnotherBrokerQueueName");

            using (var cut = CreateMessageBroker())
            {
                cut.MessageBrokerServices.Add(new Dto.MessageBrokerService { ServerName = "AnotherServer", RemoteQueueName = "AnotherBrokerQueueName", Queue = null, HandshakeDateTime = DateTimeOffset.Now });
                HandleMessage(cut, CreateResponseMessage("AnotherServer", "MyResponse"));
            }

            replyQueue.Received(1).Send(Arg.Any<ResponseMessage>());
        }

        [Fact]
        public void HandlingLocaleResponseErrorMessageShouldSendToRequestReplyQueue()
        {
            var replyQueue = GetLocaleQueue("MyReplyQueue");

            HandleMessage(new ResponseErrorMessage { RequesterServerName = "MyTestServer", ReplyQueue = "MyReplyQueue", Exception = new Exception("MyException") });

            replyQueue.Received(1).Send(Arg.Any<ResponseErrorMessage>());
        }

        [Fact]
        public void CanReceiveMessageBrokerHandshake()
        {
            var messageBrokerHandshakeMessage = new MessageBrokerHandshakeMessage
            {
                MessageBrokerId = "1",
                ServerName = "AnotherServer",
                QueueName = "AnotherRemoteQueueName",
                LocaleSubscribeHandlers = new List<LocaleSubscribeHandler>
                {
                    new LocaleSubscribeHandler { QueueName = "AnotherSubscriberQueue" }
                },
                LocaleRequestHandlers = new List<LocaleRequestHandler>
                {
                    new LocaleRequestHandler { QueueName = "AnotherSubscriberQueue" }
                }
            };

            using (var cut = CreateMessageBroker())
            {
                HandleMessage(cut, messageBrokerHandshakeMessage);

                cut.MessageBrokerServices.Count.Should().Be(1);
                cut.SubscribeHandlers.Count.Should().Be(1);
                cut.RequestHandlers.Count.Should().Be(1);
            }
        }

        [Fact]
        public void ReceiveNewMessageBrokerHandshakeShouldUpdateSubscriber()
        {
            var messageBrokerHandshakeMessage = new MessageBrokerHandshakeMessage
            {
                MessageBrokerId = "1",
                ServerName = "AnotherServer",
                QueueName = "AnotherRemoteQueueName",
                LocaleSubscribeHandlers = new List<LocaleSubscribeHandler>
                {
                    new LocaleSubscribeHandler { QueueName = "AnotherSubscriberQueue" }
                }
            };

            using (var cut = CreateMessageBroker())
            {
                HandleMessage(cut, messageBrokerHandshakeMessage);

                cut.SubscribeHandlers.ElementAt(0).Name.Should().BeNull();

                messageBrokerHandshakeMessage.LocaleSubscribeHandlers.ElementAt(0).Name = "MySubscribeName";

                HandleMessage(cut, messageBrokerHandshakeMessage);

                cut.SubscribeHandlers.ElementAt(0).Name.Should().Be("MySubscribeName");
            }
        }

        [Fact]
        public void ReceiveNewMessageBrokerHandshakeShouldUpdateRequester()
        {
            var messageBrokerHandshakeMessage = new MessageBrokerHandshakeMessage
            {
                MessageBrokerId = "1",
                ServerName = "AnotherServer",
                QueueName = "AnotherRemoteQueueName",
                LocaleRequestHandlers = new List<LocaleRequestHandler>
                {
                    new LocaleRequestHandler { QueueName = "AnotherSubscriberQueue" }
                }
            };

            using (var cut = CreateMessageBroker())
            {
                HandleMessage(cut, messageBrokerHandshakeMessage);

                cut.RequestHandlers.ElementAt(0).Name.Should().BeNull();

                messageBrokerHandshakeMessage.LocaleRequestHandlers.ElementAt(0).Name = "MyRequestName";

                HandleMessage(cut, messageBrokerHandshakeMessage);

                cut.RequestHandlers.ElementAt(0).Name.Should().Be("MyRequestName");
            }
        }

        private IQueue GetLocaleQueue(string queueName = null)
        {
            var queue = Substitute.For<ILocaleQueue>();
            queue.Name.Returns(queueName ?? "Unknown");

            _queueFactory.CreateLocale(queueName == null ? Arg.Any<string>() : Arg.Is<string>(n => n.Contains(queueName)), Arg.Any<bool>(), Arg.Any<LocaleQueueMode>(), Arg.Any<bool>(), Arg.Any<AccessMode>()).Returns(queue);

            return queue;
        }

        private IQueue GetRemoteQueue(string serverName, string queueName = null)
        {
            var queue = Substitute.For<IRemoteQueue>();
            queue.Name.Returns(queueName ?? "Unknown");

            _queueFactory.CreateRemote(serverName, queueName == null ? Arg.Any<string>() : Arg.Is<string>(n => n.Contains(queueName)), Arg.Any<bool>(), Arg.Any<RemoteQueueMode>(), Arg.Any<bool>(), Arg.Any<AccessMode>()).Returns(queue);

            return queue;
        }

        private void HandleMessage(object message)
        {
            using (var cut = CreateMessageBroker())
            {
                HandleMessage(cut, message);
            }
        }

        private static void HandleMessage(IMessageBroker cut, object message)
        {
            cut.Handler(message);
        }

        private IMessageBroker CreateMessageBroker()
        {
            return new MessageBroker(NullLogger.Instance, _messageBrokerConfig, _repositoryContextFactory, _queueHandlerFactory, _queueFactory, _processInformation);
        }
    }
}