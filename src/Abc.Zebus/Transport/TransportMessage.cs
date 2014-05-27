﻿using Abc.Zebus.Util.Annotations;
using ProtoBuf;

namespace Abc.Zebus.Transport
{
    [ProtoContract]
    public class TransportMessage
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly MessageId Id;

        [ProtoMember(2, IsRequired = true)]
        public readonly MessageTypeId MessageTypeId;

        [ProtoMember(3, IsRequired = true)]
        public readonly byte[] MessageBytes;

        [ProtoMember(4, IsRequired = true)]
        public readonly OriginatorInfo Originator;

        [ProtoMember(5, IsRequired = false)]
        public string Environment { get; set; }

        public TransportMessage(MessageTypeId messageTypeId, byte[] messageBytes, Peer self)
            : this(messageTypeId, messageBytes, self.Id, self.EndPoint, MessageId.NextId())
        {
        }

        public TransportMessage(MessageTypeId messageTypeId, byte[] messageBytes, PeerId selfPeerId, string selfPeerEndPoint, MessageId messageId)
            : this (messageTypeId, messageBytes, CreateOriginator(selfPeerId, selfPeerEndPoint), messageId)
        {
        }

        public TransportMessage(MessageTypeId messageTypeId, byte[] messageBytes, OriginatorInfo originator, MessageId messageId)
        {
            Id = messageId;
            MessageTypeId = messageTypeId;
            MessageBytes = messageBytes;
            Originator = originator;
        }

        [UsedImplicitly]
        private TransportMessage()
        {
        }

        public bool ForcePersistenceAck { get; set; }

        private static OriginatorInfo CreateOriginator(PeerId peerId, string peerEndPoint)
        {
            return new OriginatorInfo(peerId, peerEndPoint, MessageContext.CurrentMachineName, MessageContext.GetInitiatorUserName());
        }
    }
}