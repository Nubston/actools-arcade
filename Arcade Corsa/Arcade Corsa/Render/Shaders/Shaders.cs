/* GENERATED AUTOMATICALLY */
/* DON’T MODIFY */

using System.Reflection;
using System.Resources;
using System.Runtime.InteropServices;
using AcTools.Render.Base.Shaders;
using AcTools.Render.Base.Structs;
using AcTools.Render.Base.Utils;
using SlimDX;
using SlimDX.D3DCompiler;
using SlimDX.Direct3D11;
// ReSharper disable InconsistentNaming
// ReSharper disable LocalizableElement

namespace ArcadeCorsa.Render.Shaders {
	internal static class ShadersResourceManager {
		internal static readonly ResourceManager Manager = new ResourceManager("ArcadeCorsa.Shaders", Assembly.GetExecutingAssembly());
	}

	public class EffectDarkMaterial : IEffectWrapper, IEffectMatricesWrapper {
		[StructLayout(LayoutKind.Sequential)]
        public struct StandartMaterial {
            public float Ambient;
            public float Diffuse;
            public float Specular;
            public float SpecularExp;
            public Vector3 Emissive;
            public uint Flags;
            public Vector3 _padding;

			public static readonly int Stride = Marshal.SizeOf(typeof(StandartMaterial));
        }

		[StructLayout(LayoutKind.Sequential)]
        public struct ReflectiveMaterial {
            public float FresnelC;
            public float FresnelExp;
            public float FresnelMaxLevel;

			public static readonly int Stride = Marshal.SizeOf(typeof(ReflectiveMaterial));
        }

		[StructLayout(LayoutKind.Sequential)]
        public struct MapsMaterial {
            public float DetailsUvMultipler;
            public float DetailsNormalBlend;

			public static readonly int Stride = Marshal.SizeOf(typeof(MapsMaterial));
        }

		[StructLayout(LayoutKind.Sequential)]
        public struct AlphaMaterial {
            public float Alpha;

			public static readonly int Stride = Marshal.SizeOf(typeof(AlphaMaterial));
        }

		[StructLayout(LayoutKind.Sequential)]
        public struct NmUvMultMaterial {
            public float DiffuseMultipler;
            public float NormalMultipler;

			public static readonly int Stride = Marshal.SizeOf(typeof(NmUvMultMaterial));
        }

		public const uint HasNormalMap = 1;
		public const uint UseDiffuseAlphaAsMap = 2;
		public const uint UseNormalAlphaAsAlpha = 64;
		public const uint AlphaTest = 128;
		public const uint IsAdditive = 16;
		public const uint HasDetailsMap = 4;
		public const uint IsCarpaint = 32;
		public const bool EnableShadows = true;
		public const int NumSplits = 1;
		public const int ShadowMapSize = 2048;
		private ShaderBytecode _b;
		public Effect E;

        public ShaderSignature InputSignaturePT, InputSignaturePNTG;
        public InputLayout LayoutPT, LayoutPNTG;

		public EffectTechnique TechStandard, TechAlpha, TechReflective, TechNm, TechNmUvMult, TechAtNm, TechMaps, TechDiffMaps, TechGl, TechAmbientShadow, TechMirror, TechFlatMirror;

		public EffectMatrixVariable FxShadowViewProj { get; private set; }
		public EffectMatrixVariable FxWorld { get; private set; }
		public EffectMatrixVariable FxWorldInvTranspose { get; private set; }
		public EffectMatrixVariable FxWorldViewProj { get; private set; }
		public EffectResourceVariable FxShadowMaps, FxDiffuseMap, FxNormalMap, FxMapsMap, FxDetailsMap, FxDetailsNormalMap, FxReflectionCubemap;
		public EffectScalarVariable FxFlatMirrored;
		public EffectVectorVariable FxEyePosW { get; private set; }
		public EffectVectorVariable FxLightDir { get; private set; }
		public EffectVariable FxMaterial, FxReflectiveMaterial, FxMapsMaterial, FxAlphaMaterial, FxNmUvMultMaterial;

