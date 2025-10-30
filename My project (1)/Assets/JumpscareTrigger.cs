using UnityEngine;

public class JumpscareTrigger : MonoBehaviour
{
    public GameObject jumpscarePanel; // asignas el panel desde el inspector
    public float scareDuration = 2f; // segundos que dura el jumpscare

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) // si el jugador entra
        {
            StartCoroutine(ShowJumpscare());
        }
    }

    private System.Collections.IEnumerator ShowJumpscare()
    {
        jumpscarePanel.SetActive(true);
        yield return new WaitForSeconds(scareDuration);
        jumpscarePanel.SetActive(false);
    }
}

