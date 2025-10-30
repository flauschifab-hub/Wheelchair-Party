using UnityEngine;
using Photon.Pun;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PhotonView))]
public class ThifMovement : MonoBehaviourPunCallbacks, IPunObservable
{
    [Header("Movement Settings")]
    public float moveSpeed = 5f;
    public float crouchSpeed = 2f;
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    public float animationSmooth = 15f;
    public float jumpCooldown = 0.5f;

    [Header("Crouch Settings")]
    public KeyCode crouchKey = KeyCode.LeftControl;
    public float crouchHeight = 1f;
    public float standingHeight = 2f;
    public float crouchTransitionSpeed = 8f;

    [Header("References")]
    public Animator animator;
    public GameObject playerModel;
    public Transform cameraPivot;
    public Camera playerCamera;

    [Header("Mouse Settings")]
    public float mouseSensitivity = 120f;
    public float minPitch = -75f;
    public float maxPitch = 85f;
    public bool lockCursor = true;

    private CharacterController controller;
    private Vector3 velocity;
    private bool isGrounded;
    private bool isCrouching;
    private float lastJumpTime;
    private float xRotation = 0f;

    private float animX;
    private float animY;

    private float inputX;
    private float inputY;
    private bool jumpInput;
    private bool crouchHeld;

    // Remote sync vars
    private Vector3 remotePosition;
    private Quaternion remoteRotation;
    private Quaternion remoteModelRotation;

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
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            ProcessInputs();
            HandleCrouch();
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
        crouchHeld = Input.GetKey(crouchKey);
    }

    void HandleMovement()
    {
        isGrounded = controller.isGrounded;

        if (isGrounded && velocity.y < 0f)
            velocity.y = -2f;

        // Movement vector
        Vector3 moveDir = transform.forward * inputY + transform.right * inputX;
        if (moveDir.magnitude > 1f) moveDir.Normalize();

        // Determine speed based on crouch state
        float currentSpeed = isCrouching ? crouchSpeed : moveSpeed;
        controller.Move(moveDir * currentSpeed * Time.deltaTime);

        // Jump only when standing and grounded
        if (jumpInput && isGrounded && !isCrouching && Time.time - lastJumpTime >= jumpCooldown)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastJumpTime = Time.time;
            animator?.SetTrigger("Jump");
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);

        // Local input for anims
        Vector3 localInput = transform.InverseTransformDirection(moveDir);
        animX = Mathf.Lerp(animX, localInput.x, Time.deltaTime * animationSmooth);
        animY = Mathf.Lerp(animY, localInput.z, Time.deltaTime * animationSmooth);
    }

    void HandleCrouch()
    {
        bool isMoving = Mathf.Abs(inputX) > 0.1f || Mathf.Abs(inputY) > 0.1f;

        // You can only start crouching if idle and grounded
        if (!isCrouching && crouchHeld && !isMoving && isGrounded)
        {
            isCrouching = true;
        }

        // If crouch key released → stand up
        if (isCrouching && !crouchHeld)
        {
            isCrouching = false;
        }

        // Smooth controller height
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        Vector3 center = controller.center;
        center.y = controller.height / 2f;
        controller.center = center;

        // Smooth crouch blend (for transitions)
        float currentBlend = animator.GetFloat("CrouchBlend");
        float targetBlend = isCrouching ? 1f : 0f;
        float newBlend = Mathf.Lerp(currentBlend, targetBlend, Time.deltaTime * 8f);
        animator.SetFloat("CrouchBlend", newBlend);

        animator.SetBool("isCrouching", isCrouching);
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
        transform.position = Vector3.Lerp(transform.position, remotePosition, Time.deltaTime * 10f);
        transform.rotation = Quaternion.Slerp(transform.rotation, remoteRotation, Time.deltaTime * 10f);

        if (playerModel != null)
            playerModel.transform.rotation = Quaternion.Slerp(playerModel.transform.rotation, remoteModelRotation, Time.deltaTime * 10f);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
            if (playerModel != null)
                stream.SendNext(playerModel.transform.rotation);
            stream.SendNext(animX);
            stream.SendNext(animY);
            stream.SendNext(isGrounded);
            stream.SendNext(isCrouching);
        }
        else
        {
            remotePosition = (Vector3)stream.ReceiveNext();
            remoteRotation = (Quaternion)stream.ReceiveNext();
            remoteModelRotation = (Quaternion)stream.ReceiveNext();
            animX = (float)stream.ReceiveNext();
            animY = (float)stream.ReceiveNext();
            isGrounded = (bool)stream.ReceiveNext();
            isCrouching = (bool)stream.ReceiveNext();
            UpdateAnimator();
        }
    }
}
