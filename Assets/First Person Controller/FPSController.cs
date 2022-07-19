using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FPSController : MonoBehaviour
{
    CharacterController characterController;

    [Header ("Character Options")]
    public float walkSpeed = 6.0f;
    public float runSpeed = 10.0f;
    public float jumpSpeed = 8.0f;
    public float gravity = 20.0f;

    [Header("Camera Options")]
    public Camera cam;
    public float mouseHorizontal = 2.0f;
    public float mouseVertical = 2.0f;
    public float minRotation = 2.0f;
    public float maxRotation = 2.0f;
    float h_mouse, v_mouse;

    private Vector3 move = Vector3.zero;
    // Start is called before the first frame update
    void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    // Update is called once per frame
    void Update()
    {
        h_mouse = mouseHorizontal * Input.GetAxis("Mouse X");
        v_mouse += mouseVertical * Input.GetAxis("Mouse Y");

        v_mouse = Mathf.Clamp(v_mouse, minRotation, maxRotation);
        cam.transform.localEulerAngles = new Vector3(-v_mouse, 0, 0);
        transform.Rotate(0, h_mouse, 0);

        if (characterController.isGrounded)
        {
            move = new Vector3(Input.GetAxis("Horizontal"), 0.0f, Input.GetAxis("Vertical"));

            if (Input.GetKey(KeyCode.LeftShift))
                move = transform.TransformDirection(move) * runSpeed;
            else
                move = transform.TransformDirection(move) * walkSpeed;

            if (Input.GetKey(KeyCode.Space))
            {
                move.y = jumpSpeed;
            }
        }

        move.y -= gravity * Time.deltaTime;

        characterController.Move(move * Time.deltaTime);
    }
}
