using System;
using Assets.Scripts.Objects;
using Assets.Scripts.Vizzy;
using ModApi.Flight.Sim;
using UnityEngine;

namespace Assets.Scripts.Behaviours {
    public class PlanetLatLogAslPositionBehaviour : OriginOffsetPositionBehaviour {
        private IPlanetNode parent;

        protected Vector3d OriginOffsetPlanetVector {
            get {
                var latLongAsl = this.VizzyGlObject.OriginOffset;
                return this.parent.SurfaceVectorToPlanetVector(
                    this.parent.GetSurfacePosition(
                        // What 0.01745329 ???
                        latLongAsl.x * 0.01745329,
                        latLongAsl.y * 0.01745329,
                        AltitudeType.AboveSeaLevel,
                        latLongAsl.z)
                );
            }
        }

        protected override void OnInitialized() {
            base.OnInitialized();
            this.parent = this.VizzyGlObject.GetOriginNode() as IPlanetNode;
        }

        protected override void OnUpdate() {
            if (this.parent != null) {
                var flightScene = Game.Instance.FlightScene;

                // TODO: handle case where our offset vector is too big for a Vector3 (in this
                // case we probably should not be drawing the object).
                switch (this.VizzyGlObject.View) {
                    case ViewType.Game when VizzyGLContext.GetCurrentView() == ViewType.Game: {
                        var referenceFrame = flightScene.ViewManager.GameView.ReferenceFrame;

                        var craftNode = flightScene.CraftNode;
                        if (craftNode.Parent == this.parent) {
                            this.gameObject.transform.position =
                                referenceFrame.PlanetToFramePosition(this.OriginOffsetPlanetVector);
                        } else {
                            var craftParentToTargetParentOffset =
                                this.parent.Parent == craftNode.Parent ?
                                    // Our target is orbiting a moon of our parent
                                    this.parent.Position :
                                    // Catch all for targets orbiting other planetary bodies
                                    this.parent.SolarPosition - craftNode.Parent.SolarPosition;
                            this.gameObject.transform.position =
                                referenceFrame.PlanetToFramePosition(
                                    craftParentToTargetParentOffset + this.OriginOffsetPlanetVector
                                );
                        }

                        break;
                    }
                    case ViewType.Map when VizzyGLContext.GetCurrentView() == ViewType.Map: {
                        if (VizzyGLContext.MapViewCoordinateConverter != null) {
                            // Determine the solar position
                            this.gameObject.transform.position =
                                VizzyGLContext.MapViewCoordinateConverter
                                    .ConvertSolarToMapView(this.parent.SolarPosition + this.OriginOffsetPlanetVector)
                                    .ToVector3();
                        }

                        break;
                    }
                }
            }
        }
    }
}
