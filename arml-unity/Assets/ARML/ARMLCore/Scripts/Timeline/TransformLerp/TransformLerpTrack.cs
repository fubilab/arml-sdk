using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ARML.Timeline
{
    [TrackClipType(typeof(TransformLerpClip))]
    [TrackBindingType(typeof(Transform))]
    public class TransformLerpTrack : TrackAsset
    {
        
    }
}