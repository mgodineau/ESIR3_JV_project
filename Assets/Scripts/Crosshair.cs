using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Crosshair : MonoBehaviour
{

    // SCRIPT A METTRE SUR LE RETICULE



    private RectTransform reticle;

    public float restingSize;
    public float maxSize;
    public float speed;
    private float currentSize;





    // Start is called before the first frame update
    void Start()
    {
        reticle = GetComponent<RectTransform>();

    }



    void Update()
    {
        if (isMoving)
        {
            currentSize = Mathf.Lerp(currentSize, maxSize, Time.deltaTime * speed);
        }
        else
        {
            currentSize = Mathf.Lerp(currentSize, restingSize, Time.deltaTime * speed);
        }
        reticle.sizeDelta = new Vector2(currentSize, currentSize);
    }

    

    bool isMoving
    {

        get
        {
            if (Input.GetAxis("Horizontal") != 0 ||
                Input.GetAxis("Vertical") != 0 ||
                Input.GetAxis("Mouse X") != 0 ||
                Input.GetAxis("Mouse Y") != 0)
                return true;
            else
                return false;
        }
    }



   
}
