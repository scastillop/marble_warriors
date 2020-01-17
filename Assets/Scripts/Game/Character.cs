using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Character : MonoBehaviour
{
    public string characterName;
    public int position;
    public Stat initialStat;
    public Stat actualStat;
    public List<Skill> skills;
    public GameObject sliderPrefab;
    private GameObject hpSlider;
    private Animator animator;
    private Quaternion initialRotation;
    private Vector3 initialPosition;
    private Vector3 targetPosition;
    private Vector3 affectedPosition;
    private string actionPerforming;
    private string actionStatus;
    private UnityAction executeChanges;
    private UnityAction nextAction;
    private float walkSpeed;
    public List<GameObject> effectsPrefab;
    public List<GameObject> projectilesPrefab;
    private int projectileIndex;
    public GameObject effectAttackPrefab;
    public GameObject effectDefensePrefab;
    private GameObject effectAttackInstance;
    private GameObject effectDefenseInstance;
    public GameObject effectAttackLowPrefab;
    public GameObject effectDefenseLowPrefab;
    private GameObject effectAttackLowInstance;
    private GameObject effectDefenseLowInstance;


    private void Start()
    {
        //guardo la rotacion original del personaje
        this.initialRotation = transform.rotation;
        //guardo la posicion original del personaje
        this.initialPosition = transform.position;
        //seteo el id de posicion (1 al 10)
        this.targetPosition = transform.position;
        //seteo la velocidad al caminar
        this.walkSpeed = 6f;
        //seteo mi animator (para ejecutar animaciones posteriormente)
        this.animator = GetComponent<Animator>();
        //obtengo la posicion del cavas (UI)
        Transform canvasTransform = GameObject.Find("Canvas").GetComponent<Canvas>().transform;
        Vector2 canvasPosition = canvasTransform.position;
        //instancio la barra de vida
        this.hpSlider = Instantiate(sliderPrefab);
        //le añado al canvas (UI)
        this.hpSlider.GetComponent<Slider>().transform.SetParent(canvasTransform, false);
        this.hpSlider.GetComponent<RectTransform>().transform.SetAsFirstSibling();
        //seteo el valor de la barra de vida
        UpdateBars();
        //si el personaje murio, ejecuta su animacion
        if (this.actualStat.hp == 0)
        {
            this.animator.Play("Death",0,1);
            this.actionStatus = "dying";
            //y oculto su barra de vida
            this.hpSlider.GetComponent<CanvasGroup>().alpha = 0;
        }
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


    private void Update()
    {
        //verifico si el personaje esta realizando alguna accion
        switch (this.actionStatus)
        {
            //si el personaje esta despertando
            case "awaking":
                //si el personaje ya termino de animarse paso al siguiente estado (caminar)
                if (!AnimatorIsPlaying("Awake"))
                {
                    //si es que necesito caminar
                    if (this.transform.position != this.targetPosition)
                    {
                        //activo la animacion de caminar
                        this.animator.Play("Walk");
                        //cambio el estado
                        this.actionStatus = "going";
                    }
                    //si no necesito caminar
                    else
                    {
                        //ejecuto la animacion de la accion
                        this.animator.Play(this.actionPerforming);
                        //cambio el estado de la accion a "actuando"
                        this.actionStatus = "acting";
                        //giro al personaje hacia su enemigo
                        transform.LookAt(this.affectedPosition);
                    }
                }
                break;
            //si el personaje se dirige a realizar una accion
            case "going":
                //si aun no llega a su objetivo
                if (this.transform.position != this.targetPosition)
                {
                    //actualizo la barra de vida para que se mantenga sobre el personaje
                    UpdateBars();
                    //actualizo la posicion de los efectos (ataque/defensa)
                    UpdateEffectsPosition();
                    //giro al personaje hacia su objetivo
                    this.transform.LookAt(this.targetPosition);
                    //muevo al personaje
                    this.transform.position = Vector3.MoveTowards(this.transform.position, this.targetPosition, Time.deltaTime * this.walkSpeed);
                    //giro paulatino
                    /*
                    //determino que empiece a rotar cuando quede cierta distancia
                    if(Vector3.Distance(this.transform.position, this.targetPosition)<2f)
                    {
                        //calcula la rotacion final
                        var rotation = Quaternion.LookRotation(this.targetPosition - this.transform.position);
                        //lo giro paulatinamente
                        this.transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, Time.deltaTime * 1.1f);
                    }
                    */
                }
                //si ya llego a su objetivo
                else
                {
                    //ejecuto la animacion de la accion
                    this.animator.Play(this.actionPerforming);

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
                    //si no necesito caminar
                    if (this.initialPosition != this.targetPosition)
                    {
                        //activo la animacion de caminar
                        this.animator.Play("Walk");
                        //cambio el estado de la accion para que el personaje regrese a su posicion inicial
                        this.actionStatus = "backing";
                    }
                    else
                    {
                        //detengo la animacion
                        this.animator.Play("Sleep");
                        //cambio el estado de la accion a "durmiendo"
                        this.actionStatus = "stand";
                        //giro al personaje hacia su orientacion original
                        transform.rotation = this.initialRotation;
                        //continuo con la siguiente accion
                        this.nextAction.Invoke();
                    }
                }
                break;
            //si el personaje esta regresando a su posicion original
            case "backing":
                //si aun no llega a su posicion inicial
                if (transform.position != this.initialPosition)
                {
                    //actualizo la barra de vide para que se mantenga sobre el personaje
                    UpdateBars();
                    //actualizo la posicion de los efectos (ataque/defensa)
                    UpdateEffectsPosition();
                    //giro al personaje hacia su objetivo
                    transform.LookAt(this.initialPosition);
                    //muevo al personaje
                    transform.position = Vector3.MoveTowards(transform.position, this.initialPosition, Time.deltaTime * this.walkSpeed);
                }
                //si ya llego a su posicion inicial
                else
                {
                    //detengo la animacion
                    this.animator.Play("Sleep");
                    //cambio el estado de la accion a "durmiendo"
                    this.actionStatus = "stand";
                    //giro al personaje hacia su orientacion original
                    transform.rotation = this.initialRotation;
                    //continuo con la siguiente accion
                    this.nextAction.Invoke();
                }
                break;
            //si el personaje esta durmiendo
            case "stand":
               
                break;
            case "dying":
                
                break;
        }
    }

    public void PerformAction(int projectileIndex, string actionName, Vector3 affectedPosition, Vector3 targetPosition, UnityAction executeChanges, UnityAction nextAction)
    {
        //guardo el proyectil
        this.projectileIndex = projectileIndex;
        // debo mover al personaje a la posicion del enemigo
        this.targetPosition = targetPosition;
        //le indico cual es su nuevo objetivo
        this.affectedPosition = affectedPosition;
        //le indico cual es la habilidad que debe ejecutar
        this.actionPerforming = actionName;
        //le indico los cambios a realizar (en el afectado)
        this.executeChanges = executeChanges;
        //le indico cual es la siguiente acciona  realizar
        this.nextAction = nextAction;
        //comienzo la animacion de despertarse
        this.animator.Play("Awake");
        //le indico que se esta realizando una accion
        this.actionStatus = "awaking";

    }

    //funcion que detecto si es que el personaje esta realizando una animacion
    private bool AnimatorIsPlaying()
    {
        return this.animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f;
    }

    //funcion que determina si el personaje esta realizando una animacion en particular
    private bool AnimatorIsPlaying(string stateName)
    {
        if (this.animator.GetCurrentAnimatorStateInfo(0).IsName(stateName)&& !AnimatorIsPlaying())
        {
            return false;
        }
        else
        {
            return true;
        }
    }

    //funcion que sirve para actualizar las estadisticas del personaje
    public void SetStat(Stat stat, int effectIndex)
    {
        //verifico si se debe activar un efecto
        if (effectIndex >= 0)
        {
            //si es cero o mas, activo el efecto
            GameObject effect = Instantiate(this.effectsPrefab[effectIndex], this.transform.position, this.transform.rotation);
            //dejo programada su eliminacion
            StartCoroutine(DestroyDelay(effect, 10f));
        }
        this.actualStat = stat;
        UpdateBars();
        //si el personaje murio, ejecuta su animacion
        if (this.actualStat.hp == 0)
        {            
            this.animator.Play("Death");
            this.actionStatus = "dying";
            //y oculto su barra de vida
            this.hpSlider.GetComponent<CanvasGroup>().alpha = 0;
        }
        UpdateEffects();
    }

    //funcion que ejecuta los cambios (que realizan las acciones)
    private void ExecuteAction()
    {
        //aplico los cambios en el afectado
        this.executeChanges.Invoke();
    }

    //funcion que destruye objetos de la pantalla
    private IEnumerator DestroyDelay(GameObject objecto, float duration)
    {
        //espero los segundos
        yield return new WaitForSeconds(duration);
        //destruyo el objeto
        Destroy(objecto);
        
    }

    //funcion que activa o desactiva los efectos cuando un personaje tiene los estado alterados
    public void UpdateEffects()
    {
        //si el ataque esta sobre lo normal
        if (this.actualStat.atk > this.initialStat.atk)
        {
            //si no esta activo ya el efecto
            if (!this.effectAttackInstance)
            {   
                //activo el efecto
                this.effectAttackInstance = Instantiate(this.effectAttackPrefab, this.transform.position, this.transform.rotation);
            }
        }
        //si el ataque es menos de lo normal
        else if (this.actualStat.atk < this.initialStat.atk)
        {
            //si no esta activo ya el efecto
            if (!this.effectAttackLowInstance)
            {
                //activo el efecto
                this.effectAttackLowInstance = Instantiate(this.effectAttackLowPrefab, this.transform.position, this.transform.rotation);
            }
        }
        //si el ataque es normal
        else 
        {
            //si estaba activo el efecto
            if (this.effectAttackInstance)
            {
                //desactivo el efecto
                Destroy(this.effectAttackInstance);
            }
            //si estaba activo el efecto de ataque bajo
            if (this.effectAttackLowInstance)
            {
                //desactivo el efecto
                Destroy(this.effectAttackLowInstance);
            }
        }
        //si la defensa esta sobre lo normal
        if (this.actualStat.def > this.initialStat.def )
        {
            //si no esta activo ya el efecto
            if (!this.effectDefenseInstance)
            {
                //activo el efecto
                this.effectDefenseInstance = Instantiate(this.effectDefensePrefab, this.transform.position, this.transform.rotation);
            }
        }
        //si la defensa es menos de lo normal
        else if (this.actualStat.def < this.initialStat.def)
        {
            //si no esta activo ya el efecto
            if (!this.effectDefenseLowInstance)
            {
                //activo el efecto
                this.effectDefenseLowInstance = Instantiate(this.effectDefenseLowPrefab, this.transform.position, this.transform.rotation);
            }
        }
        //si la defensa es normal
        else
        {
            //si estaba activo el efecto
            if (this.effectDefenseInstance)
            {
                //desactivo el efecto
                Destroy(this.effectDefenseInstance);
            }
            //si estaba activo el efecto de defensa bajo
            if (this.effectDefenseLowInstance)
            {
                //desactivo el efecto
                Destroy(this.effectDefenseLowInstance);
            }
        }
        //si el personaje esta muerto
        if(this.actualStat.hp == 0)
        {
            //ataque
            //si estaba activo el efecto
            if (this.effectAttackInstance)
            {
                //desactivo el efecto
                Destroy(this.effectAttackInstance);
            }
            //si estaba activo el efecto de ataque bajo
            if (this.effectAttackLowInstance)
            {
                //desactivo el efecto
                Destroy(this.effectAttackLowInstance);
            }
            //defensa
            //si estaba activo el efecto
            if (this.effectDefenseInstance)
            {
                //desactivo el efecto
                Destroy(this.effectDefenseInstance);
            }
            //si estaba activo el efecto de defensa bajo
            if (this.effectDefenseLowInstance)
            {
                //desactivo el efecto
                Destroy(this.effectDefenseLowInstance);
            }
        }
    }

    //funcion que actualiza la posicion de los efectos
    private void UpdateEffectsPosition()
    {
        if (this.effectAttackInstance)
        {
            //si esta activo el efecto de ataque
            this.effectAttackInstance.transform.position = this.transform.position;
        }
        if (this.effectDefenseInstance)
        {
            //si esta activo el efecto de defensa
            this.effectDefenseInstance.transform.position = this.transform.position;
        }
        if (this.effectAttackLowInstance)
        {
            //si esta activo el efecto de ataque bajo
            this.effectAttackLowInstance.transform.position = this.transform.position;
        }
        if (this.effectDefenseLowInstance)
        {
            //si esta activo el efecto de defensa baja
            this.effectDefenseLowInstance.transform.position = this.transform.position;
        }
    }

    //funcion que dispara proyectiles
    public void ShootProjectile()
    {
        if (this.projectileIndex > -1)
        {
            GameObject vfx;
            var playerPos = this.affectedPosition;
            foreach (Transform item in gameObject.GetComponentsInChildren<Transform>(true))
            {
                if (item.name == "ProjectileOrigin")
                {
                    Vector3 temp = new Vector3(0, 4.0f, 0);
                    vfx = Instantiate(projectilesPrefab[this.projectileIndex], item.gameObject.transform.position, Quaternion.identity);
                    Physics.IgnoreCollision(projectilesPrefab[this.projectileIndex].GetComponent<Collider>(), GetComponent<Collider>());
                    vfx.transform.LookAt(playerPos + temp);
                }
            }
        }
    }

    //funcion que oculta la lanza del lancero
    public void hideSpear()
    {
        foreach (Transform item in gameObject.GetComponentsInChildren<Transform>(true))
        {
            if (item.name == "Spear")
            {
                item.gameObject.SetActive(false);
            }
        }
    }

    //funcion que muestra la lanza del lancero
    public void showSpear()
    {
        foreach (Transform item in gameObject.GetComponentsInChildren<Transform>(true))
        {
            if (item.name == "Spear")
            {
                item.gameObject.SetActive(true);
            }
        }
    }
}
