using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.Behaviours {
    public abstract class OriginOffsetPositionBehaviour : MonoBehaviour {
        protected VizzyGLObject VizzyGlObject { get; private set; }

        public void Initialize(VizzyGLObject vizzyGlObject) {
            this.VizzyGlObject = vizzyGlObject;
            this.OnInitialized();
        }

        public void Update() {
            if (this.VizzyGlObject != null) {
                this.OnUpdate();
            }
        }

        protected virtual void OnInitialized() {
        }

        protected abstract void OnUpdate();
    }
}
