using UnityEngine;
using System.Collections;

namespace UnityEditor
{
    public class DroidShaderEditor : ShaderGUI
    {
        MaterialProperty colorR;
        MaterialProperty colorG;
        MaterialProperty colorB;
        MaterialProperty colorGlow;
        MaterialProperty maskTexture;
        MaterialProperty normalsTexture;
        MaterialProperty specularTexture;
        MaterialProperty aoTexture;
        MaterialProperty detailTexture;
        MaterialProperty vertexColor;
        float detailScale;
        float aoScale;

        MaterialEditor m_MaterialEditor;

        static Texture2D renderTo;

        public void FindProperties(MaterialProperty[] props)
        {
            colorR = FindProperty("_Color", props);
            colorG = FindProperty("_Color1", props);
            colorB = FindProperty("_Color2", props);
            colorGlow = FindProperty("_GlowColor", props);
            maskTexture = FindProperty("_MaskTex", props);
            normalsTexture = FindProperty("_BumpMap", props);
            specularTexture = FindProperty("_SpecMap", props);
            aoTexture = FindProperty("_AOMap", props);
            detailTexture = FindProperty("_DetailMap", props);
            vertexColor = FindProperty("_Power", props);
            detailScale = detailTexture.textureScaleAndOffset.x;
            aoScale = FindProperty("_AOScale", props).floatValue;
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            FindProperties(properties);
            m_MaterialEditor = materialEditor;
            Material material = m_MaterialEditor.target as Material;

            ShaderPropertiesGUI(material);
        }

        public void ShaderPropertiesGUI(Material material)
        {
            EditorGUI.BeginChangeCheck();
            {
                m_MaterialEditor.ColorProperty(colorR, "Color (R)");
                m_MaterialEditor.ColorProperty(colorG, "Color (G)");
                m_MaterialEditor.ColorProperty(colorB, "Color (B)");
                m_MaterialEditor.ColorProperty(colorGlow, "Glow Color (A)");
                m_MaterialEditor.TexturePropertySingleLine(new GUIContent("RGB Mask"), maskTexture);
                m_MaterialEditor.TexturePropertySingleLine(new GUIContent("Normals"), normalsTexture);
                m_MaterialEditor.TexturePropertySingleLine(new GUIContent("Specular"), specularTexture);
                GUILayout.BeginHorizontal();
                m_MaterialEditor.TexturePropertySingleLine(new GUIContent("Ambient-Occlusion"), aoTexture);
                aoScale = GUILayout.HorizontalSlider(aoScale, 1, 10, GUILayout.Width(100));
                aoScale = EditorGUILayout.FloatField(aoScale, GUILayout.Width(50));
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                m_MaterialEditor.TexturePropertySingleLine(new GUIContent("Pattern"), detailTexture);
                detailScale = GUILayout.HorizontalSlider(detailScale, 1, 50, GUILayout.Width(100));
                detailScale = EditorGUILayout.FloatField(detailScale, GUILayout.Width(50));
                GUILayout.EndHorizontal();

                m_MaterialEditor.FloatProperty(vertexColor, "Vertex Color Intensity");
            }
            if (EditorGUI.EndChangeCheck())
            {
                MaterialChanged(material);
            }

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Low"))
                material.shader.maximumLOD = 100;
            if (GUILayout.Button("Medium"))
                material.shader.maximumLOD = 200;
            if (GUILayout.Button("High"))
                material.shader.maximumLOD = 300;
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Bake Standard Textures"))
                RenderDiffuse();
        }

        void MaterialChanged(Material material)
        {
            Vector4 scale = detailTexture.textureScaleAndOffset;
            detailTexture.textureScaleAndOffset = new Vector4(detailScale, detailScale, scale.z, scale.w);
            material.SetFloat("_AOScale", aoScale);

            SetKeyword(material, "_NORMALMAP", material.GetTexture("_BumpMap"));
            SetKeyword(material, "_DETAILMAP", material.GetTexture("_DetailMap"));
            SetKeyword(material, "_AOMAP", material.GetTexture("_AOMap"));
            SetKeyword(material, "_VERTEXCOLOR", material.GetFloat("_Power") > 0);
        }

