using System;
using System.Collections.Generic;
using System.Xml.Linq;
using ModApi.Craft.Program;
using UnityEngine;

namespace Assets.Scripts.Vizzy {
    [Serializable]
    public class UpdateObjectInstruction : VizzyGLInstructionBase {
        public const String XmlName = "UpdateObject";

        [ProgramNodeProperty]
        private String _prop;
        public MutablePrimitiveProperty Property { get; private set; }

        public override List<ListItemInfo> GetListItems(string listId) {
            return new List<ListItemInfo> {
                new ListItemInfo(
                    "color",
                    "Color",
                    "Update the primary color of the object.",
                    ListItemInfoType.Vector),
                new ListItemInfo(
                    "opacity",
                    "Opacity",
                    "Update the opacity of the object (0 - transparent, 1 - opaque).",
                    ListItemInfoType.Number),
                new ListItemInfo(
                    "scale",
                    "Scale",
                    "Update the scale of the object.",
                    ListItemInfoType.Vector),
                new ListItemInfo(
                    "rotation",
                    "Rotation",
                    "Update the rotation of the object (as Euler Angles).",
                    ListItemInfoType.Vector),
                new ListItemInfo(
                    "origin-offset",
                    "Origin Offset",
                    "Update the offset of the object's origin from its frame of reference.",
                    ListItemInfoType.Vector)
            };
        }

        /// <summary>Gets the selected value of the specified list.</summary>
        /// <param name="listId">The list identifier.</param>
        /// <returns>The currently selected value.</returns>
        public override String GetListValue(string listId) {
            return this._prop;
        }

        /// <summary>Sets the selected value of the specified list.</summary>
        /// <param name="listId">The list identifier.</param>
        /// <param name="value">The value to select.</param>
        public override void SetListValue(String listId, String value) {
            this._prop = value.ToLowerInvariant();
            switch (this._prop) {
                case "color":
                    this.Property = MutablePrimitiveProperty.Color;
                    break;
                case "opacity":
                    this.Property = MutablePrimitiveProperty.Opacity;
                    break;
                case "scale":
                    this.Property = MutablePrimitiveProperty.Scale;
                    break;
                case "rotation":
                    this.Property = MutablePrimitiveProperty.Rotation;
                    break;
                case "origin-offset":
                    this.Property = MutablePrimitiveProperty.OriginOffset;
                    break;
                default:
                    Debug.Log($"Unrecognized object property: {this._prop}");
                    this.Property = default;
                    break;
            }
        }

        public override void OnDeserialized(XElement xml) {
            base.OnDeserialized(xml);
            this.SetListValue("prop", this._prop);
        }

        protected override void ExecuteImpl(IThreadContext context) {
            var name = this.GetExpression(0).Evaluate(context).TextValue;
            var valueResult = this.GetExpression(1).Evaluate(context);
            if (this.DrawingContext.Objects.TryGetValue(name, out var updateObject)) {
                switch (this.Property) {
                    case MutablePrimitiveProperty.Color:
                        updateObject.Color = valueResult.VectorValue.ToVector3();
                        Debug.Log($"Updated {name}.Color to {updateObject.Color}");
                        break;
                    case MutablePrimitiveProperty.Opacity:
                        updateObject.Opacity = (Single)valueResult.NumberValue;
                        Debug.Log($"Updated {name}.Opacity to {updateObject.Opacity}");
                        break;
                    case MutablePrimitiveProperty.Scale:
                        updateObject.Scale = valueResult.VectorValue.ToVector3();
                        Debug.Log($"Updated {name}.Scale to {updateObject.Scale}");
                        break;
                    case MutablePrimitiveProperty.Rotation:
                        updateObject.Rotation = valueResult.VectorValue.ToVector3();
                        Debug.Log($"Updated {name}.Rotation to {updateObject.Rotation}");
                        break;
                    case MutablePrimitiveProperty.OriginOffset:
                        updateObject.OriginOffset = valueResult.VectorValue;
                        Debug.Log($"Updated {name}.OriginOffset to {updateObject.OriginOffset}");
                        break;
                }
            } else {
                Debug.Log($"VizzyGL Object not found: '{name}'");
            }
        }
    }

    public enum MutablePrimitiveProperty {
        Color = 1,
        Opacity,
        Scale,
        Rotation,
        OriginOffset
    }
}
