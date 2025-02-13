using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ItemHolder : NetworkBehaviour
{
    public int selectedWeapon = 0;
    public InputActionProperty switchInput;
    public Transform weaponsPivot;
    public float delay = 0.1f;

    public List<EquipableItemScriptableObject> items;

    bool canSwitch;

    // Start is called before the first frame update
    IEnumerator Start()
    {
        if (weaponsPivot == null)
        {
            weaponsPivot = GetComponent<Transform>();
        }
        yield return new WaitForSeconds(delay);
        if (IsOwner) SelectWeaponServerRpc();
    }

    // Update is called once per frame
    void Update()
    {
        if (!IsOwner || Cursor.lockState != CursorLockMode.Locked) return;

        float switchValue = switchInput.action.ReadValue<float>();
        if (switchValue >= 0.5f && switchInput.action.WasPerformedThisFrame())
        {
            if (!canSwitch) return;
            if (selectedWeapon >= weaponsPivot.childCount - 1)
                selectedWeapon = 0;
            else
                selectedWeapon++;
            SelectWeaponServerRpc();
        }
        if (switchValue <= -0.5f && switchInput.action.WasPerformedThisFrame())
        {
            if (!canSwitch) return;
            if (selectedWeapon <= 0)
                selectedWeapon = weaponsPivot.childCount - 1;
            else
                selectedWeapon--;
            SelectWeaponServerRpc();
        }


        #region Number Keys
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            if (!canSwitch) return;
            selectedWeapon = 0;
            SelectWeaponServerRpc();
        }
        if (Keyboard.current.digit2Key.wasPressedThisFrame && weaponsPivot.childCount >= 2)
        {
            if (!canSwitch) return;
            selectedWeapon = 1;
            SelectWeaponServerRpc();
        }
        if (Keyboard.current.digit3Key.wasPressedThisFrame && weaponsPivot.childCount >= 3)
        {
            if (!canSwitch) return;
            selectedWeapon = 2;
            SelectWeaponServerRpc();
        }
        if (Keyboard.current.digit4Key.wasPressedThisFrame && weaponsPivot.childCount >= 4)
        {
            if (!canSwitch) return;
            selectedWeapon = 3;
            SelectWeaponServerRpc();
        }
        if (Keyboard.current.digit5Key.wasPressedThisFrame && weaponsPivot.childCount >= 5)
        {
            if (!canSwitch) return;
            selectedWeapon = 4;
            SelectWeaponServerRpc();
        }
        if (Keyboard.current.digit6Key.wasPressedThisFrame && weaponsPivot.childCount >= 6)
        {
            if (!canSwitch) return;
            selectedWeapon = 5;
            SelectWeaponServerRpc();
        }
        if (Keyboard.current.digit7Key.wasPressedThisFrame && weaponsPivot.childCount >= 7)
        {
            if (!canSwitch) return;
            selectedWeapon = 6;
            SelectWeaponServerRpc();
        }
        if (Keyboard.current.digit8Key.wasPressedThisFrame && weaponsPivot.childCount >= 8)
        {
            if (!canSwitch) return;
            selectedWeapon = 7;
            SelectWeaponServerRpc();
        }
        if (Keyboard.current.digit9Key.wasPressedThisFrame && weaponsPivot.childCount >= 9)
        {
            if (!canSwitch) return;
            selectedWeapon = 8;
            SelectWeaponServerRpc();
        }
        if (Keyboard.current.digit0Key.wasPressedThisFrame && weaponsPivot.childCount >= 10)
        {
            if (!canSwitch) return;
            selectedWeapon = 9;
            SelectWeaponServerRpc();
        }
        #endregion
    }

    [ServerRpc]
    void SelectWeaponServerRpc()
    {
        canSwitch = false;
        Invoke(nameof(ResetCooldown), delay);
        
        if (weaponsPivot.GetChild(0) != null) Destroy(weaponsPivot.GetChild(0));

        if (IsOwner) Instantiate(items[selectedWeapon].itemObject, weaponsPivot);
    }

    void ResetCooldown()
    {
        canSwitch = true;
    }
}