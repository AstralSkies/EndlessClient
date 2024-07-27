using AutomaticTypeMapper;
using EOLib.Domain.Interact;
using EOLib.Domain.Interact.Guild;
using EOLib.Domain.Login;
using EOLib.Domain.Map;
using EOLib.Net.Handlers;
using Moffat.EndlessOnline.SDK.Protocol.Net;
using Moffat.EndlessOnline.SDK.Protocol.Net.Client;
using Moffat.EndlessOnline.SDK.Protocol.Net.Server;
using EOLib.Domain.Notifiers;
using Optional;
using System.Collections.Generic;
using System.Diagnostics;

namespace EOLib.PacketHandlers.Guild
{
    [AutoMappedType]
    public class GuildDepositBankHandler : InGameOnlyPacketHandler<GuildBuyServerPacket>
    {
        private readonly IGuildSessionRepository _guildSessionRepository;

       
        public override PacketFamily Family => PacketFamily.Guild;

        public override PacketAction Action => PacketAction.Buy;

        public GuildDepositBankHandler(IPlayerInfoProvider playerInfoProvider,
                                 IGuildSessionRepository guildSessionRepository)
            : base(playerInfoProvider)
        {
            _guildSessionRepository = guildSessionRepository;
        }

        public override bool HandlePacket(GuildBuyServerPacket packet)
        {
            // Updates the user's gold.
            return true;
        }
    }
}
