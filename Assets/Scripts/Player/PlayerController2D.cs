using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController2D : MonoBehaviour
{
    public Animator animator;
    public Rigidbody rb;
    private CameraMovement cameraMovement;
    public bool rotationMode;
    [Range(1, 4)] public float speed;

    private int direction;  //0 => up/backwards, 1 => right, 2 => down/forwards, 3 => left
    private int isMovingHorizontally, isMovingVertically;
    private bool hasMoved;

    void Start()
    {
        direction = 0;
        isMovingHorizontally = isMovingVertically = 0;
        cameraMovement = Camera.main.GetComponent<CameraMovement>();
        hasMoved = false;
    }

    void Update()
    {
        Move();
        if (!hasMoved || rotationMode)
        {
            if (rotationMode)
            {
                Rotate90();
            }
            else
            {
                Rotate();
            }
        }
    }
    private void Rotate90()
    {
        float rotationAngle = 0;

        if (Input.GetKeyDown("q"))
            rotationAngle = 90;
        else if (Input.GetKeyDown("e"))
            rotationAngle = -90;

        rb.transform.Rotate(0, 0, rotationAngle);
        cameraMovement.Rotate(rotationAngle);
    }
    private void Rotate()
    {
        float rotationAngle = Input.GetAxis("Mouse X") * -1;
        rb.transform.Rotate(0, 0, rotationAngle);
        cameraMovement.Rotate(rotationAngle);
    }
    private void Move()
    {
        isMovingHorizontally = isMovingVertically = 0;
        Vector3 displacement = Vector3.zero;

        if (Input.GetKey("w"))
        {
            direction = 0;
            isMovingVertically += 1;
            displacement += transform.up;
        }
        if (Input.GetKey("s"))
        {
            direction = 2;
            isMovingVertically -= 1;
            displacement += transform.up * -1;
        }
        if (Input.GetKey("d"))
        {
            direction = 1;
            isMovingHorizontally += 1;
            displacement += transform.right;
        }
        if (Input.GetKey("a"))
        {
            direction = 3;
            isMovingHorizontally -= 1;
            displacement += transform.right * -1;
        }
        hasMoved = Vector3.SqrMagnitude(displacement) != 0;
        rb.MovePosition(rb.position + displacement.normalized * speed * Time.deltaTime);

        animator.SetInteger("Direction", direction);
        animator.SetBool("IsMoving", isMovingHorizontally != 0 || isMovingVertically != 0);
    }
}
