using AcTools.Render.Base;
using AcTools.Render.Base.Objects;
using AcTools.Render.Base.Utils;
using AcTools.Render.Kn5Specific.Materials;
using AcTools.Render.Kn5Specific.Textures;
using ArcadeCorsa.Render.Shaders;
using JetBrains.Annotations;

namespace ArcadeCorsa.Render.DarkRenderer.Materials {
    public class Kn5MaterialSimpleMaps : Kn5MaterialSimpleReflective {
        private EffectDarkMaterial.MapsMaterial _material;
        private IRenderableTexture _txNormal, _txMaps, _txDetails, _txDetailsNormal;

        public Kn5MaterialSimpleMaps([NotNull] Kn5MaterialDescription description) : base(description) { }

        public override void Initialize(IDeviceContextHolder contextHolder) {
            _txNormal = Kn5Material.ShaderName.Contains("damage") ? null : GetTexture("txNormal", contextHolder);
            _txMaps = GetTexture("txMaps", contextHolder);
            _txDetails = GetTexture("txDetail", contextHolder);
            _txDetailsNormal = GetTexture("txNormalDetail", contextHolder);

            if (_txNormal != null) {
                Flags |= EffectDarkMaterial.HasNormalMap;
            }
            
            if (Equals(Kn5Material.GetPropertyValueAByName("isAdditive"), 2.0f)) {
                Flags |= EffectDarkMaterial.IsCarpaint;
            }

            if (Kn5Material.GetPropertyValueAByName("useDetail") > 0) {
                Flags |= EffectDarkMaterial.HasDetailsMap;
            }

            if (Kn5Material.ShaderName.Contains("_AT")) {
                Flags |= EffectDarkMaterial.UseNormalAlphaAsAlpha;
            }

            _material = new EffectDarkMaterial.MapsMaterial {
                DetailsUvMultipler = Kn5Material.GetPropertyValueAByName("detailUVMultiplier"),
                DetailsNormalBlend = _txDetailsNormal == null ? 0f : Kn5Material.GetPropertyValueAByName("detailNormalBlend")
            };

            base.Initialize(contextHolder);
        }

        public override bool Prepare(IDeviceContextHolder contextHolder, SpecialRenderMode mode) {
            if (!base.Prepare(contextHolder, mode)) return false;

            Effect.FxMapsMaterial.Set(_material);
            Effect.FxNormalMap.SetResource(_txNormal);
            Effect.FxDetailsMap.SetResource(_txDetails);
            Effect.FxDetailsNormalMap.SetResource(_txDetailsNormal);
            Effect.FxMapsMap.SetResource(_txMaps);
            return true;
        }

        public override void Draw(IDeviceContextHolder contextHolder, int indices, SpecialRenderMode mode) {
            Effect.TechMaps.DrawAllPasses(contextHolder.DeviceContext, indices);
        }
    }
}