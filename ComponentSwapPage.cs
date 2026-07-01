using UnityEngine;

public class ComponentSwapPage : MonoBehaviour
{
    [SerializeField]
    private GameObject leftButton;

    [SerializeField]
    private GameObject rightButton;

    [SerializeField]
    private int itemsPerPage = 36;

    private int currentPage = 0;
    private int totalPages = 0;

    private void Start()
    {
        RefreshPage();
    }

    public void NextPage(int pageOffset)
    {
        currentPage += pageOffset;
        RefreshPage();
    }

    private void RefreshPage()
    {
        int childCount = transform.childCount;

        // ไม่มี Item
        if (childCount == 0)
        {
            leftButton.SetActive(false);
            rightButton.SetActive(false);

            currentPage = 0;
            totalPages = 0;

            return;
        }

        // คำนวณจำนวนหน้า
        totalPages = Mathf.CeilToInt(childCount / (float)itemsPerPage);

        // จำกัดไม่ให้ออกนอกช่วง
        currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);

        // อัปเดตปุ่ม
        leftButton.SetActive(currentPage > 0);
        rightButton.SetActive(currentPage < totalPages - 1);

        // แสดงเฉพาะ Item ของหน้าปัจจุบัน
        int startIndex = currentPage * itemsPerPage;
        int endIndex = startIndex + itemsPerPage;

        foreach (Transform child in transform)
        {
            int index = child.GetSiblingIndex();

            bool shouldShow = index >= startIndex && index < endIndex;

            child.gameObject.SetActive(shouldShow);
        }

        Debug.Log($"Page {currentPage + 1}/{totalPages}");
    }
}
