﻿using EndlessClient.Audio;
using EndlessClient.Controllers;
using EndlessClient.GameExecution;
using EndlessClient.HUD.Spells;
using EndlessClient.Input;
using EndlessClient.Rendering.Character;
using EndlessClient.Rendering.Chat;
using EndlessClient.Rendering.Effects;
using EndlessClient.Rendering.Factories;
using EndlessClient.Rendering.Sprites;
using EOLib;
using EOLib.Domain.Extensions;
using EOLib.Domain.NPC;
using EOLib.Graphics;
using EOLib.IO.Repositories;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Optional;
using System;
using XNAControls;

namespace EndlessClient.Rendering.NPC
{
    public class NPCRenderer : DrawableGameComponent, INPCRenderer
    {
        private readonly ICharacterRendererProvider _characterRendererProvider;
        private readonly IENFFileProvider _enfFileProvider;
        private readonly INPCSpriteSheet _npcSpriteSheet;
        private readonly IGridDrawCoordinateCalculator _gridDrawCoordinateCalculator;
        private readonly IHealthBarRendererFactory _healthBarRendererFactory;
        private readonly IChatBubbleFactory _chatBubbleFactory;
        private readonly INPCInteractionController _npcInteractionController;
        private readonly IMapInteractionController _mapInteractionController;
        private readonly IUserInputProvider _userInputProvider;
        private readonly ISpellSlotDataProvider _spellSlotDataProvider;
        private readonly ISfxPlayer _sfxPlayer;
        private readonly int _readonlyTopPixel, _readonlyBottomPixel;
        private readonly IEffectRenderer _effectRenderer;
        private readonly IHealthBarRenderer _healthBarRenderer;

        private DateTime _lastStandingAnimation;
        private int _fadeAwayAlpha;
        private bool _isDying, _isBlankSprite;

        private XNALabel _nameLabel;
        private IChatBubble _chatBubble;

        public int TopPixel => _readonlyTopPixel;

        public int BottomPixel => _readonlyBottomPixel;

        public int TopPixelWithOffset => _readonlyTopPixel + DrawArea.Y;

        public int BottomPixelWithOffset => _readonlyBottomPixel + DrawArea.Y;

        public Rectangle DrawArea { get; private set; }

        public Rectangle MapProjectedDrawArea { get; private set; }

        public bool MouseOver => DrawArea.Contains(_userInputProvider.CurrentMouseState.Position);

        public bool MouseOverPreviously => DrawArea.Contains(_userInputProvider.PreviousMouseState.Position);

        public EOLib.Domain.NPC.NPC NPC { get; set; }

        public bool IsDead { get; private set; }

        public Rectangle EffectTargetArea => DrawArea;

        public NPCRenderer(INativeGraphicsManager nativeGraphicsManager,
                           IEndlessGameProvider endlessGameProvider,
                           ICharacterRendererProvider characterRendererProvider,
                           IENFFileProvider enfFileProvider,
                           INPCSpriteSheet npcSpriteSheet,
                           IGridDrawCoordinateCalculator gridDrawCoordinateCalculator,
                           IHealthBarRendererFactory healthBarRendererFactory,
                           IChatBubbleFactory chatBubbleFactory,
                           INPCInteractionController npcInteractionController,
                           IMapInteractionController mapInteractionController,
                           IUserInputProvider userInputProvider,
                           ISpellSlotDataProvider spellSlotDataProvider,
                           ISfxPlayer sfxPlayer,
                           EOLib.Domain.NPC.NPC initialNPC)
            : base((Game)endlessGameProvider.Game)
        {
            NPC = initialNPC;

            _characterRendererProvider = characterRendererProvider;
            _enfFileProvider = enfFileProvider;
            _npcSpriteSheet = npcSpriteSheet;
            _gridDrawCoordinateCalculator = gridDrawCoordinateCalculator;
            _healthBarRendererFactory = healthBarRendererFactory;
            _chatBubbleFactory = chatBubbleFactory;
            _npcInteractionController = npcInteractionController;
            _mapInteractionController = mapInteractionController;
            _userInputProvider = userInputProvider;
            _spellSlotDataProvider = spellSlotDataProvider;
            _sfxPlayer = sfxPlayer;

            DrawArea = GetStandingFrameRectangle();
            _readonlyTopPixel = GetTopPixel();
            _readonlyBottomPixel = GetBottomPixel();

            _lastStandingAnimation = DateTime.Now;
            _fadeAwayAlpha = 255;

            _effectRenderer = new EffectRenderer(nativeGraphicsManager, _sfxPlayer, this);
            _healthBarRenderer = _healthBarRendererFactory.CreateHealthBarRenderer(this);
        }

