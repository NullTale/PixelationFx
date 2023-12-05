using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//  Pixelation © NullTale - https://twitter.com/NullTale/
namespace VolFx
{
    [Serializable, VolumeComponentMenu("Vol/Pixelation")]
    public sealed class PixelationVol : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter         m_Scale     = new ClampedFloatParameter(1, 0, 1f);
        public NoInterpClampedFloatParameter m_Grid      = new NoInterpClampedFloatParameter(1f, 0, 1f);
        public GridParameter                 m_Type      = new GridParameter(Grid.Circle, false);
        public NoInterpColorParameter        m_Color     = new NoInterpColorParameter(Color.black);
        public ClampedIntParameter           m_Posterize = new ClampedIntParameter(64, 1, 64);

        // =======================================================================
        [Serializable]
        public class GridParameter : VolumeParameter<Grid>
        {
            public GridParameter(Grid value, bool overrideState) : base(value, overrideState) { }
        }

        [Serializable]
        public enum Grid
        {
            Circle,
            Square,
        }
        
        // =======================================================================
        public bool IsActive() => active && (m_Scale.value < 1 || m_Posterize.value < m_Posterize.max);

        public bool IsTileCompatible() => false;
    }
}