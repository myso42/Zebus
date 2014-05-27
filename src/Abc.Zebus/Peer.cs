﻿using System;
using Abc.Zebus.Util.Annotations;
using ProtoBuf;

namespace Abc.Zebus
{
    [ProtoContract]
    public class Peer
    {
        [ProtoMember(1, IsRequired = true)]
        public readonly PeerId Id;

        [ProtoMember(2, IsRequired = true)]
        public string EndPoint;

        [ProtoMember(3, IsRequired = true)]
        public bool IsUp;

        [ProtoMember(4, IsRequired = false)]
        public bool IsResponding;

        public Peer(PeerId id, string endPoint, bool isUp = true)
        {
            Id = id;
            EndPoint = endPoint;
            IsUp = isUp;
            IsResponding = isUp;
        }

        [UsedImplicitly]
        private Peer()
        {
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}", Id, EndPoint);
        }

        public string GetMachineNameFromEndPoint()
        {
            var uri = new Uri(EndPoint);
            return uri.Host;
        }
    }
}