        public override void Initialize()
        {
            UpdateDrawAreas();

            _nameLabel = new XNALabel(Constants.FontSize08pt5)
            {
                Visible = false,
                TextWidth = 89,
                TextAlign = LabelAlignment.MiddleCenter,
                ForeColor = Color.White,
                AutoSize = true,
                Text = _enfFileProvider.ENFFile[NPC.ID].Name,
                DrawOrder = 30,
                KeepInClientWindowBounds = false,
            };
            _nameLabel.Initialize();

            if (!_nameLabel.Game.Components.Contains(_nameLabel))
                _nameLabel.Game.Components.Add(_nameLabel);

            _nameLabel.DrawPosition = GetNameLabelPosition();

            base.Initialize();
        }

        public override void Update(GameTime gameTime)
        {
            if (!Visible) return;

            UpdateDrawAreas();
            UpdateStandingFrameAnimation();
            UpdateDeadState();

            var currentMousePosition = _userInputProvider.CurrentMouseState.Position - DrawArea.Location;
            var currentFrame = _npcSpriteSheet.GetNPCTexture(_enfFileProvider.ENFFile[NPC.ID].Graphic, NPC.Frame, NPC.Direction);

            if (currentFrame != null && currentFrame.Bounds.Contains(currentMousePosition))
            {
                var colorData = new Color[1];
                currentFrame.GetData(0, new Rectangle(currentMousePosition.X, currentMousePosition.Y, 1, 1), colorData, 0, 1);

                _nameLabel.Visible = !_healthBarRenderer.Visible && !_isDying && (_isBlankSprite || colorData[0].A > 0);
                _nameLabel.DrawPosition = GetNameLabelPosition();

                if (!_userInputProvider.ClickHandled &&
                    _userInputProvider.CurrentMouseState.LeftButton == ButtonState.Released &&
                    _userInputProvider.PreviousMouseState.LeftButton == ButtonState.Pressed)
                {
                    if (_spellSlotDataProvider.SpellIsPrepared)
                    {
                        _mapInteractionController.LeftClick(NPC);
                    }
                    else
                    {
                        if (_isBlankSprite || colorData[0].A > 0)
                        {
                            _npcInteractionController.ShowNPCDialog(NPC);
                        }
                    }
                }
            }
            else
            {
                _nameLabel.Visible = false;
            }

            _effectRenderer.Update();
            _healthBarRenderer.Update(gameTime);

            base.Update(gameTime);
        }

        public void DrawToSpriteBatch(SpriteBatch spriteBatch)
        {
            if (!Visible) return;

            var data = _enfFileProvider.ENFFile[NPC.ID];

            var color = Color.FromNonPremultiplied(255, 255, 255, _fadeAwayAlpha);
            var effects = NPC.IsFacing(EODirection.Left, EODirection.Down) ? SpriteEffects.None : SpriteEffects.FlipHorizontally;

            _effectRenderer.DrawBehindTarget(spriteBatch);

            var texture = _npcSpriteSheet.GetNPCTexture(data.Graphic, NPC.Frame, NPC.Direction);
            if (texture != null)
            {
                spriteBatch.Draw(texture, DrawArea, null, color, 0f, Vector2.Zero, effects, 1f);
            }

            _effectRenderer.DrawInFrontOfTarget(spriteBatch);

            _healthBarRenderer.DrawToSpriteBatch(spriteBatch);
        }

        public void StartDying()
        {
            _isDying = true;
        }

        public void ShowDamageCounter(int damage, int percentHealth, bool isHeal)
        {
            var optionalDamage = damage.SomeWhen(d => d > 0);
            _healthBarRenderer.SetDamage(optionalDamage, percentHealth);
        }

        public void ShowChatBubble(string message, bool isGroupChat)
        {
            if (_chatBubble == null)
                _chatBubble = _chatBubbleFactory.CreateChatBubble(this);
            _chatBubble.SetMessage(message, isGroupChat: false);
        }

        #region Effects

        public bool EffectIsPlaying()
        {
            return _effectRenderer.State == EffectState.Playing;
        }

        public void ShowWaterSplashies() { }

        public void ShowWarpArrive() { }

        public void ShowWarpLeave() { }

        public void ShowPotionAnimation(int potionId) { }

        public void ShowSpellAnimation(int spellGraphic)
        {
            _effectRenderer.PlayEffect(EffectType.Spell, spellGraphic);
        }

