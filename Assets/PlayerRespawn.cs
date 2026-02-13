using UnityEngine;

public class PlayerRespawn : MonoBehaviour
{
    public Transform respawnPoint;
    public int health = 100;

    public void TakeDamage(int damage)
    {
        health -= damage;
        Debug.Log("Player Health: " + health);

        Respawn();
    }

    void Respawn()
    {
        GetComponent<CharacterController>().enabled = false;
        transform.position = respawnPoint.position;
        GetComponent<CharacterController>().enabled = true;
    }
}
