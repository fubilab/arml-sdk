using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Canvas))]
[DisallowMultipleComponent]
public class MenuController : MonoBehaviour
{
    [SerializeField]
    private Page initialPage;
    [SerializeField]
    private GameObject firstFocusItem;

    private Canvas rootCanvas;
    private Stack<Page> pageStack = new Stack<Page>();

    private void Awake()
    {
        rootCanvas = GetComponent<Canvas>();
    }

    // Start is called before the first frame update
    void Start()
    {
        if(firstFocusItem != null)
        {
            EventSystem.current.SetSelectedGameObject(firstFocusItem);
        }

        if(initialPage != null)
        {
            PushPage(initialPage);
        }
    }

    private void OnCancel()
    {
        if(rootCanvas.enabled && rootCanvas.gameObject.activeInHierarchy)
        {
            if(pageStack.Count > 0)
            {
                PopPage();
            }
        }
    }

    public bool IsPageInStack(Page page)
    {
        return pageStack.Contains(page);
    }

    public bool IsPageOnTopOfStack(Page page)
    {
        return pageStack.Count > 0 &&  page == pageStack.Peek();
    }

    public void PushPage(Page page)
    {
        page.Enter(true);

        if(pageStack.Count > 0)
        {
            //Check if the next page on stack exits on new page push, if it does, exit (pop) without sound as we are already playing push sound
            Page currentPage = pageStack.Peek();

            if (currentPage.exitOnNewPagePush)
            {
                currentPage.Exit(false);
            }
        }

        pageStack.Push(page);
    }

    public void PopPage()
    {
        if(pageStack.Count > 1)
        {
            Page page = pageStack.Pop();
            page.Exit(true);

            Page newCurrentPage = pageStack.Peek();
            if (newCurrentPage.exitOnNewPagePush)
            {
                newCurrentPage.Enter(false);
            }
        }
        else
        {
            Debug.LogWarning("Trying to pop a page but only 1 page remains in the stack.");
        }
    }

    public void PopAllPages()
    {
        for (int i = 1; i < pageStack.Count; i++)
        {
            PopPage();
        }
    }
}
