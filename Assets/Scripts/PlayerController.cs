using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class PlayerController : MonoBehaviourPunCallbacks
{
    [Header("Camera Move")]
    [SerializeField] private Transform viewPoint;
    [SerializeField] [Range(0, 1)] private float mouseSensitivity = 1f;
    private float verticalRotationStore;
    private Vector2 mouseInput;
    [SerializeField] private bool invertLook;
    private Camera mainCamera;

    [Header("Player Move")]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float runSpeed;
    private float activeMoveSpeed;
    private Vector3 moveDirection;
    private Vector3 movement;
    [SerializeField] private float jumpForce;
    [SerializeField] private float gravityModifier;
    [SerializeField] private CharacterController characterController;

    [Header("Ground")]
    [SerializeField] private Transform groundCheckPoint;
    private bool isGrounded;
    [SerializeField] private LayerMask groundLayers;

    [Header("Gun")]
    [SerializeField] private GameObject bulletImpact;
    private float shotCounter;
    [SerializeField] private float muzzleDisplayTime;
    private float muzzleCounter;

    [SerializeField] private float maxHeat;
    [SerializeField] private float coolRate;
    [SerializeField] private float overheatCoolRate;
    private float heatCounter;
    private bool overHeated;

    [SerializeField] private Gun[] allGuns;
    private int selectedGun;

    public GameObject playerHitImpact;

    public int maxHealth = 100;
    private int currentHealth;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        mainCamera = Camera.main;

        UIController.instance.weaponTempSlider.maxValue = maxHeat;

        SwitchGun();

        currentHealth = maxHealth;

        UIController.instance.healthSlider.maxValue = maxHealth;
        UIController.instance.healthSlider.value = currentHealth;
    }

    private void Update()
    {
        if (photonView.IsMine)
        {
            // MOVEMENT

            mouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")) * mouseSensitivity;

            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.y + mouseInput.x, transform.rotation.eulerAngles.z);

            verticalRotationStore += mouseInput.y;
            verticalRotationStore = Mathf.Clamp(verticalRotationStore, -60f, 60f);

            if (invertLook)
            {
                viewPoint.rotation = Quaternion.Euler(verticalRotationStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
            }
            else
            {
                viewPoint.rotation = Quaternion.Euler(-verticalRotationStore, viewPoint.rotation.eulerAngles.y, viewPoint.rotation.eulerAngles.z);
            }

            moveDirection = new Vector3(Input.GetAxisRaw("Horizontal"), 0f, Input.GetAxisRaw("Vertical"));

            if (Input.GetKey(KeyCode.LeftShift))
            {
                activeMoveSpeed = runSpeed;
            }
            else
            {
                activeMoveSpeed = moveSpeed;
            }

            float yVelocity = movement.y;
            movement = ((transform.forward * moveDirection.z) + (transform.right * moveDirection.x)).normalized * activeMoveSpeed;
            movement.y = yVelocity;

            if (characterController.isGrounded)
            {
                movement.y = 0f;
            }

            isGrounded = Physics.Raycast(groundCheckPoint.position, Vector3.down, .25f, groundLayers);

            if (Input.GetButtonDown("Jump") && isGrounded)
            {
                movement.y = jumpForce;
            }

            movement.y += Physics.gravity.y * Time.deltaTime * gravityModifier;

            characterController.Move(movement * Time.deltaTime);

            // SHOOTING

            if (allGuns[selectedGun].muzzleFlash.activeInHierarchy)
            {
                muzzleCounter -= Time.deltaTime;

                if (muzzleCounter <= 0)
                {
                    allGuns[selectedGun].muzzleFlash.SetActive(false);
                }
            }

            if (!overHeated)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Shoot();
                }

                if (Input.GetMouseButton(0) && allGuns[selectedGun].isAutomatic)
                {
                    shotCounter -= Time.deltaTime;

                    if (shotCounter <= 0)
                    {
                        Shoot();
                    }
                }

                heatCounter -= coolRate * Time.deltaTime;

            }
            else
            {
                heatCounter -= overheatCoolRate * Time.deltaTime;
                if (heatCounter <= 0)
                {
                    overHeated = false;

                    UIController.instance.overheatedMessage.gameObject.SetActive(false);
                }
            }

            if (heatCounter < 0)
            {
                heatCounter = 0f;
            }
            UIController.instance.weaponTempSlider.value = heatCounter;

            if (Input.GetAxisRaw("Mouse ScrollWheel") > 0f)
            {
                selectedGun++;

                if (selectedGun >= allGuns.Length)
                {
                    selectedGun = 0;
                }
                SwitchGun();
            }
            else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0f)
            {
                selectedGun--;

                if (selectedGun < 0)
                {
                    selectedGun = allGuns.Length - 1;
                }
                SwitchGun();
            }

            for (int i = 0; i < allGuns.Length; i++)
            {
                if (Input.GetKeyDown((i + 1).ToString()))
                {
                    selectedGun = i;
                    SwitchGun();
                }
            }

            // CURSOR SETTINGS

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Cursor.lockState = CursorLockMode.None;
            }
            else if (Cursor.lockState == CursorLockMode.None)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    Cursor.lockState = CursorLockMode.Locked;
                }
            }
        }
    }

    private void Shoot()
    {
        Ray ray = mainCamera.ViewportPointToRay(new Vector3(.5f, .5f, 0f));
        ray.origin = mainCamera.transform.position;

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.gameObject.tag == "Player")
            {
                Debug.Log($"Hit {hit.collider.gameObject.GetPhotonView().Owner.NickName}");
                PhotonNetwork.Instantiate(playerHitImpact.name, hit.point, Quaternion.identity);

                hit.collider.gameObject.GetPhotonView().RPC("DealDamage", RpcTarget.All, photonView.Owner.NickName, allGuns[selectedGun].shotDamage);
            }
            else
            {
                GameObject bulletImpactObject = Instantiate(bulletImpact, hit.point + (hit.normal * 0.002f), Quaternion.LookRotation(hit.normal, Vector3.up));
                Destroy(bulletImpactObject, 10f);
            }
        }

        shotCounter = allGuns[selectedGun].timeBetweenShots;

        heatCounter += allGuns[selectedGun].heatPerShot;
        if (heatCounter >= maxHeat)
        {
            heatCounter = maxHeat;

            overHeated = true;

            UIController.instance.overheatedMessage.gameObject.SetActive(true);
        }

        allGuns[selectedGun].muzzleFlash.SetActive(true);
        muzzleCounter = muzzleDisplayTime;
    }

    [PunRPC]
    public void DealDamage(string damager, int damageAmount)
    {
        TakeDamage(damager, damageAmount);
    }
    public void TakeDamage(string damager, int damageAmount)
    {
        if (photonView.IsMine)
        {
            currentHealth -= damageAmount;

            if (currentHealth <= 0)
            {
                currentHealth = 0;
                PlayerSpawner.instance.Die(damager);
            }
        }

        UIController.instance.healthSlider.value = currentHealth;
    }
    private void LateUpdate()
    {
        if (photonView.IsMine)
        {
            mainCamera.transform.position = viewPoint.position;
            mainCamera.transform.rotation = viewPoint.rotation;
        }
    }
    private void SwitchGun()
    {
        foreach (Gun gun in allGuns)
        {
            gun.gameObject.SetActive(false);
        }

        allGuns[selectedGun].gameObject.SetActive(true);
        allGuns[selectedGun].muzzleFlash.SetActive(false);
    }
}
