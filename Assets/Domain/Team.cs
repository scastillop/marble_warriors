using System.Collections.Generic;
using UnityEngine;

public class Team
{
    private int user_id;
    public List<GameObject> characters;

    //constructor
    public Team(int user_id)
    {
        this.user_id = user_id;
        this.characters = new List<GameObject>();
    }

    //agrega un personaje al equipo
    public void AddChar(GameObject character)
    {
        this.characters.Add(character);

    }


}