using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillDataLoader : MonoBehaviour
{
    private const string SkillIconLoadPath = "";

    [Header("CSV URL")]
    [SerializeField] private string _skillCsvUrl =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vR9F5MSLHEOJg7A8euPIs8WimRo-nZ9S7YwK5yHixCZMNlnPSUysFEyxwX9ZUw92XTNw127RO24LYVm/pub?gid=453944318&single=true&output=csv";
    [SerializeField] private string _skillNodeCsvUrl =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vR9F5MSLHEOJg7A8euPIs8WimRo-nZ9S7YwK5yHixCZMNlnPSUysFEyxwX9ZUw92XTNw127RO24LYVm/pub?gid=1195616475&single=true&output=csv";
    [SerializeField] private string _skillNodeParameterCsvUrl =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vR9F5MSLHEOJg7A8euPIs8WimRo-nZ9S7YwK5yHixCZMNlnPSUysFEyxwX9ZUw92XTNw127RO24LYVm/pub?gid=945734435&single=true&output=csv";

    [Header("References")]
    [SerializeField] private SkillDatabase _skillDatabase;

    private string _skillCsv;
    private string _skillNodeCsv;
    private string _skillNodeParameterCsv;

    private void Start()
    {
        StartCoroutine(LoadAllSkillData());
    }

    private IEnumerator LoadAllSkillData()
    {
        string skillCsv = null;
        string nodeCsv = null;
        string parameterCsv = null;

        yield return CsvLoader.DownloadCsv(_skillCsvUrl, csv => skillCsv = csv);
        if (string.IsNullOrWhiteSpace(skillCsv))
        {
            Debug.LogError("스킬 CSV 다운로드에 실패했거나 내용이 비어 있습니다.");
            yield break;
        }

        yield return CsvLoader.DownloadCsv(_skillNodeCsvUrl, csv => nodeCsv = csv);
        if (string.IsNullOrWhiteSpace(nodeCsv))
        {
            Debug.LogError("스킬 노드 CSV 다운로드에 실패했거나 내용이 비어 있습니다.");
            yield break;
        }

        yield return CsvLoader.DownloadCsv(_skillNodeParameterCsvUrl, csv => parameterCsv = csv);
        if (string.IsNullOrWhiteSpace(parameterCsv))
        {
            Debug.LogError("노드 파라미터 CSV 다운로드에 실패했거나 내용이 비어 있습니다.");
            yield break;
        }

        if (!CsvLoader.TryParseCsv(skillCsv, out IReadOnlyList<SkillDataCsvHeader> skillRows)) yield break;
        if (!CsvLoader.TryParseCsv(nodeCsv, out IReadOnlyList<SkillNodeDataCsvHeader> nodeRows)) yield break;
        if (!CsvLoader.TryParseCsv(parameterCsv, out IReadOnlyList<SkillNodeParameterDataCsvHeader> parameterRows))
        {
            yield break;
        }

        if (!TryCreateSkillList(skillRows, out var skills)) yield break;
        if (!TryCreateSkillNodeList(nodeRows, out var nodes)) yield break;
        if (!TryCreateSkillNodeParameterList(parameterRows, out var parameters)) yield break;

        _skillDatabase.Initialize(skills, nodes, parameters);
    }

    private bool TryCreateSkillList(IReadOnlyList<SkillDataCsvHeader> rows, out IReadOnlyList<SkillData> skillsData)
    {
        skillsData = new List<SkillData>();
        List<SkillData> skills = new();
        foreach (SkillDataCsvHeader row in rows)
        {
            string path = SkillIconLoadPath + row.IconName;
            Sprite icon = Resources.Load<Sprite>(path);
            // TODO: 스킬 아이콘 추가
            // if (icon == null) throw new InvalidOperationException($"{row.Id} 스킬 아이콘이 없습니다.");
            skills.Add(new SkillData(row.Id, row.DisplayName, row.Description, icon));
        }

        skillsData = skills;
        Debug.Log($"{skillsData.Count} skills have been loaded.");
        return true;
    }

    private bool TryCreateSkillNodeList(IReadOnlyList<SkillNodeDataCsvHeader> rows,
        out IReadOnlyList<SkillNodeDefinition> skillNodesData)
    {
        skillNodesData = new List<SkillNodeDefinition>();
        List<SkillNodeDefinition> skillNodes = new();
        foreach (SkillNodeDataCsvHeader row in rows)
        {
            skillNodes.Add(new SkillNodeDefinition(row.Id, row.DisplayName, row.Description, row.SkillId,
                row.PrerequisiteNodeId, row.MaxLevel, row.UpgradeCost));
        }

        skillNodesData = skillNodes;
        Debug.Log($"{skillNodesData.Count} skill nodes have been loaded.");
        return true;
    }

    private bool TryCreateSkillNodeParameterList(IReadOnlyList<SkillNodeParameterDataCsvHeader> rows,
        out IReadOnlyList<SkillNodeParameterData> skillNodeParametersData)
    {
        skillNodeParametersData = new List<SkillNodeParameterData>();
        List<SkillNodeParameterData> skillNodeParameters = new();
        foreach (SkillNodeParameterDataCsvHeader row in rows)
        {
            if (!Enum.TryParse<ParameterType>(row.ParameterType, out ParameterType parameterType))
            {
                Debug.LogError($"{row.ParameterType} is not a valid parameter type.");
                continue;
            }
            skillNodeParameters.Add(new SkillNodeParameterData(row.NodeId, row.Level, parameterType, row.Value));
        }

        skillNodeParametersData = skillNodeParameters;
        Debug.Log($"{skillNodeParametersData.Count} skill node parameters have been loaded.");
        return true;
    }
}
