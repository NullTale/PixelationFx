using System;
using UnityEngine;

//  Pixelation © NullTale - https://twitter.com/NullTale/
namespace VolFx
{
    [ShaderName("Hidden/Vol/Pixelation")]
    public class PixelationPass : VolFx.Pass
    {
        private static readonly int s_Pixels = Shader.PropertyToID("_Pixels");
        private static readonly int s_Color  = Shader.PropertyToID("_Color");
        
        [CurveRange(0, .005f, 1, 1)] [Tooltip("Scale interpolation relative to the volume scale parameter")]
        public AnimationCurve _scaleLerp = new AnimationCurve(new Keyframe[]
            {
                new Keyframe(0, 0.0049f, .0508f, 0.0508f, .0f, .742f),
                new Keyframe(1, 0.5095f, 3.6093f, 3.6093f, .1646f, .0f)
            });
        
        [Range(0, 1)] [Tooltip("At what scale level the grid become visible")]
        public float          _gridTreshold = 0.45f;
        
        private bool               _posterLast;
        private PixelationVol.Grid _gridLast;
        private bool               _firstRun;

        // =======================================================================
        public override void Init()
        {
            _firstRun = true;
        }

        public override bool Validate(Material mat)
        {
            var settings = Stack.GetComponent<PixelationVol>();

            if (settings.IsActive() == false)
                return false;
            
            _validateMat(mat, settings.m_Posterize.overrideState, settings.m_Type.value);

            var val     = _scaleLerp.Evaluate(settings.m_Scale.value) * Screen.height;
            var epsilon = 1f / (float)Screen.height;
            if (val < epsilon)
                val = epsilon;
            
            var aspect  = Screen.width / (float)Screen.height;
            var gridMul = settings.m_Type.value == PixelationVol.Grid.Square ? 1f : 1.4142f;
            var pixels  = new Vector4(val * aspect, val, settings.m_Grid.value * .5f * gridMul, settings.m_Posterize.value);
            
            mat.SetVector(s_Pixels, pixels);
            mat.SetColor(s_Color, (val / Screen.height) < _gridTreshold ? settings.m_Color.value : Color.clear);
            return true;
        }
        
        private void _validateMat(Material mat, bool poster, PixelationVol.Grid grid)
        {
            if (_firstRun)
            {
                // always apply parameters if first run
                _firstRun = false;
                
                poster = !_posterLast;
                grid = _gridLast == PixelationVol.Grid.Circle ? PixelationVol.Grid.Square : PixelationVol.Grid.Circle;
            }
            
            if (_posterLast != poster)
            { 
                _posterLast = poster;
                
                if (poster) mat.EnableKeyword("_POSTER");
                else        mat.DisableKeyword("_POSTER");
            }
            
            if (_gridLast != grid)
            {
                _gridLast = grid;
                
                mat.DisableKeyword("_CIRCLE");
                mat.DisableKeyword("_SQUARE");
                
                switch (grid)
                {
                    case PixelationVol.Grid.Circle:
                        mat.EnableKeyword("_CIRCLE");
                        break;
                    case PixelationVol.Grid.Square:
                        mat.EnableKeyword("_SQUARE");
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(grid), grid, null);
                }
            }
        }
    }
}