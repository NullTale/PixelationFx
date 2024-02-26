using System;
using System.Linq;
using UnityEngine;

namespace VolFx
{
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
}