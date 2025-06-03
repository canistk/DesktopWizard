using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.Rendering;
using UnityEditor.Rendering.Universal;
using UnityEditor.AnimatedValues; // AnimBool
using PropFlags = UnityEditor.MaterialProperty.PropFlags;
using PropType = UnityEditor.MaterialProperty.PropType;
using Kit2;

namespace Kit
{
    public abstract class CustomShaderGUI : ShaderGUI
    {
        protected class LocalCache
        {
            public readonly Material material;
            public MaterialEditor materialEditor;
            public Dictionary<string, AnimBool> fold;

            public LocalCache(Material material_, MaterialEditor materialEditor_, MaterialProperty[] properties)
            {
                material = material_;
                fold = new Dictionary<string, AnimBool>(4);
                materialEditor = materialEditor_;
            }
        }
        private LocalCache m_Cache;
        private static bool s_UndoPerformed = false;
        private UndoRedoInfo s_UndoRedoInfo = new UndoRedoInfo
        {
            undoGroup = 0,
			undoName = "CustomShaderGUI",
		};

		~CustomShaderGUI()
        {
            Undo.undoRedoPerformed -= OnEditorUndo;
            m_Cache = null;
        }
        private void OnEditorUndo()
        {
            // Due to Shader Property will cache the old value, 
            // we need to force child-class to rebuild the cache.
            s_UndoPerformed = true;
            if (m_Cache != null &&
                m_Cache.materialEditor)
            {
                m_Cache.materialEditor.UndoRedoPerformed(s_UndoRedoInfo);
            }
        }

        protected AnimBool GetOrAddAnimBool(string id, bool startValue = true)
        {
            if (m_Cache == null)
                return null;
            if (!m_Cache.fold.ContainsKey(id))
                m_Cache.fold.Add(id, new AnimBool(startValue));
            return m_Cache.fold[id];
        }

        protected bool hasKeyword(MaterialEditor materialEditor, string keywordName)
        {
            Material material = materialEditor.target as Material;
            return System.Array.IndexOf(material.shaderKeywords, keywordName) == -1;
        }

        protected void DrawToggleKeyword(string label, in string keyword, in Material material)
        {
            bool value = System.Array.IndexOf(material.shaderKeywords, keyword) == -1;
            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.Toggle(label, value);
            if (EditorGUI.EndChangeCheck())
            {
                if (value)
                    material.EnableKeyword(keyword);
                else
                    material.DisableKeyword(keyword);
            }
        }

        protected void DrawToggleKeyword(MaterialEditor materialEditor, MaterialProperty property)
        {
            bool value = property.floatValue > 0.5;
            EditorGUI.BeginChangeCheck();
            value = EditorGUILayout.Toggle(property.displayName, value);
            if (EditorGUI.EndChangeCheck())
            {
                property.floatValue = value ? 1.0f : 0.0f;
                Material material = materialEditor.target as Material;
                if (value)
                    material.EnableKeyword(property.name);
                else
                    material.DisableKeyword(property.name);
            }
        }

        protected abstract void FetchProperty(LocalCache cache, in MaterialEditor materialEditor, in MaterialProperty[] properties);

        #region Ignore property
        private List<string> m_IgnoreProperties = new List<string>(10);
        protected void RegisterIgnoreProperty(MaterialProperty property)
        {
            if (!m_IgnoreProperties.Contains(property.displayName))
                m_IgnoreProperties.Add(property.displayName);
        }
        protected void RegisterIgnoreProperty(params MaterialProperty[] properties)
        {
            foreach (var property in properties)
            {
                RegisterIgnoreProperty(property);
            }
        }
        protected void UnregisterIgnoreProperty(MaterialProperty property)
        {
            m_IgnoreProperties.Remove(property.displayName);
        }
        #endregion Ignore property

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            Material material = materialEditor.target as Material;
            if (s_UndoPerformed || m_Cache == null || m_Cache.material != material)
            {
                // Undo the old property need to update to fetch the new value from U3D editor.
                s_UndoPerformed = false;
                m_IgnoreProperties.Clear();

                m_Cache = new LocalCache(material, materialEditor, properties);
                FetchProperty(m_Cache, materialEditor, properties);
                Undo.undoRedoPerformed -= OnEditorUndo;
                Undo.undoRedoPerformed += OnEditorUndo;
            }

