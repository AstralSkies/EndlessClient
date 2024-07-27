using System;
using System.Diagnostics;
using AutomaticTypeMapper;
using EOLib.Net.Communication;
using Moffat.EndlessOnline.SDK.Data;
using Moffat.EndlessOnline.SDK.Protocol.Net;
using Moffat.EndlessOnline.SDK.Protocol.Net.Client;
using Moffat.EndlessOnline.SDK.Protocol.Net.Server;

namespace EOLib.Domain.Interact.Guild
{
    [AutoMappedType]
    public class GuildActions : IGuildActions
    {
        private readonly IGuildSessionProvider _guildSessionProvider;
        private readonly IPacketSendService _packetSendService;

        public GuildActions(IGuildSessionProvider guildSessionProvider,
                            IPacketSendService packetSendService)
        {
            _guildSessionProvider = guildSessionProvider;
            _packetSendService = packetSendService;
        }

        public void Lookup(string identity)
        {
            _packetSendService.SendPacket(new GuildReportClientPacket
            {
                SessionId = _guildSessionProvider.SessionID,
                GuildIdentity = identity
            });
        }

        public void ViewMembers(string response)
        {
            _packetSendService.SendPacket(new GuildTellClientPacket
            {
                SessionId = _guildSessionProvider.SessionID,
                GuildIdentity = response
            });
        }

        // Guild Bank Info. Limit this to a 3
        public void BankInfo(string response)
        { 
            _packetSendService.SendPacket(new GuildTakeClientPacket
            {
                SessionId = _guildSessionProvider.SessionID,
                InfoType = GuildInfoType.Bank,
                GuildTag = response
            });
        }

        public void DepositAmount(int amount)
        {
            _packetSendService.SendPacket(new GuildBuyClientPacket
            {
                SessionId = _guildSessionProvider.SessionID,
                GoldAmount = amount,
            });
        }

        public void LeaveGuild()
        {
            _packetSendService.SendPacket(new GuildRemoveClientPacket
            {
                SessionId = _guildSessionProvider.SessionID
            });
        }
    }

    public interface IGuildActions
    {
        void Lookup(string identity);
        void ViewMembers(string response); 
        void LeaveGuild();
        void BankInfo(string response);

        void DepositAmount(int amount);
    }
}
