Shader "Fran/StencilGeom"
{
    Properties
    {
        [IntRange] _StencilID ("Stencil ID", Range(0,255)) = 0
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Opaque" 
            "RenderPipeline" = "UniversalPipeline" 
            "Queue" = "Geometry-1"
        }
        
        Pass
        {
            Blend Zero One
            ZWrite Off

            Stencil
            {
                Ref [_StencilID]
                Comp Always 
                Pass Replace
                Fail Keep
            }
        }

    }
}
