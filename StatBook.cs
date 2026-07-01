using TMPro;
using UnityEngine;

public class StatBook : MonoBehaviour
{
    [SerializeField]
    private Status status;

    [SerializeField]
    private GameObject hpPointRoot;

    [SerializeField]
    private GameObject mpPointRoot;

    [SerializeField]
    private GameObject cdPointRoot;

    [SerializeField]
    private GameObject valPointRoot;

    [SerializeField]
    private GameObject strPointRoot;

    [SerializeField]
    private GameObject lukPointRoot;

    [SerializeField]
    private TMP_Text spText;

    private void Awake()
    {
        if (status == null)
        {
            status = GetComponentInParent<Status>();
        }
    }

    private void OnEnable()
    {
        if (status != null)
        {
            status.StatsChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (status != null)
        {
            status.StatsChanged -= Refresh;
        }
    }

    public void SetStatus(Status newStatus)
    {
        if (status != null)
        {
            status.StatsChanged -= Refresh;
        }

        status = newStatus;

        if (status != null && isActiveAndEnabled)
        {
            status.StatsChanged += Refresh;
        }

        Refresh();
    }

    public void UpgradeHP()
    {
        Upgrade(PlayerStatId.HP);
    }

    public void UpgradeMP()
    {
        Upgrade(PlayerStatId.MP);
    }

    public void UpgradeCD()
    {
        Upgrade(PlayerStatId.CD);
    }

    public void UpgradeVAL()
    {
        Upgrade(PlayerStatId.VAL);
    }

    public void UpgradeSTR()
    {
        Upgrade(PlayerStatId.STR);
    }

    public void UpgradeLUK()
    {
        Upgrade(PlayerStatId.LUK);
    }

    public void DowngradeHP()
    {
        Downgrade(PlayerStatId.HP);
    }

    public void DowngradeMP()
    {
        Downgrade(PlayerStatId.MP);
    }

    public void DowngradeCD()
    {
        Downgrade(PlayerStatId.CD);
    }

    public void DowngradeVAL()
    {
        Downgrade(PlayerStatId.VAL);
    }

    public void DowngradeSTR()
    {
        Downgrade(PlayerStatId.STR);
    }

    public void DowngradeLUK()
    {
        Downgrade(PlayerStatId.LUK);
    }

    public void Refresh()
    {
        if (!IsStatusReady())
        {
            Clear();
            return;
        }

        SetPointRoot(hpPointRoot, status.HPStat);
        SetPointRoot(mpPointRoot, status.MPStat);
        SetPointRoot(cdPointRoot, status.CDStat);
        SetPointRoot(valPointRoot, status.VALStat);
        SetPointRoot(strPointRoot, status.STRStat);
        SetPointRoot(lukPointRoot, status.LUKStat);

        if (spText != null)
        {
            spText.text = status.SP.ToString();
        }
    }

    public void Upgrade(PlayerStatId stat)
    {
        if (!IsStatusReady())
        {
            return;
        }

        status.UpgradeStat(stat);
        Refresh();
    }

    public void Downgrade(PlayerStatId stat)
    {
        if (!IsStatusReady())
        {
            return;
        }

        status.DowngradeStat(stat);
        Refresh();
    }

    private bool IsStatusReady()
    {
        return status != null && status.Object != null && status.Object.IsValid;
    }

    private void Clear()
    {
        SetPointRoot(hpPointRoot, 0);
        SetPointRoot(mpPointRoot, 0);
        SetPointRoot(cdPointRoot, 0);
        SetPointRoot(valPointRoot, 0);
        SetPointRoot(strPointRoot, 0);
        SetPointRoot(lukPointRoot, 0);

        if (spText != null)
        {
            spText.text = "0";
        }
    }

    private void SetPointRoot(GameObject pointRoot, int activeCount)
    {
        if (pointRoot == null)
        {
            return;
        }

        activeCount = Mathf.Clamp(activeCount, 0, pointRoot.transform.childCount);
        for (int i = 0; i < pointRoot.transform.childCount; i++)
        {
            pointRoot.transform.GetChild(i).gameObject.SetActive(i < activeCount);
        }
    }
}
