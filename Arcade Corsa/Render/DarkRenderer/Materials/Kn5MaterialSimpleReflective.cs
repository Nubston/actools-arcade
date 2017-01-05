using AcTools.Render.Base;
using AcTools.Render.Base.Objects;
using AcTools.Render.Base.Utils;
using AcTools.Render.Kn5Specific.Materials;
using ArcadeCorsa.Render.Shaders;
using JetBrains.Annotations;

namespace ArcadeCorsa.Render.DarkRenderer.Materials {
    public class Kn5MaterialSimpleReflective : Kn5MaterialSimple {
        private EffectDarkMaterial.ReflectiveMaterial _material;

        public Kn5MaterialSimpleReflective([NotNull] Kn5MaterialDescription description) : base(description) { }

        public override void Initialize(IDeviceContextHolder contextHolder) {
            if (Equals(Kn5Material.GetPropertyValueAByName("isAdditive"), 1.0f)) {
                Flags |= EffectDarkMaterial.IsAdditive;
            }

            _material = new EffectDarkMaterial.ReflectiveMaterial {
                FresnelC = Kn5Material.GetPropertyValueAByName("fresnelC"),
                FresnelExp = Kn5Material.GetPropertyValueAByName("fresnelEXP"),
                FresnelMaxLevel = Kn5Material.GetPropertyValueAByName("fresnelMaxLevel")
            };

            base.Initialize(contextHolder);
        }

        public override bool Prepare(IDeviceContextHolder contextHolder, SpecialRenderMode mode) {
            if (!base.Prepare(contextHolder, mode)) return false;

            Effect.FxReflectiveMaterial.Set(_material);
            return true;
        }

        public override void Draw(IDeviceContextHolder contextHolder, int indices, SpecialRenderMode mode) {
            Effect.TechReflective.DrawAllPasses(contextHolder.DeviceContext, indices);
        }
    }
}