using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PhotonView))]
public class MultiplayerCharacterController : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float animationSmooth = 15f;
    public float jumpCooldown = 0.5f;

    [Header("References")]
    public Animator animator;
    public GameObject playerModel;   // Character mesh
    public Transform cameraPivot;    // Camera pivot (up/down)
    public Camera playerCamera;

    [Header("Mouse Settings")]
    public float mouseSensitivity = 120f;
    public float minPitch = -75f;
    public float maxPitch = 85f;
    public bool lockCursor = true;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private float lastJumpTime;

    private float animX;
    private float animY;

    private float inputX;
    private float inputY;
    private bool jumpInput;
    private float xRotation = 0f;

    // Remote sync vars
    private Vector3 remotePosition;
    private Quaternion remoteRotation;
    private Quaternion remoteModelRotation;
    private float remoteVelocityY;

    void Awake()
    {
        controller = GetComponent<CharacterController>();

        if (photonView.IsMine)
        {
            if (playerModel != null) playerModel.SetActive(false);
        }
        else
        {
            if (playerCamera != null) playerCamera.enabled = false;
        }
    }

    void Start()
    {
        if (photonView.IsMine && lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Initialize remote position to current transform
        remotePosition = transform.position;
        remoteRotation = transform.rotation;
        remoteModelRotation = playerModel != null ? playerModel.transform.rotation : Quaternion.identity;
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            ProcessInputs();
            HandleMovement();
            UpdateAnimator();
        }
        else
        {
            SmoothRemoteMovement();
        }
    }

    void LateUpdate()
    {
        if (photonView.IsMine)
            HandleLook();
    }

    void ProcessInputs()
    {
        inputX = Input.GetAxisRaw("Horizontal");
        inputY = Input.GetAxisRaw("Vertical");
        jumpInput = Input.GetButtonDown("Jump");
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        Vector3 moveDir = transform.forward * inputY + transform.right * inputX;
        if (moveDir.magnitude > 1f) moveDir.Normalize();

        controller.Move(moveDir * moveSpeed * Time.deltaTime);

        // Jump with cooldown
        if (jumpInput && isGrounded && Time.time - lastJumpTime >= jumpCooldown)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastJumpTime = Time.time;
            animator?.SetTrigger("Jump");
        }

        // Gravity
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Animator: convert moveDir to local space
        Vector3 localInput = transform.InverseTransformDirection(moveDir);
        animX = Mathf.Lerp(animX, localInput.x, Time.deltaTime * animationSmooth);
        animY = Mathf.Lerp(animY, localInput.z, Time.deltaTime * animationSmooth);
    }

    void HandleLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        transform.Rotate(Vector3.up * mouseX);

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minPitch, maxPitch);
        if (cameraPivot != null)
            cameraPivot.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    void UpdateAnimator()
    {
        if (animator == null) return;
        animator.SetFloat("Horizontal", animX);
        animator.SetFloat("Vertical", animY);
        animator.SetBool("isGrounded", isGrounded);
    }

    void SmoothRemoteMovement()
    {
        // Interpolation factor based on network update rate
        float distance = Vector3.Distance(transform.position, remotePosition);
        float moveSpeed = distance / PhotonNetwork.SerializationRate;

        // Position
        Vector3 targetPos = remotePosition + Vector3.up * remoteVelocityY * Time.deltaTime;
        transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);

        // Rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, remoteRotation, 10f * Time.deltaTime);

        // Model rotation
        if (playerModel != null)
            playerModel.transform.rotation = Quaternion.Slerp(playerModel.transform.rotation, remoteModelRotation, 10f * Time.deltaTime);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            stream.SendNext(playerModel != null ? playerModel.transform.rotation : Quaternion.identity);
            stream.SendNext(animX);
            stream.SendNext(animY);
            stream.SendNext(isGrounded);
            stream.SendNext(velocity.y); // sync vertical velocity
        }
        else
        {
            remotePosition = (Vector3)stream.ReceiveNext();
            remoteRotation = (Quaternion)stream.ReceiveNext();
            remoteModelRotation = (Quaternion)stream.ReceiveNext();
            animX = (float)stream.ReceiveNext();
            animY = (float)stream.ReceiveNext();
            isGrounded = (bool)stream.ReceiveNext();
            remoteVelocityY = (float)stream.ReceiveNext();
            UpdateAnimator();
        }
    }
}
