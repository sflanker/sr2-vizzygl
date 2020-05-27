using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using Assets.Scripts.Flight.MapView.Interfaces;
using Assets.Scripts.Objects;
using Jundroo.ModTools;
using Jundroo.ModTools.Serialization.Xml;
using ModApi.Craft;
using ModApi.Craft.Parts;
using ModApi.Craft.Parts.Attributes;
using ModApi.Flight;
using ModApi.Mods;
using ModApi.Scenes.Events;
using UnityEngine;
using Object = System.Object;

namespace Assets.Scripts.Vizzy {
    public class VizzyGLContext : PartModifierScript<VizzyGLContextData>, IVizzyGLContext {
        public static IMapViewCoordinateConverter MapViewCoordinateConverter { get; private set; }
        public static IObjectContainerProvider ObjectContainerProvider { get; private set; }

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

        public static ViewType GetCurrentView() {
            return Game.InFlightScene ?
                (Game.Instance.FlightScene.ViewManager?.MapViewManager?.MapView?.Visible == true ?
                    ViewType.Map :
                    ViewType.Game) :
                default;
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

                Debug.Log($"Adding Object '{vizzyGlObject.Name}' (in current view? {this.View == GetCurrentView()})");
                vizzyGlObject.Initialize(this.Data.Part.PartScript.CraftScript, this.View == GetCurrentView());
                this.Data.Objects[vizzyGlObject.Name] = vizzyGlObject;
            }
        }

