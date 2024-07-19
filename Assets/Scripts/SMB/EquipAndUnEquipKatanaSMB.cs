using UnityEngine;

public class EquipAndUnEquipKatanaSMB : StateMachineBehaviour
{
    public bool Equip;
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        MyPlayerController controller = animator.GetComponent<MyPlayerController>();
        if (Equip)
            controller.OnEquipKatana();
        else
            controller.OnUnarmKatana();
    }
}
