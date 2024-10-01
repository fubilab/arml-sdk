using System.Collections;
using UnityEngine;

namespace ARML
{
    /// <summary>
    /// Handles the frequency and weight of a character's blendshape blink.
    /// </summary>
    public class CharacterBlink : MonoBehaviour
    {
        [SerializeField] SkinnedMeshRenderer renderer;
        [SerializeField] int blinkBlendShapeIndex = 16;
        [SerializeField] float blinkRateMin = 1f;
        [SerializeField] float blinkRateMax = 10f;
        private float currentBlinkWeight = 0f;

        // Start is called before the first frame update
        void Start()
        {
            if (renderer == null) return;

            StartCoroutine(Blink());
        }

        IEnumerator Blink()
        {
            //Close eyes
            float timeElapsed = 0f;
            while (timeElapsed < 0.1f)
            {
                currentBlinkWeight = Mathf.Lerp(0f, 100f, timeElapsed / 0.1f);
                renderer.SetBlendShapeWeight(blinkBlendShapeIndex, currentBlinkWeight);
                timeElapsed += Time.deltaTime;
                yield return null;
            }
            currentBlinkWeight = 100f;

            //Wait
            yield return new WaitForSeconds(0.1f);

            //Open Eyes
            timeElapsed = 0f;
            while (timeElapsed < 0.1f)
            {
                currentBlinkWeight = Mathf.Lerp(100f, 0f, timeElapsed / 0.1f);
                renderer.SetBlendShapeWeight(blinkBlendShapeIndex, currentBlinkWeight);
                timeElapsed += Time.deltaTime;
                yield return null;
            }
            currentBlinkWeight = 0f;

            //Restart
            yield return new WaitForSeconds(Random.Range(blinkRateMin, blinkRateMax));
            StartCoroutine(Blink());
        }
    }
}