using UnityEngine;
using UnityEngine.Playables;

namespace ARML
{
    public class TransformLerpBehaviour : PlayableBehaviour
    {
        public double customClipStart;
        public double customClipEnd;

        public Vector3 targetPositionOffset;
        public Vector3 targetRotationOffset;
        public Vector3 targetScaleAbsolute;
        public AnimationCurve animCurve;

        private Transform targetTransform;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private Vector3 initialScale = Vector3.one;

        private bool firstFrameHappened;
        private bool isCurrentClip;
        private PlayableAsset clip;


        public override void ProcessFrame(Playable playable, FrameData info, object playerData)
        {
            // Get the target transform from the playerData object (assuming it's a GameObject with a Transform component).
            targetTransform = playerData as Transform;

            if (targetTransform == null)
                return;

            if (!firstFrameHappened)
            {
                initialPosition = targetTransform.localPosition;
                initialRotation = targetTransform.rotation;
                initialScale = targetTransform.localScale;

                //If no animation curve specified, default to linear
                if (animCurve == null)
                {
                    animCurve = new AnimationCurve();
                    animCurve = AnimationCurve.Linear(0, 0, 1, 1);
                }

                firstFrameHappened = true;
            }

            // Calculate the progress based on the current time of the TransformLerpClip.
            float clipTime = (float)(playable.GetTime() / playable.GetDuration());

            // Lerp position if enabled.
            if (targetPositionOffset != Vector3.zero)
            {
                Vector3 lerpedPosition = Vector3.Lerp(initialPosition, initialPosition + targetPositionOffset, animCurve.Evaluate(clipTime));
                targetTransform.localPosition = lerpedPosition;
            }

            // Lerp rotation if enabled.
            if (targetRotationOffset != Vector3.zero)
            {
                Quaternion lerpedRotation = Quaternion.Slerp(initialRotation, initialRotation * Quaternion.Euler(targetRotationOffset), animCurve.Evaluate(clipTime));
                targetTransform.localRotation = lerpedRotation;
            }

            // Lerp scale if enabled.
            if (targetScaleAbsolute != initialScale)
            {
                Vector3 lerpedScale = Vector3.Lerp(initialScale, targetScaleAbsolute, clipTime);
                targetTransform.localScale = lerpedScale;
            }
        }

        //Trying to set clip initial and end position when scrolling through timeline or clicking off - not necessary but useful
        public override void OnBehaviourPause(Playable playable, FrameData info)
        {
            if (!targetTransform) return;

            double currentDirectorTime = playable.GetGraph().GetRootPlayable(0).GetTime();

            //If timeline time is BEFORE start of clip, set initial values
            if (currentDirectorTime < customClipStart)
            {
                targetTransform.localPosition = initialPosition;
                targetTransform.localRotation = initialRotation;
            }
            //If timeline time is AFTER end of clip, set final values
            else if (currentDirectorTime > customClipEnd)
            {
                targetTransform.localPosition = initialPosition + targetPositionOffset;
                targetTransform.localRotation = initialRotation * Quaternion.Euler(targetRotationOffset);

            }

            isCurrentClip = false;

            //var timeline = playableDirector.playableAsset as TimelineAsset;
            //foreach (var track in timeline.GetOutputTracks())
            //{
            //    foreach (var clip in track.GetClips())
            //        Debug.Log(clip.start);

            //}
        }

        public override void OnBehaviourPlay(Playable playable, FrameData info)
        {
            isCurrentClip = true;
        }

    }
}