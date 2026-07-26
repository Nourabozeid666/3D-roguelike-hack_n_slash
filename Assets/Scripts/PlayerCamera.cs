    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class PlayerCamera : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] float sensitivity = 1f;
        private int maxAngle = 85;
        private int minAngle = -25;
        // [SerializeField] private Vector3 offset;
        private InputSystem controls;

        void Awake()
        {
            controls = new InputSystem();
        }
        void OnEnable()
        {
            controls.PlayerMovement.Enable();
        }

        void OnDisable()
        {
            controls.PlayerMovement.Disable();
        }
        void Start()
        {
            // transform.position = player.position + offset;
            controls.PlayerMovement.Camera.performed += ctx =>
            {
                Vector2 value = ctx.ReadValue<Vector2>();
                value.y = -value.y;
                // Debug.Log(value.x + " , " + value.y);
                Vector3 dir = (transform.position - player.position).normalized;
                float currentPitch = Mathf.Asin(dir.y) * Mathf.Rad2Deg;
                value.y = Mathf.Clamp(value.y, minAngle - currentPitch, maxAngle - currentPitch);
                transform.RotateAround(player.position, Vector3.up, value.x * sensitivity);
                transform.RotateAround(player.position, transform.right, value.y * sensitivity);

            };
        }

        // Update is called once per frame
        void Update()
        {
            // transform.position = player.position + new Vector3(0, offset.y, 0);
        }
    }
