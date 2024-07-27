using System.Diagnostics;
using EndlessClient.Content;
using EndlessClient.Dialogs.Factories;
using EndlessClient.Dialogs.Services;
using EOLib;
using EOLib.Domain.Character;
using EOLib.Domain.Interact.Guild;
using EOLib.Domain.Map;
using EOLib.Graphics;
using EOLib.IO.Repositories;
using EOLib.Localization;
using System;
using System.Collections.Generic;
using System.Linq;
using XNAControls;
using EndlessClient.Audio;
using Microsoft.Xna.Framework;
using Optional.Collections;
using System.Windows.Documents;

namespace EndlessClient.Dialogs
{
    public class GuildDialog : ScrollingListDialog
    {
        private const int AdjustedDrawAreaOffset = 10;
        private const int MaxGuildTag = 3;
        private bool _balanceUpdateNeeded = false;
        private enum GuildDialogState
        {
            Initial,
            InformationMenu,
            Registration,
            Administration,
            GuildBank,
        }

        private enum DialogAction
        {
            None,
            GuildLookUp,
            MemberList,
            LeaveGuild,
            KickPlayer,
            DepositFunds,
        }

        private readonly INativeGraphicsManager _nativeGraphicsManager;
        private readonly IEODialogButtonService _dialogButtonService;
        private readonly IEODialogIconService _dialogIconService;
        private readonly ILocalizedStringFinder _localizedStringFinder;
        private readonly ITextInputDialogFactory _textInputDialogFactory;
        private readonly IGuildActions _guildActions;
        private readonly IContentProvider _contentProvider;
        private readonly ICurrentMapStateProvider _currentMapStateProvider;
        private readonly IENFFileProvider _enfFileProvider;
        private readonly IEOMessageBoxFactory _messageBoxFactory;
        private readonly ICharacterRepository _characterRepository;
        private readonly IEOMessageBoxFactory _eoMessageBoxFactory;
        private readonly ISfxPlayer _sfxPlayer;
        private readonly Stack<GuildDialogState> _stateStack = new Stack<GuildDialogState>();
        private readonly IGuildSessionProvider _guildSessionProvider;
        private readonly ICharacterInventoryProvider _characterInventoryProvider;
        private readonly IItemTransferDialogFactory _itemTransferDialogFactory;
        private readonly IEIFFileProvider _eifFileProvider;

        private GuildDialogState _state;
        private DialogAction _currentDialogAction;

        private HashSet<string> _previousMemberHashes;
        private int _guildBankBalance;
        private bool _bankInfoRequested = false;

        public GuildDialog(
            INativeGraphicsManager nativeGraphicsManager,
            IEODialogButtonService dialogButtonService,
            IEODialogIconService dialogIconService,
            ILocalizedStringFinder localizedStringFinder,
            ITextInputDialogFactory textInputDialogFactory,
            IGuildActions guildActions,
            IContentProvider contentProvider,
            ICurrentMapStateProvider currentMapStateProvider,
            IENFFileProvider enfFileProvider,
            IEOMessageBoxFactory messageBoxFactory,
            ICharacterRepository characterRepository,
            IEOMessageBoxFactory eoMessageBoxFactory,
            ISfxPlayer sfxPlayer,
            IGuildSessionProvider guildSessionProvider,
            ICharacterInventoryProvider characterInventoryProvider,
            IItemTransferDialogFactory itemTransferDialogFactory,
            IEIFFileProvider eifFileProvider)
            : base(nativeGraphicsManager, dialogButtonService, DialogType.Guild)
        {
            _nativeGraphicsManager = nativeGraphicsManager;
            _dialogButtonService = dialogButtonService;
            _dialogIconService = dialogIconService;
            _localizedStringFinder = localizedStringFinder;
            _textInputDialogFactory = textInputDialogFactory;
            _guildActions = guildActions;
            _contentProvider = contentProvider;
            _currentMapStateProvider = currentMapStateProvider;
            _enfFileProvider = enfFileProvider;
            _messageBoxFactory = messageBoxFactory;
            _characterRepository = characterRepository;
            _eoMessageBoxFactory = eoMessageBoxFactory;
            _sfxPlayer = sfxPlayer;
            _guildSessionProvider = guildSessionProvider;
            _guildBankBalance = guildSessionProvider.GuildBankBalance;
            _characterInventoryProvider = characterInventoryProvider;
            _itemTransferDialogFactory = itemTransferDialogFactory;
            _eifFileProvider = eifFileProvider;
            SetState(GuildDialogState.Initial);

            _previousMemberHashes = new HashSet<string>(_guildSessionProvider.Members.Select(m => CreateMemberHash(m.Key, m.Value)));

            BackAction += (_, _) =>
            {
                if (_stateStack.Count > 0)
                {
                    var previousState = _stateStack.Pop();
                    SetState(previousState);
                }
                else
                {
                    SetState(GuildDialogState.Initial);
                }
                _bankInfoRequested = false;
                _currentDialogAction = DialogAction.None;
            };

            Title = _localizedStringFinder.GetString(EOResourceID.GUILD_GUILD_MASTER);
        }

