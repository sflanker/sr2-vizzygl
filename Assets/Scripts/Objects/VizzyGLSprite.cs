using System;
using System.Xml.Linq;
using UnityEngine;

namespace Assets.Scripts.Objects {
    [Serializable]
    public class VizzyGLSprite : VizzyGLObject {

        [SerializeField] private String _imageDataEncoded;
        private Byte[] _imageData;

        protected override bool SupportsColor => false;

        public VizzyGLSprite(Byte[] imageData, String name, ViewType view, PositionType originType, String originPlanetName) :
            base(name, view, originType, originPlanetName) {

            this._imageData = imageData;
            this._imageDataEncoded = Convert.ToBase64String(imageData);
        }

        public VizzyGLSprite(Byte[] imageData, String name, ViewType view, PositionType originType, Int32 originCraftId) :
            base(name, view, originType, originCraftId) {

            this._imageData = imageData;
            this._imageDataEncoded = Convert.ToBase64String(imageData);
        }

        protected internal override void OnDeserialized(XElement element) {
            base.OnDeserialized(element);

            this._imageData = Convert.FromBase64String(this._imageDataEncoded);
        }

        protected override GameObject CreateGameObject() {
            var spriteObject = new GameObject($"Sprite {this.Name}");
            var renderer = spriteObject.AddComponent<SpriteRenderer>();
            renderer.sprite = LoadNewSprite(this._imageData);
            return spriteObject;
        }

        private static Sprite LoadNewSprite(Byte[] imageData, Single pixelsPerUnit = 100.0f) {
            // Load a PNG or JPG image from a byte array to a Texture2D, assign this texture to a new sprite and return its reference

            var spriteTexture = new Texture2D(2, 2);
            if (!spriteTexture.LoadImage(imageData)) {
                Debug.LogWarning("Loading sprite image data failed.");
                return null;
            }

            return Sprite.Create(
                spriteTexture,
                new Rect(0, 0, spriteTexture.width, spriteTexture.height),
                new Vector2(0, 0),
                pixelsPerUnit);
        }
    }
}
