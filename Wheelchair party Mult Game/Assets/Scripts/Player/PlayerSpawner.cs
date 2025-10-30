using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon; // For Hashtable

public class PlayerSpawner : MonoBehaviourPunCallbacks
{
    [Header("Player Prefabs (must be in Resources)")]
    public string policePrefabName = "PolicePrefab";
    public string thiefPrefabName = "ThiefPrefab";
    public string civiPrefabName = "CiviPrefab";

    [Header("Optional Spawn Points")]
    public Transform[] spawnPoints;

    private void Start()
    {
        if (PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InRoom)
            SpawnPlayer();
    }

    public override void OnJoinedRoom()
    {
        if (!PhotonNetwork.AutomaticallySyncScene)
            SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        if (PhotonNetwork.LocalPlayer.TagObject != null)
            return;

        // Determine spawn position
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;

        if (spawnPoints != null && spawnPoints.Length > 0)
        {
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];
            spawnPos = point.position;
            spawnRot = point.rotation;
        }

        // Get role from custom property
        object roleObj;
        string role = "Civi"; // default
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Role", out roleObj))
        {
            role = (string)roleObj;
        }

        string prefabName = role switch
        {
            "Police" => policePrefabName,
            "Thief" => thiefPrefabName,
            "Civi" => civiPrefabName,
            _ => civiPrefabName
        };

        GameObject player = PhotonNetwork.Instantiate(prefabName, spawnPos, spawnRot);
        PhotonNetwork.LocalPlayer.TagObject = player;

        Debug.Log($"[Photon] Spawned {role} prefab '{prefabName}' at {spawnPos}");
    }
}
