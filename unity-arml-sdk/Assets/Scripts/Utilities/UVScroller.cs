using UnityEngine;

/// <summary>
/// Scrolls the UV coordinates of a material's texture, creating a moving texture effect.
/// Useful for creating animations like flowing water or moving clouds.
/// </summary>
public class UVScroller : MonoBehaviour
{
    [SerializeField] Vector2 uvSpeed = new Vector2(0.0f, 0.0f);
    private Renderer renderer;

    /// <summary>
    /// Initializes the component by getting the Renderer attached to the GameObject.
    /// </summary>
    private void Start()
    {
        renderer = GetComponent<Renderer>();
    }

    /// <summary>
    /// Continuously updates the UV offset of the material based on the specified speed,
    /// effectively scrolling the texture.
    /// </summary>
    private void Update()
    {
        // Calculate new UV offset based on time and speed
        Vector2 offset = uvSpeed * Time.time;

        // Set the offset to the material
        renderer.material.SetTextureOffset("_BaseMap", offset);
    }
}
