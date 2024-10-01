using TMPro;
using UnityEngine;

namespace ARML
{
    /// <summary>
    /// Calculates and displays the distance between two objects, with an optional offset, using TextMeshPro.
    /// </summary>
    public class DistanceCalculator : MonoBehaviour
    {
        [SerializeField] private GameObject objectA;
        [SerializeField] private GameObject objectB;
        [SerializeField] private Vector3 offset = Vector3.zero;

        private TMP_Text displayText;

        /// <summary>
        /// Called before the first frame update. Initializes the TextMeshPro component.
        /// </summary>
        private void Start()
        {
            displayText = GetComponent<TMP_Text>();
        }

        /// <summary>
        /// Called once per frame. Calculates and displays the distance between the two specified objects, considering an offset.
        /// </summary>
        void Update()
        {
            if (objectA == null || objectB == null) return;

            Vector3 distance = objectB.transform.position - objectA.transform.position;
            distance = distance + offset;

            displayText.text = distance.ToString();
        }
    }
}