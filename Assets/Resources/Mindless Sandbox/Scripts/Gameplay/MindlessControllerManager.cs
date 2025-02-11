using UnityEngine;
using UnityEngine.InputSystem;

namespace MindlessSandbox
{
    public class MindlessControllerManager : MonoBehaviour
    {
        public Vector2 Move;
        public Vector2 Look;
        public bool Jump;
        public bool Crouch;

        public void MoveInput(InputAction.CallbackContext context)
        {
            Move = context.action.ReadValue<Vector2>();
        }

        public void LookInput(InputAction.CallbackContext context)
        {
            Look = context.action.ReadValue<Vector2>();
        }

        public void JumpInput(InputAction.CallbackContext context)
        {
            Jump = context.action.IsPressed();
        }

        public void CrouchInput(InputAction.CallbackContext context)
        {
            Crouch = context.action.IsPressed();
        }

    }
}