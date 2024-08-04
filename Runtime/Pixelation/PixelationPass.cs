using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//  Pixelation © NullTale - https://twitter.com/NullTale/
namespace VolFx
{
    [ShaderName("Hidden/Vol/Pixelation")]
    public class PixelationPass : VolFx.Pass
    {
        private static readonly int s_Pixels    = Shader.PropertyToID("_Pixels");
        private static readonly int s_Color     = Shader.PropertyToID("_Color");
        private static readonly int s_Roundness = Shader.PropertyToID("_Roundness");
        private static readonly int s_LutTex    = Shader.PropertyToID("_LutTex");
        
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
        
        private                 bool                             _paletteLast;
        private                 bool                             _crispLast;
        private                 bool                             _firstRun;
        private                 Dictionary<Texture2D, Texture2D> _paletteCache = new Dictionary<Texture2D, Texture2D>();

        // =======================================================================
        public static class LutGenerator
        {
            private static Texture2D _lut16;
            private static Texture2D _lut32;
            private static Texture2D _lut64;

            // =======================================================================
            [Serializable]
            public enum LutSize
            {
                x16,
                x32,
                x64
            }

            [Serializable]
            public enum Gamma
            {
                rec601,
                rec709,
                rec2100,
                average,
            }
            
            // =======================================================================
            public static Texture2D Generate(Texture2D _palette, LutSize lutSize = LutSize.x16, Gamma gamma = Gamma.rec601)
            {
                var clean  = _getLut(lutSize);
                var lut    = clean.GetPixels();
                var colors = _palette.GetPixels();
                
                var _lutPalette = new Texture2D(clean.width, clean.height, TextureFormat.ARGB32, false);

                // grade colors from lut to palette by rgb 
                var palette = lut.Select(lutColor => colors.Select(gradeColor => (grade: compare(lutColor, gradeColor), color: gradeColor)).OrderBy(n => n.grade).First())
                                .Select(n => n.color)
                                .ToArray();
                
                _lutPalette.SetPixels(palette);
                _lutPalette.filterMode = FilterMode.Point;
                _lutPalette.wrapMode   = TextureWrapMode.Clamp;
                _lutPalette.Apply();
                
                return _lutPalette;

                // -----------------------------------------------------------------------
                float compare(Color a, Color b)
                {
                    // compare colors by grayscale distance
                    var weight = gamma switch
                    {
                        Gamma.rec601  => new Vector3(0.299f, 0.587f, 0.114f),
                        Gamma.rec709  => new Vector3(0.2126f, 0.7152f, 0.0722f),
                        Gamma.rec2100 => new Vector3(0.2627f, 0.6780f, 0.0593f),
                        Gamma.average => new Vector3(0.33333f, 0.33333f, 0.33333f),
                        _             => throw new ArgumentOutOfRangeException()
                    };

                    // var c = a.ToVector3().Mul(weight) - b.ToVector3().Mul(weight);
                    var c = new Vector3(a.r * weight.x, a.g * weight.y, a.b * weight.z) - new Vector3(b.r * weight.x, b.g * weight.y, b.b * weight.z);
                    
                    return c.magnitude;
                }
            }

            // =======================================================================
            internal static int _getLutSize(LutSize lutSize)
            {
                return lutSize switch
                {
                    LutSize.x16 => 16,
                    LutSize.x32 => 32,
                    LutSize.x64 => 64,
                    _           => throw new ArgumentOutOfRangeException()
                };
            }
            
            internal static Texture2D _getLut(LutSize lutSize)
            {
                var size = _getLutSize(lutSize);
                var _lut = lutSize switch
                {
                    LutSize.x16 => _lut16,
                    LutSize.x32 => _lut32,
                    LutSize.x64 => _lut64,
                    _           => throw new ArgumentOutOfRangeException(nameof(lutSize), lutSize, null)
                };
                
                if (_lut != null && _lut.height == size)
                     return _lut;
                
                _lut            = new Texture2D(size * size, size, TextureFormat.RGBA32, 0, false);
                _lut.filterMode = FilterMode.Point;

                for (var y = 0; y < size; y++)
                for (var x = 0; x < size * size; x++)
                    _lut.SetPixel(x, y, _lutAt(x, y));
                
                _lut.Apply();
                return _lut;

                // -----------------------------------------------------------------------
                Color _lutAt(int x, int y)
                {
                    return new Color((x % size) / (size - 1f), y / (size - 1f), Mathf.FloorToInt(x / (float)size) * (1f / (size - 1f)), 1f);
                }
            }
        }

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
                mat.SetTexture(s_LutTex, paletteLut);
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
            
            // if (_crisp)
            //     gridscale.
            
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