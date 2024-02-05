using UnityEngine;

/// <summary>
/// Singleton behavior class
/// </summary>
public class SingletonBehavior<T> : MonoBehaviour where T : Component
{
    private static T _instance;
    public static T Instance
    {
        get
        {
            if (!_instance)
            {
                T[] objects = FindObjectsOfType<T>();
                if (objects.Length > 1)
                {
                    throw new System.Exception("SingletonBehavior cannot be attached to more than one GameObject.");
                }
                _instance = objects[0];
            }
            return _instance;
        }
    }
}