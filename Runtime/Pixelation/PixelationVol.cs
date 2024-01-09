using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//  Pixelation © NullTale - https://twitter.com/NullTale/
namespace VolFx
{
    [Serializable, VolumeComponentMenu("VolFx/Pixelation")]
    public sealed class PixelationVol : VolumeComponent, IPostProcessComponent
    {
        public ClampedFloatParameter         m_Scale     = new ClampedFloatParameter(1, 0, 1f);
        public NoInterpClampedFloatParameter m_Grid      = new NoInterpClampedFloatParameter(1f, 0, 1f);
        public ClampedFloatParameter         m_Roundness = new ClampedFloatParameter(.5f, 0f, 1f);
        public NoInterpColorParameter        m_Color     = new NoInterpColorParameter(Color.black);
        public ClampedIntParameter           m_Posterize = new ClampedIntParameter(64, 1, 64);
        
        // =======================================================================
        public bool IsActive() => active && (m_Scale.value < 1 || m_Posterize.value < m_Posterize.max);

        public bool IsTileCompatible() => false;
    }
}