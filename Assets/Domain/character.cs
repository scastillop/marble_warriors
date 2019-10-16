using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Character : MonoBehaviour
{
    public int id;
    public string characterName;
    public int position;
    public int model;
    public Stat initialStat;
    public Stat actualStat;
    public List<Skill> skills;
    public GameObject sliderPrefab;
    public GameObject hpSlider;
    private Animator animator;
    private Quaternion initialRotation;
    private Vector3 initialPosition;
    private Vector3 targetPosition;
    private Vector3 affectedPosition;
    int actionPerforming;
    private string actionStatus;
    private UnityAction executeChanges;
    private UnityAction nextAction;

    private void Start()
    {
        //guardo la rotacion original del personaje
        this.initialRotation = transform.rotation;
        //guardo la posicion original del personaje
        this.initialPosition = transform.position;
        //seteo el id de posicion (1 al 10)
        this.targetPosition = transform.position;
        //seteo mi animator (para ejecutar animaciones posteriormente)
        this.animator = GetComponent<Animator>();
        //obtengo la posicion del cavas (UI)
        Transform canvasTransform = GameObject.Find("Canvas").GetComponent<Canvas>().transform;
        Vector2 canvasPosition = canvasTransform.position;
        //instancio la barra de vida
        this.hpSlider = Instantiate(sliderPrefab);
        //le añado al canvas (UI)
        this.hpSlider.GetComponent<Slider>().transform.SetParent(canvasTransform, false);    
        //seteo el valor de la barra de vida
        UpdateBars();
        
    }

    public void UpdateBars()
    {
        //obtengo la posicion del personaje
        Vector3 charPosition = transform.position;
        //modifico la posicion para que la barra de vida quede sobre la cabeza del personaje
        charPosition.y = charPosition.y + 6;
        charPosition.x = charPosition.x + 0.5f;
        //estimo la posicion en pantalla
        Vector2 barPosition = Camera.main.WorldToScreenPoint(charPosition);
        //le seteo la posicion
        hpSlider.GetComponent<Slider>().transform.position = barPosition;
        //seteo el valor de la barra
        this.hpSlider.GetComponent<Slider>().value = (float) this.actualStat.hp / this.initialStat.hp;
    }

    void OnGUI()
    {
       
    }

    private void Update()
    {
        //verifico si el personaje esta realizando alguna accion
        switch (this.actionStatus)
        {
            //si el personaje se dirige a realizar una accion
            case "going":
                //si aun no llega a su objetivo
                if (transform.position != this.targetPosition)
                {
                    //activo la animacion de caminar
                    animator.Play("walk_sword");
                    //actualizo la barra de vida para que se mantenga sobre el personaje
                    UpdateBars();
                    //giro al personaje hacia su objetivo
                    transform.LookAt(this.targetPosition);
                    //muevo al personaje
                    transform.position = Vector3.MoveTowards(transform.position, this.targetPosition, Time.deltaTime + 0.05f);
                }
                //si ya llego a su objetivo
                else
                {
                    //ejecuto la animacion de la accion
                    if (actionPerforming == 1)
                    {
                        //si es el ataque basico (id=1)
                        animator.Play("sword_attack_2");
                    }
                    //cambio el estado de la accion a "actuando"
                    this.actionStatus = "acting";
                    //giro al personaje hacia su enemigo
                    transform.LookAt(this.affectedPosition);
                }
                break;
            //si el personaje esta realizando una accion
            case "acting":
                //si el personaje ya termino de moverse
                if (!AnimatorIsPlaying())
                {
                    //cambio el estado de la accion para que el personaje regrese a su posicion inicial
                    this.actionStatus = "backing";
                    //aplico los cambios en el afectado
                    this.executeChanges.Invoke();
                }
                break;
            //si el personaje esta regresando a su posicion original
            case "backing":
                //si aun no llega a su posicion inicial
                if (transform.position != this.initialPosition)
                {
                    //activo la animacion de caminar
                    animator.Play("walk_sword");
                    //actualizo la barra de vide para que se mantenga sobre el personaje
                    UpdateBars();
                    //giro al personaje hacia su objetivo
                    transform.LookAt(this.initialPosition);
                    //muevo al personaje
                    transform.position = Vector3.MoveTowards(transform.position, this.initialPosition, Time.deltaTime + 0.05f);
                }
                //si ya llego a su posicion inicial
                else
                {
                    //cambio el estado de la accion a "detenido"
                    this.actionStatus = "stand";
                    //giro al personaje hacia su orientacion original
                    transform.rotation = this.initialRotation;
                    //detengo la animacion
                    animator.Play("stand");
                    //continuo con la siguiente accion
                    this.nextAction.Invoke();
                }
                break;
        }
    }

    public void performAction(int actionId, Vector3 affectedPosition, Vector3 targetPosition, UnityAction executeChanges, UnityAction nextAction)
    {
        //primero debo mover al personaje a la posicion del enemigo
        this.targetPosition = targetPosition;
        //le indico cual es su nuevo objetivo
        this.affectedPosition = affectedPosition;
        //le indico cual es la habilidad que debe ejecutar
        this.actionPerforming = actionId;
        //le indico que se esta realizando una accion
        this.actionStatus = "going";
        //le indico los cambios a realizar (en el afectado)
        this.executeChanges = executeChanges;
        //le indico cual es la siguiente acciona  realizar
        this.nextAction = nextAction;
    }

    //funcion que detecto si es que el personaje esta realizando una animacion
    private bool AnimatorIsPlaying()
    {
        return animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1.2f;
    }

    //funcion que determina si el personaje esta realizando una animacion en particular
    private bool AnimatorIsPlaying(string stateName)
    {
        return AnimatorIsPlaying() && animator.GetCurrentAnimatorStateInfo(0).IsName(stateName);
    }

    //funcion que sirve para actualizar las estadisticas del personaje
    public void setStat(Stat stat)
    {
        this.actualStat = stat;
        UpdateBars();
    }
}
