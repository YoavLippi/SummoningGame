using UnityEngine;
using Unity.Netcode;

public class RoleManager : NetworkBehaviour
{
	public GameObject librarianUI;
	public GameObject apprenticeUI;

	public override void OnNetworkSpawn()
	{
		// Disable both initially to avoid flickering
		librarianUI.SetActive(false);
		apprenticeUI.SetActive(false);

		if (IsServer) // The Host/Librarian
		{
			librarianUI.SetActive(true);
		}
		else // The Client/Apprentice
		{
			apprenticeUI.SetActive(true);
		}
	}
}
