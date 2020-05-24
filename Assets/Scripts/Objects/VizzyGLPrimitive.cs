using System;
using UnityEngine;

namespace Assets.Scripts.Objects {
    [Serializable]
    public class VizzyGLPrimitive : VizzyGLObject {
        [SerializeField] private PrimitiveType _primitive;

        public PrimitiveType Primitive => this._primitive;

        public VizzyGLPrimitive() {
        }

        public VizzyGLPrimitive(PrimitiveType primitive, String name, ViewType view, PositionType originType, String originPlanetName) :
            base(name, view, originType, originPlanetName) {

            this._primitive = primitive;
        }

        public VizzyGLPrimitive(PrimitiveType primitive, String name, ViewType view, PositionType originType, Int32 originCraftId) :
            base(name, view, originType, originCraftId) {

            this._primitive = primitive;
        }

        protected override GameObject CreateGameObject() {
            var prim = GameObject.CreatePrimitive(this._primitive);
            var collider = prim.GetComponent<Collider>();
            if (collider != null) {
                UnityEngine.Object.Destroy(collider);
            } else {
                Debug.Log($"Primitive type {this._primitive} does not have a {nameof(Collider)}.");
            }

            return prim;
        }
    }
}
