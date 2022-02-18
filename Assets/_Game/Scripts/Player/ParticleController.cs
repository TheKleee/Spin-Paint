using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleController : MonoBehaviour
{
    PlayerController pCont;
    [SerializeField] int id;

    public ParticleSystem part;
    public List<ParticleCollisionEvent> collisionEvents = new List<ParticleCollisionEvent>();

    private void Start()
    {
        pCont = FindObjectOfType<PlayerController>();
    }

    private void OnParticleCollision(GameObject floor)
    {
        if (/*floor.CompareTag("Floor") &&*/ pCont.startPaint)
        {
            int numColEvents = GetComponent<ParticleSystem>().GetCollisionEvents(floor, collisionEvents);
            for (int i = 0; i < numColEvents; i++)
            {
                Vector3 pos = collisionEvents[i].intersection;
                if(pos != Vector3.zero)
                    pCont.CheckPaint(id, pos);
            }
        }
    }
}