        #endregion

        private Rectangle GetStandingFrameRectangle()
        {
            var data = _enfFileProvider.ENFFile[NPC.ID];
            var baseFrame = _npcSpriteSheet.GetNPCTexture(data.Graphic, NPCFrame.Standing, EODirection.Down);
            return new Rectangle(0, 0, baseFrame.Width, baseFrame.Height);
        }

        private int GetTopPixel()
        {
            var data = _enfFileProvider.ENFFile[NPC.ID];
            var frameTexture = _npcSpriteSheet.GetNPCTexture(data.Graphic, NPCFrame.Standing, EODirection.Down);
            var frameData = new Color[frameTexture.Width * frameTexture.Height];
            frameTexture.GetData(frameData);

            int i = 0;
            while (i < frameData.Length && frameData[i].A == 0) i++;

            return (_isBlankSprite = i == frameData.Length) ? 0 : i / frameTexture.Height;
        }

        private int GetBottomPixel()
        {
            var data = _enfFileProvider.ENFFile[NPC.ID];
            var frameTexture = _npcSpriteSheet.GetNPCTexture(data.Graphic, NPCFrame.Standing, EODirection.Down);
            var frameData = new Color[frameTexture.Width * frameTexture.Height];
            frameTexture.GetData(frameData);

            int i = frameData.Length - 1;
            while (i >= 0 && frameData[i].A == 0) i--;

            return (_isBlankSprite = i < 0) ? frameTexture.Height : i / frameTexture.Height;
        }

        private void UpdateDrawAreas()
        {
            _characterRendererProvider.MainCharacterRenderer
                .MatchSome(mainRenderer =>
                {
                    var data = _enfFileProvider.ENFFile[NPC.ID];
                    var frameTexture = _npcSpriteSheet.GetNPCTexture(data.Graphic, NPC.Frame, NPC.Direction);
                    var metaData = _npcSpriteSheet.GetNPCMetadata(data.Graphic);

                    var metaDataOffsetX = NPC.Frame == NPCFrame.Attack2 ? metaData.AttackOffsetX : metaData.OffsetX;
                    var metaDataOffsetY = NPC.Frame == NPCFrame.Attack2 ? metaData.AttackOffsetY - metaData.OffsetY : -metaData.OffsetY;

                    var renderCoordinates = _gridDrawCoordinateCalculator.CalculateDrawCoordinates(NPC) +
                        new Vector2(metaDataOffsetX - frameTexture.Width / 2, metaDataOffsetY);
                    DrawArea = frameTexture.Bounds.WithPosition(renderCoordinates);

                    var oneGridSize = new Vector2(mainRenderer.DrawArea.Width,
                                                  mainRenderer.DrawArea.Height);
                    MapProjectedDrawArea = new Rectangle(
                        (int)renderCoordinates.X + (frameTexture.Width / 2) - (int)oneGridSize.X,
                        BottomPixelWithOffset - (int)oneGridSize.Y,
                        (int)oneGridSize.X,
                        (int)oneGridSize.Y);
                });
        }

        private void UpdateStandingFrameAnimation()
        {
            var now = DateTime.Now;

            var data = _enfFileProvider.ENFFile[NPC.ID];
            var metaData = _npcSpriteSheet.GetNPCMetadata(data.Graphic);

            if (!metaData.HasStandingFrameAnimation
                || !NPC.IsActing(NPCActionState.Standing)
                || (now - _lastStandingAnimation).TotalMilliseconds < 250)
                return;

            _lastStandingAnimation = now;
            NPC = NPC.WithFrame(NPC.Frame == NPCFrame.Standing ? NPCFrame.StandingFrame1 : NPCFrame.Standing);
        }

        private void UpdateDeadState()
        {
            if (!_isDying) return;

            if (_fadeAwayAlpha >= 3)
                _fadeAwayAlpha -= 3;
            IsDead = _fadeAwayAlpha <= 0 && !EffectIsPlaying();
        }

        private Vector2 GetNameLabelPosition()
        {
            var data = _enfFileProvider.ENFFile[NPC.ID];
            var offset = _npcSpriteSheet.GetNPCMetadata(data.Graphic).NameLabelOffset;
            return new Vector2(DrawArea.X + (DrawArea.Width - _nameLabel.ActualWidth) / 2f,
                               TopPixelWithOffset - _nameLabel.ActualHeight - offset);

        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _nameLabel.Dispose();
                _chatBubble?.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
