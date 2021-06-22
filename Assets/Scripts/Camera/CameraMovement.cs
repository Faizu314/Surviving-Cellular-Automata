using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    [Header("Camera")]
    public Transform player;
    public bool useLateUpdate;
    [Range(0.01f, 1f)] public float movementSmoothness;
    [Range(0.01f, 100f)] public float rotationSmoothness;
    public float angleOfIncident;

    private float rotationDestination = 0f;

    //private void Start()
    //{
    //    Quaternion rotation = Quaternion.identity;
    //    rotation.
    //    transform.SetPositionAndRotation(Vector3.forward * -10, )
    //}

    private void FixedUpdate()
    {
        if (!useLateUpdate)
        {
            Vector3 abovePlayer = new Vector3(player.position.x, player.position.y, -10);

            Vector3 destination = abovePlayer - player.transform.up * Mathf.Sin(angleOfIncident * Mathf.Deg2Rad) * 10;

            float movementSpeed = Vector3.SqrMagnitude(destination - transform.position) / movementSmoothness;
            movementSpeed *= Time.deltaTime;

            transform.position = Vector3.MoveTowards(transform.position, destination, movementSpeed);

            transform.RotateAround(player.position, Vector3.back, -rotationDestination * rotationSmoothness);
            rotationDestination -= rotationDestination * rotationSmoothness;
        }
    }

    private void LateUpdate()
    {
        if (useLateUpdate)
        {
            Vector3 abovePlayer = new Vector3(player.position.x, player.position.y, -10);

            Vector3 destination = abovePlayer - player.transform.up * Mathf.Sin(angleOfIncident * Mathf.Deg2Rad) * 10;

            float movementSpeed = Vector3.SqrMagnitude(destination - transform.position) / movementSmoothness;
            movementSpeed *= Time.deltaTime;

            transform.position = Vector3.MoveTowards(transform.position, destination, movementSpeed);

            //transform.RotateAround(player.position, Vector3.back, -rotationDestination * rotationSmoothness);
            //rotationDestination -= rotationDestination * rotationSmoothness;

            transform.RotateAround(player.position, Vector3.back, -rotationDestination * Time.deltaTime * rotationSmoothness);
            rotationDestination -= rotationDestination * Time.deltaTime * rotationSmoothness;
        }
    }

    public void Rotate(float angle)
    {
        rotationDestination += angle;
        //transform.RotateAround(player.position, Vector3.back, -angle);
    }
}
