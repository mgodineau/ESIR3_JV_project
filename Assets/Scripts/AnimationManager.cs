using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationManager : MonoBehaviour
{

    public Animator _animator;

    // Start is called before the first frame update
    void Start()
    {
        _animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        var y = Input.GetAxis("Vertical");
        var x = Input.GetAxis("Horizontal");

        Move(y, x);
    }

    private void Move(float y, float x)
    {
        _animator.SetFloat("VelY", y);
        _animator.SetFloat("VelX", x);
    }
}
