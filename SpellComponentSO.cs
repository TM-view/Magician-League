using UnityEngine;

public enum SpellComponentType
{
    Active,
    PowerUp,
    Effect,
}

[CreateAssetMenu(fileName = "New Spell Component", menuName = "Magician_League/Spell-Component")]
public class SpellComponentSO : ScriptableObject
{
    public string Name;
    public Sprite Icon;

    [TextArea]
    public string Description;
    public SpellComponentType Type;
    public int manaCost;
    public float cooldown;
    public int value;
    public float radius;
    public float range;
    public float speed;
    public float lifetime;
}
