using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class AchievementSlot : MonoBehaviour
{
    public Image categoryIcon;
    public Image lockIcon;
    public TMP_Text title;
    public TMP_Text description;

    public void Set(Achievement achievement, bool unlocked, Sprite categorySprite)
    {
        title.text = achievement.title;
        description.text = achievement.description;

        categoryIcon.sprite = categorySprite;

        lockIcon.enabled = !unlocked;

        float alpha = unlocked ? 1f : 0.5f;

        categoryIcon.color = new Color(1, 1, 1, alpha);
        title.color = new Color(1, 1, 1, alpha);
        description.color = new Color(1, 1, 1, alpha);
    }
}