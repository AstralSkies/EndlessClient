﻿// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

using System.Linq;
using EndlessClient.Dialogs;
using EOLib;
using EOLib.Domain.Character;
using EOLib.IO.Map;
using EOLib.Localization;
using XNAControls;

namespace EndlessClient.Input
{
    public class OldArrowKeyListener : OldInputKeyListenerBase
    {
        private void _checkSpecAndWalkIfValid(byte destX, byte destY, EODirection direction)
        {
            //switch (info.ReturnType)
            //{
            //    case TileInfoReturnType.IsOtherPlayer:
            //        if (Renderer.NoWall) goto case TileInfoReturnType.IsTileSpec;

            //        EOGame.Instance.Hud.SetStatusLabel(EOResourceID.STATUS_LABEL_TYPE_ACTION,
            //            EOResourceID.STATUS_LABEL_KEEP_MOVING_THROUGH_PLAYER);
            //        if (_startWalkingThroughPlayerTime == null)
            //            _startWalkingThroughPlayerTime = DateTime.Now;
            //        else if ((DateTime.Now - _startWalkingThroughPlayerTime.Value).TotalSeconds > 5)
            //        {
            //            _startWalkingThroughPlayerTime = null;
            //            goto case TileInfoReturnType.IsTileSpec;
            //        }
            //        break;
            //    case TileInfoReturnType.IsOtherNPC:
            //        if (Renderer.NoWall) goto case TileInfoReturnType.IsTileSpec;
            //        break;
            //    case TileInfoReturnType.IsWarpSpec:
            //        if (Renderer.NoWall) goto case TileInfoReturnType.IsTileSpec;

                    //var warpInfo = (Warp) info.MapElement;
                    //if (warpInfo.DoorType != DoorSpec.NoDoor)
                    //{
                    //    DoorSpec doorOpened;
                    //    if (!warpInfo.IsDoorOpened && !warpInfo.DoorPacketSent)
                    //    {
                    //        if ((doorOpened = Character.CanOpenDoor(warpInfo.DoorType)) == DoorSpec.Door)
                    //            mapRend.StartOpenDoor(warpInfo, destX, destY);
                    //    }
                    //    else
                    //    {
                    //        //normal walking
                    //        if ((doorOpened = Character.CanOpenDoor(warpInfo.DoorType)) == DoorSpec.Door)
                    //            _walkIfValid(TileSpec.None, direction, destX, destY);
                    //    }

                    //    if (doorOpened != DoorSpec.Door)
                    //    {
                    //        string strWhichKey = "[error key?]";
                    //        switch (doorOpened)
                    //        {
                    //            case DoorSpec.LockedCrystal:
                    //                strWhichKey = "Crystal Key";
                    //                break;
                    //            case DoorSpec.LockedSilver:
                    //                strWhichKey = "Silver Key";
                    //                break;
                    //            case DoorSpec.LockedWraith:
                    //                strWhichKey = "Wraith Key";
                    //                break;
                    //        }

                    //        EOMessageBox.Show(DialogResourceID.DOOR_LOCKED, XNADialogButtons.Ok, EOMessageBoxStyle.SmallDialogSmallHeader);
                    //        ((EOGame)Game).Hud.SetStatusLabel(EOResourceID.STATUS_LABEL_TYPE_WARNING,
                    //            EOResourceID.STATUS_LABEL_THE_DOOR_IS_LOCKED_EXCLAMATION,
                    //            " - " + strWhichKey);
                    //    }
                    //}
                    //else if (warpInfo.LevelRequirement != 0 && Character.Stats.Level < warpInfo.LevelRequirement)
                    //{
                    //    EOGame.Instance.Hud.SetStatusLabel(EOResourceID.STATUS_LABEL_TYPE_WARNING,
                    //        EOResourceID.STATUS_LABEL_NOT_READY_TO_USE_ENTRANCE,
                    //        " - LVL " + warpInfo.LevelRequirement);
                    //}
                    //else
                    //{
                    //    //normal walking
                    //    _walkIfValid(TileSpec.None, direction, destX, destY);
                    //}
            //        break;
            //    case TileInfoReturnType.IsTileSpec:
            //        _walkIfValid(specAtDest, direction, destX, destY);
            //        break;
            //}
        }

        private void _walkIfValid(TileSpec spec, EODirection dir, byte destX, byte destY)
        {
            bool walkValid = true;
            switch (spec)
            {
                case TileSpec.ChairDown: //todo: make character sit in chairs
                case TileSpec.ChairLeft:
                case TileSpec.ChairRight:
                case TileSpec.ChairUp:
                case TileSpec.ChairDownRight:
                case TileSpec.ChairUpLeft:
                case TileSpec.ChairAll:
                    walkValid = Renderer.NoWall;
                    break;
                case TileSpec.Chest:
                    walkValid = Renderer.NoWall;
                    if (!walkValid)
                    {
                        var chest = OldWorld.Instance.ActiveMapRenderer.MapRef.Chests.Single(_c => _c.X == destX && _c.Y == destY);
                        if (chest != null)
                        {
                            string requiredKey = null;
                            switch (Character.CanOpenChest(chest))
                            {
                                case ChestKey.Normal: requiredKey = "Normal Key"; break;
                                case ChestKey.Silver: requiredKey = "Silver Key"; break;
                                case ChestKey.Crystal: requiredKey = "Crystal Key"; break;
                                case ChestKey.Wraith: requiredKey = "Wraith Key"; break;
                                default:
                                    ChestDialog.Show(((EOGame)Game).API, (byte)chest.X, (byte)chest.Y);
                                    break;
                            }

                            if (requiredKey != null)
                            {
                                EOMessageBox.Show(DialogResourceID.CHEST_LOCKED, XNADialogButtons.Ok, EOMessageBoxStyle.SmallDialogSmallHeader);
                                ((EOGame)Game).Hud.SetStatusLabel(EOResourceID.STATUS_LABEL_TYPE_WARNING, EOResourceID.STATUS_LABEL_THE_CHEST_IS_LOCKED_EXCLAMATION,
                                    " - " + requiredKey);
                            }
                        }
                        else
                        {
                            ChestDialog.Show(((EOGame)Game).API, destX, destY);
                        }
                    }
                    break;
                case TileSpec.BankVault:
                    walkValid = Renderer.NoWall;
                    if (!walkValid)
                    {
                        LockerDialog.Show(((EOGame)Game).API, destX, destY);
                    }
                    break;
                case TileSpec.SpikesTrap:
                    OldWorld.Instance.ActiveMapRenderer.AddVisibleSpikeTrap(destX, destY);
                    break;
                case TileSpec.Board1: //todo: boards?
                case TileSpec.Board2:
                case TileSpec.Board3:
                case TileSpec.Board4:
                case TileSpec.Board5:
                case TileSpec.Board6:
                case TileSpec.Board7:
                case TileSpec.Board8:
                    walkValid = Renderer.NoWall;
                    break;
                case TileSpec.Jukebox: //todo: jukebox?
                    walkValid = Renderer.NoWall;
                    break;
                case TileSpec.MapEdge:
                case TileSpec.Wall:
                    walkValid = Renderer.NoWall;
                    break;
            }

            if (Character.State != CharacterActionState.Walking && walkValid)
            {
                //Character.Walk(dir, destX, destY, Renderer.NoWall);
                Renderer.PlayerWalk(spec == TileSpec.Water, spec == TileSpec.SpikesTrap);
            }
        }
    }
}
