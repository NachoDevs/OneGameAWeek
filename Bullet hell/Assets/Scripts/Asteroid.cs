﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Asteroid : MonoBehaviour
{
    public bool small = false;

    public float asteroidSpeed = 10;
    public float rotateSpeed = 25;

    public GameObject ShieldPoweUp;

    GameController m_gc;

    GameObject m_player;

    PolygonCollider2D m_collider;

    // Start is called before the first frame update
    void Start()
    {
        m_gc = GameObject.FindGameObjectWithTag("GameController").GetComponent<GameController>();
        m_collider = GetComponentInChildren<PolygonCollider2D>();
        m_collider.enabled = false;

        if (!small)
        {
            GetComponentInChildren<Rigidbody2D>().AddForce((GameObject.FindGameObjectsWithTag("Player")[0].transform.position - transform.position) * asteroidSpeed);
        }

        if (m_player == null)
        {
            m_player = GameObject.FindGameObjectWithTag("Player");
        }

        StartCoroutine(ActivateHitbox());
    }

    // Update is called once per frame
    void Update()
    {
        if(Vector3.Distance(m_player.transform.position, transform.position) > 100)
        {
            Destroy(gameObject);
        }

        transform.Rotate(Vector3.forward * rotateSpeed * Time.deltaTime, Space.Self);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        //GameObject collidedGO = collision.transform.parent.gameObject;
        if (collision.gameObject.GetComponentInParent<Shield>() != null)
        {
            return;
        }

        if (!small)
        {
            int asteroidNum = Random.Range(5, 9);
            for (int i = 0; i < asteroidNum; ++i)
            {
                m_gc.Shake(.15f, .2f);

                Vector2 v2 = Quaternion.AngleAxis((360 / asteroidNum) * i, Vector3.forward) * Vector2.up;

                GameObject asteroid = Instantiate(m_gc.asteroids[Random.Range(0, m_gc.asteroids.Count)], transform.position, transform.rotation);
                asteroid.GetComponent<Asteroid>().small = true;
                asteroid.transform.localScale *= .5f;

                if(Random.Range(0, 25) >= 24)
                {
                    Instantiate(ShieldPoweUp, transform.position, Quaternion.identity);
                }

                asteroid.GetComponent<Rigidbody2D>().AddForce(v2 * 100);
            }
        }

        Destroy(gameObject);
        
    }

    IEnumerator ActivateHitbox()
    {
        yield return new WaitForSeconds(1);
        m_collider.enabled = true;
    }
}
