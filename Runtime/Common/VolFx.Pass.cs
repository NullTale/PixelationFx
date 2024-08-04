#if !VOL_FX

using System;
using System.IO;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//  Pixelation © NullTale - https://twitter.com/NullTale/
namespace VolFx
{
    public static class VolFx
    {
        [Serializable]
        public abstract class Pass : ScriptableObject
        {
            [NonSerialized]
            public Pixelation _owner;
            [SerializeField]
            internal bool _active = true;
            [SerializeField] [HideInInspector]
            private Shader _shader;
            protected Material _material;
            private   bool     _isActive;
            
            protected VolumeStack Stack => VolumeManager.instance.stack;
            
            protected virtual bool Invert => false;

            // =======================================================================
            internal bool IsActive
            {
                get => _isActive && _active && _material != null;
                set => _isActive = value;
            }
            
            public void SetActive(bool isActive)
            {
                _active = isActive;
            }
            
            internal void _init()
            {
#if UNITY_EDITOR
#if !UNITY_2022_1_OR_NEWER
                Debug.LogError($"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name} require Unity 2022 or higher");
#endif
                if (_shader == null || _material == null)
                {
                    var shaderName = GetType().GetCustomAttributes(typeof(ShaderNameAttribute), true).FirstOrDefault() as ShaderNameAttribute;
                    if (shaderName != null)
                    {
                        _shader = Shader.Find(shaderName._name);
                        UnityEditor.EditorUtility.SetDirty(this);
                    }
                }
#endif
                
                if (_shader != null)
                    _material = new Material(_shader);
                
                Init();
            }

            public virtual void Invoke(CommandBuffer cmd, RTHandle source, RTHandle dest, ScriptableRenderContext context, ref RenderingData renderingData)
            {
                Utils.Blit(cmd, source, dest, _material, 0, Invert);
            }
            
            public void Validate()
            {
#if UNITY_EDITOR
                if (_shader == null || _editorValidate)
                {
                    var shaderName = GetType().GetCustomAttributes(typeof(ShaderNameAttribute), true).FirstOrDefault() as ShaderNameAttribute;
                    if (shaderName != null)
                    {
                        _shader = Shader.Find(shaderName._name);
                        var assetPath = UnityEditor.AssetDatabase.GetAssetPath(_shader);
                        if (string.IsNullOrEmpty(assetPath) == false)
                            _editorSetup(Path.GetDirectoryName(assetPath), Path.GetFileNameWithoutExtension(assetPath));
                        
                        UnityEditor.EditorUtility.SetDirty(this);
                    }
                }
                
                if ((_material == null || _material.shader != _shader) && _shader != null)
                {
                    _material = new Material(_shader);
                    Init();
                }
#endif
                
                IsActive = Validate(_material);
            }

            /// <summary>
            /// called to initialize pass when material is created
            /// </summary>
            public virtual void Init()
            {
            }

            /// <summary>
            /// called each frame to check is render is required and setup render material
            /// </summary>
            public abstract bool Validate(Material mat);
            
            /// <summary>
            /// frame clean up function used if implemented custom Invoke function to release resources
            /// </summary>
            public virtual void Cleanup(CommandBuffer cmd)
            {
            }
            
            /// <summary>
            /// used for optimization purposes, returns true if we need to call _editorSetup function
            /// </summary>
            protected virtual bool _editorValidate => false;
            
            /// <summary>
            /// editor validation function, used to gather additional references 
            /// </summary>
            protected virtual void _editorSetup(string folder, string asset)
            {
            }
        }
    }
}
#endif