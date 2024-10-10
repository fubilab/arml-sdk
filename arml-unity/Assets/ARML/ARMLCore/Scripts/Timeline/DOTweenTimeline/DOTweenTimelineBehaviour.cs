using DG.Tweening;
using UnityEngine;
using UnityEngine.Playables;

namespace ARML.Timeline
{
    public class DOTweenTimelineBehaviour : PlayableBehaviour
    {
        public double customClipStart;
        public double customClipEnd;

        public Ease ease;
        public LoopType loopType;

        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            if (Application.isPlaying)
            {
                GameObject go = playerData as GameObject;

                //go.SetActive(setActive);
            }
        }
    }
}