﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public bool canInput = true;

    public float walkingSpeed = 5.0f;
    public float runningSpeed = 10.0f;
    public float gravityScale = 5.0f;
    public float movementSmoothTime = .1f;
    public float speedSmoothTime = .1f;
    public float turnSmoothTime = .1f;

    private bool m_isRunning;

    private float m_currSpeed;
    private float m_speedSmoothVelocity;
    private float m_turnSmoothVelocity;
    private float m_targetRotation;
    private float m_targetSpeed;

    private Vector3 m_input;
    private Vector3 m_inputDir;
    private Vector3 m_moveDirection;

    private Animator m_animController;
    private Transform m_cameraTransform;
    private Rigidbody m_rigidbody;

    // Start is called before the first frame update
    void Start()
    {
        m_animController = GetComponent<Animator>();
        m_cameraTransform = GetComponentInChildren<CameraController>().transform;
        //m_cController = GetComponent<CharacterController>();
        m_rigidbody = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if(canInput)
        {
            m_input = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
            m_targetRotation = /*Mathf.Atan2(m_inputDir.x, m_inputDir.y) * Mathf.Rad2Deg +*/ m_cameraTransform.eulerAngles.y;
            transform.eulerAngles = Vector3.up * Mathf.SmoothDampAngle(transform.eulerAngles.y, m_targetRotation, ref m_turnSmoothVelocity, turnSmoothTime);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (canInput)
        {
            m_inputDir = m_input.normalized;

            m_isRunning = Input.GetKey(KeyCode.LeftShift);
            m_targetSpeed = ((m_isRunning) ? runningSpeed : walkingSpeed) * m_inputDir.magnitude;
            m_currSpeed = Mathf.SmoothDamp(m_currSpeed, m_targetSpeed, ref m_speedSmoothVelocity, speedSmoothTime);

            m_rigidbody.AddRelativeForce(m_inputDir * m_targetSpeed * Time.deltaTime * 50);

            m_animController.SetFloat("playerSpeed", m_input.magnitude);
        }
        else
        {
            m_animController.SetFloat("playerSpeed", 0);
        }
    }
}