		public void Initialize(Device device) {
			_b = EffectUtils.Load(ShadersResourceManager.Manager, "DarkMaterial");
			E = new Effect(device, _b);

			TechStandard = E.GetTechniqueByName("Standard");
			TechAlpha = E.GetTechniqueByName("Alpha");
			TechReflective = E.GetTechniqueByName("Reflective");
			TechNm = E.GetTechniqueByName("Nm");
			TechNmUvMult = E.GetTechniqueByName("NmUvMult");
			TechAtNm = E.GetTechniqueByName("AtNm");
			TechMaps = E.GetTechniqueByName("Maps");
			TechDiffMaps = E.GetTechniqueByName("DiffMaps");
			TechGl = E.GetTechniqueByName("Gl");
			TechAmbientShadow = E.GetTechniqueByName("AmbientShadow");
			TechMirror = E.GetTechniqueByName("Mirror");
			TechFlatMirror = E.GetTechniqueByName("FlatMirror");

			for (var i = 0; i < TechAmbientShadow.Description.PassCount && InputSignaturePT == null; i++) {
				InputSignaturePT = TechAmbientShadow.GetPassByIndex(i).Description.Signature;
			}
			if (InputSignaturePT == null) throw new System.Exception("input signature (DarkMaterial, PT, AmbientShadow) == null");
			LayoutPT = new InputLayout(device, InputSignaturePT, InputLayouts.VerticePT.InputElementsValue);
			for (var i = 0; i < TechStandard.Description.PassCount && InputSignaturePNTG == null; i++) {
				InputSignaturePNTG = TechStandard.GetPassByIndex(i).Description.Signature;
			}
			if (InputSignaturePNTG == null) throw new System.Exception("input signature (DarkMaterial, PNTG, Standard) == null");
			LayoutPNTG = new InputLayout(device, InputSignaturePNTG, InputLayouts.VerticePNTG.InputElementsValue);

			FxShadowViewProj = E.GetVariableByName("gShadowViewProj").AsMatrix();
			FxWorld = E.GetVariableByName("gWorld").AsMatrix();
			FxWorldInvTranspose = E.GetVariableByName("gWorldInvTranspose").AsMatrix();
			FxWorldViewProj = E.GetVariableByName("gWorldViewProj").AsMatrix();
			FxShadowMaps = E.GetVariableByName("gShadowMaps").AsResource();
			FxDiffuseMap = E.GetVariableByName("gDiffuseMap").AsResource();
			FxNormalMap = E.GetVariableByName("gNormalMap").AsResource();
			FxMapsMap = E.GetVariableByName("gMapsMap").AsResource();
			FxDetailsMap = E.GetVariableByName("gDetailsMap").AsResource();
			FxDetailsNormalMap = E.GetVariableByName("gDetailsNormalMap").AsResource();
			FxReflectionCubemap = E.GetVariableByName("gReflectionCubemap").AsResource();
			FxFlatMirrored = E.GetVariableByName("gFlatMirrored").AsScalar();
			FxEyePosW = E.GetVariableByName("gEyePosW").AsVector();
			FxLightDir = E.GetVariableByName("gLightDir").AsVector();
			FxMaterial = E.GetVariableByName("gMaterial");
			FxReflectiveMaterial = E.GetVariableByName("gReflectiveMaterial");
			FxMapsMaterial = E.GetVariableByName("gMapsMaterial");
			FxAlphaMaterial = E.GetVariableByName("gAlphaMaterial");
			FxNmUvMultMaterial = E.GetVariableByName("gNmUvMultMaterial");
		}

        public void Dispose() {
			if (E == null) return;
			InputSignaturePT.Dispose();
            LayoutPT.Dispose();
			InputSignaturePNTG.Dispose();
            LayoutPNTG.Dispose();
            E.Dispose();
            _b.Dispose();
        }
	}


	public static class EffectExtension {		
        public static void Set(this EffectVariable variable, EffectDarkMaterial.StandartMaterial o) {
            SlimDxExtension.Set(variable, o, EffectDarkMaterial.StandartMaterial.Stride);
        }
        public static void Set(this EffectVariable variable, EffectDarkMaterial.ReflectiveMaterial o) {
            SlimDxExtension.Set(variable, o, EffectDarkMaterial.ReflectiveMaterial.Stride);
        }
        public static void Set(this EffectVariable variable, EffectDarkMaterial.MapsMaterial o) {
            SlimDxExtension.Set(variable, o, EffectDarkMaterial.MapsMaterial.Stride);
        }
        public static void Set(this EffectVariable variable, EffectDarkMaterial.AlphaMaterial o) {
            SlimDxExtension.Set(variable, o, EffectDarkMaterial.AlphaMaterial.Stride);
        }
        public static void Set(this EffectVariable variable, EffectDarkMaterial.NmUvMultMaterial o) {
            SlimDxExtension.Set(variable, o, EffectDarkMaterial.NmUvMultMaterial.Stride);
        }
	}
}
