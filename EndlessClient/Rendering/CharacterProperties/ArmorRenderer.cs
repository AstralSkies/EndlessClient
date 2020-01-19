﻿// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

using System;
using EndlessClient.Rendering.Sprites;
using EOLib;
using EOLib.Domain.Character;
using EOLib.Domain.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.Rendering.CharacterProperties
{
    public class ArmorRenderer : ICharacterPropertyRenderer
    {
        private readonly ICharacterRenderProperties _renderProperties;
        private readonly ISpriteSheet _armorSheet;

        public bool CanRender => _armorSheet.HasTexture && _renderProperties.ArmorGraphic != 0;

        public ArmorRenderer(ICharacterRenderProperties renderProperties,
                             ISpriteSheet armorSheet)
        {
            _renderProperties = renderProperties;
            _armorSheet = armorSheet;
        }

        public void Render(SpriteBatch spriteBatch, Rectangle parentCharacterDrawArea)
        {
            var offsets = GetOffsets(parentCharacterDrawArea.Size.ToVector2());
            var drawLoc = new Vector2(parentCharacterDrawArea.X - 2 + offsets.X, parentCharacterDrawArea.Y + offsets.Y);

            spriteBatch.Draw(_armorSheet.SheetTexture, drawLoc, _armorSheet.SourceRectangle, Color.White, 0.0f, Vector2.Zero, 1.0f,
                             _renderProperties.IsFacing(EODirection.Up, EODirection.Right) ? SpriteEffects.FlipHorizontally : SpriteEffects.None,
                             0.0f);
        }

        private Vector2 GetOffsets(Vector2 parentCharacterSize)
        {
            var resX = -(float)Math.Floor(Math.Abs(_armorSheet.SourceRectangle.Width - parentCharacterSize.X) / 2);
            var resY = -(float)Math.Floor(Math.Abs(_armorSheet.SourceRectangle.Height - parentCharacterSize.Y) / 2);

            resX += _renderProperties.AttackFrame == 2 ? _renderProperties.IsFacing(EODirection.Up, EODirection.Right) ? 4 : 0 : 2;
            resY -= _renderProperties.IsActing(CharacterActionState.Walking) ? 4 : 3;

            return new Vector2(resX, resY);
        }
    }
}
