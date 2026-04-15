using Unity.Netcode;
using UnityEngine;
using Unity.Cinemachine;

public class NetworkCameraSetup : NetworkBehaviour
{
	private CinemachineCamera vCam;

	void Start()
	{
		vCam = GetComponent<CinemachineCamera>();
	}

	void Update()
	{
		// We check 'Target.TrackingTarget' in the new API
		if (vCam != null && vCam.Target.TrackingTarget == null)
		{
			foreach (var player in GameObject.FindGameObjectsWithTag("Player"))
			{
				var networkObj = player.GetComponent<NetworkObject>();
				if (networkObj != null && networkObj.IsOwner)
				{
					vCam.Target.TrackingTarget = player.transform;
					vCam.Target.LookAtTarget = player.transform;
				}
			}
		}
	}
}
