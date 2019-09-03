using UnityEngine;

public class Skill

{
    public string skillName;
    public int cost;
    private Stat stats;

    public Skill(string skillName, int cost, Stat stats)
    {
        this.skillName = skillName;
        this.cost = cost;
        this.stats = stats;
    }
}