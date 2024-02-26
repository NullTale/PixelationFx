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
        public NoInterpColorParameter        m_Color     = new NoInterpColorParameter(Color.clear);
        public Texture2DParameter            m_Palette  = new Texture2DParameter(null, false);
        public ClampedFloatParameter         m_Impact   = new ClampedFloatParameter(0, 0, 1);
        
        // =======================================================================
        public bool IsActive() => active && (m_Scale.value < 1 || (m_Palette.value != null && m_Impact.value > 0f));

        public bool IsTileCompatible() => false;
    }
}