        private void SetState(GuildDialogState newState)
        {
            if (_state != newState && _stateStack.Any())
            {
                ClearItemList();
                _stateStack.Push(_state);
            }

            _state = newState;
            ClearItemList();

            switch (_state)
            {
                case GuildDialogState.Initial:
                    ConfigureInitialDialog();
                    break;
                case GuildDialogState.InformationMenu:
                    ConfigureInformationMenu();
                    break;
                case GuildDialogState.Administration:
                    ConfigureAdministrationMenu();
                    break;
                case GuildDialogState.GuildBank:
                    ConfigureGuildBankMenu();
                    break;
            }
        }

        private void ConfigureInitialDialog()
        {
            ListItemType = ListDialogItem.ListItemStyle.Large;
            Buttons = ScrollingListDialogButtons.Cancel;

            AddItemToList(CreateListDialogItem(0, DialogIcon.GuildInformation, EOResourceID.GUILD_INFORMATION, EOResourceID.GUILD_LEARN_MORE, () => SetState(GuildDialogState.InformationMenu)), false);
            AddItemToList(CreateListDialogItem(1, DialogIcon.GuildAdministration, EOResourceID.GUILD_ADMINISTRATION, EOResourceID.GUILD_JOIN_LEAVE_REGISTER, () => SetState(GuildDialogState.Administration)), false);
            AddItemToList(CreateListDialogItem(2, DialogIcon.GuildManagement, EOResourceID.GUILD_MANAGEMENT, EOResourceID.GUILD_MODIFY_RANKING_DISBAND), false);
            AddItemToList(CreateListDialogItem(3, DialogIcon.GuildBankAccount, EOResourceID.GUILD_BANK_ACCOUNT, EOResourceID.GUILD_DEPOSIT_TO_GUILD_ACCOUNT, () => SetState(GuildDialogState.GuildBank)), false);
        }

        private void ConfigureInformationMenu()
        {
            Buttons = ScrollingListDialogButtons.BackCancel;
            AddItemToList(CreateListDialogItem(0, DialogIcon.GuildLookup, EOResourceID.GUILD_JOIN_GUILD, EOResourceID.GUILD_JOIN_AN_EXISTING_GUILD, () => ShowGuildDialog(DialogAction.GuildLookUp)), false);
            AddItemToList(CreateListDialogItem(1, DialogIcon.GuildMemberlist, EOResourceID.MEMBERLIST, EOResourceID.VIEW_GUILD_MEMBERS, () => ShowGuildDialog(DialogAction.MemberList)), false);
        }

        private void ConfigureAdministrationMenu()
        {
            Buttons = ScrollingListDialogButtons.BackCancel;
            AddItemToList(CreateListDialogItem(0, DialogIcon.GuildJoin, EOResourceID.GUILD_JOIN_GUILD, EOResourceID.GUILD_JOIN_AN_EXISTING_GUILD, () => ShowGuildDialog(DialogAction.GuildLookUp)), false);
            AddItemToList(CreateListDialogItem(1, DialogIcon.GuildLeave, EOResourceID.GUILD_LEAVE_GUILD, EOResourceID.GUILD_LEAVE_YOUR_CURRENT_GUILD, () => LeaveGuildDialog()), false);
            AddItemToList(CreateListDialogItem(2, DialogIcon.GuildRegister, EOResourceID.GUILD_REGISTER_GUILD, EOResourceID.GUILD_CREATE_YOUR_OWN_GUILD), false);
        }

        private void ConfigureGuildBankMenu()
        {
            if (string.IsNullOrEmpty(_characterRepository.MainCharacter.GuildTag))
            {
                _messageBoxFactory.CreateMessageBox(DialogResourceID.GUILD_NOT_IN_GUILD).ShowDialog();
                SetState(GuildDialogState.Initial);
            }
            else
            {
                
                _guildActions.BankInfo(_characterRepository.MainCharacter.GuildTag);

                
                _guildBankBalance = _guildSessionProvider.GuildBankBalance;

                _currentDialogAction = DialogAction.DepositFunds;
                ShowGuildBankInfo();
                _bankInfoRequested = true;
            }
        }

