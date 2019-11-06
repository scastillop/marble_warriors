using System.Collections;
using UnityEngine;

public class Action
{

    public GameObject owner;
    public GameObject affected;
    public Skill skill;

    public Action(GameObject owner, GameObject affected, Skill skill)
    {
        this.owner = owner;
        this.affected = affected;
        this.skill = skill;
    }

    //funcion que retorna los datos de este objeto en un hash para serializarlo
    public Hashtable SerializableAction()
    {
        var response = new Hashtable();
        response["owner"] = this.owner.GetComponent<Character>().position;
        response["affected"] = this.affected.GetComponent<Character>().position;
        response["skill"] = this.skill.position;
        return response;
    }
}
