using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEditor.AnimatedValues; // AnimBool
using Kit;

public class URPLitShaderGUI : CustomShaderGUI
{
    MaterialProperty
        detailFaderProp, detailGrayScaleProp, detailMapProp,
        detailNormalMapScale, detailNormalMap, debugNormal;
    MaterialProperty
        lodMapLevelProp, lodMapDistanceProp, lodBiasProp, lodDebugProp;
    MaterialProperty
        translucencyProp;
    MaterialProperty[]
        translucencyProps;
    protected override void FetchProperty(LocalCache cache, in MaterialEditor materialEditor, in MaterialProperty[] properties)
    {
        detailMapProp = FindProperty("_DetailMap", properties, false);
        detailFaderProp = FindProperty("_DetailFader", properties, false);
        detailGrayScaleProp = FindProperty("_DetailGrayScale", properties, false);
        RegisterIgnoreProperty(detailGrayScaleProp, detailFaderProp);

        detailNormalMapScale = FindProperty("_DetailNormalMapScale", properties, false);
        detailNormalMap = FindProperty("_DetailNormalMap", properties, false);
        debugNormal = FindProperty("_DebugNormal", properties, false);
        RegisterIgnoreProperty(detailNormalMapScale, debugNormal);

        lodMapLevelProp = FindProperty("_LODMapLevel", properties, false);
        lodMapDistanceProp = FindProperty("_LODMapDistance", properties, false);
        lodBiasProp = FindProperty("_LODBias", properties, false);
        lodDebugProp = FindProperty("_DebugLod", properties, false);
        RegisterIgnoreProperty(lodMapDistanceProp, lodBiasProp, lodDebugProp);

        translucencyProp = FindProperty("_TSEnable", properties);
        translucencyProps = FindPropertys(properties, false, "_TSDebug", "_TSBlend", "_TSAlbedo", "_LightAmbient", "_TSFlip", "_LightThickness", "_LightDistortion", "_LightPower", "_LightScale", "_LightOffset");
        RegisterIgnoreProperty(translucencyProps);
    }

    protected override void OnDrawProperty(MaterialEditor materialEditor, MaterialProperty property, LocalCache cache)
    {
        string pName = property.name;
        bool Is(MaterialProperty prop)
        {
            return prop != null && prop.name == pName;
        }
        if (Is(detailMapProp))
        {
            DrawFadeGroup("Detail Texture", true, true, () =>
            {
                DrawProperty(materialEditor, detailFaderProp, detailGrayScaleProp);
                DrawProperty(materialEditor, detailMapProp);
            });
        }
        else if (Is(detailNormalMap))
        {
            DrawFadeGroup("Detail Normal Map", true, true, () =>
            {
                DrawProperty(materialEditor,
                    detailNormalMapScale,
                    detailNormalMap);
                DrawToggleKeyword(materialEditor, debugNormal);
                // DrawProperty(materialEditor, debugNormal);
            });
        }
        else if (Is(lodMapLevelProp))
        {
            DrawFadeGroup("Level Of Detail", true, true, () =>
            {
                DrawProperty(materialEditor,
                    lodMapLevelProp, lodMapDistanceProp, lodBiasProp);
                DrawToggleKeyword(materialEditor, lodDebugProp);
            });
        }
        else if (Is(translucencyProp))
        {
            DrawFadeGroup("Translucency Scattering", true, true, () =>
            {
                DrawToggleKeyword(materialEditor, translucencyProp);
                DrawProperty(materialEditor, translucencyProps);
            });
        }
        else
        {
            //property.flags == MaterialProperty.PropFlags.HasFlag(MaterialProperty.PropFlags.HasFlag(MaterialProperty.PropFlags.
            materialEditor.DefaultShaderProperty(property, property.displayName);
        }
    }
}