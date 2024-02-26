using System;
using System.Collections.Generic;
using UnityEngine;

//  Pixelation © NullTale - https://twitter.com/NullTale/
namespace VolFx
{
    [ShaderName("Hidden/Vol/Pixelation")]
    public class PixelationPass : VolFx.Pass
    {
        private static readonly int s_Pixels = Shader.PropertyToID("_Pixels");
        private static readonly int s_Color  = Shader.PropertyToID("_Color");
        private static readonly int s_Roundness = Shader.PropertyToID("_Roundness");
        
        [CurveRange(0, .005f, 1, 1)] [Tooltip("Scale interpolation relative to the volume scale parameter")]
        public AnimationCurve _scaleLerp = new AnimationCurve(new Keyframe[]
            {
                new Keyframe(0, 0.0049f, .0508f, 0.0508f, .0f, .742f),
                new Keyframe(1, 0.5095f, 3.6093f, 3.6093f, .1646f, .0f)
            });
        
        [Range(0, 1)] [Tooltip("At what scale level does the grid become visible")]
        public float          _gridReveal = 0.45f;
        [Tooltip("Grid impact discretization to reduce transition artifacts")]
        public bool           _gridDiscrete = true;
        [Tooltip("Pixelate without texture sampling, keep colors, but parts of the image may disappear")]
        public bool           _crisp;
        [Range(0, 1)] [Tooltip("Default roundness if override is not set, volume roundness can be used for volume interpolations")]
        public float          _roundnessDefault = 1f;
        [Tooltip("Default palette texture")]
        public Optional<Texture2D> _palette;
        
        private bool                             _paletteLast;
        private bool                             _crispLast;
        private bool                             _firstRun;
        private Dictionary<Texture2D, Texture2D> _paletteCache = new Dictionary<Texture2D, Texture2D>();

        // =======================================================================
        public override void Init()
        {
            _firstRun = true;
            _paletteCache.Clear();
        }

        public override bool Validate(Material mat)
        {
            var settings = Stack.GetComponent<PixelationVol>();

            if (settings.IsActive() == false)
                return false;
            
            // access the palette
            var palette = settings.m_Palette.value as Texture2D;
            if (palette == null)
                palette = _palette.GetValueOrDefault();
            
            Texture2D paletteLut = null;
            if (palette != null && _paletteCache.TryGetValue(palette, out paletteLut) == false)
            {
                paletteLut = LutGenerator.Generate(palette);
                _paletteCache.Add(palette, paletteLut);
            }
            var usePalette = palette != null && settings.m_Impact.value > 0f;
            if (usePalette)
            {
                mat.SetTexture("_LutTex", paletteLut);
            }
            
            _validateMat(mat, usePalette, _crisp);

            var scale  = _scaleLerp.Evaluate(settings.m_Scale.value);
            var height = _scaleLerp.Evaluate(settings.m_Scale.value) * Screen.height;
            var epsilon = 1f / (float)Screen.height;
            if (height < epsilon)
                height = epsilon;
            
            var roundness = settings.m_Roundness.overrideState ? settings.m_Roundness.value : _roundnessDefault;
            var aspect    = Screen.width / (float)Screen.height;
            var gridMul   = Mathf.Lerp(1f, 1.4142f, roundness);
            var gridscale = settings.m_Grid.value * gridMul;
            
            if (_gridDiscrete)
            {
                var gridspace = 1f / Mathf.Floor(1f / scale);
                if (gridscale % gridspace > gridspace * .5f)
                    gridscale += gridspace - gridscale % gridspace;
                else
                    gridscale -= gridscale % gridspace;
                
                // do not override screen with black color if grid not zero
                if (gridscale == 0f && settings.m_Grid.value > 0f)
                    gridscale = gridspace;
            }
            
            var pixels = new Vector4(height * aspect, height, gridscale * .5f, settings.m_Impact.value);
            
            
            mat.SetVector(s_Pixels, pixels);
            mat.SetFloat(s_Roundness, Mathf.Lerp(1.4142f, 1f, roundness));
            mat.SetColor(s_Color, (height / Screen.height) < _gridReveal ? settings.m_Color.value : Color.clear);
            return true;
        }
         
        private void _validateMat(Material mat, bool palette, bool crisp)
        {
            if (_firstRun)
            {
                // always apply parameters if first run
                _firstRun = false;
                
                palette = !_paletteLast;
                crisp   = !_crispLast;
            }
            
            if (_crispLast != crisp)
            {
                _crispLast = _crisp;
                
                if (_crisp) mat.EnableKeyword("_CRISP");
                else        mat.DisableKeyword("_CRISP");
            }
            
            if (_paletteLast != palette)
            { 
                _paletteLast = palette;
                
                if (palette) mat.EnableKeyword("_USE_PALETTE");
                else        mat.DisableKeyword("_USE_PALETTE");
            }
        }
    }
}