        void SetKeyword(Material m, string keyword, bool state)
        {
            if (state)
                m.EnableKeyword(keyword);
            else
                m.DisableKeyword(keyword);
        }

        void RenderDiffuse() 
        {
            EditorUtility.DisplayProgressBar("Baking Meaterial", "Initializing...", 0);
            Material mat = m_MaterialEditor.target as Material;
            Texture2D tex = (Texture2D)mat.GetTexture("_MaskTex");
            Color r = mat.GetColor("_Color");
            Color g = mat.GetColor("_Color1");
            Color b = mat.GetColor("_Color2");
            Color a = mat.GetColor("_GlowColor");
            
            if (renderTo == null )
                renderTo = new Texture2D(tex.width, tex.height, TextureFormat.RGBA32, true);
            else
                renderTo.Resize(tex.width, tex.height);

            //rendering diffuse
            //BakeTexture(tex, Diffuse, r, g, b, a, "Rendering Diffuse Map...");
            for (int i = 0; i < tex.height; i++ )
            {
                Color[] colors = tex.GetPixels(0, i, tex.width, 1);
                for (int c = 0; c < colors.Length; c++)
                { 
                    Color clr = colors[c];
                    colors[c] = clr.r * r + clr.g * g + clr.b * b;
                    colors[c] += clr.a * a;
                }
                renderTo.SetPixels(0, i, tex.width, 1, colors);
                EditorUtility.DisplayProgressBar("Baking Meaterial", "Rendering Diffuse Map...", i/(float)tex.height);
            }

            SaveBakedTexture(mat.name, "diff");
            EditorUtility.DisplayProgressBar("Baking Meaterial", "Saving Diffuse Map...", 1);

            //rendering glossiness
            Color _r = new Color(r.a, r.a, r.a, r.a);
            Color _g = new Color(g.a, g.a, g.a, g.a);
            Color _b = new Color(b.a, b.a, b.a, b.a);
            for (int i = 0; i < tex.height; i++)
            {
                Color[] colors = tex.GetPixels(0, i, tex.width, 1);
                for (int c = 0; c < colors.Length; c++)
                {
                    Color clr = colors[c];
                    colors[c] = clr.r * _r + clr.g * _g + clr.b * _b;
                    colors[c] += clr.a * Color.white;
                }
                renderTo.SetPixels(0, i, tex.width, 1, colors);
                EditorUtility.DisplayProgressBar("Baking Meaterial", "Rendering Glossiness Map...", i / (float)tex.height);
            }

            SaveBakedTexture(mat.name, "spec");
            EditorUtility.DisplayProgressBar("Baking Meaterial", "Saving Glossiness Map...", 1);
            
            //rendering emission
            for (int i = 0; i < tex.height; i++)
            {
                Color[] colors = tex.GetPixels(0, i, tex.width, 1);
                for (int c = 0; c < colors.Length; c++)
                {
                    Color clr = colors[c];
                    colors[c] = clr.a * a;
                }
                renderTo.SetPixels(0, i, tex.width, 1, colors);
                EditorUtility.DisplayProgressBar("Baking Meaterial", "Rendering Emission Map...", i / (float)tex.height);
            }

            SaveBakedTexture(mat.name, "glow");
            EditorUtility.DisplayProgressBar("Baking Meaterial", "Saving Emission Map...", 1);

            //rendering detailmap
            for (int i = 0; i < tex.height; i++)
            {
                Color[] colors = tex.GetPixels(0, i, tex.width, 1);
                for (int c = 0; c < colors.Length; c++)
                {
                    Color clr = colors[c];
                    colors[c] = clr.r * Color.white;
                }
                renderTo.SetPixels(0, i, tex.width, 1, colors);
                EditorUtility.DisplayProgressBar("Baking Meaterial", "Rendering Detail Mask...", i / (float)tex.height);
            }

            SaveBakedTexture(mat.name, "mask");
            EditorUtility.DisplayProgressBar("Baking Meaterial", "Saving Detail Mask...", 1);

            Material baked = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(baked, string.Format("Assets/AngryDroids/{0}_baked.mat", mat.name));
            baked.shaderKeywords = new string[]{"_DETAIL_MULX2", "_EMISSION", "_METALLICGLOSSMAP", "_NORMALMAP"};
            
            baked.SetTexture("_MainTex", (Texture)AssetDatabase.LoadAssetAtPath(string.Format("Assets/AngryDroids/{0}_diff.png", mat.name), typeof(Texture)));
            baked.SetTexture("_EmissionMap", (Texture)AssetDatabase.LoadAssetAtPath(string.Format("Assets/AngryDroids/{0}_glow.png", mat.name), typeof(Texture)));
            baked.SetColor("_EmissionColor", Color.white);
            baked.SetTexture("_DetailMask", (Texture)AssetDatabase.LoadAssetAtPath(string.Format("Assets/AngryDroids/{0}_mask.png", mat.name), typeof(Texture)));
            baked.SetTexture("_MetallicGlossMap", (Texture)AssetDatabase.LoadAssetAtPath(string.Format("Assets/AngryDroids/{0}_spec.png", mat.name), typeof(Texture)));
            baked.SetTexture("_BumpMap", mat.GetTexture("_BumpMap"));
            baked.SetTexture("_OcclusionMap", mat.GetTexture("_AOMap"));
            baked.SetTexture("_DetailAlbedoMap", mat.GetTexture("_DetailMap"));
            baked.SetTextureScale("_DetailAlbedoMap", mat.GetTextureScale("_DetailMap"));
            baked.SetFloat("_UVSec", 1);

            EditorUtility.ClearProgressBar();
            if (EditorUtility.DisplayDialog("Baking Meaterial", "Material Baking Done!", "Show Material", "Close"))
                EditorGUIUtility.PingObject(baked);
        }

