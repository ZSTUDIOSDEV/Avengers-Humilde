using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

using TMPro;

public enum lockMode
{
    Locked,
    Confined,
    None
}
public enum ShieldState
{
    InHands,
    Flying,
    Returning,
    Ragdoll,
    Stuck
}

public class ShieldController : MonoBehaviour
{
    public lockMode cursor;
    public ShieldState state = ShieldState.InHands;
    public Transform player, handHolder;
    public AudioClip shieldSound;
    public AudioSource source;
    public Camera CAM;
    //OPTIONAL
    public TMP_Text uiText;

    public Vector3 curveRotation, throwRotation;
    public float flyingSpeed, chargeSpeed, maxBounces, ySpinSpeed, xSpinSpeed, pickupRange, destroyDistance, fxDestroyTime, ragdollSpin, retractPower;
    public bool curvedStart, curvedFlying, curvedReturn;

    MeshCollider col;
    Vector3 lastPos, returnPos;
    Rigidbody rb;
    Transform shieldModel, cam;
    GameObject impactFX, trailFX;
    bool mustReturn = false, firstBounce;
    float tempBounces;
    int bounces;

    // Start is called before the first frame update
    void Start()
    {
        col = GetComponent<MeshCollider>();
        rb = GetComponent<Rigidbody>();
        cam = Camera.main.transform;    
        shieldModel = transform.Find("ShieldModel").transform;
        impactFX = transform.Find("ImpactFX").gameObject;
        trailFX = transform.Find("TrailFX").gameObject;
        CAM.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.P))
        {
            CAM.enabled = false;
        }

        if (state.Equals(ShieldState.InHands))
        {
            transform.position = handHolder.position;
            transform.rotation = handHolder.rotation;
            CAM.enabled = false;

            if (Input.GetKey(KeyCode.Mouse0))
            {
                tempBounces += chargeSpeed * Time.deltaTime;
                bounces = Mathf.RoundToInt(tempBounces);
                //OPTIONAL
                uiText.text = bounces.ToString();

                if(bounces >= maxBounces) { Throw(); }
            }
            else if (Input.GetKeyUp(KeyCode.Mouse0)) { Throw(); }

            rb.velocity = Vector3.zero;
        }
        else if (state.Equals(ShieldState.Flying) || state.Equals(ShieldState.Returning))
        {
            if (!player) Returned();

            //Movement
            transform.Translate(transform.forward * flyingSpeed * Time.deltaTime, Space.World);
            Rotation(); 

            if (Input.GetKey(KeyCode.Mouse0)) { mustReturn = true; }
        }
        else if (state.Equals(ShieldState.Ragdoll) || state.Equals(ShieldState.Returning))
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if(distance < pickupRange) { Returned(); }
        }

        if (player)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance > destroyDistance) { Returned(); }
        }
        else { Returned(); }

        if (cursor.Equals(lockMode.Confined))
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;

        }
        else if (cursor.Equals(lockMode.Locked))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else if (cursor.Equals(lockMode.None))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (state.Equals(ShieldState.Ragdoll) || (state.Equals(ShieldState.Stuck)))
        {
          if (Input.GetKey(KeyCode.Mouse1)) 
          {
                Returned();
          }
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (!state.Equals(ShieldState.InHands))
        {
            if(collision.transform.name == "Sticky") { Stick(); }
            if(collision.transform.name == "Player" && firstBounce) { Returned(); }
        }
        
        if(state.Equals(ShieldState.Returning)) { Ragdoll(); }

        if (state.Equals(ShieldState.Flying) && bounces > 0 && collision.transform.name != "Player") { Bounce(collision.GetContact(0).normal); }
    }

    void Bounce(Vector3 contactNormalDirection)
    {
        firstBounce = true;
        bounces -= 1;
        source.PlayOneShot(shieldSound);

        //Optional
        uiText.text = bounces.ToString();

        Vector3 direction = contactNormalDirection - lastPos.normalized;
        direction = direction.normalized;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.forward);
        lastPos = transform.position;

        var fx = Instantiate(impactFX, transform.position, Quaternion.identity);
        fx.SetActive(true);
        Destroy(fx, fxDestroyTime);

        if (mustReturn)
        {
            state = ShieldState.Returning;

            bounces = 1;

            //OPTIONAL
            uiText.text = "RETURNING";

            returnPos = player.position;
            transform.LookAt(returnPos);
        }
        else if (bounces <= 0) { Ragdoll(); }
    }

    void Stick()
    {
        state = ShieldState.Stuck;
        source.PlayOneShot(shieldSound);

        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.useGravity = false;
        rb.isKinematic = true;

        mustReturn = false;
        trailFX.SetActive(false);
        col.enabled = false;

        //OPTIONAL
        uiText.text = "STUCK";
    }

    void Rotation()
    {
        shieldModel.RotateAround(shieldModel.position, shieldModel.up, ySpinSpeed * Time.deltaTime);

        if(curvedStart && !firstBounce)
        {
            transform.eulerAngles = transform.eulerAngles + (curveRotation * Time.deltaTime);
        }
        else if (curvedFlying && state.Equals(ShieldState.Flying))
        {
            transform.eulerAngles = transform.eulerAngles + (curveRotation * Time.deltaTime);
        }
        else if (curvedReturn && state.Equals(ShieldState.Returning))
        {
            transform.eulerAngles = transform.eulerAngles + (curveRotation * Time.deltaTime);
        }
        else
        {
            transform.RotateAround(transform.position, transform.forward, xSpinSpeed * Time.deltaTime);
        }
    }

    void Throw()
    {
        firstBounce = false;
        transform.SetParent(null);
        transform.position = cam.position + cam.forward;
        transform.eulerAngles = cam.eulerAngles + throwRotation;
        trailFX.SetActive(true);
        col.enabled = true;
        rb.isKinematic = false;

        state = ShieldState.Flying;
    }

    void Returned()
    {
        if (!player) return;

        state = ShieldState.InHands;

        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.isKinematic = true;

        mustReturn = false;
        trailFX.SetActive(false);
        col.enabled = false;
        transform.SetParent(handHolder);
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(0, 0, 0);
        bounces = 1;
        tempBounces = 1;
        CAM.enabled = true;

        //OPTIONAL
        uiText.text = "READY";
    }

    void Ragdoll()
    {
        if (state.Equals(ShieldState.InHands)) return;

        state = ShieldState.Ragdoll;

        rb.isKinematic = false;
        rb.useGravity = true;
        rb.constraints = RigidbodyConstraints.None;
        Vector3 torque = new Vector3(Random.Range(-ragdollSpin, ragdollSpin), Random.Range(-ragdollSpin, ragdollSpin), Random.Range(-ragdollSpin, ragdollSpin));
        rb.AddTorque(torque);

        var fx = Instantiate(impactFX, transform.position, Quaternion.identity);
        fx.SetActive(true);
        Destroy(fx, fxDestroyTime);

        trailFX.SetActive(false);
        CAM.enabled = false;


        //OPTIONAL
        uiText.text = "RAGDOLL";
    }
}
