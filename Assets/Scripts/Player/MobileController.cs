using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MobileController : MonoBehaviour
{
    public Animator animator;
    public Rigidbody rb;
    [Range(1, 4)] public float speed;

    Touch touch;
    private int direction;  //0 => up/backwards, 1 => right, 2 => down/forwards, 3 => left
    private int isMovingHorizontally, isMovingVertically;

    void Awake()
    {
        if (SystemInfo.deviceType != DeviceType.Handheld)
        {
            enabled = false;
        }
    }

    void Update()
    {
        if (Input.touchCount == 0)
        {
            animator.SetBool("IsMoving", false);
            return;
        }
        touch = Input.GetTouch(0);
        Move();
    }

    private void Move()
    {
        isMovingHorizontally = isMovingVertically = 0;
        Vector3 displacement = Vector3.zero;

        if (touch.position.y >= Screen.height / 2f)
        {
            direction = 0;
            isMovingVertically += 1;
            displacement += transform.up;
        }
        if (touch.position.y < Screen.height / 2f)
        {
            direction = 2;
            isMovingVertically -= 1;
            displacement += transform.up * -1;
        }
        if (touch.position.x < Screen.width / 2f)
        {
            direction = 1;
            isMovingHorizontally += 1;
            displacement += transform.right;
        }
        if (touch.position.x >= Screen.width / 2f)
        {
            direction = 3;
            isMovingHorizontally -= 1;
            displacement += transform.right * -1;
        }
        rb.MovePosition(rb.position + displacement.normalized * speed * Time.deltaTime);

        animator.SetInteger("Direction", direction);
        animator.SetBool("IsMoving", isMovingHorizontally != 0 || isMovingVertically != 0);
    }
}
