using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ARML.Interaction
{
    /// <summary>
    /// Controls whether the camera can interact with more than one CameraPointedObject at the same time
    /// </summary>
    public class CameraObjectSelectionController : MonoBehaviour
    {
        public List<CameraPointedObject> currentlySelectedObjects = new List<CameraPointedObject>();

        [Tooltip("If on, can only select one CameraPointedObject at a time will choose the closest one")]
        public bool canOnlySelectOneObject = false;

        public void AddObjectToCurrentlySelected(CameraPointedObject c)
        {
            if (!currentlySelectedObjects.Contains(c))
            {
                currentlySelectedObjects.Add(c);
                SortCurrentlySelectedObjects();
            }
        }

        public void RemoveObjectFromCurrentlySelected(CameraPointedObject c)
        {
            if (currentlySelectedObjects.Contains(c))
            {
                currentlySelectedObjects.Remove(c);
            }
        }

        public bool IsClosestObject(CameraPointedObject c)
        {
            if (currentlySelectedObjects.Count == 0)
            {
                return false;
            }

            // Assumes the list is always sorted after any modification
            return currentlySelectedObjects[0] == c;
        }

        private void SortCurrentlySelectedObjects()
        {
            currentlySelectedObjects = currentlySelectedObjects.OrderBy(obj => obj.angleToCamera).ToList();
        }
    }
}