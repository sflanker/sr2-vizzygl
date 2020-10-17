using System;
using System.Linq;
using System.Xml.Linq;
using Assets.Scripts.Behaviours;
using Assets.Scripts.Flight.MapView;
using Assets.Scripts.Vizzy;
using JetBrains.Annotations;
using ModApi.Craft;
using ModApi.Flight.Sim;
using UnityEngine;

namespace Assets.Scripts.Objects {
    [Serializable]
    public abstract class VizzyGLObject {
        private static Material DefaultMaterial =
            ModApi.Common.Game.Instance.UserInterface.ResourceDatabase.GetResource<Material>("VizzyGL/DefaultMaterial");

        [SerializeField] private Vector3 _color;

        [SerializeField] private Single _opacity;

        [SerializeField] private Vector3 _scale;

        [SerializeField] private Vector3 _rotation;

        [SerializeField] private String _name;
        public String Name => this._name;

        [SerializeField] private PositionType _originPositionType;
        public PositionType OriginPositionType => this._originPositionType;

        [SerializeField] private Vector3d _originOffset;

        [SerializeField] private String _originPlanetName;
        [SerializeField] private Int32 _originCraftId;

        [SerializeField] private ViewType _view;
        public ViewType View => this._view;

        public GameObject GameObject { get; private set; }
        public ICraftScript Craft { get; private set; }
        protected Renderer Renderer { get; private set; }

        protected virtual Boolean SupportsColor => true;

        public event EventHandler<OffsetChangedEventArgs> OriginOffsetChanged;

        public Vector3 Color {
            get => this._color;
            set {
                if (this._color != value) {
                    this._color = value;
                    this.OnColorChanged();
                }
            }
        }

        public Single Opacity {
            get => this._opacity;
            set {
                if (this._opacity != value) {
                    this._opacity = value;
                    this.OnOpacityChanged();
                }
            }
        }

        public Vector3d OriginOffset {
            get => this._originOffset;
            set {
                if (this._originOffset != value) {
                    this._originOffset = value;
                    this.OnOriginOffsetChanged();
                }
            }
        }

        public Vector3 Scale {
            get => this._scale;
            set {
                if (this._scale != value) {
                    this._scale = value;
                    this.OnScaleChanged();
                }
            }
        }

        public Vector3 Rotation {
            get => this._rotation;
            set {
                if (this._rotation != value) {
                    this._rotation = value;
                    this.OnRotationChanged();
                }
            }
        }

        // For deserialization
        protected VizzyGLObject() {
        }

        protected VizzyGLObject(
            String name,
            ViewType view,
            PositionType originPositionType,
            String originPlanetName) {
            this._name = name;
            this._view = view;
            this._originPositionType = originPositionType;
            this._originPlanetName = originPlanetName;
        }

        protected VizzyGLObject(
            String name,
            ViewType view,
            PositionType originPositionType,
            Int32 originCraftId) {
            this._name = name;
            this._view = view;
            this._originPositionType = originPositionType;
            this._originCraftId = originCraftId;
        }

        public void Initialize([NotNull] ICraftScript craft, Boolean createGameObject) {
            this.Craft = craft ?? throw new ArgumentNullException(nameof(craft));

            if (createGameObject) {
                this.InitializeGameObject();
            }
        }

        public void SetCraft([NotNull] ICraftScript craft) {
            this.Craft = craft ?? throw new ArgumentNullException(nameof(craft));
        }

        public void InitializeGameObject() {
            if (this.Craft == null) {
                Debug.LogError($"Unable to create {nameof(UnityEngine.GameObject)} for uninitialized {this.GetType().Name}. Name: '{this.Name}'");
            } else if(this.GameObject == null) {
                this.GameObject = this.CreateGameObject();
                if (this.GameObject != null) {
                    this.Renderer = this.GameObject.GetComponent<Renderer>();
                    this.OnGameObjectCreated();
                }
            }
        }

        public void DestroyGameObject() {
            if (this.GameObject != null) {
                Debug.Log($"Destroying GameObject for VizzyGLObject {this.Name}");
                UnityEngine.Object.Destroy(this.GameObject);
                this.GameObject = null;
            }
        }

