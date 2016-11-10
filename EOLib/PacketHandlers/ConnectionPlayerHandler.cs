﻿// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

using EOLib.Net;
using EOLib.Net.Communication;
using EOLib.Net.Handlers;
using EOLib.Net.PacketProcessing;

namespace EOLib.PacketHandlers
{
    /// <summary>
    /// Handles incoming CONNECTION_PLAYER packets which are used for updating sequence numbers in the EO protocol
    /// </summary>
    public class ConnectionPlayerHandler : DefaultAsyncPacketHandler
    {
        private readonly IPacketProcessorActions _packetProcessorActions;
        private readonly IPacketSendService _packetSendService;

        public override PacketFamily Family { get { return PacketFamily.Connection; } }

        public override PacketAction Action { get { return PacketAction.Player; } }

        public override bool CanHandle { get { return true; } }

        public ConnectionPlayerHandler(IPacketProcessorActions packetProcessorActions,
                                       IPacketSendService packetSendService)
        {
            _packetProcessorActions = packetProcessorActions;
            _packetSendService = packetSendService;
        }

        public override bool HandlePacket(IPacket packet)
        {
            var seq1 = packet.ReadShort();
            var seq2 = packet.ReadChar();

            _packetProcessorActions.SetUpdatedBaseSequenceNumber(seq1, seq2);

            var response = new PacketBuilder(PacketFamily.Connection, PacketAction.Ping).Build();
            try
            {
                _packetSendService.SendPacketAsync(response)
                                  .Wait();
            }
            catch (NoDataSentException)
            {
                return false;
            }

            return true;
        }
    }
}
