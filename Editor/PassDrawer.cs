#if !VOL_FX

using System;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

//  Pixelation © NullTale - https://x.com/NullTale
namespace VolFx.Editor
{
    [CustomPropertyDrawer(typeof(VolFx.Pass), true)]
    public class PassDrawer : PropertyDrawer
    {
        // =======================================================================
        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return GetObjectReferenceHeight(property);
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var pass = property.objectReferenceValue;
            if (pass == null)
            {
                pass = ScriptableObject.CreateInstance(fieldInfo.FieldType);
                pass.hideFlags = HideFlags.HideInHierarchy | HideFlags.HideInInspector;

                if (string.IsNullOrEmpty(AssetDatabase.GetAssetPath(property.serializedObject.targetObject)) == false)
                {
                    AssetDatabase.AddObjectToAsset(pass, property.serializedObject.targetObject);
                    property.objectReferenceValue = pass;
                    
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                    EditorUtility.SetDirty(pass);
                    
                    property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                    AssetDatabase.SaveAssets();
                }
            }
            
            DrawObjectReference(property, position);
        }
        
        // =======================================================================
        public static float GetObjectReferenceHeight(SerializedProperty element)
        {
            return GetObjectReferenceHeight(element.objectReferenceValue, element.isExpanded);
        }

        public static float GetObjectReferenceHeight(Object obj, bool isExpanded, Predicate<SerializedProperty> filter = null)
        {
            if (obj == null)
                return EditorGUIUtility.singleLineHeight;

            using var so          = new SerializedObject(obj);
            var       totalHeight = 0f;

            using (var iterator = so.GetIterator())
            {
                if (iterator.NextVisible(true))
                {
                    do
                    {
                        var childProperty = so.FindProperty(iterator.name);
                        
                        if (childProperty.name.Equals("m_Script", System.StringComparison.Ordinal))
                            continue;
                        
                        if (childProperty.name.Equals("_active", System.StringComparison.Ordinal))
                            continue;
                        
                        if (filter != null && filter.Invoke(childProperty) == false)
                            continue;
                        
						// if (NaughtyAttributes.Editor.PropertyUtility.IsVisible(childProperty) == false)
                        //    continue;

                        totalHeight += EditorGUI.GetPropertyHeight(childProperty);
                    }
                    while (iterator.NextVisible(false));
                }
            }

            totalHeight += EditorGUIUtility.standardVerticalSpacing;
            return totalHeight;
        }
        
        public static void DrawObjectReference(SerializedProperty element, Rect position)
        {
            DrawObjectReference(element.objectReferenceValue, element.isExpanded, position);
        }

        public static void DrawObjectReference(Object obj, bool isExpanded, Rect position, bool decorativeBox = false, Predicate<SerializedProperty> filter = null)
        {
            if (obj == null)
                return;

            using var so = new SerializedObject(obj);

            EditorGUI.BeginChangeCheck();

            using (var iterator = so.GetIterator())
            {
                var yOffset =  EditorGUIUtility.standardVerticalSpacing;
                if (iterator.NextVisible(true))
                {
                    do
                    {
                        var childProperty = so.FindProperty(iterator.name);
                        if (filter != null && filter.Invoke(childProperty) == false)
                            continue;

                        if (childProperty.name.Equals("m_Script", StringComparison.Ordinal))
                            continue;
                        
                        if (childProperty.name.Equals("_active", System.StringComparison.Ordinal))
                            continue;

                        var childHeight = EditorGUI.GetPropertyHeight(childProperty);
                        var childRect = new Rect()
                        {
                            x      = position.x,
                            y      = position.y + yOffset,
                            width  = position.width,
                            height = childHeight
                        };

                        EditorGUI.PropertyField(childRect, iterator, true);
                        
                        yOffset += childHeight + EditorGUIUtility.standardVerticalSpacing;
                    }
                    while (iterator.NextVisible(false));
                }

                if (decorativeBox)
                {
                    var pos = position;
                    pos.x = 0f;
                    pos.y += EditorGUIUtility.singleLineHeight;
                    pos.width += 100f;
                    pos.height = yOffset - EditorGUIUtility.singleLineHeight;

                    GUI.Box(pos, GUIContent.none);
                }

                if (EditorGUI.EndChangeCheck())
                    so.ApplyModifiedProperties();
            }
        }
        
        [InitializeOnLoadMethod]
        public static void CopyrightInfo()
        {
            var type = System.Reflection.Assembly.GetExecutingAssembly().GetName().Name.Replace(".Editor", "");
            var link = "https://assetstore.unity.com/packages/vfx/shaders/fullscreen-camera-effects/271730";
            
            var ssc = SessionState.GetInt($"CopyrightInfo_{type}_33", UnityEngine.Random.Range(3, 7 + 1));
            SessionState.SetInt($"CopyrightInfo_{type}_33", ssc + 1);
            
            if (SessionState.GetBool($"CopyrightInfo_{type}", false) == false || ssc == 333)
            {
                Debug.Log($"<color=white>• You are using a <b>Non-Commercial</b> version of <b>{type}</b> developed by <b>NullTale</b></color>.\n" +
                          "When using this version, please remember to specify <b>Author Attribution</b> according to the <b>Licence</b> used.\n" +
                          "------------- - - - -  -  -\n" +
                          $"A full use <b>Licence</b> can be acquired on <a href=\"{link}\">Asset Store</a> or negotiated with the Author in cases of special licensing.\n \n");
             
                SessionState.SetBool($"CopyrightInfo_{type}", true);
            }
        }
    }
}

#endif
