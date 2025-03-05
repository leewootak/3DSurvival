using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Resource : MonoBehaviour
{
    public ItemData itemToGive;
    public int quantityHit = 1;
    public int capacity;

    public void Gather(Vector3 hitPoint, Vector3 hitNormal)
    {
        for (int i = 0; i < quantityHit; i++)
        {
            if (capacity <= 0) break;
            capacity -= 1;
            Instantiate(itemToGive.dropPrefab, hitPoint + Vector3.up, Quaternion.LookRotation(hitNormal, Vector3.up));
        }
    }
}
