﻿// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

using System.Threading.Tasks;
using EndlessClient.Dialogs.Actions;
using EndlessClient.GameExecution;
using EOLib.Data.Login;
using EOLib.Net;
using EOLib.Net.Communication;
using EOLib.Net.Connection;

namespace EndlessClient.Controllers
{
	public class LoginController : ILoginController
	{
		private readonly ILoginActions _loginActions;
		private readonly IGameStateActions _gameStateActions;
		private readonly IErrorDialogDisplayAction _errorDisplayAction;
		private readonly INetworkConnectionActions _networkConnectionActions;
		private readonly IBackgroundReceiveActions _backgroundReceiveActions;

		public LoginController(ILoginActions loginActions,
							   IGameStateActions gameStateActions,
							   IErrorDialogDisplayAction errorDisplayAction,
							   INetworkConnectionActions networkConnectionActions,
							   IBackgroundReceiveActions backgroundReceiveActions)
		{
			_loginActions = loginActions;
			_gameStateActions = gameStateActions;
			_errorDisplayAction = errorDisplayAction;
			_networkConnectionActions = networkConnectionActions;
			_backgroundReceiveActions = backgroundReceiveActions;
		}

		public async Task LoginToAccount(ILoginParameters loginParameters)
		{
			if (!_loginActions.LoginParametersAreValid(loginParameters))
				return;

			IAccountLoginData loginData;
			try
			{
				//todo: return just the login reply, character data should be put into repository
				loginData = await _loginActions.LoginToServer(loginParameters);
			}
			catch (EmptyPacketReceivedException)
			{
				SetInitialStateAndShowError();
				DisconnectAndStopReceiving();
				return;
			}
			catch (NoDataSentException)
			{
				SetInitialStateAndShowError();
				DisconnectAndStopReceiving();
				return;
			}
			catch (MalformedPacketException)
			{
				SetInitialStateAndShowError();
				DisconnectAndStopReceiving();
				return;
			}

			if (loginData.Response == LoginReply.Ok)
				_gameStateActions.ChangeToState(GameStates.LoggedIn);
			else
				_errorDisplayAction.ShowLoginError(loginData.Response);
		}

		public async Task LoginToCharacter()
		{
			throw new System.NotImplementedException();
		}

		private void SetInitialStateAndShowError()
		{
			_gameStateActions.ChangeToState(GameStates.Initial);
			_errorDisplayAction.ShowError(ConnectResult.SocketError);
		}

		private void DisconnectAndStopReceiving()
		{
			_backgroundReceiveActions.CancelBackgroundReceiveLoop();
			_networkConnectionActions.DisconnectFromServer();
		}
	}
}