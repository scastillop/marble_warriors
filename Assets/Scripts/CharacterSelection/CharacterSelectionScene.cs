using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectionScene : MonoBehaviour
{
    private Dictionary<int, string> allCharacters;
    private List<int> selectedCharacters;
    public List<GameObject> characterPrefab;
    private GameObject character;
    // Start is called before the first frame update
    void Start()
    {
        //seteo los personajes
        this.allCharacters = new Dictionary<int, string>();
        this.allCharacters.Add(0, "Guard");
        this.allCharacters.Add(1, "Archer");
        this.allCharacters.Add(2, "Wizard");
        this.allCharacters.Add(3, "Swordman");
        this.allCharacters.Add(4, "Lancer");
        this.allCharacters.Add(5, "Priest");

        //instancio el listado de personajes seleccionados
        this.selectedCharacters = new List<int>();

        //seteo las funciones de los botones de seleccion de personajes
        foreach (Button button in GameObject.Find("Selection Panel").GetComponentsInChildren<Button>())
        {
            //si es el boton de cancelar
            if (button.name.Equals("Cancel Icon"))
            {
                button.onClick.AddListener(delegate { CancelSelection(); });
            }
            //si es el boton ok
            else if (button.name.Equals("Ok Icon"))
            {
                button.onClick.AddListener(delegate { OkSelection(); });
            }
            //si es uno de los otros botones
            else
            {
                button.onClick.AddListener(delegate { CharSelection(button.GetComponent<ButtonCharSelection>().index); });
            }
        }

        //oculto el listado de personajes
        UpdateMenu();

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    //funcion que actualiza el listado de la izquierda en el menu de seleccion de personajes
    private void UpdateMenu()
    {
        //recorro los botones
        Button[] buttons = GameObject.Find("Character Menu").GetComponentsInChildren<Button>();
        for (int i=0;i<5;i++)
        {
            //si existe el personaje en la lista
            if (i<selectedCharacters.Count)
            {
                //muestro el boton
                buttons[i].GetComponent<CanvasGroup>().alpha = 1;
                //lo agrego al boton
                buttons[i].GetComponentInChildren<Text>().text = this.allCharacters[this.selectedCharacters[i]];
            }
            //si no existe
            else
            {
                //oculto el boton
                buttons[i].GetComponent<CanvasGroup>().alpha = 0;
            }
        }
        //si no hay personajes seleccionados
        if (this.selectedCharacters.Count == 0)
        {
            //desactivo el boton de cancelar
            GameObject.Find("Cancel Icon").GetComponentInChildren<Button>().interactable = false;
            //elimino el personaje
            if (this.character != null)
            {
                Destroy(this.character);
            }
        }
        //si los hay
        else
        {
            //activo el boton cancelar
            GameObject.Find("Cancel Icon").GetComponentInChildren<Button>().interactable = true;
            //cambio el personaje seleccionado
            if(this.character != null)
            {
                Destroy(this.character);
            }
            this.character = Instantiate(this.characterPrefab[this.selectedCharacters[this.selectedCharacters.Count-1]], new Vector3(164.61f, 2.33f, 173.48f), Quaternion.Euler(0.0f, 62.9f, 0.0f));
        }
        //si ya estan todos los personajes seleccionados
        if (this.selectedCharacters.Count == 5)
        {
            //activo el boton de ok
            GameObject.Find("Ok Icon").GetComponentInChildren<Button>().interactable = true;
        }
        //si no estan todos 
        else
        {
            //desactivo el boton ok
            GameObject.Find("Ok Icon").GetComponentInChildren<Button>().interactable = false;
        }
        
    }

    //funcion que se ejecuta al presionar un icono de personaje
    private void CharSelection(int index)
    {
        //verifico que el arreglo de personajes no este lleno 
        if (this.selectedCharacters.Count < 5)
        {
            //si no lo esta agrego al personaje a la lista
            this.selectedCharacters.Add(index);
        }
        //actualizo el listado
        UpdateMenu();
    }

    //funcion que se ejecuta al presionar el icono de cancelar
    private void CancelSelection()
    {
        this.selectedCharacters.RemoveAt(this.selectedCharacters.Count-1);
        UpdateMenu();
    }

    //funcion que se ejecuta al presionar el icono Ok
    private void OkSelection()
    {
        UpdateMenu();
    }
}
