using ModApi.Flight.Sim;
using UnityEngine;

namespace Assets.Scripts.Behaviours {
    public class PlanetLatLogAglPositionBehaviour : OriginOffsetPositionBehaviour {
        private IPlanetNode parent;

        protected Vector3d OriginOffsetSurfaceVector {
            get {
                var latLongAgl = this.VizzyGlObject.OriginOffset;
                return this.parent.GetSurfacePosition(
                    // What 0.01745329 ???
                    latLongAgl.x * 0.01745329,
                    latLongAgl.y * 0.01745329,
                    AltitudeType.AboveSeaLevel,
                    latLongAgl.z);
            }
        }

        protected override void OnInitialized() {
            base.OnInitialized();
            this.parent = this.VizzyGlObject.GetOriginNode() as IPlanetNode;
        }

        private int i = 0;

        protected override void OnUpdate() {
            if (this.parent != null) {
                var flightScene = Game.Instance.FlightScene;
                var referenceFrame = flightScene.ViewManager.GameView.ReferenceFrame;

                // TODO: handle case where our offset vector is too big for a Vector3 (in this
                // case we probably should not be drawing the object).
                var craftNode = flightScene.CraftNode;
                if (craftNode.Parent == this.parent) {
                    this.gameObject.transform.position =
                        referenceFrame.PlanetToFramePosition(this.parent.SurfaceVectorToPlanetVector(this.OriginOffsetSurfaceVector));
                    if (++i % 200 == 0) {
                        Debug.Log(
                            $"Updating object position based on current planet lat/long/asl: {this.parent.SurfaceVectorToPlanetVector(this.OriginOffsetSurfaceVector)}");
                        Debug.Log($"Current craft position: {craftNode.Position}");
                    }
                } else {
                    var craftParentToTargetParentOffset =
                        this.parent.Parent == craftNode.Parent ?
                            // Our target is orbiting a moon of our parent
                            this.parent.Position :
                            // Catch all for targets orbiting other planetary bodies
                            this.parent.SolarPosition - craftNode.Parent.SolarPosition;
                    this.gameObject.transform.position =
                        referenceFrame.PlanetToFramePosition(
                            craftParentToTargetParentOffset +
                            this.parent.SurfaceVectorToPlanetVector(this.OriginOffsetSurfaceVector)
                        );

                    if (++i % 200 == 0) {
                        Debug.Log("WTF");
                    }
                }
            } else if (++i % 200 == 0) {
                Debug.Log("parent unavailable");
            }
        }
    }
}
