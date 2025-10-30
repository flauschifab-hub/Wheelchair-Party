using UnityEngine;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections;

public class RockPickup : MonoBehaviourPun
{
    [Header("Pickup Settings")]
    public float pickupRange = 3f;
    public Sprite itemSprite;
    public float throwForce = 25f;
    public float throwUpwardForce = 2f;

    [Header("Hotbar Settings")]
    public float scaleUp = 1.5f;
    public float scaleSpeed = 5f;

    private Transform hotbar;
    private Image selectedItem;
    private Image previousItem;
    private Transform fpsItemPos;
    private GameObject fpsRock;
    private int equippedSlotIndex = -1;
    private GameObject rockPrefab;
    private GameObject lastLookedAtRock;
    private bool readyToThrow = true;

    // References we'll find automatically
    private Camera playerCam;
    private Transform attackPoint;

    void Start()
    {
        if (!photonView.IsMine || tag != "Thief")
        {
            enabled = false;
            return;
        }

        // Find camera automatically
        playerCam = Camera.main;
        if (playerCam == null)
        {
            playerCam = GameObject.FindObjectOfType<Camera>();
            Debug.Log("🔍 Found camera via FindObjectOfType");
        }

        if (playerCam == null)
        {
            Debug.LogError("❌ No camera found!");
            return;
        }

        // Find AttackPoint automatically
        GameObject attackPointObj = GameObject.Find("AttackPoint");
        if (attackPointObj != null)
        {
            attackPoint = attackPointObj.transform;
            Debug.Log("✅ Found AttackPoint: " + attackPoint.name);
        }
        else
        {
            // Create one if it doesn't exist
            Debug.Log("⚠️ AttackPoint not found, creating one...");
            attackPointObj = new GameObject("AttackPoint");
            attackPointObj.transform.SetParent(playerCam.transform);
            attackPointObj.transform.localPosition = new Vector3(0, 0, 0.5f);
            attackPoint = attackPointObj.transform;
        }

        // Find FPSItemPos
        fpsItemPos = playerCam.transform.Find("FPSItemPos");
        if (fpsItemPos != null)
        {
            foreach (Transform child in fpsItemPos)
                child.gameObject.SetActive(false);

            fpsRock = fpsItemPos.Find("Rock")?.gameObject;
        }

        // Find hotbar
        GameObject hotbarGO = GameObject.Find("ThiefHotbar");
        if (hotbarGO != null)
        {
            hotbar = hotbarGO.transform;
            Debug.Log("✅ Found ThiefHotbar");
        }
        else
        {
            Debug.LogError("❌ ThiefHotbar not found!");
        }

        // Load rock prefab
        rockPrefab = Resources.Load<GameObject>("RockPrefab");
        if (rockPrefab == null)
            Debug.LogError("❌ RockPrefab not found in Resources!");
        else
            Debug.Log("✅ RockPrefab loaded: " + rockPrefab.name);

        Debug.Log("🎯 RockPickup initialized - Camera: " + playerCam.name + ", AttackPoint: " + attackPoint.name);
    }

    void Update()
    {
        HandleHotbarInput();
        CheckForRockLookAndPickup();
        HandleThrow();
    }

