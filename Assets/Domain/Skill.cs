using UnityEngine;

public class Skill

{
    public int id;
    public string skillName;
    public int cost;
    public Stat stats;
    public int position;


    public Skill(int id, string skillName, int cost, Stat stats, int position)
    {
        this.id = id;
        this.skillName = skillName;
        this.cost = cost;
        this.stats = stats;
        this.position = position;
    }
}