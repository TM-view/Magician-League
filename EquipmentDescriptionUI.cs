using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class EquipmentDescriptionUI : MonoBehaviour
{
    [SerializeField]
    private GameObject parentDescription;

    [SerializeField]
    private TMP_Text typeText;

    [SerializeField]
    private TMP_Text nameText;

    [SerializeField]
    private TMP_Text descriptionText;

    [SerializeField]
    private StatLine[] statLines = new StatLine[6];

    [SerializeField]
    private Image[] powerupImages = new Image[ItemSO.MaxPowerupSlots];

    [SerializeField]
    private Vector2 descriptionOffset = new Vector2(220f, 0f);

    private RectTransform descriptionRectTransform;

    private void Awake()
    {
        if (parentDescription == null)
        {
            parentDescription = gameObject;
        }

        descriptionRectTransform = parentDescription.GetComponent<RectTransform>();
        DisableDescriptionRaycasts();
        Hide();
    }

    public void Show(ItemSO item, RectTransform source)
    {
        if (item == null)
        {
            Hide();
            return;
        }

        SetText(typeText, item.type.ToString());
        SetText(nameText, item.itemName);
        SetText(descriptionText, item.description);
        SetStats(item);
        SetPowerups(item);
        MoveToSource(source);

        if (parentDescription != null && !parentDescription.activeSelf)
        {
            parentDescription.SetActive(true);
        }
    }

    public void Hide()
    {
        if (parentDescription != null && parentDescription.activeSelf)
        {
            parentDescription.SetActive(false);
        }
    }

    private void SetStats(ItemSO item)
    {
        SetStatLine(PlayerStatId.HP, item.mainStats.HP, GetBonusStat(item, PlayerStatId.HP));
        SetStatLine(PlayerStatId.MP, item.mainStats.MP, GetBonusStat(item, PlayerStatId.MP));
        SetStatLine(PlayerStatId.CD, item.mainStats.CD, GetBonusStat(item, PlayerStatId.CD));
        SetStatLine(PlayerStatId.VAL, item.mainStats.VAL, GetBonusStat(item, PlayerStatId.VAL));
        SetStatLine(PlayerStatId.STR, item.mainStats.STR, GetBonusStat(item, PlayerStatId.STR));
        SetStatLine(PlayerStatId.LUK, item.mainStats.LUK, GetBonusStat(item, PlayerStatId.LUK));
    }

    private void SetStatLine(PlayerStatId stat, int mainStat, int bonusStat)
    {
        StatLine statLine = GetStatLine(stat);
        if (statLine == null || statLine.Root == null)
        {
            return;
        }

        bool shouldShow = mainStat + bonusStat != 0;
        statLine.Root.SetActive(shouldShow);
        if (!shouldShow || statLine.ValueText == null)
        {
            return;
        }

        statLine.ValueText.text = FormatStatLine(stat, mainStat, bonusStat);
    }

    private string FormatStatLine(PlayerStatId stat, int mainStat, int bonusStat)
    {
        string statName = stat.ToString().ToUpperInvariant().PadRight(5);
        if (mainStat == 0)
        {
            return statName + "+" + bonusStat;
        }

        if (bonusStat == 0)
        {
            return statName + mainStat;
        }

        return statName + mainStat + "+" + bonusStat;
    }

    private int GetBonusStat(ItemSO item, PlayerStatId stat)
    {
        if (item.bonusStats == null)
        {
            return 0;
        }

        int total = 0;
        for (int i = 0; i < item.bonusStats.Length; i++)
        {
            ItemStatBonus bonus = item.bonusStats[i];
            if (bonus.IsValid && bonus.Stat == stat)
            {
                total += bonus.Amount;
            }
        }

        return total;
    }

    private StatLine GetStatLine(PlayerStatId stat)
    {
        if (statLines == null)
        {
            return null;
        }

        for (int i = 0; i < statLines.Length; i++)
        {
            if (statLines[i] != null && statLines[i].Stat == stat)
            {
                return statLines[i];
            }
        }

        return null;
    }

    private void SetPowerups(ItemSO item)
    {
        for (int i = 0; i < powerupImages.Length; i++)
        {
            Image image = powerupImages[i];
            if (image == null)
            {
                continue;
            }

            SpellComponentSO powerup =
                item.powerups != null && i < item.powerups.Count ? item.powerups[i] : null;
            bool shouldShow = i < ItemSO.MaxPowerupSlots && powerup != null && powerup.Icon != null;
            image.gameObject.SetActive(shouldShow);
            if (shouldShow)
            {
                image.sprite = powerup.Icon;
            }
        }
    }

    private void MoveToSource(RectTransform source)
    {
        if (source == null || descriptionRectTransform == null)
        {
            return;
        }

        descriptionRectTransform.position = source.position;
        descriptionRectTransform.anchoredPosition += descriptionOffset;
    }

    private void DisableDescriptionRaycasts()
    {
        if (parentDescription == null)
        {
            return;
        }

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

    private void SetText(TMP_Text text, string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    [Serializable]
#pragma warning disable 0649
    private class StatLine
    {
        public PlayerStatId Stat;
        public GameObject Root;
        public TMP_Text ValueText;
    }
#pragma warning restore 0649
}