    void CheckForRockLookAndPickup()
    {
        if (playerCam == null) return;

        Ray ray = new Ray(playerCam.transform.position, playerCam.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, pickupRange))
        {
            if (hit.collider.CompareTag("Rock"))
            {
                if (lastLookedAtRock != hit.collider.gameObject)
                {
                    lastLookedAtRock = hit.collider.gameObject;
                }

                if (Input.GetKeyDown(KeyCode.E))
                {
                    PickUpRock(hit.collider.gameObject);
                }
            }
            else if (lastLookedAtRock != null)
            {
                lastLookedAtRock = null;
            }
        }
        else if (lastLookedAtRock != null)
        {
            lastLookedAtRock = null;
        }
    }

    void PickUpRock(GameObject worldRock)
    {
        if (hotbar == null || itemSprite == null) return;

        Transform freeSlot = null;
        foreach (Transform slot in hotbar)
        {
            if (slot.childCount == 0)
            {
                freeSlot = slot;
                break;
            }
        }

        if (freeSlot == null)
        {
            Debug.Log("No free hotbar slots!");
            return;
        }

        GameObject newImageGO = new GameObject("Rock_Icon");
        newImageGO.transform.SetParent(freeSlot, false);

        Image newImage = newImageGO.AddComponent<Image>();
        newImage.sprite = itemSprite;
        newImage.preserveAspect = true;

        RectTransform rt = newImageGO.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        worldRock.SetActive(false);
        Debug.Log("✅ Rock picked up and added to hotbar");
    }

    void HandleHotbarInput()
    {
        if (hotbar == null || fpsRock == null) return;

        int slotIndex = -1;
        if (Input.GetKeyDown(KeyCode.Alpha1)) slotIndex = 0;
        else if (Input.GetKeyDown(KeyCode.Alpha2)) slotIndex = 1;
        else if (Input.GetKeyDown(KeyCode.Alpha3)) slotIndex = 2;

        if (slotIndex >= 0 && slotIndex < hotbar.childCount)
        {
            Transform slot = hotbar.GetChild(slotIndex);

            if (slotIndex == equippedSlotIndex)
            {
                UnequipItem();
                return;
            }

            if (slot.childCount > 0)
            {
                previousItem = selectedItem;
                selectedItem = slot.GetChild(0).GetComponent<Image>();

                if (previousItem != null && previousItem != selectedItem)
                    StartCoroutine(ScaleTo(previousItem.transform, 1f));

                if (selectedItem != null)
                    StartCoroutine(ScaleTo(selectedItem.transform, scaleUp));

                if (slot.GetChild(0).name.Contains("Rock"))
                    fpsRock.SetActive(true);
                else
                    fpsRock.SetActive(false);

                equippedSlotIndex = slotIndex;
                Debug.Log("🔫 Equipped rock from slot " + (slotIndex + 1));
            }
        }
    }

    void HandleThrow()
    {
        if (fpsRock == null || !fpsRock.activeSelf) return;
        if (!Input.GetMouseButtonDown(0)) return;
        if (!readyToThrow) return;
        if (playerCam == null || attackPoint == null) return;

        if (rockPrefab == null)
        {
            Debug.LogError("❌ Cannot throw: rockPrefab is missing!");
            return;
        }

        Throw();
    }

    private void Throw()
    {
        readyToThrow = false;

        // Instantiate object to throw at attack point position with camera rotation
        Vector3 spawnPosition = attackPoint.position;
        Quaternion spawnRotation = playerCam.transform.rotation;

        GameObject projectile;
        
        if (PhotonNetwork.InRoom)
        {
            projectile = PhotonNetwork.Instantiate(rockPrefab.name, spawnPosition, spawnRotation);
            Debug.Log("🌐 Threw rock via Photon");
        }
        else
        {
            projectile = Instantiate(rockPrefab, spawnPosition, spawnRotation);
            Debug.Log("💻 Threw rock locally");
        }

        // Get rigidbody component
        Rigidbody projectileRb = projectile.GetComponent<Rigidbody>();

        if (projectileRb == null)
        {
            Debug.LogError("❌ No Rigidbody found on rock prefab!");
            return;
        }

        // Calculate direction using raycast (same as working script)
        Vector3 forceDirection = playerCam.transform.forward;

        RaycastHit hit;

        // Use raycast to get precise direction toward where player is aiming
        if (Physics.Raycast(playerCam.transform.position, playerCam.transform.forward, out hit, 500f))
        {
            forceDirection = (hit.point - spawnPosition).normalized;
            Debug.Log("🎯 Aiming at: " + hit.point);
        }

        // Add force using the EXACT same calculation as working script
        Vector3 forceToAdd = forceDirection * throwForce + transform.up * throwUpwardForce;

        projectileRb.AddForce(forceToAdd, ForceMode.Impulse);

        Debug.Log($"💨 Threw rock! Force: {forceToAdd.magnitude}, Direction: {forceDirection}");

        // Clean up hotbar
        CleanUpAfterThrow();

        // Reset throw cooldown
        Invoke(nameof(ResetThrow), 0.1f);
    }

    private void ResetThrow()
    {
        readyToThrow = true;
    }

    void CleanUpAfterThrow()
    {
        if (selectedItem != null)
        {
            Destroy(selectedItem.gameObject);
            Debug.Log("🗑️ Removed rock from hotbar");
        }

        if (fpsRock != null)
            fpsRock.SetActive(false);

        equippedSlotIndex = -1;
        selectedItem = null;
    }

    void UnequipItem()
    {
        if (fpsRock != null)
            fpsRock.SetActive(false);

        if (selectedItem != null)
            StartCoroutine(ScaleTo(selectedItem.transform, 1f));

        equippedSlotIndex = -1;
        selectedItem = null;
        Debug.Log("👋 Unequipped rock");
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