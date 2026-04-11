using UnityEngine;
using UnityEngine.UI;

public class AchievementUI : MonoBehaviour
{
    public static AchievementUI Instance;

    public Transform container;
    public AchievementSlot slotPrefab;

    public Image bugIcon;
    public Image upgradeIcon;
    public Image speedIcon;
    public Image competitiveIcon;
    public Image progressionIcon;

    public Sprite GetCategoryIcon(AchievementCategory category)
    {
        switch (category)
        {
            case AchievementCategory.BugHunter:
                return bugIcon.sprite;

            case AchievementCategory.Upgrade:
                return upgradeIcon.sprite;

            case AchievementCategory.Speed:
                return speedIcon.sprite;

            case AchievementCategory.Competitive:
                return competitiveIcon.sprite;

            case AchievementCategory.Progression:
                return progressionIcon.sprite;
        }

        return null;
    }

    void Awake()
    {
        Instance = this;
    }

    public void BuildUI()
    {
        foreach (var a in AchievementDatabase.GetAll())
        {
            var slot = Instantiate(slotPrefab, container);
            bool unlocked = AchievementManager.Instance.IsUnlocked(a.id);

            slot.Set(a, unlocked, GetCategoryIcon(a.category));
        }
    }

    public void ShowUnlocked(Achievement a)
    {
        Debug.Log("UI Popup: " + a.title);
        BuildUI(); // refresh UI
    }
}