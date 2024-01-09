#if !VOL_FX

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//  Pixelation © NullTale - https://twitter.com/NullTale/
namespace VolFx
{
    public class Pixelation : ScriptableRendererFeature
    {
        protected static List<ShaderTagId> k_ShaderTags;
        
        public static int s_BlitTexId       = Shader.PropertyToID("_BlitTexture");
        public static int s_BlitScaleBiasId = Shader.PropertyToID("_BlitScaleBias");
        
        [Tooltip("When to execute")]
        public RenderPassEvent _event  = RenderPassEvent.BeforeRenderingPostProcessing;
        
        public PixelationPass _pass;
        
        [HideInInspector]
        public Shader _blitShader;

        [NonSerialized]
        public Material _blit;
        
        [NonSerialized]
        public PassExecution _execution;

        // =======================================================================
        public class PassExecution : ScriptableRenderPass
        {
            public  Pixelation   _owner;
            private RenderTarget _output;
            
            // =======================================================================
            public void Init()
            {
                renderPassEvent = _owner._event;
                
                _output = new RenderTarget().Allocate(_owner.name);
            }

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                _owner._pass.Validate();
                if (_owner._pass.IsActive == false)
                    return;
                
                // allocate stuff
                var cmd = CommandBufferPool.Get(_owner.name);
                ref var cameraData = ref renderingData.cameraData;
                ref var desc = ref cameraData.cameraTargetDescriptor;
                _output.Get(cmd, in desc);

                var source = _getCameraTex(ref renderingData);
                
                // draw post process chain
                _owner._pass.Invoke(cmd, source, _output.Handle, context, ref renderingData);
                _owner.Blit(cmd, _output.Handle, source);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
                CommandBufferPool.Release(cmd);

                // -----------------------------------------------------------------------
                RTHandle _getCameraTex(ref RenderingData renderingData)
                {
                    ref var cameraData = ref renderingData.cameraData;
#if UNITY_2022_1_OR_NEWER                
                    return cameraData.renderer.cameraColorTargetHandle;
#else
                    return RTHandles.Alloc(cameraData.renderer.cameraColorTarget);
#endif
                }
            }
            
            public override void FrameCleanup(CommandBuffer cmd)
            {
                _output.Release(cmd);
                _output.Release(cmd);
                _owner._pass.Cleanup(cmd);
            }
        }
        
        // =======================================================================
        public void Blit(CommandBuffer cmd, RTHandle source, RTHandle destination)
        {
            cmd.SetGlobalVector(s_BlitScaleBiasId, new Vector4(1, 1, 0));
            cmd.SetGlobalTexture(s_BlitTexId, source);
            cmd.SetRenderTarget(destination, 0);
            cmd.DrawMesh(Utils.FullscreenMesh, Matrix4x4.identity, _blit, 0, 0);
        }
        
        public override void Create()
        {
#if UNITY_EDITOR
            _blitShader = Shader.Find("Hidden/Universal Render Pipeline/Blit");
            
            UnityEditor.EditorUtility.SetDirty(this);
#endif
            _blit      = new Material(_blitShader);
            _execution = new PassExecution() { _owner = this };
            _execution.Init();
            
            if (_pass != null)
                _pass._init();
            
            if (k_ShaderTags == null)
            {
                k_ShaderTags = new List<ShaderTagId>(new[]
                {
                    new ShaderTagId("SRPDefaultUnlit"),
                    new ShaderTagId("UniversalForward"),
                    new ShaderTagId("UniversalForwardOnly")
                });
            }
        }
        
        private void Reset()
        {
#if UNITY_EDITOR
            if (_pass != null)
            {
                UnityEditor.AssetDatabase.RemoveObjectFromAsset(_pass);
                UnityEditor.AssetDatabase.SaveAssets();
                _pass = null;
            }
#endif
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (renderingData.cameraData.cameraType != CameraType.Game)
                return;
#if UNITY_EDITOR
            if (_blit == null)
                _blit = new Material(_blitShader);
            
            if (_pass == null)
                return;
#endif
            renderer.EnqueuePass(_execution);
        }

        private void OnDestroy()
        {
#if UNITY_EDITOR
            if (_pass != null)
            {
                UnityEditor.AssetDatabase.RemoveObjectFromAsset(_pass);
                UnityEditor.AssetDatabase.SaveAssets();
                _pass = null;
            }
#endif
        }
    }
}
#endif