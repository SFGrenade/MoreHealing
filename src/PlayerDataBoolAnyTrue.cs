using HutongGames.PlayMaker;
using UnityEngine;

namespace MoreHealing;

public class PlayerDataBoolAnyTrue : FsmStateAction
{
    public FsmOwnerDefault gameObject;
    public FsmString[] boolNames;
    public FsmEvent isTrue;
    public FsmEvent isFalse;

    public override void Reset()
    {
        gameObject = null;
        boolNames = [];
        isTrue = null;
        isFalse = null;
    }

    public override void OnEnter()
    {
        GameObject ownerDefaultTarget = base.Fsm.GetOwnerDefaultTarget(this.gameObject);
        if (ownerDefaultTarget == null)
        {
            return;
        }
        GameManager component = ownerDefaultTarget.GetComponent<GameManager>();
        if (component == null)
        {
            return;
        }

        foreach (FsmString boolName in boolNames)
        {
            if (component.GetPlayerDataBool(boolName.Value))
            {
                Fsm.Event(isTrue);
            }
        }
        Fsm.Event(isFalse);
        Finish();
    }
}