        //static void BakeTexture(Texture2D source, System.Func<Color, Color, Color, Color, Color, Color> colorOperation, Color r, Color g, Color b, Color a, string message)
        //{
        //    for (int i = 0; i < source.height; i++)
        //    {
        //        Color[] colors = source.GetPixels(0, i, source.width, 1);
        //        for (int c = 0; c < colors.Length; c++)
        //        {
        //            colors[c] = colorOperation(colors[c], r, g, b, a);//  clr.r * Color.white;
        //        }
        //        renderTo.SetPixels(0, i, source.width, 1, colors);
        //        EditorUtility.DisplayProgressBar("Baking Meaterial", message, i / (float)source.height);
        //    }
        //}

        //static void BakeTexture(Texture2D source, System.Func<Color, Color> colorOperation, float colorMask, string message)
        //{
        //    for (int i = 0; i < source.height; i++)
        //    {
        //        Color[] colors = source.GetPixels(0, i, source.width, 1);
        //        for (int c = 0; c < colors.Length; c++)
        //        {
        //            colors[c] = colorOperation(colors[c]);//  clr.r * Color.white;
        //        }
        //        renderTo.SetPixels(0, i, source.width, 1, colors);
        //        EditorUtility.DisplayProgressBar("Baking Meaterial", message, i / (float)source.height);
        //    }
        //}

        //Color Diffuse(Color input, Color r, Color g, Color b, Color a) 
        //{
        //    Color result = new Color();
        //    result = input.r * r + input.g * g + input.b * b;
        //    result += input.a * a;
        //    return result;
        //}

        //Color Emission(float mask, Color a)
        //{
        //    Color result = new Color();
        //    result = mask * a;
        //    return result;
        //}

        static void SaveBakedTexture(string name, string type)
        {
            byte[] bytes = renderTo.EncodeToPNG();
            string path = string.Format("{0}/AngryDroids/{1}_{2}.png", Application.dataPath, name, type);
            System.IO.File.WriteAllBytes(path, bytes);
            AssetDatabase.ImportAsset(string.Format("Assets/AngryDroids/{0}_{1}.png", name, type));
        }
    }
}