        private ListDialogItem CreateListDialogItem(int index, DialogIcon icon, EOResourceID primaryTextResourceID, EOResourceID subTextResourceID, Action leftClickAction = null)
        {
            var item = new ListDialogItem(this, ListDialogItem.ListItemStyle.Large, index)
            {
                ShowIconBackGround = false,
                IconGraphic = _dialogIconService.IconSheet,
                IconGraphicSource = _dialogIconService.GetDialogIconSource(icon),
                PrimaryText = _localizedStringFinder.GetString(primaryTextResourceID),
                SubText = _localizedStringFinder.GetString(subTextResourceID),
                OffsetY = 48,
                OffsetX = -8,
            };
            item.DrawArea = item.DrawArea.WithSize(item.DrawArea.Width + AdjustedDrawAreaOffset, item.DrawArea.Height);

            if (leftClickAction != null)
            {
                item.LeftClick += (_, _) => leftClickAction();
                item.RightClick += (_, _) => leftClickAction();
            }

            return item;
        }

        protected override void OnUpdateControl(GameTime gameTime)
        {
            base.OnUpdateControl(gameTime);

            if (_balanceUpdateNeeded)
            {
                CheckBalanceUpdate();
            }

            if (_state == GuildDialogState.GuildBank)
            {
                HandleGuildBankUpdates();
            }
            else if (_currentDialogAction == DialogAction.MemberList)
            {
                HandleMemberListUpdates();
            }
        }

        private void CheckBalanceUpdate()
        {
            if (_guildBankBalance != _guildSessionProvider.GuildBankBalance)
            {
                _guildBankBalance = _guildSessionProvider.GuildBankBalance;
                var message = $"{_localizedStringFinder.GetString(DialogResourceID.GUILD_NEW_BALANCE)}, {_guildBankBalance}";
       
                var title = _localizedStringFinder.GetString(DialogResourceID.GUILD_DEPOSIT_NEW_BALANCE);
                var msgBox = _messageBoxFactory.CreateMessageBox(message, title, EODialogButtons.Ok);
                msgBox.DialogClosing += (_, e) =>
                {
                    if (e.Result == XNADialogResult.OK)
                    {
                        _sfxPlayer.PlaySfx(SoundEffectID.DialogButtonClick);
                    }
                };

                msgBox.ShowDialog();
                SetState(GuildDialogState.Initial);
                _balanceUpdateNeeded = false;
            }
        }

        private void HandleGuildBankUpdates()
        {
            if (_bankInfoRequested && _guildBankBalance != _guildSessionProvider.GuildBankBalance)
            {
                _guildActions.BankInfo(_characterRepository.MainCharacter.GuildTag);
                _guildBankBalance = _guildSessionProvider.GuildBankBalance;

                ShowGuildBankInfo();
            }
        }

        private void HandleMemberListUpdates()
        {
            var currentMemberHashes = new HashSet<string>(_guildSessionProvider.Members.Select(m => CreateMemberHash(m.Key, m.Value)));
            if (!_previousMemberHashes.SetEquals(currentMemberHashes))
            {
                _previousMemberHashes = currentMemberHashes;
                UpdateMemberList();
            }
        }

        private void HandleDialogAction(string responseText)
        {
            switch (_currentDialogAction)
            {
                case DialogAction.GuildLookUp:
                    ClearItemList();
                    ListItemType = ListDialogItem.ListItemStyle.Large;
                    Buttons = ScrollingListDialogButtons.BackCancel;
                    break;

                case DialogAction.MemberList:
                    ListItemType = ListDialogItem.ListItemStyle.Small;
                    Buttons = ScrollingListDialogButtons.BackCancel;
                    _guildActions.ViewMembers(responseText);
                    UpdateMemberList();
                    break;
            }
        }

        private void UpdateMemberList()
        {
            ClearItemList();

            foreach (var member in _guildSessionProvider.Members)
            {
                var memberName = member.Key;
                var rank = member.Value.Rank;
                var rankName = member.Value.RankName;

                var memberItem = new ListDialogItem(this, ListDialogItem.ListItemStyle.Small, 0)
                {
                    ShowIconBackGround = false,
                    PrimaryText = $"{rank} {memberName} {rankName}",
                };

                AddItemToList(memberItem, sortList: false);
            }
        }

