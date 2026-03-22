using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelAutoLose : LevelCondition
{
    private BoardController m_boardController;

    public override void Setup(BoardController board)
    {

        m_boardController = board;

        m_boardController.OnMoveEvent += OnMove;
    }

    private void OnMove()
    {
        if (m_conditionCompleted) return;
        if (m_boardController.GetBoard().IsBoardEmpty())
        {
            OnConditionComplete();
        }
    }

    protected override void OnDestroy()
    {
        if (m_boardController != null) m_boardController.OnMoveEvent -= OnMove;

        base.OnDestroy();
    }
}
