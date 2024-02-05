using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Playables;
using DG.Tweening;

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
