using Assets.Scripts.Objects;
using Assets.Scripts.Vizzy;
using ModApi.Flight.Sim;

namespace Assets.Scripts.Behaviours {
    public class PCIPositionBehaviour : OriginOffsetPositionBehaviour {
        private IOrbitNode parent;

        protected override void OnInitialized() {
            base.OnInitialized();
            this.parent = this.VizzyGlObject.GetOriginNode();
        }

        protected override void OnUpdate() {
            var flightScene = Game.Instance.FlightScene;
            if (this.parent != null) {
                // TODO: handle case where our offset vector is too big for a Vector3 (in this
                // case we probably should not be drawing the object).
                switch (this.VizzyGlObject.View) {
                    case ViewType.Game when VizzyGLContext.GetCurrentView() == ViewType.Game:
                        var craftNode = flightScene.CraftNode;
                        var referenceFrame = flightScene.ViewManager.GameView.ReferenceFrame;
                        if (this.parent.Parent == craftNode.Parent) {
                            this.gameObject.transform.position =
                                referenceFrame.PlanetToFramePosition(this.parent.Position + this.VizzyGlObject.OriginOffset);
                        } else if (craftNode.Parent == this.parent) {
                            this.gameObject.transform.position =
                                referenceFrame.PlanetToFramePosition(this.VizzyGlObject.OriginOffset);
                        } else {
                            var craftParentToTargetParentOffset = this.parent.SolarPosition - craftNode.Parent.SolarPosition;
                            this.gameObject.transform.position =
                                referenceFrame.PlanetToFramePosition(craftParentToTargetParentOffset + this.VizzyGlObject.OriginOffset);
                        }

                        break;
                    case ViewType.Map when VizzyGLContext.GetCurrentView() == ViewType.Map:
                        this.gameObject.transform.position =
                            VizzyGLContext.MapViewCoordinateConverter
                                .ConvertSolarToMapView(
                                    this.parent.SolarPosition + this.VizzyGlObject.OriginOffset)
                                .ToVector3();

                        break;
                }
            }
        }
    }
}
