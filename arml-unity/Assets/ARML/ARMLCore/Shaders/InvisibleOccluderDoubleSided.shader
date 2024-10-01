Shader "Fran/InvisibleOccluderDoubleSided" {
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass {
            ZWrite On
            ColorMask 0
            Cull Off
        }
    }
}