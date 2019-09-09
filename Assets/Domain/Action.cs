using UnityEngine;

public class Action
{

    private GameObject owner;
    private GameObject affected;
    private Skill skill;

    public Action(GameObject owner, GameObject affected, Skill skill)
    {
        this.owner = owner;
        this.affected = affected;
        this.skill = skill;
    }
}
