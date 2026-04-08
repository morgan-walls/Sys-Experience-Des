using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    float screenWidth = Screen.width;
    public float maxCameraRotation = 45.0f;
    private float position;
    private float cameraZRotation;
    private float velocity;
    [SerializeField] private float rotationSpeed = 0.5f;

    [SerializeField] GameObject LeftLimit;
    [SerializeField] GameObject RightLimit;

    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] Image CursorImage;

    [SerializeField] private AudioSource boatAudioSource;

    private float limit;
    private float edgeZone;
    public float edgeZoneOffset; //multiplier how far away from the edge the resistance zone is
    public float edgeResistance = 5f;  // how strong the slowdown is
    public float maxSpeed = 5f;


    private void Start()
    {
        Cursor.visible = false;

        limit = RightLimit.transform.position.x;
        edgeZone = limit - (limit * edgeZoneOffset);
    }

    private void Update()
    {
        RotateCamera();
        MovePlayer();
        UpdateBoatSoundSource();
    }

    private void RotateCamera()
    {
        float mouseXPosition = Input.mousePosition.x;

        CursorImage.transform.position = new Vector3(Input.mousePosition.x, CursorImage.transform.position.y, CursorImage.transform.position.z);

        mouseXPosition = Mathf.Clamp(mouseXPosition, 0, screenWidth);

        float normalized = mouseXPosition / screenWidth;   // 0 - 1
        normalized -= 0.5f; normalized *= 2.0f; //set range between -1 and 1

        cameraZRotation = Mathf.Lerp(cameraZRotation, normalized * maxCameraRotation, rotationSpeed); //set it between -45 and 45

        transform.rotation = Quaternion.Euler(0.0f, 0.0f, cameraZRotation * -1);
    }

    private void MovePlayer()
    {
        position = transform.position.x;

        float rotationNormal = cameraZRotation / maxCameraRotation;

        velocity = rotationNormal * Time.deltaTime + velocity;

        // --- EDGE CALC --
        float distFromCenter = Mathf.Abs(position);
        float distToEdge = limit - distFromCenter;

        // 0 (safe) → 1 (at edge)
        float edgeFactor = Mathf.Clamp01(1f - (distToEdge / edgeZone));

        // Which side player is closer to
        float directionToEdge = Mathf.Sign(position);

        // moving toward that edge?
        bool movingTowardEdge = Mathf.Sign(velocity) == directionToEdge;

        // --- APPLY RESISTANCE ---
        if (movingTowardEdge)
        {
            float resistance = edgeFactor * edgeResistance;
            velocity -= resistance * velocity * Time.deltaTime;
        }

        // --- CLAMP SPEED ---
        velocity = Mathf.Clamp(velocity, -maxSpeed, maxSpeed);

        // --- APPLY MOVEMENT ---
        position += velocity * Time.deltaTime;

        // --- HARD LIMIT (safety clamp) ---
        if (position > limit)
        {
            position = limit;
            if (velocity > 0) velocity = 0;
        }
        else if (position < -limit)
        {
            position = -limit;
            if (velocity < 0) velocity = 0;
        }

        float x = transform.position.x;
        x += velocity;
        transform.position = new Vector3(x, transform.position.y, transform.position.z);

        //infoText.text = x.ToString();
    }

    public float boatStereoPanMax = 0.6f;

    private void UpdateBoatSoundSource()
    {
        boatAudioSource.panStereo = (boatStereoPanMax/limit) * transform.position.x * -1;

        infoText.text = boatAudioSource.panStereo.ToString();
    }
}