using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelAutoLose : LevelCondition
{
    private string m_setupString;
    public override void Setup(string setupString, Text txt, BoardController board)
    {
        base.Setup(setupString, txt);

        m_setupString = setupString;

        UpdateText();
    }
    protected override void UpdateText()
    {
        m_txt.text = string.Format(m_setupString);
    }
}
