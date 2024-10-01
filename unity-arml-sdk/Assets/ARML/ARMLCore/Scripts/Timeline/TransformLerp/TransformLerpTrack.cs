using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace ARML
{
    [TrackClipType(typeof(TransformLerpClip))]
    [TrackBindingType(typeof(Transform))]
    public class TransformLerpTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            foreach (var clip in GetClips())
            {
                var customClip = clip.asset as TransformLerpClip;
                if (customClip != null)
                {
                    customClip.customClipStart = clip.start;
                    customClip.customClipEnd = clip.end;
                }
            }
            return ScriptPlayable<TransformLerpMixerBehaviour>.Create(graph, inputCount);
        }
    }
}