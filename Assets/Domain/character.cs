using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    public int id;
    public string characterName;
    public int position;
    public int model;
    public Stat initialStat;
    public Stat actualStat;
    public List<Skill> skills;

}
