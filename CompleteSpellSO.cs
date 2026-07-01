using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Complete Spell", menuName = "Magician_League/Complete-Spell")]
public class CompleteSpellSO : ScriptableObject
{
    public string Name;
    public List<SpellComponentSO> Components;
    public int manaCost;

    public void Reset()
    {
        Name = "";
        Components = new List<SpellComponentSO>(new SpellComponentSO[10]);
        manaCost = 0;
    }
}
