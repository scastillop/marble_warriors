using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed;
    private Vector3 affectedPosition;


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {      
        if (speed != 0)
        {    
            transform.position += transform.forward * (speed * Time.deltaTime);
        }
    }


    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("choqué");
        speed = 0;
        Destroy(gameObject);
    }
}
