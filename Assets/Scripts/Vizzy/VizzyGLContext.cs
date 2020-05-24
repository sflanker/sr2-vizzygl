using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using Assets.Scripts.Objects;
using Jundroo.ModTools.Serialization.Xml;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Attributes;
using UnityEngine;

namespace Assets.Scripts.Vizzy {
    public class VizzyGLContext : PartModifierScript<VizzyGLContextData>, IVizzyGLContext {
        public Vector3 Color {
            get => this.Data.Color;
            set => this.Data.Color = value;
        }

        public Single Opacity {
            get => this.Data.Opacity;
            set => this.Data.Opacity = value;
        }

        public Vector3 Scale {
            get => this.Data.Scale;
            set => this.Data.Scale = value;
        }

        public Vector3 Rotation {
            get => this.Data.Rotation;
            set => this.Data.Rotation = value;
        }

        public PositionType Origin {
            get => this.Data.Origin;
            set => this.Data.Origin = value;
        }

        public Int32 CraftId {
            get => this.Data.CraftId;
            set => this.Data.CraftId = value;
        }

        public String PlanetName {
            get => this.Data.PlanetName;
            set => this.Data.PlanetName = value;
        }

        public ViewType View {
            get => this.Data.View;
            set => this.Data.View = value;
        }

        public IReadOnlyDictionary<String, VizzyGLObject> Objects {
            get {
                lock (this.Data.Objects) {
                    return new ReadOnlyDictionary<String, VizzyGLObject>(
                        this.Data.Objects
                    );
                }
            }
        }

        public void AddObject(VizzyGLObject vizzyGlObject) {
            lock (this.Data.Objects) {
                if (this.Data.Objects.TryGetValue(vizzyGlObject.Name, out var existing)) {
                    // Replacing existing object
                    existing.DestroyGameObject();
                }

                vizzyGlObject.Color = this.Color;
                vizzyGlObject.Opacity = this.Opacity;
                vizzyGlObject.Scale = this.Scale;
                vizzyGlObject.Rotation = this.Rotation;

                vizzyGlObject.Initialize(this.Data.Part.PartScript.CraftScript);
                this.Data.Objects[vizzyGlObject.Name] = vizzyGlObject;
            }
        }

        public void RemoveObject(String objectName) {
            lock (this.Data.Objects) {
                if (this.Data.Objects.TryGetValue(objectName, out var existing)) {
                    existing.DestroyGameObject();
                    this.Data.Objects.Remove(objectName);
                } else {
                    Debug.Log($"Unable to remove object, not found: {objectName}");
                }
            }
        }

        public void DestroyScript() {
            // Unload objects
            lock (this.Data.Objects) {
                foreach (var obj in this.Data.Objects.Values) {
                    obj.DestroyGameObject();
                }
            }
        }

        protected override void OnInitialized() {
            lock (this.Data.Objects) {
                foreach (var obj in this.Data.Objects.Values) {
                    obj.Initialize(this.Data.Part.PartScript.CraftScript);
                }
            }

            base.OnInitialized();
        }

        public override void OnCraftLoaded(ICraftScript craftScript, Boolean movedToNewCraft) {
            base.OnCraftLoaded(craftScript, movedToNewCraft);

            if (movedToNewCraft && craftScript != null) {
                lock (this.Data.Objects) {
                    foreach (var obj in this.Data.Objects.Values) {
                        obj.SetCraft(craftScript);
                    }
                }
            }
        }
    }

    [Serializable]
    public class VizzyGLContextData : PartModifierData {
        private static UnityXmlSerializer _serializer = new UnityXmlSerializer(new UnityXmlSerializerContext(false, true));

        private VizzyGLContext _script;

        internal Dictionary<String, VizzyGLObject> Objects { get; } = new Dictionary<string, VizzyGLObject>();

        [SerializeField]
        [PartModifierProperty]
        private Vector3 _color = new Vector3(1, 0, 0);
        public Vector3 Color { get => this._color; set => this._color = value; }

        [SerializeField]
        [PartModifierProperty]
        private Single _opacity = 1.0f;
        public Single Opacity { get => this._opacity; set => this._opacity = value; }

        [SerializeField]
        [PartModifierProperty]
        private Vector3 _scale = new Vector3(1, 1, 1);
        public Vector3 Scale { get => this._scale; set => this._scale = value; }

        [SerializeField]
        [PartModifierProperty]
        private Vector3 _rotation = Vector3.zero;
        public Vector3 Rotation { get => this._rotation; set => this._rotation = value; }

        [SerializeField]
        [PartModifierProperty]
        private PositionType _origin = PositionType.CraftLocal;
        public PositionType Origin { get => this._origin; set => this._origin = value; }

        [SerializeField]
        [PartModifierProperty]
        private Int32 _craftId;
        public Int32 CraftId { get => this._craftId; set => this._craftId = value; }

        [SerializeField]
        [PartModifierProperty]
        private String _planetName;
        public String PlanetName { get => this._planetName; set => this._planetName = value; }

        [SerializeField]
        [PartModifierProperty]
        private ViewType _view = ViewType.Flight;
        public ViewType View { get => this._view; set => this._view = value; }

        // Used for deserialization
        public VizzyGLContextData() {
            Debug.Log("New Vizzy Context Created for Deserialization.");
            this.InspectorEnabled = false;
        }

        public VizzyGLContextData(PartData part) {
            Debug.Log("New VizzyGL Context Created from Part Data");
            this.Part = part;
            this.InspectorEnabled = false;
            // I think we can get away without DefaultXml
        }

        public override PartModifierScript CreateScript() {
            return this._script = new VizzyGLContext();
        }

        public override void DestroyScript() {
            if (this._script != null) {
                this._script.DestroyScript();
                this._script = null;
            }
        }

        public override PartModifierScript GetScript() {
            return this._script;
        }

        protected override void OnCreated(XElement partModifierXml) {
            base.OnCreated(partModifierXml);

            // load children
            var objectsElement = partModifierXml.Elements().SingleOrDefault(e => e.Name.LocalName == "VizzyGLObjects");
            if (objectsElement != null && objectsElement.HasElements) {
                var objectTypes =
                    typeof(VizzyGLObject).Assembly.GetTypes()
                        .Where(t => t.Namespace != null && t.Namespace.StartsWith(typeof(VizzyGLObject).Namespace ?? ""))
                        .Where(t => typeof(VizzyGLObject).IsAssignableFrom(t) && !t.IsAbstract)
                        .ToDictionary(t => t.Name);

                lock (this.Objects) {
                    foreach (var element in objectsElement.Elements()) {
                        var name = element.Name.LocalName;

                        if (objectTypes.TryGetValue(name, out var objectType)) {
                            var obj = (VizzyGLObject)_serializer.Deserialize(element, objectType);
                            this.Objects[obj.Name] = obj;
                        } else {
                            Debug.Log($"Unable to deserialize unrecognized VizzyGLObject type: '{name}'");
                        }
                    }
                }
            }
        }

        public override XElement GenerateStateXml(Boolean optimizeXml = true) {
            var element = base.GenerateStateXml(optimizeXml);
            var objects = new XElement("VizzyGLObjects");

            lock (this.Objects) {
                foreach (var obj in this.Objects.Values) {
                    objects.Add(_serializer.Serialize(obj));
                }
            }

            if (objects.HasElements) {
                element.Add(objects);
            }

            return element;
        }
    }
}