        private void ShowGuildDialog(DialogAction action)
        {
            _currentDialogAction = action;

            var promptMessageId = action == DialogAction.GuildLookUp ? EOResourceID.GUILD_VIEW_INFO_PROMPT : EOResourceID.GUILD_VIEW_INFO_PROMPT;
            var dlg = _textInputDialogFactory.Create(_localizedStringFinder.GetString(promptMessageId), maxInputChars: MaxGuildTag);

            dlg.DialogClosing += (_, e) =>
            {
                if (e.Result == XNADialogResult.OK)
                {
                    var responseText = dlg.ResponseText?.Trim();
                    if (!string.IsNullOrWhiteSpace(responseText))
                    {
                        HandleDialogAction(responseText);
                    }
                }
            };

            dlg.ShowDialog();
        }

        private void LeaveGuildDialog()
        {
            ClearItemList();
            ListItemType = ListDialogItem.ListItemStyle.Large;
            Buttons = ScrollingListDialogButtons.BackCancel;

            var actions = new List<Action>
            {
                () =>
                {
                    var dlg = _messageBoxFactory.CreateMessageBox(DialogResourceID.GUILD_PROMPT_LEAVE_GUILD);
                    _guildActions.LeaveGuild();
                    _characterRepository.MainCharacter = _characterRepository.MainCharacter.WithGuildTag(string.Empty);
                    dlg.ShowDialog();
                }
            };

            AddTextAsListItems(
                _contentProvider.Fonts[Constants.FontSize09],
                true,
                actions,
                _localizedStringFinder.GetString(EOResourceID.GUILD_LEAVE_GUILD),
                _localizedStringFinder.GetString(EOResourceID.GUILD_YOU_ARE_ABOUT_TO_LEAVE_THE_GUILD),
                _localizedStringFinder.GetString(EOResourceID.GUILD_AFTER_YOU_HAVE_LEFT),
                _localizedStringFinder.GetString(EOResourceID.GUILD_CLICK_HERE_TO_LEAVE_YOUR_GUILD));
        }

        private void ShowGuildBankInfo()
        {
            ClearItemList();
            ListItemType = ListDialogItem.ListItemStyle.Large;
            Buttons = ScrollingListDialogButtons.BackCancel;

            var actions = new List<Action>
            {
                () =>
                {
                    var characterGold = _characterInventoryProvider.ItemInventory.SingleOrNone(x => x.ItemID == 1);
                    characterGold.Match(
                        some: gold =>
                        {
                            if (gold.Amount == 0)
                            {
                                ShowMessageBox(_localizedStringFinder.GetString(DialogResourceID.BANK_ACCOUNT_UNABLE_TO_DEPOSIT));
                            }
                            else if (gold.Amount > 1)
                            {
                                ShowItemTransferDialog(gold.Amount);
                            }
                        },
                        none: () =>
                        {
                            ShowMessageBox(_localizedStringFinder.GetString(DialogResourceID.BANK_ACCOUNT_UNABLE_TO_DEPOSIT));
                        });
                }
            };

            AddTextAsListItems(
                _contentProvider.Fonts[Constants.FontSize09],
                true,
                actions,
                $"{_localizedStringFinder.GetString(EOResourceID.GUILD_BANK_STATUS_LABEL)} {_guildBankBalance}",
                _localizedStringFinder.GetString(EOResourceID.GUILD_BANK_DEPOSIT_ONLY_INFO),
                _localizedStringFinder.GetString(EOResourceID.GUILD_BANK_USAGE_INFO),
                _localizedStringFinder.GetString(EOResourceID.GUILD_DEPOSIT_PROMPT));
        }

        private void ShowItemTransferDialog(int goldAmount)
        {
            var dlg = _itemTransferDialogFactory.CreateItemTransferDialog(
                _eifFileProvider.EIFFile[1].Name,
                ItemTransferDialog.TransferType.BankTransfer,
                goldAmount,
                EOResourceID.DIALOG_TRANSFER_DEPOSIT);

            dlg.DialogClosing += (_, e) =>
            {
                if (e.Result == XNADialogResult.OK)
                {
                    _guildActions.DepositAmount(dlg.SelectedAmount);
                    _guildActions.BankInfo(_characterRepository.MainCharacter.GuildTag);
                    _balanceUpdateNeeded = true;
                }
            };

            dlg.ShowDialog();
        }

        private void ShowMessageBox(string message)
        {
            var dlg = _messageBoxFactory.CreateMessageBox(message);
            dlg.ShowDialog();
        }

        private string CreateMemberHash(string memberName, (int Rank, string RankName) memberInfo)
        {
            return $"{memberName}:{memberInfo.Rank}:{memberInfo.RankName}";
        }
    }
}
