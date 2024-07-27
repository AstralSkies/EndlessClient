using AutomaticTypeMapper;
using EOLib.Domain.Interact;
using EOLib.Domain.Interact.Guild;
using EOLib.Domain.Login;
using EOLib.Domain.Map;
using EOLib.Net.Handlers;
using Moffat.EndlessOnline.SDK.Protocol.Net;
using Moffat.EndlessOnline.SDK.Protocol.Net.Client;
using Moffat.EndlessOnline.SDK.Protocol.Net.Server;
using Optional;
using System.Collections.Generic;
using System.Diagnostics;
using EOLib.Domain.Notifiers;

namespace EOLib.PacketHandlers.Guild
{
    [AutoMappedType]

    public class GuildSellServerHandler : InGameOnlyPacketHandler<GuildSellServerPacket>
    {
        private readonly IGuildSessionRepository _guildSessionRepository;
 
        public override PacketFamily Family => PacketFamily.Guild;

        public override PacketAction Action => PacketAction.Sell;

        public GuildSellServerHandler(IPlayerInfoProvider playerInfoProvider,
                                 IGuildSessionRepository guildSessionRepository)
            : base(playerInfoProvider)
        {
            _guildSessionRepository = guildSessionRepository;
        }

        public override bool HandlePacket(GuildSellServerPacket packet)
        {
            _guildSessionRepository.GuildBankBalance = packet.GoldAmount;
            return true;
        }
    }
}