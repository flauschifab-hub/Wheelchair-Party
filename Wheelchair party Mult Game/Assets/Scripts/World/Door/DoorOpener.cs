using UnityEngine;
using Photon.Pun;

public class DoorOpener : MonoBehaviourPun
{
    [Header("Interaction Settings")]
    public float interactRange = 3f;     
    public KeyCode interactKey = KeyCode.E;

    private Camera playerCam;

    void Start()
    {
        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }

        playerCam = Camera.main;
    }

    void Update()
    {
        if (Input.GetKeyDown(interactKey))
        {
            TryInteractWithDoor();
        }
    }

    void TryInteractWithDoor()
    {
        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            Debug.Log($"Raycast hit: {hit.collider.name}");

            GameObject target = hit.collider.gameObject;

            if (target.CompareTag("Door"))
            {
                Debug.Log("Hit object is tagged Door.");

                // Try to find Animator or DoorController anywhere on the object or its parents
                Animator anim = target.GetComponentInParent<Animator>();
                DoorController door = target.GetComponentInParent<DoorController>();

                if (door != null)
                {
                    Debug.Log("Found DoorController — unlocking door.");
                    door.UnlockAndOpen();
                }
                else if (anim != null)
                {
                    Debug.Log("Found Animator, triggering Opening.");
                    anim.SetTrigger("Opening");
                }
                else
                {
                    Debug.LogWarning("No DoorController or Animator found on door or parent!");
                }
            }
        }
    }
}
