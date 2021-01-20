﻿using System;
using System.Collections.Generic;
using EOLib.Net.Handlers;

namespace EOLib.Net.API
{
    public delegate void PlayerTakeSpikeDamageEvent(short damage, short hp, short maxhp);
    public delegate void OtherPlayerTakeSpikeDamageEvent(short playerID, byte playerPercentHealth, bool isPlayerDead, int damageAmount);
    public delegate void TimedMapDrainHPEvent(short damage, short hp, short maxhp, List<TimedMapHPDrainData> otherCharacterData);
    public delegate void TimedMapDrainTPEvent(short amount, short tp, short maxtp);
    public delegate void EffectPotionUseEvent(short playerID, int effectID);

    public struct TimedMapHPDrainData
    {
        private readonly short _playerID;
        private readonly byte _playerPercentHealth;
        private readonly short _damageDealt;

        public short PlayerID => _playerID;
        public byte PlayerPercentHealth => _playerPercentHealth;
        public short DamageDealt => _damageDealt;

        internal TimedMapHPDrainData(short playerID, byte percentHealth, short damageDealt)
        {
            _playerID = playerID;
            _playerPercentHealth = percentHealth;
            _damageDealt = damageDealt;
        }
    }

    partial class PacketAPI
    {
        public event Action OnTimedSpike;
        public event OtherPlayerTakeSpikeDamageEvent OnOtherPlayerTakeSpikeDamage;
        public event TimedMapDrainHPEvent OnTimedMapDrainHP;
        public event EffectPotionUseEvent OnEffectPotion;

        private void _createEffectMembers()
        {
            m_client.AddPacketHandler(new FamilyActionPair(PacketFamily.Effect, PacketAction.Admin), _handleEffectAdmin, true);
            m_client.AddPacketHandler(new FamilyActionPair(PacketFamily.Effect, PacketAction.Report), _handleEffectReport, true);
            m_client.AddPacketHandler(new FamilyActionPair(PacketFamily.Effect, PacketAction.TargetOther), _handleEffectTargetOther, true);
            m_client.AddPacketHandler(new FamilyActionPair(PacketFamily.Effect, PacketAction.Player), _handleEffectPlayer, true);
        }

        //sent to players around a player taking spike damage
        private void _handleEffectAdmin(OldPacket pkt)
        {
            if (OnOtherPlayerTakeSpikeDamage == null) return;

            short playerID = pkt.GetShort();
            byte playerPercentHealth = pkt.GetChar();
            bool playerIsDead = pkt.GetChar() != 0;
            int damageAmount = pkt.GetThree();

            OnOtherPlayerTakeSpikeDamage(playerID, playerPercentHealth, playerIsDead, damageAmount);
        }

        //timed spikes
        private void _handleEffectReport(OldPacket pkt)
        {
            pkt.GetChar(); //always 83 - sent from eoserv when Map::TimedSpikes is called
            //as of rev 487 this is not sent anywhere else. May need to update event handler if this changes.
            if (OnTimedSpike != null)
                OnTimedSpike();
        }

        //map hp drain
        private void _handleEffectTargetOther(OldPacket pkt)
        {
            if (OnTimedMapDrainHP == null)
                return;

            short damage = pkt.GetShort();
            short hp = pkt.GetShort();
            short maxhp = pkt.GetShort();

            var otherCharacters = new List<TimedMapHPDrainData>((pkt.Length - pkt.ReadPos) / 5);
            while (pkt.ReadPos != pkt.Length)
            {
                otherCharacters.Add(new TimedMapHPDrainData(
                    playerID: pkt.GetShort(),
                    percentHealth: pkt.GetChar(),
                    damageDealt: pkt.GetShort())
                );
            }

            OnTimedMapDrainHP(damage, hp, maxhp, otherCharacters);
        }

        //potion effect (only known use based on eoserv code)
        private void _handleEffectPlayer(OldPacket pkt)
        {
            if (OnEffectPotion != null)
                OnEffectPotion(playerID: pkt.GetShort(),
                               effectID: pkt.GetThree());
        }
    }
}