        protected internal virtual void OnDeserialized(XElement element) {
        }

        protected virtual void OnGameObjectCreated() {
            Debug.Log($"VizzyGL Object Initialized (Origin: {this._originPositionType} + {this._originOffset})");

            if (this.SupportsColor) {
                this.Renderer.material = new Material(DefaultMaterial) {
                    color = new Color(
                        this._color.x,
                        this._color.y,
                        this._color.z,
                        this._opacity
                    )
                };
            }

            if (this.View == ViewType.Map) {
                this.GameObject.layer = VizzyGLContext.ObjectContainerProvider.Crafts.gameObject.layer;
            }

            this.GameObject.transform.localScale = this._scale;
            this.GameObject.transform.localRotation = Quaternion.Euler(this._rotation);

            // Attach positioning behavior
            this.AttachBehaviors();
        }

        protected virtual void AttachBehaviors() {
            OriginOffsetPositionBehaviour behavior;

            switch (this._originPositionType) {
                case PositionType.CraftLocal:
                    behavior = this.GameObject.AddComponent<CraftLocalPositionBehaviour>();
                    break;
                case PositionType.CraftPCI:
                case PositionType.PlanetPCI:
                    behavior = this.GameObject.AddComponent<PCIPositionBehaviour>();
                    break;
                case PositionType.PlanetLatLonAsl:
                    behavior = this.GameObject.AddComponent<PlanetLatLonAslPositionBehaviour>();
                    break;
                case PositionType.PlanetLatLonAgl:
                    Debug.Log($"Attaching LatLonAglPositionBehaviour to {this._name}");
                    behavior = this.GameObject.AddComponent<PlanetLatLonAglPositionBehaviour>();
                    break;
                default:
                    Debug.LogWarning($"Unrecognized origin type: {this._originPositionType}");
                    return;
            }

            behavior.Initialize(this);
        }

        protected virtual void OnColorChanged() {
            if (this.GameObject != null) {
                this.Renderer.material.color = new Color(
                    this._color.x,
                    this._color.y,
                    this._color.z,
                    this._opacity
                );
            }
        }

        protected virtual void OnOpacityChanged() {
            if (this.GameObject != null) {
                this.Renderer.material.color = new Color(
                    this._color.x,
                    this._color.y,
                    this._color.z,
                    this._opacity
                );
            }
        }

        protected virtual void OnOriginOffsetChanged() {
            this.OriginOffsetChanged?.Invoke(this, new OffsetChangedEventArgs(this._originOffset));
        }

        protected virtual void OnScaleChanged() {
            if (this.GameObject != null) {
                this.GameObject.transform.localScale = this._scale;
            }
        }

        protected virtual void OnRotationChanged() {
            if (this.GameObject != null) {
                this.GameObject.transform.localRotation = Quaternion.Euler(this._rotation);
            }
        }

        protected abstract GameObject CreateGameObject();

        public IOrbitNode GetOriginNode() {
            switch (this._originPositionType) {
                case PositionType.CraftLocal:
                case PositionType.CraftPCI:
                    return Game.Instance.FlightScene.FlightState.CraftNodes.SingleOrDefault(c => c.NodeId == this._originCraftId);
                case PositionType.PlanetPCI:
                case PositionType.PlanetLatLonAsl:
                case PositionType.PlanetLatLonAgl:
                    return Game.Instance.FlightScene.FlightState.RootNode.FindPlanet(this._originPlanetName);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }

    public class OffsetChangedEventArgs : EventArgs {
        public Vector3d NewOffset { get; }

        public OffsetChangedEventArgs(Vector3d newOffset) {
            this.NewOffset = newOffset;
        }
    }

    public enum ViewType {
        Game = 1,
        Map
    }

    public enum PositionType {
        CraftLocal = 1,
        CraftPCI,
        PlanetPCI,
        PlanetLatLonAsl,
        PlanetLatLonAgl,

        [Obsolete("This was a typo")]
        PlanetLatLogAsl = PlanetLatLonAsl
    }
}
