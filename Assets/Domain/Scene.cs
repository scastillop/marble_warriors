using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Scene : MonoBehaviour

{
    public GameObject characterPrefab;
    private Team allied;
    private Team enemy;
    private Vector3[] positions;
    private Quaternion[] rotations;
    public Camera mainCamera;

    //metodo que se ejecuta al iniciar la escena
    private void Start()
    {
        //generando equipos
        this.allied = new Team(1); //id jugador 1
        this.enemy = new Team(2); //id jugador 2

        //genero las posiciones por defecto para los 10 personajes
        this.positions = new Vector3[10];
        for (int i = 0; i < 5; i++)
        {
            this.positions[i] = new Vector3(160.0f, 0.0f, 164.0f + (i*3));
            this.positions[i+5] = new Vector3(140.0f, 0.0f, 164.0f + (i * 3));
        }

        //genero las rotaciones por defecto para los 10 personajes
        this.rotations = new Quaternion[10];
        for (int i = 0; i < 5; i++)
        {
            this.rotations[i] = Quaternion.Euler(0.0f, -90.0f, 0.0f);
            this.rotations[i+5] = Quaternion.Euler(0.0f, 90.0f, 0.0f);
        }

        //genero Personajes aliados (agregandolos a su equipo)
        for (int i = 0; i < 5; i++)
        {
            this.allied.AddChar(MakeChar(i));
        }

        //genero Personajes enemigos (agregandolos a su equipo)
        for (int i = 5; i < 10; i++)
        {
            this.enemy.AddChar(MakeChar(i));
        }

        //seteo datos de los Personajes en la UI
        int j = 0;
        foreach (Button button in GameObject.Find("Character Menu").GetComponentsInChildren<Button>())
        {
            button.GetComponent<ButtonChar>().character = allied.characters[j];
            button.GetComponent<ButtonChar>().characterNumber = j;

            //seteo las funcionalidades de los botones de los personajes
            button.onClick.AddListener(delegate { CharClick(button); });
            j++;
        }

        //actualizo la UI de estado de los peronajes
        UpdateCharacterMenu();

        //seteo las funcionalidades del menu de acciones
        foreach (Button button in GameObject.Find("Actions Menu").GetComponentsInChildren<Button>())
        {
            button.onClick.AddListener(delegate { SkillClick(button); });
        }
    }

    //funcion que genera Personajes por posicion (por ahora para efectos de prueba)
    private GameObject MakeChar(int position)
    {
        //genero las habilidades del personaje
        Stat statSkill = new Stat(-10, 0, 0, 0, 0, 0, 0);
        List<Skill> skillSet = new List<Skill>();
        skillSet.Add(new Skill("Basic Attack", 10, statSkill));

        //genero las estadisticas del personaje
        Stat statChar = new Stat(100, 100, 100, 100, 100, 100, 100);

        //instancio el personaje en pantalla
        GameObject character = Instantiate(this.characterPrefab, this.positions[position], this.rotations[position]);

        //seteo el id, nombbre, habilidades, estadisticas y posicion del personaje
        character.GetComponent<Character>().id = 1;
        character.GetComponent<Character>().characterName = "Swordman";
        character.GetComponent<Character>().position = position;
        character.GetComponent<Character>().actualStat = statChar;
        character.GetComponent<Character>().initialStat = statChar;
        character.GetComponent<Character>().skills = skillSet;

        return character;
    }

    //funcion que actualiza las barras de vida y mana
    private void UpdateCharacterMenu()
    {
        foreach (Button button in GameObject.Find("Character Menu").GetComponentsInChildren<Button>())
        {
            //seteo el nombre del personaje en el boton
            button.GetComponentInChildren<Text>().text = button.GetComponent<ButtonChar>().character.GetComponent<Character>().characterName;
            foreach(Slider slider in button.GetComponentsInChildren<Slider>())
            {
                //seteo la barra de vida
                if (slider.name.Equals("hp"))
                {
                    slider.value = button.GetComponent<ButtonChar>().character.GetComponent<Character>().initialStat.hp / button.GetComponent<ButtonChar>().character.GetComponent<Character>().actualStat.hp;
                }
                //seteo la barra de mana
                else if (slider.name.Equals("mp"))
                {
                    slider.value = button.GetComponent<ButtonChar>().character.GetComponent<Character>().initialStat.mp / button.GetComponent<ButtonChar>().character.GetComponent<Character>().actualStat.mp;
                }
            }
        }
    }

    //funcion que se ejecuta al seleccionar un Personaje en el menu
    private void CharClick(Button button)
    {
        //cambio la posicion del menu de acciones

        GameObject.Find("Actions Menu").GetComponent<RectTransform>().position = new Vector2(GameObject.Find("Actions Menu").GetComponent<RectTransform>().position.x, button.transform.position.y );

        //muestro el menu de acciones
        GameObject.Find("Actions Menu").GetComponent<CanvasGroup>().alpha = 1f;
        GameObject.Find("Actions Menu").GetComponent<CanvasGroup>().interactable = true;
        GameObject.Find("Actions Menu").GetComponent<CanvasGroup>().blocksRaycasts = true;

        //vacío los botones del menu acciones
        foreach (Button actButton in GameObject.Find("Actions Menu").GetComponentsInChildren<Button>())
        {
            actButton.GetComponentInChildren<Text>().text = "";
            actButton.GetComponent<CanvasGroup>().interactable = false;
        }

        //seteo las habilidades en cada boton
        int i = 0;
        foreach (Skill skill in button.GetComponent<ButtonChar>().character.GetComponent<Character>().skills)
        {
            int j = 0;
            foreach (Button actButton in GameObject.Find("Actions Menu").GetComponentsInChildren<Button>())
            {
                if (i == j)
                {
                    //seteo el nombre de la habilidad
                    actButton.GetComponentInChildren<Text>().text = skill.skillName;
                    //seteo datos de relacionados al personaje y habilidad en el boton
                    actButton.GetComponent<ButtonAct>().charNumber = button.GetComponent<ButtonChar>().characterNumber;
                    actButton.GetComponent<ButtonAct>().skillNumber = i;
                    //hago el boton clickeable
                    actButton.GetComponent<CanvasGroup>().interactable = true;
                }
                j++;
            }
            i++;
        }
    }

    //funcion que se ejecuta al presionar una habilidad en el menu de acciones
    private void SkillClick(Button button)
    {
        Debug.Log("charnumber: "+button.GetComponent<ButtonAct>().charNumber+" skillNumber: "+button.GetComponent<ButtonAct>().skillNumber);
    }

    //funcion que se ejecuta en cada frame del juego
    private void Update()
    {

        // maneja eventos de touch en la pantalla
        foreach (Touch touch in Input.touches)
        {
            HandleTouch(touch.fingerId, mainCamera.ScreenToWorldPoint(touch.position), touch.phase);
        }

        // simula eventos de touch al clickear
        if (Input.touchCount == 0)
        {
            if (Input.GetMouseButtonDown(0))
            {
                HandleTouch(10, Input.mousePosition, TouchPhase.Began);
            }
            if (Input.GetMouseButton(0))
            {
                HandleTouch(10, Input.mousePosition, TouchPhase.Moved);
            }
            if (Input.GetMouseButtonUp(0))
            {
                HandleTouch(10, Input.mousePosition, TouchPhase.Ended);
            }
        }
    }

    //funcion que recibe los 
    private void HandleTouch(int touchFingerId, Vector3 touchPosition, TouchPhase touchPhase)
    {
        switch (touchPhase)
        {
            //fase 1 (cuandos se presiona)
            case TouchPhase.Began:
                //generando el raycast
                RaycastHit hit;
                Ray ray = mainCamera.ScreenPointToRay(touchPosition);
                Debug.DrawRay(ray.origin, ray.direction * 50, Color.red, 50000000f);
                if (Physics.Raycast(ray, out hit))
                {
                    GameObject character = hit.collider.gameObject;
                    Debug.Log(character.GetComponent<Character>().position);
                }
                break;
            //fase 2 (cuando se mantiene)
            case TouchPhase.Moved:
                
                break;
            //fase 3 (cuando se suelta)
            case TouchPhase.Ended:
                // TODO
                break;
        }
    }
}