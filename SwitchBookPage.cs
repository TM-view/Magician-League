using UnityEngine;
using UnityEngine.UI;

public class SwitchBookPage : MonoBehaviour
{
    [SerializeField]
    private Sprite bookPage1Image;

    [SerializeField]
    private Sprite bookPage2Image;

    [SerializeField]
    private GameObject bookPage1;

    [SerializeField]
    private GameObject bookPage2;

    private int bookPage = 1;
    private Image myImage;

    void Start()
    {
        myImage = GetComponent<Image>();
        RefreshPage();
    }

    public void NextPage(int pageOffset)
    {
        bookPage = pageOffset;
        RefreshPage();
    }

    void RefreshPage()
    {
        if (bookPage == 1)
        {
            bookPage1.SetActive(true);
            bookPage2.SetActive(false);
            myImage.sprite = bookPage1Image;
        }
        else if (bookPage == 2)
        {
            bookPage1.SetActive(false);
            bookPage2.SetActive(true);
            myImage.sprite = bookPage2Image;
        }
    }
}