        public void RemoveObject(String objectName) {
            lock (this.Data.Objects) {
                if (this.Data.Objects.TryGetValue(objectName, out var existing)) {
                    existing.DestroyGameObject();
                    this.Data.Objects.Remove(objectName);
                } else {
                    Debug.LogWarning($"Unable to remove object, not found: {objectName}");
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

        protected override void OnInitialized() {
            Debug.Log($"VizzyGL Context {this.GetHashCode()} Initialized with data {this.Data.GetHashCode()}");
            Game.Instance.SceneManager.SceneLoaded += SceneManagerOnSceneLoaded;
            Game.Instance.SceneManager.SceneUnloaded += SceneManagerOnSceneUnloaded;

            lock (this.Data.Objects) {
                foreach (var obj in this.Data.Objects.Values) {
                    obj.Initialize(this.PartScript.CraftScript, false);
                }
            }

            if (Game.InFlightScene) {
                var flightScene = Game.Instance.FlightScene;
                if (flightScene.ViewManager?.MapViewManager != null) {
                    flightScene.ViewManager.MapViewManager.ForegroundStateChanged += OnCurrentViewChanges;

                    var mapView = flightScene.ViewManager.MapViewManager.IsInForeground;
                    if (mapView) {
                        MapViewCoordinateConverter =
                            (IMapViewCoordinateConverter)Game.Instance.FlightScene.ViewManager.MapViewManager.MapView;
                        ObjectContainerProvider =
                            (IObjectContainerProvider)Game.Instance.FlightScene.ViewManager.MapViewManager.MapView;
                    }

                    OnCurrentViewChanges(mapView);
                } else {
                    Debug.Log($"FlightScene Initialization Incomplete ({this.GetHashCode()}). Waiting...");
                    flightScene.Initialized += FlightSceneOnInitialized;
                }
            }
        }

        private void FlightSceneOnInitialized(IFlightScene flightScene) {
            Debug.Log($"FlightScene Initialization Complete! {this.GetHashCode()}");
            flightScene.Initialized -= FlightSceneOnInitialized;
            flightScene.ViewManager.MapViewManager.ForegroundStateChanged += OnCurrentViewChanges;

            var mapView = flightScene.ViewManager.MapViewManager.IsInForeground;
            if (mapView) {
                MapViewCoordinateConverter =
                    (IMapViewCoordinateConverter)Game.Instance.FlightScene.ViewManager.MapViewManager.MapView;
                ObjectContainerProvider =
                    (IObjectContainerProvider)Game.Instance.FlightScene.ViewManager.MapViewManager.MapView;
            }

            OnCurrentViewChanges(mapView);
        }

        protected override void OnDisposed() {
            Debug.Log($"VizzyGLContext.OnDisposed {this.GetHashCode()}");
            if (this.Data != null) {
                foreach (var obj in this.Data.Objects.Values) {
                    obj.DestroyGameObject();
                }
            }

            if (Game.Instance.SceneManager != null) {
                Game.Instance.SceneManager.SceneLoaded -= SceneManagerOnSceneLoaded;
                Game.Instance.SceneManager.SceneUnloaded -= SceneManagerOnSceneUnloaded;
            }
            if (Game.Instance.FlightScene != null) {
                Game.Instance.FlightScene.Initialized -= FlightSceneOnInitialized;
            }

            if (this.PartScript != null) {
                Debug.Log($"Removing {nameof(VizzyGLContext)} from {this.PartScript.Data.Name}.");
                this.PartScript.Modifiers.Remove(this);
            }

            base.OnDisposed();
        }

        private void SceneManagerOnSceneLoaded(Object sender, SceneEventArgs e) {
            Debug.Log($"Scene Loaded (InFlightScene: {Game.InFlightScene}). {this.GetHashCode()}");
            if (Game.InFlightScene) {
                Game.Instance.FlightScene.ViewManager.MapViewManager.ForegroundStateChanged += OnCurrentViewChanges;

                MapViewCoordinateConverter =
                    (IMapViewCoordinateConverter)Game.Instance.FlightScene.ViewManager.MapViewManager.MapView;
                ObjectContainerProvider =
                    (IObjectContainerProvider)Game.Instance.FlightScene.ViewManager.MapViewManager.MapView;
            }
        }

        private void SceneManagerOnSceneUnloaded(Object sender, SceneEventArgs e) {
            Debug.Log($"Scene Unloaded. Deleting VizzyGL Game Objects. {this.GetHashCode()}");
            try {
                foreach (var obj in this.Data.Objects.Values) {
                    obj.DestroyGameObject();
                }
            } catch (Exception ex) {
                Debug.LogWarning($"An error occurred deleting VizzyGL Game Objects: {ex.Message}");
                Debug.LogException(ex);
            }

            Game.Instance.SceneManager.SceneLoaded -= SceneManagerOnSceneLoaded;
            Game.Instance.SceneManager.SceneUnloaded -= SceneManagerOnSceneUnloaded;
            Game.Instance.FlightScene.Initialized -= FlightSceneOnInitialized;

            if (Game.Instance.FlightScene?.ViewManager?.MapViewManager != null) {
                Game.Instance.FlightScene.ViewManager.MapViewManager.ForegroundStateChanged -= OnCurrentViewChanges;
            }
        }

        private void OnCurrentViewChanges(Boolean inMapView) {
            var currentView = GetCurrentView();
            if (currentView == ViewType.Map) {
                MapViewCoordinateConverter =
                    (IMapViewCoordinateConverter)Game.Instance.FlightScene.ViewManager.MapViewManager.MapView;
                ObjectContainerProvider =
                    (IObjectContainerProvider)Game.Instance.FlightScene.ViewManager.MapViewManager.MapView;
            }

            Debug.Log($"Current View Changed: {currentView}");
            lock (this.Data.Objects) {
                Debug.Log($"Initializing {this.Data.Objects.Values.Count(o => o.View == currentView)} objects for {currentView} view. {this.GetHashCode()}");
                foreach (var obj in this.Data.Objects.Values) {
                    if (obj.View == currentView) {
                        obj.InitializeGameObject();
                    } else {
                        obj.DestroyGameObject();
                    }
                }
            }
        }
    }

    [Serializable]
    public sealed class VizzyGLContextData : PartModifierData<VizzyGLContext> {
        private static UnityXmlSerializer _serializer = new UnityXmlSerializer(new UnityXmlSerializerContext(false, true));

        internal Dictionary<String, VizzyGLObject> Objects { get; } = new Dictionary<String, VizzyGLObject>();

        [SerializeField] [PartModifierProperty]
        private Vector3 _color = new Vector3(1, 0, 0);

        public Vector3 Color {
            get => this._color;
            set => this._color = value;
        }

        [SerializeField] [PartModifierProperty]
        private Single _opacity = 1.0f;

        public Single Opacity {
            get => this._opacity;
            set => this._opacity = value;
        }

        [SerializeField] [PartModifierProperty]
        private Vector3 _scale = new Vector3(1, 1, 1);

        public Vector3 Scale {
            get => this._scale;
            set => this._scale = value;
        }

        [SerializeField] [PartModifierProperty]
        private Vector3 _rotation = Vector3.zero;

        public Vector3 Rotation {
            get => this._rotation;
            set => this._rotation = value;
        }

        [SerializeField] [PartModifierProperty]
        private PositionType _origin = PositionType.CraftLocal;

        public PositionType Origin {
            get => this._origin;
            set => this._origin = value;
        }

        [SerializeField] [PartModifierProperty]
        private Int32 _craftId;

        public Int32 CraftId {
            get => this._craftId;
            set => this._craftId = value;
        }

        [SerializeField] [PartModifierProperty]
        private String _planetName;

        public String PlanetName {
            get => this._planetName;
            set => this._planetName = value;
        }

        [SerializeField] [PartModifierProperty]
        private ViewType _view = ViewType.Game;

        public ViewType View {
            get => this._view;
            set => this._view = value;
        }

        // Used for deserialization
        public VizzyGLContextData() {
            this.InspectorEnabled = false;
        }

        public VizzyGLContextData(PartData part) {
            this.Part = part;
            this.InspectorEnabled = false;
            // I think we can get away without DefaultXml
        }

        protected override VizzyGLContext CreateScriptComponent(IPartScript partScript) {
            var script = base.CreateScriptComponent(partScript);
            Debug.Log($"VizzyGL Context Data {this.GetHashCode()} Creating Script {script.GetHashCode()}");
            return script;
        }

        public override void GetModRequirements(AddModRequirementDelegate addModRequirement) {
            base.GetModRequirements(addModRequirement);
            addModRequirement(Assets.Scripts.Mod.Instance.ModInfo, true);
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

        public override void RestoreFromState(XElement stateElement, bool restoreAll) {
            base.RestoreFromState(stateElement, restoreAll);

            // load children
            var objectsElement = stateElement.Elements().SingleOrDefault(e => e.Name.LocalName == "VizzyGLObjects");
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
                            Debug.LogWarning($"Unable to deserialize unrecognized VizzyGLObject type: '{name}'");
                        }
                    }
                }
            }
        }
    }
}
