using System.Collections.Generic;
using UnityEngine;

public class Team
{
    private int user_id;
    public List<GameObject> characters;

    //constructor
    public Team(List<GameObject> characters)
    {
        this.characters = characters;
    }

    //agrega un personaje al equipo
    public void AddChar(GameObject character)
    {
        this.characters.Add(character);

    }

    public GameObject get(int index)
    {
        return characters[index];
    }


}