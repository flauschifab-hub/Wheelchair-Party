using UnityEngine;

public class DoorController : MonoBehaviour
{
    private Animator anim;
    private bool isUnlocked = false;
    private bool isOpen = false;

    void Start()
    {
        anim = GetComponent<Animator>();
        if (anim == null)
            Debug.LogWarning($"{name} has no Animator!");
    }

    public void UnlockAndOpen()
    {
        if (!isUnlocked)
        {
            isUnlocked = true;
            PlayOpenAnimation();
        }
    }

    void Update()
    {
        // Allow toggling door after unlocked
        if (isUnlocked && Input.GetKeyDown(KeyCode.E))
        {
            Ray ray = new Ray(Camera.main.transform.position, Camera.main.transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, 3f))
            {
                if (hit.collider.gameObject == gameObject)
                {
                    ToggleDoor();
                }
            }
        }
    }

    void ToggleDoor()
    {
        if (isOpen)
            PlayCloseAnimation();
        else
            PlayOpenAnimation();
    }

    void PlayOpenAnimation()
    {
        if (anim != null)
            anim.SetTrigger("Opening");
        isOpen = true;
    }

    void PlayCloseAnimation()
    {
        if (anim != null)
            anim.SetTrigger("Closing");
        isOpen = false;
    }
}
