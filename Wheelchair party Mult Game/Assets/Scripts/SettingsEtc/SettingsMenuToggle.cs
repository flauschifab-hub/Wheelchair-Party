using UnityEngine;

public class SettingsMenuToggle : MonoBehaviour
{
    private GameObject settingsPanel;
    private bool isSettingsOpen = false;

    void Awake()
    {
        // Find the panel in the scene by name or tag
        settingsPanel = GameObject.Find("SettingsPanel"); // Replace with the actual name of your panel
        if (settingsPanel != null)
            settingsPanel.SetActive(false); // Ensure it starts hidden
        else
            Debug.LogWarning("SettingsPanel not found in the scene!");

        // Lock and hide the cursor initially
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Only allow local player to toggle if using Photon
        var pv = GetComponent<Photon.Pun.PhotonView>();
        if (pv != null && !pv.IsMine) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettingsMenu();
        }
    }

    private void ToggleSettingsMenu()
    {
        if (settingsPanel == null) return;

        isSettingsOpen = !isSettingsOpen;
        settingsPanel.SetActive(isSettingsOpen);

        Cursor.lockState = isSettingsOpen ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible = isSettingsOpen;
    }
}
