using ModApi.Craft;
using UnityEngine;

namespace Assets.Scripts.Behaviours {
    public class CraftLocalPositionBehaviour : OriginOffsetPositionBehaviour {
        private ICraftNode craft;

        protected override void OnInitialized() {
            base.OnInitialized();
            this.craft = this.VizzyGlObject.GetOriginNode() as ICraftNode;
        }

        protected override void OnUpdate() {
            var flightScene = Game.Instance.FlightScene;
            if (this.craft != null) {
                var referenceFrame = flightScene.ViewManager.GameView.ReferenceFrame;

                // TODO: handle case where our offset vector is too big for a Vector3 (in this
                // case we probably should not be drawing the object).
                var craftNode = flightScene.CraftNode;
                if (craftNode == this.craft) {
                    this.gameObject.transform.position =
                        referenceFrame.PlanetToFramePosition(this.craft.Position + (this.craft.Heading * this.VizzyGlObject.OriginOffset));
                } else if (craftNode.Parent == this.craft.Parent) {
                    // The two crafts are orbiting the same planet, so use planet position.
                    var craftToTargetCraftOffset = this.craft.Position - craftNode.Position;
                    this.gameObject.transform.position =
                        referenceFrame.PlanetToFramePosition(craftToTargetCraftOffset + (this.craft.Heading * this.VizzyGlObject.OriginOffset));
                } else {
                    var craftToTargetCraftOffset = this.craft.SolarPosition - craftNode.SolarPosition;
                    this.gameObject.transform.position =
                        referenceFrame.PlanetToFramePosition(craftToTargetCraftOffset + (this.craft.Heading * this.VizzyGlObject.OriginOffset));
                }

                this.gameObject.transform.localRotation = this.craft.Heading.ToQuaternion() * Quaternion.Euler(this.VizzyGlObject.Rotation);
            }
        }
    }
}
