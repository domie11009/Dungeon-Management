﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CursePanel : MonoBehaviour {
    
    [SerializeField]
    private TextMeshProUGUI curseCountTextMesh;
    [SerializeField]
    private TextMeshProUGUI costTextMesh;
    [SerializeField]
    private float percentageMult = 1;
    [SerializeField]
    private int upgradeCost
    {
        get
        {
            float value = upgradeCost_initial;
            for (int i = 0; i < GameManager.Instance.playerData.curseUpgradeCount; i++)
            {
                value = value * (1 + (upgradeCost_upRateByCount / 100));
            }
            return (int)value;
        }
    }
    [SerializeField]
    private float upgradeCost_initial;
    [SerializeField]
    private float upgradeCost_upRateByCount;
    [Space(4)]
    [SerializeField]
    private float applyRate_upRateByCount;
    [SerializeField]
    private float speedDecRate_upRateByCount;
    [SerializeField]
    private float damageMultRate_upRateByCount;

    [Space(8)]
    [SerializeField]
    private GameObject slotPrefab;
    [SerializeField]
    private GameObject contentObj;


    //===
    public void Intialize()
    {
        Load();
        DataHandler.Instance.dataUpdated += UpdateList;
        UpdateList();
    }

    public void Save()
    {
        PlayerPrefs.SetFloat("CurseApplyRate", Curse.ApplyRateEnhancedRate);
        PlayerPrefs.SetFloat("SpeedDecRate", Curse.SpeedSetEnhancedRate);
        PlayerPrefs.SetFloat("DamageMultRate", Curse.DamageSetEnhancedRate);
    }
    void Load()
    {
        if (GameManager.Instance.playerData.isNewBegin)
        {
            Curse.ApplyRateEnhancedRate = 1f;
            Curse.SpeedSetEnhancedRate = 1f;
            Curse.DamageSetEnhancedRate = 1f;
        }
        else
        {
            Curse.ApplyRateEnhancedRate = PlayerPrefs.GetFloat("CurseApplyRate");
            Curse.SpeedSetEnhancedRate = PlayerPrefs.GetFloat("SpeedDecRate");
            Curse.DamageSetEnhancedRate = PlayerPrefs.GetFloat("DamageMultRate");
        }
    }

    //===
    void UpdateList()
    {
        List<Curse> curseList = DataHandler.Instance.playerCurses;
        if (curseList.Count < contentObj.transform.childCount)
        {
            for (int i = curseList.Count; i < contentObj.transform.childCount; i++)
            {
                contentObj.transform.GetChild(i).gameObject.SetActive(false);
            }
        }
        else if (curseList.Count > contentObj.transform.childCount)
        {
            for (int i = contentObj.transform.childCount; i < curseList.Count; i++)
            {
                GameObject slotObj = Instantiate(slotPrefab).gameObject;
                slotObj.transform.SetParent(contentObj.transform);
            }
        }

        for (int i = 0; i < curseList.Count; i++)
        {
            GameObject slotObj = contentObj.transform.GetChild(i).gameObject;
            slotObj.SetActive(true);
            slotObj.transform.Find("icon").GetComponent<Image>().sprite
                = curseList[i].form.sprite;
            int applyRate = (int)curseList[i].CurseApplyRate;
            int speedDecRate = (int)curseList[i].SpeedSetRate;
            int damageMultRate = (int)curseList[i].damageSetRate;
            slotObj.transform.Find("contentTextMesh").GetComponent<TextMeshProUGUI>().text
                = curseList[i].form.name.Substring(4) + "\n<size=10%>\n<size=85%>hit rate : "
                + applyRate.ToString() + "%";
            if(damageMultRate > 0)
            {
                slotObj.transform.Find("contentTextMesh").GetComponent<TextMeshProUGUI>().text
                    += "\ndamage rate : " + damageMultRate.ToString() + "%";
            }
            if (speedDecRate > 0)
            {
                slotObj.transform.Find("contentTextMesh").GetComponent<TextMeshProUGUI>().text
                    += "\nslow rate : " + (100 - speedDecRate).ToString() + "%";
            }
            TextMeshProUGUI gradeTextMesh = slotObj.transform.Find("gradeTextMesh").GetComponent<TextMeshProUGUI>();
            gradeTextMesh.text = curseList[i].form.grade.ToString();
            if (curseList[i].form.grade == Grade.X) { gradeTextMesh.text = "?"; }
            gradeTextMesh.color = DataHandler.Instance.GetGradeColor(curseList[i].form.grade);
        }

        curseCountTextMesh.text = "Possessing\nTotems\n<size=140%>\n" + curseList.Count.ToString();
        costTextMesh.text = "Upgrade\n<sprite=3> " + upgradeCost.ToString();
    }

    //===
    public void OnClick_Upgrade()
    {
        if(DataHandler.Instance.playerCurses.Count <= 0)
        {
            EffectHandler.Instance.SystemMessage("No totems to upgrade");
        }
        else if (GameManager.Instance.playerData.gold < upgradeCost)
        {
            EffectHandler.Instance.SystemMessage("Not enough gold");
        }
        else
        {
            GameManager.Instance.playerData.gold -= upgradeCost;
            GameManager.Instance.playerData.curseUpgradeCount++;

            Curse.ApplyRateEnhancedRate += applyRate_upRateByCount / 100;
            Curse.SpeedSetEnhancedRate -= speedDecRate_upRateByCount / 100;
            Curse.DamageSetEnhancedRate += damageMultRate_upRateByCount / 100;

            for (int i = 0; i < DataHandler.Instance.playerCurses.Count; i++)
            {
                GameObject slotObj = contentObj.transform.GetChild(i).gameObject;
                Animator anim = slotObj.GetComponent<Animator>();
                anim.Play("expand");

                Curse curse = DataHandler.Instance.playerCurses[i];
                float per = Random.Range(0, 100);
                float percent = DataHandler.Instance.GetGradeUpPercentage(curse.form.grade);
                percent = percent + (percent * (DataHandler.rankUpExtraRate / 100)) * percentageMult;
                if (per <= 50 + percent / 2 &&
                    per >= 50 - percent / 2)
                {
                    Grade targetGrade = (Grade)((int)curse.form.grade + 1);
                    List<CurseForm> supForms = DataHandler.Instance.GetCurseFormsAtGrade(targetGrade);
                    int index = Random.Range(0, supForms.Count);
                    curse.ChangeForm(supForms[index]);
                }
                curse.ReStat();
            }
            AudioHandler.Instance.PlaySFX(AudioHandler.Instance.sfxFolder[0]);
            GameManager.Instance.SaveGame();
            UpdateList();
        }
    }
}
