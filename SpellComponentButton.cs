using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class SpellComponentButton
    : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IBeginDragHandler,
        IEndDragHandler,
        IDragHandler,
        IPointerClickHandler,
        IDropHandler
{
    [SerializeField]
    private UIManager uiManager;
    public SpellComponentSO spellComponent;

    [SerializeField]
    private Image draggingIcon;

    [SerializeField]
    private bool canChange = false;

    [SerializeField]
    private Sprite emptyIcon;

    [SerializeField]
    private GameObject completeSpellComponentParent;

    [SerializeField]
    private GameObject componentInfoPanel;

    [SerializeField]
    private Image spellComponentIcon;

    [SerializeField]
    private TMP_Text spellComponentNameText;

    [SerializeField]
    private TMP_Text spellComponentDescriptionText;

    [SerializeField]
    private TMP_Text spellComponentManaCostText;

    [SerializeField]
    private TMP_Text spellComponentConditionText;

    [SerializeField]
    private Transform spellSlot;

    [SerializeField]
    private RectTransform CipRT;

    [SerializeField]
    private RectTransform MyRT;

    private bool TryGetSelectedCompleteSpellButton(out CompleteSpellButton completeSpellButton)
    {
        completeSpellButton = null;

        if (uiManager == null || spellSlot == null)
        {
            return false;
        }

        if (uiManager.latestSpellIndex < 0 || uiManager.latestSpellIndex >= spellSlot.childCount)
        {
            return false;
        }

        completeSpellButton = spellSlot
            .GetChild(uiManager.latestSpellIndex)
            .GetComponent<CompleteSpellButton>();

        return completeSpellButton != null;
    }

    private bool CanPlaceComponent(
        SpellComponentSO component,
        CompleteSpellButton completeSpellButton,
        int componentIndex
    )
    {
        if (
            component == null
            || completeSpellButton == null
            || completeSpellButton.completeSpells == null
            || completeSpellButton.completeSpells.Components == null
        )
        {
            return false;
        }

        if (
            componentIndex < 0
            || componentIndex >= completeSpellButton.completeSpells.Components.Count
        )
        {
            return false;
        }

        if (!FirstNonEmptyComponentWouldBeActive(component, completeSpellButton, componentIndex))
        {
            return false;
        }

        if (component.Type == SpellComponentType.Active)
        {
            return true;
        }

        if (component.Type == SpellComponentType.Effect)
        {
            return HasActiveBefore(completeSpellButton, componentIndex);
        }

        return HasEffectInCurrentSegment(completeSpellButton, componentIndex);
    }

    private bool FirstNonEmptyComponentWouldBeActive(
        SpellComponentSO newComponent,
        CompleteSpellButton completeSpellButton,
        int componentIndex
    )
    {
        for (int i = 0; i < completeSpellButton.completeSpells.Components.Count; i++)
        {
            SpellComponentSO existingComponent =
                i == componentIndex
                    ? newComponent
                    : completeSpellButton.completeSpells.Components[i];

            if (existingComponent == null)
            {
                continue;
            }

            return existingComponent.Type == SpellComponentType.Active;
        }

        return false;
    }

    private bool HasActiveBefore(CompleteSpellButton completeSpellButton, int componentIndex)
    {
        for (int i = componentIndex - 1; i >= 0; i--)
        {
            SpellComponentSO previousComponent = completeSpellButton.completeSpells.Components[i];
            if (previousComponent != null && previousComponent.Type == SpellComponentType.Active)
            {
                return true;
            }
        }

        return false;
    }

    private bool HasEffectInCurrentSegment(
        CompleteSpellButton completeSpellButton,
        int componentIndex
    )
    {
        for (int i = componentIndex - 1; i >= 0; i--)
        {
            SpellComponentSO previousComponent = completeSpellButton.completeSpells.Components[i];
            if (previousComponent == null)
            {
                continue;
            }

            if (previousComponent.Type == SpellComponentType.Active)
            {
                return false;
            }

            if (previousComponent.Type == SpellComponentType.Effect)
            {
                return true;
            }
        }

        return false;
    }

    private void PlaceComponent(
        SpellComponentSO component,
        CompleteSpellButton completeSpellButton,
        int componentIndex
    )
    {
        spellComponent = component;
        completeSpellButton.completeSpells.Components[componentIndex] = component;
        RefreshDataPanel();
    }

    void Start()
    {
        if (draggingIcon != null)
        {
            draggingIcon.raycastTarget = false;
        }

        RefreshDataPanel();
    }

    public void RefreshDataPanel()
    {
        if (spellComponent != null)
        {
            if (spellComponentIcon != null)
            {
                spellComponentIcon.sprite = spellComponent.Icon;
            }

            if (spellComponentNameText != null)
            {
                spellComponentNameText.text = spellComponent.Name;
            }

            if (spellComponentDescriptionText != null)
            {
                spellComponentDescriptionText.text = spellComponent.Description;
            }

            if (spellComponentManaCostText != null)
            {
                spellComponentManaCostText.text =
                    "ManaCost: "
                    + spellComponent.manaCost.ToString()
                    + "\nCooldown: "
                    + Mathf.Max(0f, spellComponent.cooldown).ToString("0.##")
                    + "s";
            }

            // spellComponentConditionText.text = spellComponent.conditionDescription;
        }
        else
        {
            if (spellComponentIcon != null)
            {
                spellComponentIcon.sprite = emptyIcon;
            }

            if (spellComponentNameText != null)
            {
                spellComponentNameText.text = "";
            }

            if (spellComponentDescriptionText != null)
            {
                spellComponentDescriptionText.text = "";
            }

            if (spellComponentManaCostText != null)
            {
                spellComponentManaCostText.text = "";
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (
            eventData.button == PointerEventData.InputButton.Right
            && TryGetSelectedCompleteSpellButton(out CompleteSpellButton completeSpellButton)
        )
        {
            if (canChange)
            {
                int componentIndex = transform.GetSiblingIndex();
                if (
                    completeSpellButton.completeSpells != null
                    && completeSpellButton.completeSpells.Components != null
                    && componentIndex >= 0
                    && componentIndex < completeSpellButton.completeSpells.Components.Count
                )
                {
                    completeSpellButton.completeSpells.Components[componentIndex] = null;
                }

                spellComponent = null;
                RefreshDataPanel();
            }
            else
            {
                if (
                    spellComponent == null
                    || completeSpellComponentParent == null
                    || completeSpellButton.completeSpells == null
                    || completeSpellButton.completeSpells.Components == null
                )
                {
                    return;
                }

                for (int i = 0; i < completeSpellButton.completeSpells.Components.Count; i++)
                {
                    if (i >= completeSpellComponentParent.transform.childCount)
                    {
                        break;
                    }

                    SpellComponentSO component = completeSpellButton.completeSpells.Components[i];
                    if (component == null)
                    {
                        if (!CanPlaceComponent(spellComponent, completeSpellButton, i))
                        {
                            continue;
                        }

                        completeSpellButton.completeSpells.Components[i] = spellComponent;
                        completeSpellComponentParent
                            .transform.GetChild(i)
                            .GetComponent<SpellComponentButton>()
                            .spellComponent = spellComponent;
                        completeSpellComponentParent
                            .transform.GetChild(i)
                            .GetComponent<SpellComponentButton>()
                            .RefreshDataPanel();
                        break;
                    }
                }
            }
        }
    }

    // เมาส์ชี้เข้า
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (spellComponent != null)
        {
            RefreshDataPanel();
            if (!canChange)
                CipRT.anchoredPosition = MyRT.anchoredPosition + new Vector2(290f, 245f);
            else
                CipRT.anchoredPosition = MyRT.anchoredPosition + new Vector2(200f, 0f);
            componentInfoPanel.SetActive(true);
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (uiManager == null)
        {
            return;
        }

        if (uiManager.spellComponentBeingDragged == null || !canChange)
        {
            return;
        }

        if (!TryGetSelectedCompleteSpellButton(out CompleteSpellButton completeSpellButton))
        {
            return;
        }

        int componentIndex = transform.GetSiblingIndex();
        if (
            completeSpellButton.completeSpells == null
            || completeSpellButton.completeSpells.Components == null
        )
        {
            return;
        }

        if (
            !CanPlaceComponent(
                uiManager.spellComponentBeingDragged,
                completeSpellButton,
                componentIndex
            )
        )
        {
            return;
        }

        PlaceComponent(uiManager.spellComponentBeingDragged, completeSpellButton, componentIndex);
    }

    // เมาส์ออก
    public void OnPointerExit(PointerEventData eventData)
    {
        componentInfoPanel.SetActive(false);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (uiManager == null || spellComponent == null || draggingIcon == null)
        {
            return;
        }

        uiManager.spellComponentBeingDragged = spellComponent;
        uiManager.isDraggingComponent = true;
        draggingIcon.sprite = spellComponent.Icon;
        draggingIcon.gameObject.SetActive(true);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (draggingIcon == null)
        {
            return;
        }

        draggingIcon.rectTransform.anchoredPosition =
            eventData.position - new Vector2(Screen.width, Screen.height) / 2f;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (uiManager != null)
        {
            uiManager.isDraggingComponent = false;
        }

        if (draggingIcon != null)
        {
            draggingIcon.gameObject.SetActive(false);
        }

        StartCoroutine(ReleaseDrag());
    }

    IEnumerator ReleaseDrag()
    {
        yield return new WaitForSeconds(0.1f);
        if (uiManager != null)
        {
            uiManager.spellComponentBeingDragged = null;
        }
    }
}
