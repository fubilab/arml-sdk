using UnityEngine;
using UnityEngine.Playables;

namespace ARML
{
    public class TransformLerpMixerBehaviour : PlayableBehaviour
    {
        private bool _firstFrameHappened;
        private Quaternion _defaultRotation;
        private Transform _trackBinding;

        //public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        //{
        //    _trackBinding = playerData as Transform;

        //    if (!_trackBinding)
        //    { return; }

        //    int inputCount = playable.GetInputCount();
        //    if (inputCount == 0)
        //    { return; }

        //    //Picked up this trick from the LightControlMixerBehaviour in the DefaultPlayables examples.
        //    if (!_firstFrameHappened)
        //    {
        //        _defaultRotation = _trackBinding.rotation;
        //        _firstFrameHappened = true;
        //    }

        //    double playTime = playable.GetGraph().GetRootPlayable(0).GetTime();
        //    Quaternion rotation = Quaternion.identity;

        //    for (int i = 0; i < inputCount; i++)
        //    {
        //        var inputPlayable = (ScriptPlayable<TransformLerpBehaviour>)playable.GetInput(i);
        //        TransformLerpBehaviour input = inputPlayable.GetBehaviour();

        //        double customDuration = input.customClipEnd - input.customClipStart;
        //        float normalizedClipPosition = Mathf.Clamp01((float)((playTime - input.customClipStart) / customDuration));

        //        //Insert logic here
        //    }

        //    //_trackBinding.rotation = _defaultRotation * rotation;
        //}
    }
}