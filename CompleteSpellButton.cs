using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CompleteSpellButton
    : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerClickHandler
{
    private const int MaxSpellNameLength = 14;
    private Image myImage;

    [SerializeField]
    private GameObject completeSpellDataPanel;

    [SerializeField]
    private TMP_InputField spellNameInputText;

    [SerializeField]
    private TMP_Text spellNameText;

    [SerializeField]
    private TMP_Text componentListText;

    [SerializeField]
    private TMP_Text manaCostText;

    [SerializeField]
    private UIManager uiManager;

    [SerializeField]
    private List<GameObject> completeSpellsComponentButtons;
    private RectTransform completeSpellDataPanelRT;
    private RectTransform MyRT;
    private int myIndex;
    private int componentsCount;

    [SerializeField]
    private CompleteSpellSO originalSpell;
    public CompleteSpellSO completeSpells;
    private bool initialized;
    private bool nameInputListenerRegistered;

    void Awake()
    {
        EnsureInitialized();
    }

    void Start()
    {
        EnsureInitialized();
        RegisterNameInputListener();
        if (myIndex == 0)
            UpdateButtonColor();
    }

    public void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        myIndex = transform.GetSiblingIndex();
        completeSpellDataPanelRT =
            completeSpellDataPanel != null
                ? completeSpellDataPanel.GetComponent<RectTransform>()
                : null;
        MyRT = GetComponent<RectTransform>();
        myImage = GetComponent<Image>();

        if (completeSpells == null && originalSpell != null)
        {
            completeSpells = Instantiate(originalSpell);
            if (myIndex != 0)
            {
                completeSpells.Reset();
            }
        }

        ClampCurrentSpellName();

        initialized = true;
    }

    public void SelectForEditing()
    {
        EnsureInitialized();
        RegisterNameInputListener();

        if (completeSpells == null || completeSpells.Components == null)
        {
            return;
        }

        int slotCount = Mathf.Min(
            completeSpells.Components.Count,
            completeSpellsComponentButtons.Count
        );

        for (int i = 0; i < slotCount; i++)
        {
            SpellComponentSO spell = completeSpells.Components[i];
            SpellComponentButton componentButton = completeSpellsComponentButtons[i]
                .GetComponent<SpellComponentButton>();

            if (componentButton == null)
            {
                continue;
            }

            componentButton.spellComponent = spell;
            componentButton.RefreshDataPanel();
        }

        if (spellNameInputText != null)
        {
            ClampCurrentSpellName();
            spellNameInputText.SetTextWithoutNotify(completeSpells.Name);
        }
    }

    private void RegisterNameInputListener()
    {
        if (nameInputListenerRegistered || spellNameInputText == null)
        {
            return;
        }

        spellNameInputText.characterLimit = MaxSpellNameLength;
        spellNameInputText.onValueChanged.AddListener(UpdateName);
        nameInputListenerRegistered = true;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        EnsureInitialized();

        if (completeSpells == null || completeSpells.Components == null)
        {
            return;
        }

        componentsCount = completeSpells.Components.Count(c => c != null);
        if (completeSpellDataPanel != null)
        {
            completeSpellDataPanel.SetActive(componentsCount > 0);
        }

        if (componentsCount > 0)
        {
            if (spellNameText != null)
            {
                spellNameText.text = completeSpells.Name;
            }

            string componentsText = "";
            int manacost = 0;
            float cooldown = 0f;

            foreach (SpellComponentSO component in completeSpells.Components)
            {
                if (component == null)
                    continue;

                componentsText += "- " + component.Name + "\n";
                manacost += component.manaCost;
                cooldown += Mathf.Max(0f, component.cooldown);
            }

            componentListText.text = componentsText;
            if (completeSpellDataPanelRT != null && MyRT != null)
            {
                completeSpellDataPanelRT.anchoredPosition =
                    MyRT.anchoredPosition + new Vector2(200f, 30f);
            }

            if (manaCostText != null)
            {
                manaCostText.text =
                    "Mana Cost: " + manacost.ToString() + "\nCooldown: " + Mathf.Max(0.5f, cooldown).ToString("0.##") + "s";
            }
        }
    }

    private void UpdateName(string newName)
    {
        EnsureInitialized();

        if (uiManager == null || uiManager.latestSpellIndex != myIndex || completeSpells == null)
            return;

        string clampedName = ClampSpellName(newName);
        completeSpells.Name = clampedName;
        if (spellNameInputText != null && spellNameInputText.text != clampedName)
        {
            spellNameInputText.SetTextWithoutNotify(clampedName);
        }

        UpdateButtonColor();
    }

    private void ClampCurrentSpellName()
    {
        if (completeSpells == null)
        {
            return;
        }

        completeSpells.Name = ClampSpellName(completeSpells.Name);
    }

    private string ClampSpellName(string spellName)
    {
        if (string.IsNullOrEmpty(spellName))
        {
            return "";
        }

        return spellName.Length <= MaxSpellNameLength
            ? spellName
            : spellName.Substring(0, MaxSpellNameLength);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        EnsureInitialized();

        if (uiManager == null || uiManager.latestSpellIndex == -1)
            return;

        SelectForEditing();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (completeSpellDataPanel != null)
        {
            completeSpellDataPanel.SetActive(false);
        }
    }

    public void UpdateButtonColor()
    {
        EnsureInitialized();

        if (completeSpells == null || completeSpells.Components == null || myImage == null)
        {
            return;
        }

        componentsCount = completeSpells.Components.Count(c => c != null);
        if (componentsCount > 0)
        {
            if (completeSpells.Name == "")
            {
                myImage.color = Color.red;
            }
            else
            {
                myImage.color = Color.green;
            }
        }
        else
        {
            myImage.color = Color.white;
        }
    }
}