            for (int i = 0; i < properties.Length; ++i)
            {
                if (m_IgnoreProperties.Contains(properties[i].displayName))
                    continue;
                using (var checker = new EditorGUI.ChangeCheckScope())
                {
                    OnDrawProperty(materialEditor, properties[i], m_Cache);
                    if (checker.changed)
					{
						materialEditor.RegisterPropertyChangeUndo(properties[i].displayName);
                        EditorUtility.SetDirty(materialEditor.target);
						materialEditor.PropertiesChanged();
						materialEditor.Repaint();
					}
				}
            }
        }

        /// <summary>Draw single property</summary>
        /// <example>
        /// EditorGUI.BeginChangeCheck();
        /// materialEditor.ShaderProperty(parallaxDepthMap, parallaxDepthMap.displayName);
        /// if (EditorGUI.EndChangeCheck())
        /// {
        ///     materialEditor.RegisterPropertyChangeUndo(parallaxDepthMap.displayName);
        /// }
        /// </example>
        /// <param name="materialEditor"></param>
        /// <param name="property"></param>
        /// <param name="cache"></param>
        protected abstract void OnDrawProperty(MaterialEditor materialEditor, MaterialProperty property, LocalCache cache);

        protected void DrawProperty(MaterialEditor materialEditor, params MaterialProperty[] properties)
        {
            foreach (MaterialProperty property in properties)
            {
                DrawProperty(materialEditor, property);
            }
        }

        protected void DrawProperty(MaterialEditor materialEditor, MaterialProperty prop)
        {
            var label = new GUIContent(prop.displayName);
            switch (prop.type)
            {
                case PropType.Color:
                    bool hdr = prop.flags.HasFlag(PropFlags.HDR);
                    prop.colorValue = EditorGUILayout.ColorField(label, prop.colorValue, true, true, hdr);
                    break;
                case PropType.Vector:
                    prop.vectorValue = EditorGUILayout.Vector4Field(label, prop.vectorValue);
                    break;
                case PropType.Float:
                    prop.floatValue = EditorGUILayout.FloatField(label, prop.floatValue);
                    break;
                case PropType.Range:
                    prop.floatValue = EditorGUILayout.Slider(label, prop.floatValue, prop.rangeLimits.x, prop.rangeLimits.y);
                    break;
                case PropType.Texture:
                    materialEditor.TextureProperty(prop, prop.displayName);
                    break;
            }
        }

        protected void DrawFadeGroup(string label, bool toggleOnLabelClick, bool collapse, System.Action func)
            => DrawFadeGroup(label, toggleOnLabelClick, GetOrAddAnimBool(label, !collapse), func);

        protected void DrawFadeGroup(string label, bool toggleOnLabelClick, AnimBool animBool,
            System.Action func)
        {
            if (animBool == null)
                return;

            // https://docs.unity.cn/2022.2/Documentation/ScriptReference/EditorGUILayout.FadeGroupScope.html
            animBool.target = EditorGUILayout.Foldout(animBool.target, label, toggleOnLabelClick, EditorStyles.foldoutHeader);
            if (func == null)
                return;
            using (var group = new EditorGUILayout.FadeGroupScope(animBool.faded))
            {
                if (group.visible)
                {
                    func?.Invoke();
                }
            }
        }


        protected MaterialProperty[] FindPropertys(MaterialProperty[] properties, bool propertyIsMandatory, params string[] names)
        {
            List<MaterialProperty> rst = new List<MaterialProperty>(properties.Length);
            List<string> hash = new List<string>(names);
            for(int i = 0; i < properties.Length; ++i)
            {
                int cnt = hash.Count;
                while(cnt-->0)
                {
                    if (properties[i].name == names[cnt])
                    {
                        hash.RemoveAt(cnt);
                        rst.Add(properties[i]);
                        break; // while
                    }
                }
            }
            return rst.ToArray();
        }
    }
}