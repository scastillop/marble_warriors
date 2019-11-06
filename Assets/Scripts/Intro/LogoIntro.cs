using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LogoIntro : MonoBehaviour
{
    public float waitForShow;
    public float FadeRate;
    private CanvasGroup canvas;
    private bool showingLogo;

    // Start is called before the first frame update
    void Start()
    {
        this.showingLogo = false;
        this.canvas = this.GetComponent<CanvasGroup>();
        StartCoroutine(waitforshowLogo(waitForShow));
    }

    // Update is called once per frame
    void Update()
    {
        if (this.canvas.alpha < 1 && this.showingLogo)
        {
            this.canvas.alpha = this.canvas.alpha + this.FadeRate * Time.deltaTime;
        }
    }

    private IEnumerator waitforshowLogo(float duration)
    {
        //espero los segundos
        yield return new WaitForSeconds(duration);
        //inicio el
        this.showingLogo = true;
    }
}
