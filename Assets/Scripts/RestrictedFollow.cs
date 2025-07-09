using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RestrictedFollow : MonoBehaviour
{
    [SerializeField] Transform target;

    [Space]
    [SerializeField] bool followX;
    [SerializeField] bool followY;
    [SerializeField] bool followZ;

    [Space]
    [SerializeField] float followTime;

    // ---

    Vector3 moveVelocity;

    private void Update()
    {
        Vector3 desiredPos = new Vector3(followX ? target.position.x : transform.position.x, followY ? target.position.y : transform.position.y, followZ ? target.position.z : transform.position.z);

        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref moveVelocity, followTime);
    }
}
