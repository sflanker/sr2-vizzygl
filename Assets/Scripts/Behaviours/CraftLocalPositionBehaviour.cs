using System;
using Assets.Scripts.Objects;
using Assets.Scripts.Vizzy;
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
                // TODO: handle case where our offset vector is too big for a Vector3 (in this
                // case we probably should not be drawing the object).
                switch (this.VizzyGlObject.View) {
                    case ViewType.Game when VizzyGLContext.GetCurrentView() == ViewType.Game:
                        var referenceFrame = flightScene.ViewManager.GameView.ReferenceFrame;
                        var craftNode = flightScene.CraftNode;
                        if (craftNode == this.craft || craftNode.Parent == this.craft.Parent) {
                            this.gameObject.transform.position =
                                referenceFrame.PlanetToFramePosition(this.craft.Position + (this.craft.Heading * this.VizzyGlObject.OriginOffset));
                        } else {
                            var craftParentToCraftParentOffset = this.craft.SolarPosition - craftNode.Parent.SolarPosition;
                            this.gameObject.transform.position =
                                referenceFrame.PlanetToFramePosition(craftParentToCraftParentOffset + (this.craft.Heading * this.VizzyGlObject.OriginOffset));
                        }

                        break;
                    case ViewType.Map when VizzyGLContext.GetCurrentView() == ViewType.Map:
                        this.gameObject.transform.position =
                            VizzyGLContext.MapViewCoordinateConverter
                                .ConvertSolarToMapView(
                                    this.craft.SolarPosition + (this.craft.Heading * this.VizzyGlObject.OriginOffset))
                                .ToVector3();

                        break;

                    default:
                        Debug.Log($"Skipping update to object '{this.VizzyGlObject.Name}' not in current view ({VizzyGLContext.GetCurrentView()})");
                        break;
                }

                this.gameObject.transform.localRotation = this.craft.Heading.ToQuaternion() * Quaternion.Euler(this.VizzyGlObject.Rotation);
            }
        }
    }
}
