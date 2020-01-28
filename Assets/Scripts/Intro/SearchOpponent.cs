using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SearchOpponent : MonoBehaviour
{
    public float waitForShow;
    public float FadeRate;
    private CanvasGroup canvas;
    private bool showingLogo;
    private bool blinking;
    private bool fadeIn;

    // Start is called before the first frame update
    void Start()
    {
        this.showingLogo = false;
        this.fadeIn = true;
        this.canvas = this.GetComponent<CanvasGroup>();
        StartCoroutine(WaitforshowLogo(waitForShow));
    }

    // Update is called once per frame
    void Update()
    {
        //
        if (this.canvas.alpha == 1 && this.showingLogo)
        {
            this.fadeIn = false;
        }
        if (this.canvas.alpha == 0 && this.showingLogo)
        {
            this.fadeIn = true;
        }
        //oscurecer
        if (this.canvas.alpha < 1 && this.showingLogo && this.fadeIn)
        {
            this.canvas.alpha = this.canvas.alpha + this.FadeRate * Time.deltaTime;
        }
        //aclarar
        if (this.canvas.alpha > 0 && this.showingLogo && !this.fadeIn)
        {
            this.canvas.alpha = this.canvas.alpha - this.FadeRate * Time.deltaTime;
        }
    }

    private IEnumerator WaitforshowLogo(float duration)
    {
        //espero los segundos
        yield return new WaitForSeconds(duration);
        //inicio el
        this.showingLogo = true;
    }

    public void StopBlinking()
    {
        this.showingLogo = false;
        this.canvas.alpha = 1;
    }

}
