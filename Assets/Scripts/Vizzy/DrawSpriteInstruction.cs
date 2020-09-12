using System;
using Assets.Scripts.Objects;
using ModApi.Craft.Program;
using UnityEngine;

namespace Assets.Scripts.Vizzy {
    [Serializable]
    public class DrawSpriteInstruction : VizzyGLInstructionBase {
        public const String XmlName = "DrawSprite";

        protected override void ExecuteImpl(IThreadContext context) {
            var imageData = this.DrawingContext.Sprite;

            if (imageData == null) {
                Debug.LogWarning("No sprite currently specified.");
                return;
            }

            var objectName = this.GetExpression(1).Evaluate(context).TextValue;

            if (String.IsNullOrWhiteSpace(objectName)) {
                Debug.LogWarning("Unable to draw VizzyGL graphic with no name.");
                return;
            }

            VizzyGLSprite vizzyGlObject;
            switch (this.DrawingContext.Origin) {
                case PositionType.CraftLocal:
                case PositionType.CraftPCI:
                    vizzyGlObject =
                        new VizzyGLSprite(
                            imageData,
                            objectName,
                            this.DrawingContext.View,
                            this.DrawingContext.Origin,
                            this.DrawingContext.CraftId);
                    break;
                case PositionType.PlanetPCI:
                case PositionType.PlanetLatLonAsl:
                case PositionType.PlanetLatLonAgl:
                    vizzyGlObject =
                        new VizzyGLSprite(
                            imageData,
                            objectName,
                            this.DrawingContext.View,
                            this.DrawingContext.Origin,
                            this.DrawingContext.PlanetName);
                    break;
                default:
                    vizzyGlObject = null;
                    break;
            }

            if (vizzyGlObject != null) {
                vizzyGlObject.OriginOffset =
                    this.GetExpression(0).Evaluate(context).VectorValue;
                this.DrawingContext.AddObject(vizzyGlObject);
            }
        }
    }
}
