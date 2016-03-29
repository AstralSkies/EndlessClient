﻿// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

using System;
using System.Collections.Generic;
using System.Linq;
using EOLib;
using EOLib.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using XNAControls;

namespace EndlessClient.Controls.ControlSets
{
	public abstract class BaseGameStateControlSet
	{
		#region IGameStateControlSet implementation

		protected readonly List<IGameComponent> _allComponents;

		public IReadOnlyList<IGameComponent> AllComponents
		{
			get { return _allComponents; }
		}

		public IReadOnlyList<XNAControl> XNAControlComponents
		{
			get { return _allComponents.OfType<XNAControl>().ToList(); }
		}

		#endregion

		private Texture2D _mainButtonTexture, _secondaryButtonTexture;
		private Texture2D[] _textBoxTextures;

		protected BaseGameStateControlSet()
		{
			_allComponents = new List<IGameComponent>(16);
		}

		public void InitializeResources(INativeGraphicsManager gfxManager,
										ContentManager xnaContentManager)
		{
			_mainButtonTexture = gfxManager.TextureFromResource(GFXTypes.PreLoginUI, 13, true);
			_secondaryButtonTexture = gfxManager.TextureFromResource(GFXTypes.PreLoginUI, 14, true);

			_textBoxTextures = new[]
			{
				xnaContentManager.Load<Texture2D>("tbBack"),
				xnaContentManager.Load<Texture2D>("tbLeft"),
				xnaContentManager.Load<Texture2D>("tbRight"),
				xnaContentManager.Load<Texture2D>("cursor")
			};
		}

		public abstract void InitializeControls(IGameStateControlSet currentControlSet);

		protected static IGameComponent GetControl(IGameStateControlSet currentControlSet,
												   GameControlIdentifier whichControl,
												   Func<IGameComponent> componentFactory)
		{
			return currentControlSet.FindComponentByControlIdentifier(whichControl) ?? componentFactory();
		}

		#region Initial State

		protected XNAButton GetCreateAccountButton()
		{
			return MainButtonCreationHelper(GameControlIdentifier.InitialCreateAccount);
		}

		protected XNAButton GetLoginButton()
		{
			return MainButtonCreationHelper(GameControlIdentifier.InitialLogin);
		}

		protected XNAButton GetViewCreditsButton()
		{
			return MainButtonCreationHelper(GameControlIdentifier.InitialViewCredits);
		}

		protected XNAButton GetExitButton()
		{
			return MainButtonCreationHelper(GameControlIdentifier.InitialExitGame);
		}

		private XNAButton MainButtonCreationHelper(GameControlIdentifier whichControl)
		{
			int i;
			switch (whichControl)
			{
				case GameControlIdentifier.InitialCreateAccount: i = 0; break;
				case GameControlIdentifier.InitialLogin: i = 1; break;
				case GameControlIdentifier.InitialViewCredits: i = 2; break;
				case GameControlIdentifier.InitialExitGame: i = 3; break;
				default: throw new ArgumentException("Invalid control specified for helper", "whichControl");
			}

			var widthFactor = _mainButtonTexture.Width / 2;
			var heightFactor = _mainButtonTexture.Height / 4;
			var outSource = new Rectangle(0, i * heightFactor, widthFactor, heightFactor);
			var overSource = new Rectangle(widthFactor, i * heightFactor, widthFactor, heightFactor);

			return new XNAButton(_mainButtonTexture, new Vector2(26, 278 + i * 40), outSource, overSource);
		}

		protected XNALabel GetVersionInfoLabel()
		{
			return new XNALabel(new Rectangle(25, 453, 1, 1), Constants.FontSize07)
			{
				Text = " ",
				ForeColor = Constants.BeigeText
			};
		}

		#endregion

		#region Create Account State

		protected XNATextBox GetCreateAccountNameTextBox()
		{
			var tb = AccountInputTextBoxCreationHelper(GameControlIdentifier.CreateAccountName);
			tb.MaxChars = 16;
			return tb;
		}

		protected XNATextBox GetCreateAccountPasswordTextBox()
		{
			var tb = AccountInputTextBoxCreationHelper(GameControlIdentifier.CreateAccountPassword);
			tb.PasswordBox = true;
			tb.MaxChars = 12;
			return tb;
		}

		protected XNATextBox GetCreateAccountConfirmTextBox()
		{
			var tb = AccountInputTextBoxCreationHelper(GameControlIdentifier.CreateAccountPasswordConfirm);
			tb.PasswordBox = true;
			tb.MaxChars = 12;
			return tb;
		}

		protected XNATextBox GetCreateAccountRealNameTextBox()
		{
			return AccountInputTextBoxCreationHelper(GameControlIdentifier.CreateAccountRealName);
		}

		protected XNATextBox GetCreateAccountLocationTextBox()
		{
			return AccountInputTextBoxCreationHelper(GameControlIdentifier.CreateAccountLocation);
		}

		protected XNATextBox GetCreateAccountEmailTextBox()
		{
			return AccountInputTextBoxCreationHelper(GameControlIdentifier.CreateAccountEmail);
		}

		private XNATextBox AccountInputTextBoxCreationHelper(GameControlIdentifier whichControl)
		{
			int i;
			switch (whichControl)
			{
				case GameControlIdentifier.CreateAccountName: i = 0; break;
				case GameControlIdentifier.CreateAccountPassword: i = 1; break;
				case GameControlIdentifier.CreateAccountPasswordConfirm: i = 2; break;
				case GameControlIdentifier.CreateAccountRealName: i = 3; break;
				case GameControlIdentifier.CreateAccountLocation: i = 4; break;
				case GameControlIdentifier.CreateAccountEmail: i = 5; break;
				default: throw new ArgumentException("Invalid control specified for helper", "whichControl");
			}

			//set the first  3 Y coord to start at 69  and move up by 51 each time
			//set the second 3 Y coord to start at 260 and move up by 51 each time
			var txtYCoord = (i < 3 ? 69 : 260) + i%3*51;
			var drawArea = new Rectangle(358, txtYCoord, 240, _textBoxTextures[0].Height);
			return new XNATextBox(drawArea, _textBoxTextures, Constants.FontSize08)
			{
				LeftPadding = 4,
				MaxChars = 35,
				DefaultText = " "
			};
		}

		protected XNAButton GetCreateButton(bool isCreateCharacterButton)
		{
			return new XNAButton(_secondaryButtonTexture,
								 new Vector2(isCreateCharacterButton ? 334 : 359, 417),
								 new Rectangle(0, 0, 120, 40),
								 new Rectangle(120, 0, 120, 40));
		}

		protected XNAButton GetCreateAccountCancelButton()
		{
			return new XNAButton(_secondaryButtonTexture,
								 new Vector2(481, 417),
								 new Rectangle(0, 40, 120, 40),
								 new Rectangle(120, 40, 120, 40));
		}

		#endregion
	}
}
