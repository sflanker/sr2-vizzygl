using System;
using System.Collections.Generic;
using Assets.Scripts.Objects;
using UnityEngine;

namespace Assets.Scripts.Vizzy {
    public interface IVizzyGLContext {
        Vector3 Color { get; set; }

        Single Opacity { get; set; }

        Vector3 Scale { get; set; }

        Vector3 Rotation { get; set; }

        PositionType Origin { get; set; }

        Int32 CraftId { get; set; }

        String PlanetName { get; set; }

        ViewType View { get; set; }

        IReadOnlyDictionary<String, VizzyGLObject> Objects { get; }

        void AddObject(VizzyGLObject gameObject);
    }
}
