﻿using System;
using AcTools.Render.Base;
using AcTools.Render.Base.Cameras;
using AcTools.Render.Base.Materials;
using AcTools.Render.Base.Objects;
using AcTools.Render.Base.Structs;
using ArcadeCorsa.Render.Shaders;
using SlimDX;

namespace ArcadeCorsa.Render.DarkRenderer {
    public class FlatMirror : RenderableList {
        public class FlatMirrorObject : TrianglesRenderableObject<InputLayouts.VerticePT> {
            private static readonly InputLayouts.VerticePT[] BaseVertices;
            private static readonly ushort[] BaseIndices;

            static FlatMirrorObject() {
                BaseVertices = new InputLayouts.VerticePT[4];
                for (var i = 0; i < BaseVertices.Length; i++) {
                    BaseVertices[i] = new InputLayouts.VerticePT(
                            new Vector3(i < 2 ? 1 : -1, 0, i % 2 == 0 ? -1 : 1),
                            new Vector2(i < 2 ? 1 : 0, i % 2)
                            );
                }

                BaseIndices = new ushort[] { 0, 2, 1, 3, 1, 2 };
            }

            private IRenderableMaterial _material;

            public Matrix Transform;

            public FlatMirrorObject(Matrix transform) : base(null, BaseVertices, BaseIndices) {
                Transform = transform;
            }

            protected override void Initialize(IDeviceContextHolder contextHolder) {
                base.Initialize(contextHolder);

                _material = contextHolder.GetMaterial(BasicMaterials.FlatMirrorKey);
                _material.Initialize(contextHolder);
            }

            protected override void DrawInner(IDeviceContextHolder contextHolder, ICamera camera, SpecialRenderMode mode) {
                if (!_material.Prepare(contextHolder, mode)) return;
                base.DrawInner(contextHolder, camera, mode);

                _material.SetMatrices(Transform * ParentMatrix, camera);
                _material.Draw(contextHolder, Indices.Length, mode);
            }

            public override void Dispose() {
                base.Dispose();
                _material?.Dispose();
            }
        }

        private readonly FlatMirrorObject _object;

        public FlatMirror(IRenderableObject mirroredObject, Plane plane) {
            LocalMatrix = Matrix.Reflection(plane);
            Add(mirroredObject.Clone());

            var point = plane.Normal * plane.D;
            _object = new FlatMirrorObject(
                    Matrix.Scaling(1000f, 1000f, 1000f) * Matrix.Translation(point)) {
                        ParentMatrix = Matrix
                    };
        }

        public override void Draw(IDeviceContextHolder contextHolder, ICamera camera, SpecialRenderMode mode, Func<IRenderableObject, bool> filter = null) {
            if (mode != SpecialRenderMode.Simple && mode != SpecialRenderMode.SimpleTransparent) return;

            var state = contextHolder.DeviceContext.Rasterizer.State;
            try {
                contextHolder.DeviceContext.Rasterizer.State = contextHolder.States.InvertedState;
                contextHolder.GetEffect<EffectDarkMaterial>().FxFlatMirrored.Set(true);
                base.Draw(contextHolder, camera, mode, filter);
            } finally {
                contextHolder.DeviceContext.Rasterizer.State = state;
                contextHolder.GetEffect<EffectDarkMaterial>().FxFlatMirrored.Set(false);
            }

            _object.Draw(contextHolder, camera, mode, filter);
        }
    }
}