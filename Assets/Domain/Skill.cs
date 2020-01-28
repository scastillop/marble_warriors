using UnityEngine;

public class Skill

{
    public string skillName;
    public int cost;
    public Stat stats;
    public int position;


    public Skill(string skillName, int cost, Stat stats, int position)
    {
        this.skillName = skillName;
        this.cost = cost;
        this.stats = stats;
        this.position = position;
    }
}