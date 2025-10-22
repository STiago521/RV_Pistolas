using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Pistola : MonoBehaviour
{
    public GameObject ShootFx, HitFx;
    public Transform firePoint;
    public LineRenderer line;
    public int damage = 25;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        XRGrabInteractable grabInteract = GetComponent<XRGrabInteractable>();
        grabInteract.activated.AddListener(x => Disparando());
    }


    public void Disparando()
    {
        StartCoroutine(Disparo());

    }

    IEnumerator Disparo()
    {
        RaycastHit hit;
        bool hitInfo = Physics.Raycast(firePoint.position, firePoint.forward, out hit, 50f);

        Instantiate(ShootFx, firePoint.position, Quaternion.identity);

        if (hitInfo)
        {
            line.SetPosition(0, firePoint.position);
            line.SetPosition(1, hit.point);
        }
        else
        {
            line.SetPosition(0, firePoint.position);
            line.SetPosition(1, firePoint.position + firePoint.forward * 20);
        }
        line.enabled = true;
        yield return new WaitForSeconds(0.02f);
        line.enabled = false;

    }
    // Update is called once per frame
    void Update()
    {
        
    }
}
