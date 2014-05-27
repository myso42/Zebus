﻿using System;
using System.Linq;
using Abc.Zebus.Core;
using Abc.Zebus.Dispatch;
using Abc.Zebus.Lotus;
using Abc.Zebus.Serialization;
using Abc.Zebus.Testing;
using Abc.Zebus.Testing.Extensions;
using Abc.Zebus.Testing.Transport;
using Abc.Zebus.Testing.UnitTesting;
using Abc.Zebus.Tests.Messages;
using Abc.Zebus.Transport;
using Abc.Zebus.Util;
using Moq;
using NUnit.Framework;

namespace Abc.Zebus.Tests.Core
{
    public partial class BusTests
    {
        [Test]
        public void should_dispatch_received_message()
        {
            var command = new FakeCommand(123);
            var invokerCalled = false;
            SetupDispatch(command, _ => invokerCalled = true);

            var transportMessageReceived = command.ToTransportMessage(_peerUp);
            _transport.RaiseMessageReceived(transportMessageReceived);

            invokerCalled.ShouldBeTrue();
        }

        [Test]
        public void should_handle_command_locally()
        {
            var command = new FakeCommand(1);
            var handled = false;
            SetupDispatch(command, _ => handled = true);
            SetupPeersHandlingMessage<FakeCommand>(_self);

            var completed = _bus.Send(command).Wait(500);

            handled.ShouldBeTrue();
            completed.ShouldBeTrue();
            _transport.Messages.ShouldBeEmpty();
        }

        [Test]
        public void should_not_handle_command_locally_when_local_dispatch_is_disabled()
        {
            var command = new FakeCommand(1);
            var handled = false;
            SetupDispatch(command, _ => handled = true);
            SetupPeersHandlingMessage<FakeCommand>(_self);

            using (LocalDispatch.Disable())
            using (MessageId.PauseIdGeneration())
            {
                var completed = _bus.Send(command).Wait(5);

                handled.ShouldBeFalse();
                completed.ShouldBeFalse();
                _transport.ExpectExactly(new TransportMessageSent(command.ToTransportMessage(_self), _self));
            }
        }

        [Test]
        public void should_handle_event_locally()
        {
            var message = new FakeEvent(1);
            var handled = false;
            SetupDispatch(message, x => handled = true);
            SetupPeersHandlingMessage<FakeEvent>(_self, _peerUp);

            _bus.Publish(message);

            handled.ShouldBeTrue();

            var sentMessage = _transport.Messages.Single();
            sentMessage.Targets.Single().ShouldEqual(_peerUp);
        }

        [Test]
        public void should_not_handle_event_locally_when_local_dispatch_is_disabled()
        {
            var message = new FakeEvent(1);
            var handled = false;
            SetupDispatch(message, x => handled = true);
            SetupPeersHandlingMessage<FakeEvent>(_self);

            using (LocalDispatch.Disable())
            using (MessageId.PauseIdGeneration())
            {
                _bus.Publish(message);

                handled.ShouldBeFalse();

                _transport.ExpectExactly(new TransportMessageSent(message.ToTransportMessage(_self), _self));
            }
        }

        [Test]
        public void should_ack_transport_message_when_dispatch_done()
        {
            var command = new FakeCommand(123);
            SetupDispatch(command);
            SetupPeersHandlingMessage<FakeCommand>(_peerUp);

            var task = _bus.Send(command);
            var transportMessage = command.ToTransportMessage();
            _transport.RaiseMessageReceived(transportMessage);

            task.Wait(10);

            _transport.AckedMessages.Count.ShouldEqual(1);
            _transport.AckedMessages[0].Id.ShouldEqual(transportMessage.Id);
        }

        [Test]
        public void should_create_message_dispatch()
        {
            var command = new FakeCommand(123);
            var dispatch = _bus.CreateMessageDispatch(command.ToTransportMessage());

            dispatch.Message.ShouldEqualDeeply(command);
        }

        [Test]
        public void should_not_send_acknowledgement_when_message_handled()
        {
            var command = new FakeCommand(123);
            var dispatch = _bus.CreateMessageDispatch(command.ToTransportMessage());

            dispatch.SetHandlerCount(1);
            dispatch.SetHandled(null, null);

            _transport.ExpectNothing();
        }

        [Test]
        public void should_send_CustomMessageProcessingFailed_if_unable_to_deserialize_message()
        {
            using (MessageId.PauseIdGeneration())
            {
                SetupPeersHandlingMessage<CustomProcessingFailed>(_peerUp);

                var serializerMock = new Mock<IMessageSerializer>();
                serializerMock.Setup(serializer => serializer.Deserialize(It.IsAny<MessageTypeId>(), It.IsAny<byte[]>()))
                              .Throws(new Exception("message"));

                var bus = new Bus(_transport, _directoryMock.Object, serializerMock.Object, _messageDispatcherMock.Object, new DefaultStoppingStrategy());
                bus.Configure(_self.Id, "test");

                var command = new FakeCommand(123);
                var transportMessage = command.ToTransportMessage();
                _transport.RaiseMessageReceived(transportMessage);

                var processingFailed = new CustomProcessingFailed(typeof(Bus).FullName, "message", SystemDateTime.UtcNow);
                var processingFailedTransportMessage = new TransportMessage(processingFailed.TypeId(), new byte[0], _self);
                _transport.ExpectExactly(new TransportMessageSent(processingFailedTransportMessage, _peerUp));
            }
        }

        [Test]
        public void should_stop_dispatcher_before_transport()
        {
            var transportMock = new Mock<ITransport>();
            var bus = new Bus(transportMock.Object, _directoryMock.Object, _messageSerializer, _messageDispatcherMock.Object, new DefaultStoppingStrategy());
            var sequence = new SetupSequence();
            _messageDispatcherMock.Setup(dispatch => dispatch.Stop()).InSequence(sequence);
            transportMock.Setup(transport => transport.Stop()).InSequence(sequence);
            bus.Configure(_self.Id, "test");

            bus.Start();
            bus.Stop();
            sequence.Verify();
        }
    }
}