using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class StatButtonClick
    : MonoBehaviour,
        IPointerClickHandler,
        IPointerEnterHandler,
        IPointerExitHandler
{
    [SerializeField]
    private StatBook statBook;

    [SerializeField]
    private PlayerStatId stat;

    [SerializeField]
    private bool handleLeftClick;

    [SerializeField]
    private GameObject parentDescription;

    [SerializeField]
    private TMP_Text description;

    [SerializeField]
    private Vector2 descriptionOffset = new Vector2(-220f, 0f);

    private RectTransform myRectTransform;
    private RectTransform descriptionRectTransform;

    private void Awake()
    {
        if (statBook == null)
        {
            statBook = GetComponentInParent<StatBook>();
        }

        myRectTransform = GetComponent<RectTransform>();
        if (parentDescription != null)
        {
            descriptionRectTransform = parentDescription.GetComponent<RectTransform>();
            DisableDescriptionRaycasts();
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (statBook == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            statBook.Downgrade(stat);
            return;
        }

        if (handleLeftClick && eventData.button == PointerEventData.InputButton.Left)
        {
            statBook.Upgrade(stat);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (description != null)
        {
            description.text = GetDescription(stat);
        }

        MoveDescriptionToLeftOfButton();

        if (parentDescription != null && !parentDescription.activeSelf)
        {
            parentDescription.SetActive(true);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (parentDescription != null)
        {
            parentDescription.SetActive(false);
        }
    }

    private void MoveDescriptionToLeftOfButton()
    {
        if (myRectTransform == null || descriptionRectTransform == null)
        {
            return;
        }

        descriptionRectTransform.position = myRectTransform.position;
        descriptionRectTransform.anchoredPosition += descriptionOffset;
    }

    private void DisableDescriptionRaycasts()
    {
        CanvasGroup canvasGroup = parentDescription.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = parentDescription.AddComponent<CanvasGroup>();
        }

        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;

        Graphic[] graphics = parentDescription.GetComponentsInChildren<Graphic>(true);
        for (int i = 0; i < graphics.Length; i++)
        {
            graphics[i].raycastTarget = false;
        }
    }

    private string GetDescription(PlayerStatId statId)
    {
        switch (statId)
        {
            case PlayerStatId.HP:
                return "Increases max health and natural health regeneration.";
            case PlayerStatId.MP:
                return "Increases max mana and natural mana regeneration.";
            case PlayerStatId.CD:
                return "Reduces spell cooldown time.";
            case PlayerStatId.VAL:
                return "Increases spell damage and healing effectiveness.";
            case PlayerStatId.STR:
                return "Increases movement speed and collision damage.";
            case PlayerStatId.LUK:
                return "Increases the chance for attacks to apply spell component status effects.";
            case PlayerStatId.None:
                return "The cost for a status upgrade is based on the level of the specific status.";
            default:
                return "";
        }
    }
}
