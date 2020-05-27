using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Assets.Scripts.Objects;
using ModApi.Craft.Program;
using UnityEngine;

namespace Assets.Scripts.Vizzy {
    [Serializable]
    public class DrawPrimitiveInstruction : VizzyGLInstructionBase {
        public const String XmlName = "DrawPrimitive";

        [ProgramNodeProperty]
        private String _type;
        public PrimitiveType Type { get; private set; }

        public override List<ListItemInfo> GetListItems(string listId) {
            return new List<ListItemInfo> {
                new ListItemInfo(
                    "sphere",
                    "Sphere",
                    "Draw a sphere.",
                    ListItemInfoType.None),
                new ListItemInfo(
                    "cube",
                    "Cube",
                    "Draw a cube.",
                    ListItemInfoType.None),
                new ListItemInfo(
                    "cylinder",
                    "Cylinder",
                    "Draw a cylinder.",
                    ListItemInfoType.None)
            };
        }

        /// <summary>Gets the selected value of the specified list.</summary>
        /// <param name="listId">The list identifier.</param>
        /// <returns>The currently selected value.</returns>
        public override string GetListValue(string listId) {
            return this._type;
        }

        /// <summary>Sets the selected value of the specified list.</summary>
        /// <param name="listId">The list identifier.</param>
        /// <param name="value">The value to select.</param>
        public override void SetListValue(String listId, String value) {
            this._type = value;
            if (Enum.TryParse<PrimitiveType>(value, ignoreCase: true, out var primType)) {
                this.Type = primType;
            } else {
                this.Type = default;
                Debug.LogWarning($"Unrecognized primitive type: {value}");
            }
        }

        public override void OnDeserialized(XElement xml) {
            base.OnDeserialized(xml);
            this.SetListValue("type", this._type);
        }

        protected override void ExecuteImpl(IThreadContext context) {
            VizzyGLPrimitive vizzyGlObject;
            switch (this.DrawingContext.Origin) {
                case PositionType.CraftLocal:
                case PositionType.CraftPCI:
                    vizzyGlObject =
                        new VizzyGLPrimitive(
                            this.Type,
                            this.GetExpression(1).Evaluate(context).TextValue ?? String.Empty,
                            this.DrawingContext.View,
                            this.DrawingContext.Origin,
                            this.DrawingContext.CraftId);
                    break;
                case PositionType.PlanetPCI:
                case PositionType.PlanetLatLogAsl:
                    vizzyGlObject =
                        new VizzyGLPrimitive(
                            this.Type,
                            this.GetExpression(1).Evaluate(context).TextValue ?? String.Empty,
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
