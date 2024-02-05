using DG.Tweening;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

public class DOTweenTimelineClip : PlayableAsset//, IPropertyPreview
{
    public Ease ease;
    public LoopType loopType;

    [HideInInspector] public double customClipStart { get; set; }
    [HideInInspector] public double customClipEnd { get; set; }

    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<DOTweenTimelineBehaviour>.Create(graph);

        var behaviour = playable.GetBehaviour();
        //behaviour.targetPositionOffset = targetPositionOffset;

        behaviour.customClipStart = customClipStart;
        behaviour.customClipEnd = customClipEnd;

        return playable;
    }
}