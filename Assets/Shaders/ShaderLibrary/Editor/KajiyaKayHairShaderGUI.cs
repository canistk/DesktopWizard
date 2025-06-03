using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using UnityEditor.AnimatedValues; // AnimBool
using Kit;

public class KajiyaKayHairShaderGUI : CustomShaderGUI
{
    MaterialProperty
        parallaxPropV40,    parallaxPropV41,    parallaxDepthMap,
        primaryColorProp,   primaryPropV40,     primaryPropV41,
        secondaryColorProp, secondaryPropV40,   secondaryPropV41;
    protected override void FetchProperty(LocalCache cache, in MaterialEditor materialEditor, in MaterialProperty[] properties)
    {
        primaryColorProp    = FindProperty("_PrimaryColor", properties, true);
        primaryPropV40      = FindProperty("_PrimaryV40", properties, true);
        primaryPropV41      = FindProperty("_PrimaryV41", properties, true);
        secondaryColorProp  = FindProperty("_SecondaryColor", properties, true);
        secondaryPropV40    = FindProperty("_SecondaryV40", properties, true);
        secondaryPropV41    = FindProperty("_SecondaryV41", properties, true);
        parallaxDepthMap    = FindProperty("_ParallaxDepthMap", properties, true);
        parallaxPropV40     = FindProperty("_ParallaxV40", properties, true);
        parallaxPropV41     = FindProperty("_ParallaxV41", properties, true);
        RegisterIgnoreProperty(primaryPropV40, primaryPropV41,
            secondaryPropV40, secondaryPropV41,
            parallaxPropV40, parallaxPropV41);
    }

    private void DrawSliders(MaterialEditor materialEditor, MaterialProperty v40Prop, MaterialProperty v41Prop)
    {
        var v40 = v40Prop.vectorValue;
        EditorGUI.BeginChangeCheck();
        v40.x = EditorGUILayout.Slider("shift", v40.x, -5f, 5f);
        v40.y = EditorGUILayout.Slider("strength", v40.y,  0f, 1f);
        v40.z = EditorGUILayout.Slider("oil", v40.z,  0f, 1f);
        v40.w = EditorGUILayout.Slider("weight", v40.w, 0f, 1f);
        var v41 = v41Prop.vectorValue;
        v41.x = EditorGUILayout.Slider("width", v41.x, 0f, 1f);
        v41.y = EditorGUILayout.Slider("feather", v41.y, 0f,1f);
        //v41.z = EditorGUILayout.Slider("dissolve", v41.z, 0f, 1f);
        //v41.w = EditorGUILayout.Slider("none", v41.w, 0f, 5f);
        if (EditorGUI.EndChangeCheck())
        {
            materialEditor.RegisterPropertyChangeUndo(v40Prop.displayName);
            v40Prop.vectorValue = v40;
            materialEditor.RegisterPropertyChangeUndo(v41Prop.displayName);
            v41Prop.vectorValue = v41;
        }
    }

    protected override void OnDrawProperty(MaterialEditor materialEditor, MaterialProperty property, LocalCache cache)
    {
        string pName = property.name;
        if (pName == primaryColorProp.name)
        {
            DrawFadeGroup("Primary Strand Specular", true, false, () =>
            {
                materialEditor.ShaderProperty(primaryColorProp, primaryColorProp.displayName);
                DrawSliders(materialEditor, primaryPropV40, primaryPropV41);
            });
        }
        else if (pName == secondaryColorProp.name)
        {
            DrawFadeGroup("Secondary Strand Specular", true, false, () =>
            {
                materialEditor.ShaderProperty(secondaryColorProp, secondaryColorProp.displayName);
                DrawSliders(materialEditor, secondaryPropV40, secondaryPropV41);
            });
        }
        else if (pName == parallaxDepthMap.name)
        {
            DrawFadeGroup("Parallax", true, false, () =>
            {
                EditorGUI.BeginChangeCheck();
                materialEditor.ShaderProperty(parallaxDepthMap, parallaxDepthMap.displayName);
                if (EditorGUI.EndChangeCheck())
                {
                    materialEditor.RegisterPropertyChangeUndo(parallaxDepthMap.displayName);
                }
                {
                    var v = parallaxPropV40.vectorValue;
                    EditorGUI.BeginChangeCheck();
                    v.x = EditorGUILayout.IntSlider("Parallax layer", (int)v.x, 0, 5);
                    v.y = EditorGUILayout.Slider("Parallax fading", v.y, 0.0f, 1.1f);
                    Vector2 zw = EditorGUILayout.Vector2Field("Parallax Shift UV", new Vector2(v.z, v.w));
                    if (EditorGUI.EndChangeCheck())
                    {
                        materialEditor.RegisterPropertyChangeUndo(parallaxPropV40.displayName);
                        parallaxPropV40.vectorValue = new Vector4(v.x, v.y, zw[0], zw[1]);
                    }
                }
                {
                    var v = parallaxPropV41.vectorValue;
                    EditorGUI.BeginChangeCheck();
                    v.x = EditorGUILayout.FloatField("Parallax shift tail", v.x);
                    v.y = EditorGUILayout.FloatField("Parallax height", v.y);
                    //Vector2 yz = EditorGUILayout.Vector2Field("Shift UV", new Vector2(v.y, v.z));
                    //v.w = EditorGUILayout.FloatField("Rotate", v.w);
                    if (EditorGUI.EndChangeCheck())
                    {
                        materialEditor.RegisterPropertyChangeUndo(parallaxPropV41.displayName);
                        parallaxPropV41.vectorValue = v;
                    }
                }
            });
        }
        else
        {
            //property.flags == MaterialProperty.PropFlags.HasFlag(MaterialProperty.PropFlags.HasFlag(MaterialProperty.PropFlags.
            //materialEditor.DefaultShaderProperty(property, property.displayName);
            materialEditor.ShaderProperty(property, property.displayName);
        }
    }

}