using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    [SerializeField] GameObject playerCamera;
    float screenWidth = Screen.width;
    public float maxCameraRotation = 45.0f;
    private float cameraZRotation;
    private Rigidbody rb;
    public float speed = 50f;              // input force strength
    public float maxSpeed = 10f;

    public float limit = 10f;
    public float resistanceExponent = 2f;
    public float maxResistance = 40.0f;      // should be similar to speed
    [SerializeField] private float rotationSpeed = 0.5f;

    [SerializeField] GameObject RightLimit;

    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] Image CursorImage;

    [SerializeField] private AudioSource boatAudioSource;
    private bool movingTowardEdge;

    private LineRenderer rope;

    private void Start()
    {
        Cursor.visible = false;
        
        limit = RightLimit.transform.position.x;
        rb = GetComponent<Rigidbody>();
        waterHeight = transform.position.y;
        rope = GetComponent<LineRenderer>();
    }

    private void FixedUpdate()
    {
        RotateCamera();
        MovePlayer();
        UpdateBoatSoundSource();
        UpdateVerticalPosition();
        if (Input.GetKeyDown(KeyCode.Space))
        {
            JumpPlayer(1f);
        }
        SwivelCamera();
        UpdateRopePosition();
    }

    private void RotateCamera()
    {
        float mouseXPosition = Input.mousePosition.x;

        CursorImage.transform.position = new Vector3(Mathf.Clamp(Input.mousePosition.x, 0.0f, screenWidth), CursorImage.transform.position.y, CursorImage.transform.position.z);

        mouseXPosition = Mathf.Clamp(mouseXPosition, 0, screenWidth);

        float normalized = mouseXPosition / screenWidth;   // 0 - 1
        normalized -= 0.5f; normalized *= 2.0f; //set range between -1 and 1

        cameraZRotation = Mathf.Lerp(cameraZRotation, normalized * maxCameraRotation, rotationSpeed * Time.deltaTime); //set it between -45 and 45

        transform.rotation = Quaternion.Euler(0.0f, 0.0f, cameraZRotation * -1);
    }

    private void MovePlayer()
    {
        if (!inAir)
        {
            float position = transform.position.x;

            // --- INPUT FORCE (from camera) ---
            float rotationNormal = cameraZRotation / maxCameraRotation; // -1 to 1
            float inputForce = rotationNormal * speed;

            // --- RESISTANCE FORCE ---
            float normalizedPos = position / limit;

            float resistance = Mathf.Pow(Mathf.Abs(normalizedPos), resistanceExponent) * maxResistance;

            float velX = rb.linearVelocity.x;

            // check if moving toward edge
            movingTowardEdge = Mathf.Sign(velX) == Mathf.Sign(position);

            if (movingTowardEdge)
            {
                // resist movement outward
                resistance *= -Mathf.Sign(position);
            }
            else
            {
                // assist movement inward (optional, weaker feels better)
                resistance *= -Mathf.Sign(position) * 0.5f;
            }

            // --- TOTAL FORCE ---
            float totalForce = inputForce + resistance;

            // --- APPLY FORCE ---
            rb.AddForce(new Vector3(totalForce, 0f, 0f), ForceMode.Force);

            // --- CLAMP VELOCITY ---
            Vector3 vel = rb.linearVelocity;
            vel.x = Mathf.Clamp(vel.x, -maxSpeed, maxSpeed);
            rb.linearVelocity = vel;

            // --- DEBUG ---
            infoText.text = rb.linearVelocity.x.ToString("F2") + " | " + resistance.ToString("F2") + "\n" + position.ToString("F2") + " | MTE: " + movingTowardEdge;
        }
    }

    public float waterHeight = 2f;
    public float springStrength = 50f;
    public float damping = 10f;
    public float gravity = 25f;
    public float fallMultiplier = 2f;
    public float riseMultiplier = 1.5f;
    public bool inAir;

    private void UpdateVerticalPosition()
    {
        Vector3 vel = rb.linearVelocity;
        float displacement = transform.position.y - waterHeight;

        // --- Check if airborne ---
        inAir = transform.position.y > waterHeight + 0.1f;

        // --- Apply spring + damping only in water ---
        if (!inAir)
        {
            float springForce = -springStrength * displacement;
            float dampingForce = -damping * vel.y;
            rb.AddForce(Vector3.up * (springForce + dampingForce), ForceMode.Acceleration);
        }

        // --- Custom gravity ---
        if (vel.y < 0)
            rb.AddForce(Vector3.down * gravity * fallMultiplier, ForceMode.Acceleration);
        else if (vel.y > 0)
            rb.AddForce(Vector3.down * gravity * riseMultiplier, ForceMode.Acceleration);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Wake"))
        {
            if (movingTowardEdge)
            {
                JumpPlayer(0.2f);
            }
            else
            {

                JumpPlayer(1f);
            }
        }
    }

    private void JumpPlayer(float multiplier)
    {
        rb.AddForce(new Vector3(0.0f, Mathf.Abs(rb.linearVelocity.x) * multiplier, 0.0f), ForceMode.Impulse);
    }

    public float cameraTurnMax;
    public float cameraTurnSpeed;

    private void SwivelCamera()
    {
        Quaternion rotation = playerCamera.transform.localRotation;

        if (Input.GetKey(KeyCode.A))
        {
            if (!Input.GetKey(KeyCode.D))
            {
                rotation = Quaternion.Euler(rotation.eulerAngles.x, rotation.eulerAngles.y - cameraTurnSpeed, rotation.eulerAngles.z);
            }
        }
        else if (Input.GetKey(KeyCode.D))
        {
            rotation = Quaternion.Euler(rotation.eulerAngles.x, rotation.eulerAngles.y + cameraTurnSpeed, rotation.eulerAngles.z);
        }

        float clampedY;

        if (rotation.eulerAngles.y > 180)
        {
            clampedY = Mathf.Clamp(rotation.eulerAngles.y - 360, -maxCameraRotation, maxCameraRotation);
        }
        else
        {
            clampedY = Mathf.Clamp(rotation.eulerAngles.y, -maxCameraRotation, maxCameraRotation);
        }

        // Make a quaternion representing **only that Y rotation**
        Quaternion yRotation = Quaternion.Euler(rotation.eulerAngles.x, clampedY, rotation.eulerAngles.z);

        // Combine with your existing rotation if needed
        playerCamera.transform.localRotation = yRotation; // multiplies quaternions correctly
    }

    private void UpdateRopePosition()
    {
        rope.SetPosition(0, new Vector3(playerCamera.transform.position.x, playerCamera.transform.position.y - 1, playerCamera.transform.position.z));
        rope.SetPosition(1, boatAudioSource.transform.position);
    }

    public float boatStereoPanMax = 0.6f;

    private void UpdateBoatSoundSource()
    {
        boatAudioSource.panStereo = (boatStereoPanMax/limit) * transform.position.x * -1;
    }
}