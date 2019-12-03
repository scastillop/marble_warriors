using UnityEngine;

public class Skill

{
    public int id;
    public string skillName;
    public int cost;
    public Stat stats;
    public int position;
    public string distance;
    public string type;
    public string animation;
    public string target;

    public Skill(int id, string skillName, int cost, Stat stats, int position, string distance, string type, string animation, string target)
    {
        this.id = id;
        this.skillName = skillName;
        this.cost = cost;
        this.stats = stats;
        this.position = position;
        this.distance = distance;
        this.type = type;
        this.animation = animation;
        this.target = target;
    }
}