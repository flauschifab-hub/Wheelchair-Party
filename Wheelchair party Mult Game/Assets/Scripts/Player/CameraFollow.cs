using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("References")]
    public Animator playerAnimator; // assign your player's Animator here

    [Header("Settings")]
    public float crouchOffsetY = -1f; // how much to lower on crouch
    public float moveSpeed = 5f;      // how fast the transition happens

    private float defaultY;
    private float crouchY;

    void Start()
    {
        // Store initial local Y position
        defaultY = transform.localPosition.y;
        crouchY = defaultY + crouchOffsetY;

        // Auto-find animator if not assigned
        if (playerAnimator == null)
        {
            playerAnimator = GetComponentInParent<Animator>();
            if (playerAnimator == null)
                Debug.LogWarning("CameraCrouchAdjust: No Animator assigned or found in parent!");
        }
    }

    void LateUpdate()
    {
        if (playerAnimator == null) return;

        bool isCrouching = playerAnimator.GetBool("isCrouching");

        // Get current local position
        Vector3 localPos = transform.localPosition;

        // Smoothly move only Y
        float targetY = isCrouching ? crouchY : defaultY;
        localPos.y = Mathf.Lerp(localPos.y, targetY, Time.deltaTime * moveSpeed);

        transform.localPosition = localPos;
    }
}
