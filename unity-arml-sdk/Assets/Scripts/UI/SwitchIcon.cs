using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SwitchIcon : MonoBehaviour
{
    [SerializeField] Sprite firstImage;
    [SerializeField] Sprite secondImage;
    private int currentImage;

    public void ToggleIconImage(Image imageRenderer)
    {
        if(currentImage == 0)
        {
            imageRenderer.sprite = secondImage;
            currentImage = 1;
        }
        else
        {
            imageRenderer.sprite = firstImage;
            currentImage = 0;
        }
    }
}
