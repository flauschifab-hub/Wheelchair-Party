using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;

public class CrowBarPickup : MonoBehaviourPun
{
    [Header("Pickup Settings")]
    public float pickupRange = 3f;      // Max distance to pick up items
    public Sprite itemSprite;           // Drag & drop sprite for the hotbar icon

    [Header("Hotbar Settings")]
    public float scaleUp = 1.5f;        // Scale when selected
    public float scaleSpeed = 5f;       // Smooth scale speed

    private Camera playerCam;
    private Transform hotbar;
    private Image selectedItem;
    private Image previousItem;

    private Transform fpsItemPos;        // Parent of FPS items under camera
    private GameObject fpsCrowbar;      // The visual FPS crowbar
    private int equippedSlotIndex = -1; // Track currently equipped slot

    void Start()
    {
        if (!photonView.IsMine || tag != "Thief")
        {
            enabled = false;
            return;
        }

        playerCam = Camera.main;

        // Find FPSItemPos under the camera
        fpsItemPos = playerCam.transform.Find("FPSItemPos");
        if (fpsItemPos != null)
        {
            // Disable all children at start
            foreach (Transform child in fpsItemPos)
                child.gameObject.SetActive(false);

            // Get reference to the FPS Crowbar specifically
            fpsCrowbar = fpsItemPos.Find("Crowbar")?.gameObject;
        }
        else
        {
            Debug.LogWarning("FPSItemPos not found under the camera!");
        }

        // Find the ThiefHotbar
        GameObject hotbarGO = GameObject.Find("ThiefHotbar");
        if (hotbarGO != null)
            hotbar = hotbarGO.transform;
        else
            Debug.LogWarning("ThiefHotbar not found!");
    }

    void Update()
    {
        HandleHotbarInput();
        CheckForCrowBarPickup();
    }

    void CheckForCrowBarPickup()
    {
        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
            // Check by name instead of tag
            if (hit.collider.name == "Crowbar" && Input.GetKeyDown(KeyCode.E))
            {
                PickUpCrowBar(hit.collider.gameObject);
            }
        }
    }

    void PickUpCrowBar(GameObject worldCrowbar)
    {
        if (hotbar == null || itemSprite == null) return;

        // Find nearest free hotbar slot
        Transform freeSlot = null;
        float closestDistance = float.MaxValue;

        foreach (Transform slot in hotbar)
        {
            if (slot.childCount == 0)
            {
                float dist = Vector3.Distance(transform.position, slot.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    freeSlot = slot;
                }
            }
        }

        if (freeSlot == null)
        {
            Debug.Log("No free hotbar slots!");
            return;
        }

        // Create hotbar icon
        GameObject newImageGO = new GameObject("Crowbar_Icon");
        newImageGO.transform.SetParent(freeSlot, false);

        Image newImage = newImageGO.AddComponent<Image>();
        newImage.sprite = itemSprite;
        newImage.preserveAspect = true;

        RectTransform rt = newImageGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        // Disable the world crowbar
        worldCrowbar.SetActive(false);

        Debug.Log("Picked up Crowbar into " + freeSlot.name);
    }

    void HandleHotbarInput()
    {
        if (hotbar == null || fpsCrowbar == null) return;

        int slotIndex = -1;
        if (Input.GetKeyDown(KeyCode.Alpha1)) slotIndex = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) slotIndex = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) slotIndex = 2;

        if (slotIndex >= 0 && slotIndex < hotbar.childCount)
        {
            Transform slot = hotbar.GetChild(slotIndex);

            // If same slot is pressed, unequip
            if (slotIndex == equippedSlotIndex)
            {
                UnequipItem();
                return;
            }

            if (slot.childCount > 0)
            {
                previousItem = selectedItem;
                selectedItem = slot.GetChild(0).GetComponent<Image>();

                // Scale previous item back
                if (previousItem != null && previousItem != selectedItem)
                    StartCoroutine(ScaleTo(previousItem.transform, 1f));

                // Scale selected item up
                if (selectedItem != null)
                    StartCoroutine(ScaleTo(selectedItem.transform, scaleUp));

                // Equip the FPS Crowbar if this slot contains it
                if (slot.GetChild(0).name.Contains("Crowbar"))
                    fpsCrowbar.SetActive(true);
                else
                    fpsCrowbar.SetActive(false);

                equippedSlotIndex = slotIndex; // Update currently equipped slot
            }
        }
    }

    void UnequipItem()
    {
        // Hide FPS Crowbar
        if (fpsCrowbar != null)
            fpsCrowbar.SetActive(false);

        // Scale selected item back
        if (selectedItem != null)
            StartCoroutine(ScaleTo(selectedItem.transform, 1f));

        equippedSlotIndex = -1;
        selectedItem = null;
    }

    IEnumerator ScaleTo(Transform target, float targetScale)
    {
        Vector3 startScale = target.localScale;
        Vector3 endScale = Vector3.one * targetScale;
        float t = 0f;

        while (t < 1f)
        {
            t += Time.deltaTime * scaleSpeed;
            target.localScale = Vector3.Lerp(startScale, endScale, t);
            yield return null;
        }

        target.localScale = endScale;
    }
}
