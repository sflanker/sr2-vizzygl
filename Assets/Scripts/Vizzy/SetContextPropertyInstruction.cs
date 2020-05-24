using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Assets.Scripts.Objects;
using ModApi.Craft.Program;
using UnityEngine;

namespace Assets.Scripts.Vizzy {
    [Serializable]
    public class SetContextPropertyInstruction : VizzyGLInstructionBase {
        public const String XmlName = "SetContextProperty";

        private String _prop;
        public ContextProperty Property { get; private set; }

        [ProgramNodeProperty] private String _origin;
        public PositionType Origin { get; private set; }

        [ProgramNodeProperty] private String _view;
        public ViewType View { get; private set; }

        public override List<ListItemInfo> GetListItems(string listId) {
            switch (listId) {
                case "origin":
                    return new List<ListItemInfo> {
                        new ListItemInfo(
                            "craft",
                            "Craft PCI",
                            "Position objects relative to a craft with an offset in PCI coordinates.",
                            ListItemInfoType.Number),
                        new ListItemInfo(
                            "craft-local",
                            "Craft Local",
                            "Position objects relative to a craft with an offset in local coordinates.",
                            ListItemInfoType.Number),
                        new ListItemInfo(
                            "planet",
                            "Planet PCI",
                            "Position objects relative to a planet with an offset in PCI coordinates.",
                            ListItemInfoType.Text),
                        new ListItemInfo(
                            "planet-lat-long",
                            "Planet Lat/Long/AGL",
                            "Position objects relative to a planet with an offset in Lat/Long/AGL coordinates.",
                            ListItemInfoType.Text)
                    };
                case "view":
                    return new List<ListItemInfo> {
                        new ListItemInfo(
                            "flight",
                            "Flight",
                            "Draw objects in the flight view.",
                            ListItemInfoType.None),
                        new ListItemInfo(
                            "map",
                            "Map",
                            "Draw objects in the map view.",
                            ListItemInfoType.None)
                    };
                default:
                    Debug.Log($"Unrecognized list id: {listId}");
                    return new List<ListItemInfo>();
            }
        }

        /// <summary>Gets the selected value of the specified list.</summary>
        /// <param name="listId">The list identifier.</param>
        /// <returns>The currently selected value.</returns>
        public override String GetListValue(String listId) {
            switch (listId) {
                case "origin":
                    return this._origin;
                case "view":
                    return this._view;
                default:
                    return null;
            }
        }

        /// <summary>Sets the selected value of the specified list.</summary>
        /// <param name="listId">The list identifier.</param>
        /// <param name="value">The value to select.</param>
        public override void SetListValue(String listId, String value) {
            switch (this.Property) {
                case ContextProperty.Origin:
                    if (listId != "origin") {
                        Debug.Log($"Property<->ListId mismatch, expecting 'origin' but '{listId}' was specified.");
                    }
                    this._origin = value.ToLowerInvariant();
                    switch (this._origin) {
                        case "craft":
                            this.Origin = PositionType.CraftPCI;
                            break;
                        case "craft-local":
                            this.Origin = PositionType.CraftLocal;
                            break;
                        case "planet":
                            this.Origin = PositionType.PlanetPCI;
                            break;
                        case "planet-lat-long":
                            this.Origin = PositionType.PlanetLatLogAgl;
                            break;
                        default:
                            this.Origin = default;
                            break;
                    }

                    if (this.Origin == default) {
                        Debug.Log($"Unrecognized position type: {this._origin}");
                    }

                    break;
                case ContextProperty.View:
                    if (listId != "view") {
                        Debug.Log($"Property<->ListId mismatch, expecting 'view' but '{listId}' was specified.");
                    }
                    this._view = value.ToLowerInvariant();
                    switch (this._view) {
                        case "flight":
                            this.View = ViewType.Flight;
                            break;
                        case "map":
                            this.View = ViewType.Map;
                            break;
                        default:
                            this.View = default;
                            break;
                    }

                    if (this.View == default) {
                        Debug.Log($"Unrecognized view type: {this._view}");
                    }

                    break;
                default:
                    Debug.Log(
                        $"Unable to set value for list {listId} for property type {this.Property}.");
                    break;
            }
        }

        public override void OnDeserialized(XElement xml) {
            base.OnDeserialized(xml);

            this._prop = xml.Attribute("prop")?.Value;
            if (!String.IsNullOrEmpty(this._prop)) {
                this.Property =
                    (ContextProperty)Enum.Parse(typeof(ContextProperty), this._prop, ignoreCase: true);
            }

            switch (this.Property) {
                case ContextProperty.Origin when this.Origin == default:
                    Debug.Log("Manually deserializing _origin");
                    this.SetListValue("origin", this._origin);
                    break;
                case ContextProperty.View when this.View == default:
                    Debug.Log("Manually deserializing _view");
                    this.SetListValue("view", this._view);
                    break;
            }
        }

        public override void OnSerialized(XElement xml) {
            base.OnSerialized(xml);

            xml.SetAttributeValue("prop", this._prop);
        }

        protected override void ExecuteImpl(IThreadContext context) {
            switch (this.Property) {
                case ContextProperty.Color:
                    this.DrawingContext.Color =
                        this.GetExpression(0).Evaluate(context).VectorValue.ToVector3();
                    Debug.Log($"VizzyGL Drawing Color Set To: {this.DrawingContext.Color}");
                    break;
                case ContextProperty.Opacity:
                    this.DrawingContext.Opacity =
                        (Single)this.GetExpression(0).Evaluate(context).NumberValue;
                    Debug.Log($"VizzyGL Drawing Opacity Set To: {this.DrawingContext.Opacity}");
                    break;
                case ContextProperty.Scale:
                    this.DrawingContext.Scale =
                        this.GetExpression(0).Evaluate(context).VectorValue.ToVector3();
                    Debug.Log($"VizzyGL Drawing Scale Set To: {this.DrawingContext.Scale}");
                    break;
                case ContextProperty.Rotation:
                    this.DrawingContext.Rotation =
                        this.GetExpression(0).Evaluate(context).VectorValue.ToVector3();
                    Debug.Log($"VizzyGL Drawing Rotation Set To: {this.DrawingContext.Rotation}");
                    break;
                case ContextProperty.Origin when this.Origin != default:
                    this.DrawingContext.Origin = this.Origin;
                    switch (this.Origin) {
                        case PositionType.CraftLocal:
                        case PositionType.CraftPCI:
                            this.DrawingContext.CraftId =
                                (Int32)this.GetExpression(0).Evaluate(context).NumberValue;
                            Debug.Log($"VizzyGL Drawing Origin Set To: {this.DrawingContext.Origin} - Craft {this.DrawingContext.CraftId}");
                            break;
                        case PositionType.PlanetPCI:
                        case PositionType.PlanetLatLogAgl:
                            this.DrawingContext.PlanetName =
                                this.GetExpression(0).Evaluate(context).TextValue;
                            Debug.Log($"VizzyGL Drawing Origin Set To: {this.DrawingContext.Origin} - Planet {this.DrawingContext.PlanetName}");
                            break;
                    }

                    break;
                case ContextProperty.View when this.View != default:
                    this.DrawingContext.View = this.View;
                    break;
            }
        }
    }

    public enum ContextProperty {
        Color = 1,
        Opacity,
        Scale,
        Rotation,
        Origin,
        View
    }
}
