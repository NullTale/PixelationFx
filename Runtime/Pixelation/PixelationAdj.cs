#if ARTISTIC_TOOLS
using System;
using System.Linq;
using UnityEngine;

namespace VolFx
{
    [AddComponentMenu("Layers/Mod/Pixelation")]
    public class PixelationAdj : ArtisticTools.Layer.Adjustment
    {
        private static readonly int s_Pixels    = Shader.PropertyToID("_Pixels");
        private static readonly int s_Color     = Shader.PropertyToID("_Color");
        private static readonly int s_Roundness = Shader.PropertyToID("_Roundness");
        
        public override string ShaderName => "Hidden/Vol/Pixelation";

        [Range(0, 1)]
        public float m_Scale = 1f;
        [Range(0, 1)]
        public float m_Grid = 1f;
        [Range(0, 1)]
        public float m_Roundness = .5f;
        [HideInInspector]
        public Color m_Color     = Color.clear;
        
        [HideInInspector]
        [CurveRange(0, .005f, 1, 1)] [Tooltip("Scale interpolation relative to the volume scale parameter")]
        public AnimationCurve _scaleLerp = new AnimationCurve(new Keyframe[]
        {
            new Keyframe(0, 0.0049f, .0508f, 0.0508f, .0f, .742f),
            new Keyframe(1, 0.5095f, 3.6093f, 3.6093f, .1646f, .0f)
        });
        
        [HideInInspector]
        [Range(0, 1)] [Tooltip("At what scale level does the grid become visible")]
        public float _gridReveal = 0.45f;
        [HideInInspector]
        [Tooltip("Grid impact discretization to reduce transition artifacts")]
        public bool _gridDiscrete = true;
        [Tooltip("Pixelate without texture sampling, keep colors, but parts of the image may disappear")]
        public bool _crisp = true;
        
        private bool     _posterLast;
        private bool     _crispLast;
        private bool     _firstRun;
        
        // =======================================================================
        public override void Init()
        {
            _firstRun = true;
        }

        public override bool Validate(Material mat)
        {
            _validateMat(mat, false, _crisp);

            var scale   = _scaleLerp.Evaluate(m_Scale);
            var height  = _scaleLerp.Evaluate(m_Scale) * Screen.height;
            var epsilon = 1f / (float)Screen.height;
            if (height < epsilon)
                height = epsilon;
            
            var roundness = m_Roundness;
            var aspect    = Screen.width / (float)Screen.height;
            var gridMul   = Mathf.Lerp(1f, 1.4142f, roundness);
            var gridscale = m_Grid * gridMul;
            
            if (_gridDiscrete)
            {
                var gridspace = 1f / Mathf.Floor(1f / scale);
                if (gridscale % gridspace > gridspace * .5f)
                    gridscale += gridspace - gridscale % gridspace;
                else
                    gridscale -= gridscale % gridspace;
                
                // do not override screen with black color if grid not zero
                if (gridscale == 0f && m_Grid > 0f)
                    gridscale = gridspace;
            }
            
            var pixels = new Vector4(height * aspect, height, gridscale * .5f, 64f);
            
            mat.SetVector(s_Pixels, pixels);
            mat.SetFloat(s_Roundness, Mathf.Lerp(1.4142f, 1f, roundness));
            mat.SetColor(s_Color, (height / Screen.height) < _gridReveal ? m_Color : Color.clear);
            return true;
        }
        
        private void _validateMat(Material mat, bool poster, bool crisp)
        {
            if (_firstRun)
            {
                // always apply parameters if first run
                _firstRun = false;
                
                poster = !_posterLast;
                crisp  = !_crispLast;
            }
            
            if (_crispLast != crisp)
            {
                _crispLast = _crisp;
                
                if (_crisp) mat.EnableKeyword("_CRISP");
                else        mat.DisableKeyword("_CRISP");
            }
            
            if (_posterLast != poster)
            { 
                _posterLast = poster;
                
                if (poster) mat.EnableKeyword("_POSTER");
                else        mat.DisableKeyword("_POSTER");
            }
        }
    }
}
#endif