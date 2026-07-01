using UnityEngine;

public class UIManager : MonoBehaviour
{
    [SerializeField]
    private GameObject spellPanel;

    [SerializeField]
    private GameObject completeSpellsPanel;
    public int latestSpellIndex = 0;
    public SpellComponentSO spellComponentBeingDragged;
    public bool isDraggingComponent = false;
    public bool spellPanelOpen = false;

    public bool IsSpellBookOpen => spellPanel != null && spellPanel.activeSelf;
    public bool SpellPanelOpen => IsSpellBookOpen;
    public bool BlocksSpellCasting => IsSpellBookOpen;

    [SerializeField]
    private Transform previousBtn;

    void Start()
    {
        if (spellPanel != null)
        {
            spellPanel.SetActive(spellPanelOpen);
            spellPanelOpen = spellPanel.activeSelf;
        }

        if (latestSpellIndex < 0)
        {
            latestSpellIndex = 0;
        }

        SelectLatestCompleteSpell(spellPanelOpen);
    }

    public void ToggleSpellPanel() //เปิดปิดสมุด
    {
        Debug.Log("Toggling Spell Panel");
        spellPanelOpen = !spellPanelOpen;
        if (spellPanel != null)
        {
            spellPanel.SetActive(spellPanelOpen);
            spellPanelOpen = spellPanel.activeSelf;
        }

        if (spellPanelOpen)
        {
            SelectLatestCompleteSpell(true);
        }
    }

    public void ToggleComponentOfCompleteSpellpanel(Transform btn) //เปิดปิดหรือสลับสลอตใส่คอมโพเนนต์ของเวทย์มนต์
    {
        int index = btn.GetSiblingIndex();
        bool shouldOpenPanel = completeSpellsPanel == null || !completeSpellsPanel.activeSelf;

        if (index != latestSpellIndex || shouldOpenPanel)
        {
            if (completeSpellsPanel != null)
            {
                completeSpellsPanel.SetActive(true);
            }

            if (previousBtn != null)
            {
                previousBtn.GetComponent<CompleteSpellButton>().UpdateButtonColor();
            }

            previousBtn = btn;
            latestSpellIndex = index;

            CompleteSpellButton selectedButton = btn.GetComponent<CompleteSpellButton>();
            if (selectedButton != null)
            {
                selectedButton.SelectForEditing();
            }
        }
        else
        {
            if (completeSpellsPanel != null)
            {
                completeSpellsPanel.SetActive(false);
            }
        }
    }

    private void SelectLatestCompleteSpell(bool openCompleteSpellPanel)
    {
        CompleteSpellButton selectedButton = FindCompleteSpellButton(latestSpellIndex);
        if (selectedButton == null)
        {
            return;
        }

        previousBtn = selectedButton.transform;

        if (openCompleteSpellPanel && completeSpellsPanel != null)
        {
            completeSpellsPanel.SetActive(true);
        }

        selectedButton.SelectForEditing();
        selectedButton.UpdateButtonColor();
    }

    private CompleteSpellButton FindCompleteSpellButton(int spellIndex)
    {
        CompleteSpellButton[] buttons = FindObjectsOfType<CompleteSpellButton>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            CompleteSpellButton button = buttons[i];
            if (button == null)
            {
                continue;
            }

            button.EnsureInitialized();
            if (button.transform.GetSiblingIndex() == spellIndex)
            {
                return button;
            }
        }

        return null;
    }
}
