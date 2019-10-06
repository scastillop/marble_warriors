using System.Collections.Generic;
using UnityEngine;
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
    public Animator animator;
    private Vector3 newPosition;

    private void Start()
    {
        //seteo la posicion
        newPosition = transform.position;
        //seteo mi animator
        animator = GetComponent<Animator>();
        //obtengo la posicion del cavas (UI)
        Transform canvasTransform = GameObject.Find("Canvas").GetComponent<Canvas>().transform;
        Vector2 canvasPosition = canvasTransform.position;
        //instancio la barra
        hpSlider = Instantiate(sliderPrefab);
        //le añado al canvas (UI)
        hpSlider.GetComponent<Slider>().transform.SetParent(canvasTransform, false);
        
        //seteo el valor de la barra de vida
        UpdateBars();

        /*
        switch (Random.Range(0, 4))
        {
            case 0:
                animator.Play("scream");
                break;
            case 1:
                animator.Play("idle");
                break;
            case 2:
                animator.Play("look_sword");
                break;
            case 3:
                animator.Play("stand");
                break;
        }
        */
    }

    public void UpdateBars()
    {
        //obtengo la posicion del personaje
        Vector3 charPosition = transform.position;
        //modifico la posicion para que la barra de vida quede sobre la cabeza
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
        
        if(transform.position != newPosition)
        {
            animator.Play("walk sword");
            UpdateBars();
            transform.LookAt(newPosition);
            //Vector3 stepBack = newPosition;
            //stepBack.x = stepBack.x+3f;
            transform.position = Vector3.MoveTowards(transform.position, newPosition, Time.deltaTime + 0.1f);
        }
        else
        {
            animator.Play("void");
        }
        
    }

    public void Move(Vector3 position)
    {
        newPosition = position;
    }
}
