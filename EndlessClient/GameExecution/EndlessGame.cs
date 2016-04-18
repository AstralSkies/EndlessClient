﻿// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

using EndlessClient.ControlSets;
using EOLib.Graphics;
using Microsoft.Xna.Framework;

namespace EndlessClient.GameExecution
{
	public class EndlessGame : Game, IEndlessGame
	{
		private readonly IGraphicsDeviceRepository _graphicsDeviceRepository;
		private readonly IControlSetRepository _controlSetRepository;
		private readonly IControlSetFactory _controlSetFactory;

		private readonly IGraphicsDeviceManager _graphicsDeviceManager;

		public EndlessGame(IClientWindowSizeProvider windowSizeProvider,
						   IGraphicsDeviceRepository graphicsDeviceRepository,
						   IControlSetRepository controlSetRepository,
						   IControlSetFactory controlSetFactory)
		{
			_graphicsDeviceRepository = graphicsDeviceRepository;
			_controlSetRepository = controlSetRepository;
			_controlSetFactory = controlSetFactory;
			
			_graphicsDeviceManager = new GraphicsDeviceManager(this)
			{
				PreferredBackBufferWidth = windowSizeProvider.Width,
				PreferredBackBufferHeight = windowSizeProvider.Height
			};

			Content.RootDirectory = "Content";
		}

		protected override void Initialize()
		{
			IsMouseVisible = true;

			base.Initialize();
		}

		protected override void LoadContent()
		{
			//the GraphicsDevice doesn't exist until Initialize() is called by the framework
			//Ideally, this would be set in a DependencyContainer, but I'm not sure of a way to do that now
			_graphicsDeviceRepository.GraphicsDevice = GraphicsDevice;

			var controls = _controlSetFactory.CreateControlsForState(
				GameStates.Initial,
				_controlSetRepository.CurrentControlSet);

			_controlSetRepository.CurrentControlSet = controls;

			base.LoadContent();
		}
	}
}