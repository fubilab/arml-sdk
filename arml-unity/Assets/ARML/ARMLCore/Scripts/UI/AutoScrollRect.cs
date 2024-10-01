/*
MIT License
 
Copyright (c) 2023 Martin Jonasson
 
Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:
 
The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.
 
THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class AutoScrollRect : MonoBehaviour
{

    public ScrollRect scrollRect;
    public RectTransform viewportRectTransform;
    public RectTransform contentRectTransform;

    RectTransform selectedRectTransform;

    void Update()
    {
        var selected = EventSystem.current.currentSelectedGameObject;
        // nothing is selected, bail
        if (selected == null) return;

        // whatever is selected isn't a descendant of the scroll rect, we can ignore it
        if (!selected.transform.IsChildOf(contentRectTransform)) return;

        selectedRectTransform = selected.GetComponent<RectTransform>();
        var viewportRect = viewportRectTransform.rect;

        // transform the selected rect from its local space to the content rect space
        var selectedRect = selectedRectTransform.rect;
        var selectedRectWorld = selectedRect.Transform(selectedRectTransform);
        var selectedRectViewport = selectedRectWorld.InverseTransform(viewportRectTransform);

        // now we can calculate if we're outside the viewport either on top or on the bottom
        var outsideOnTop = selectedRectViewport.yMax - viewportRect.yMax;
        var outsideOnBottom = viewportRect.yMin - selectedRectViewport.yMin;

        // if these values are positive, we're outside the viewport
        // if they are negative, we're inside, i zero any "inside" values here to keep things easier to reason about
        if (outsideOnTop < 0) outsideOnTop = 0;
        if (outsideOnBottom < 0) outsideOnBottom = 0;

        // pick the direction to scroll
        // if the selection is big it could possibly be outside on both ends, i prioritize the top here
        var delta = outsideOnTop > 0 ? outsideOnTop : -outsideOnBottom;

        // if no scroll, we bail
        if (delta == 0) return;

        // now we transform the content rect into the viewport space
        var contentRect = contentRectTransform.rect;
        var contentRectWorld = contentRect.Transform(contentRectTransform);
        var contentRectViewport = contentRectWorld.InverseTransform(viewportRectTransform);

        // using this we can calculate how much of the content extends past the viewport
        var overflow = contentRectViewport.height - viewportRect.height;

        // now we can use the overflow from earlier to work out how many units the normalized scroll will move us, so
        // we can scroll exactly to where we need to
        var unitsToNormalized = 1 / overflow;
        scrollRect.verticalNormalizedPosition += delta * unitsToNormalized;
    }
}

internal static class RectExtensions
{
    /// <summary>
    /// Transforms a rect from the transform local space to world space.
    /// </summary>
    public static Rect Transform(this Rect r, Transform transform)
    {
        return new Rect
        {
            min = transform.TransformPoint(r.min),
            max = transform.TransformPoint(r.max),
        };
    }

    /// <summary>
    /// Transforms a rect from world space to the transform local space
    /// </summary>
    public static Rect InverseTransform(this Rect r, Transform transform)
    {
        return new Rect
        {
            min = transform.InverseTransformPoint(r.min),
            max = transform.InverseTransformPoint(r.max),
        };
    }
}