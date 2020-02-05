﻿// Original Work Copyright (c) Ethan Moffat 2014-2016
// This file is subject to the GPL v2 License
// For additional details, see the LICENSE file

using EndlessClient.Content;
using EndlessClient.Rendering.Sprites;
using EOLib;
using EOLib.Domain.Character;
using EOLib.Domain.Extensions;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace EndlessClient.Rendering.CharacterProperties
{
    public class HatRenderer : BaseCharacterPropertyRenderer
    {
        private readonly IShaderProvider _shaderProvider;
        private readonly ISpriteSheet _hatSheet;
        private readonly ISpriteSheet _hairSheet;
        private readonly HairRenderLocationCalculator _hairRenderLocationCalculator;

        public override bool CanRender => _hatSheet.HasTexture && _renderProperties.HatGraphic != 0;

        public HatRenderer(IShaderProvider shaderProvider,
                           ICharacterRenderProperties renderProperties,
                           ISpriteSheet hatSheet,
                           ISpriteSheet hairSheet)
            : base(renderProperties)
        {
            _shaderProvider = shaderProvider;
            _hatSheet = hatSheet;
            _hairSheet = hairSheet;

            _hairRenderLocationCalculator = new HairRenderLocationCalculator(_renderProperties);
        }

        public override void Render(SpriteBatch spriteBatch, Rectangle parentCharacterDrawArea)
        {
            var hairDrawLoc = _hairRenderLocationCalculator.CalculateDrawLocationOfCharacterHair(_hairSheet.SourceRectangle, parentCharacterDrawArea);
            var offsets = GetOffsets();

            _shaderProvider.Shaders[ShaderRepository.HairClip].CurrentTechnique.Passes[0].Apply();
            Render(spriteBatch, _hatSheet, hairDrawLoc + offsets);
        }

        private Vector2 GetOffsets()
        {
            var xOff = 0f;
            var yOff = -3f;

            if (_renderProperties.IsRangedWeapon && _renderProperties.AttackFrame == 1)
            {
                yOff -= _renderProperties.IsFacing(EODirection.Down, EODirection.Right)
                    ? 1 - _renderProperties.Gender // female needs an additional y adjustment for these specific directions
                    : 0;
            }

            var flippedOffset = _renderProperties.IsFacing(EODirection.Up, EODirection.Right) ? -2 : 0;

            return new Vector2(xOff + flippedOffset, yOff);
        }
    }
}
