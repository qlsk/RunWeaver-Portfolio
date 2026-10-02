using System;
using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class SkillDefinitionLoader : MonoBehaviour
{
    private const string SkillIconLoadPath = "";

    [Header("CSV URL")]
    [SerializeField] private string _skillCsvUrl =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vR9F5MSLHEOJg7A8euPIs8WimRo-nZ9S7YwK5yHixCZMNlnPSUysFEyxwX9ZUw92XTNw127RO24LYVm/pub?gid=453944318&single=true&output=csv";
    [SerializeField] private string _skillNodeCsvUrl =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vR9F5MSLHEOJg7A8euPIs8WimRo-nZ9S7YwK5yHixCZMNlnPSUysFEyxwX9ZUw92XTNw127RO24LYVm/pub?gid=1195616475&single=true&output=csv";
    [SerializeField] private string _skillNodeParameterCsvUrl =
        "https://docs.google.com/spreadsheets/d/e/2PACX-1vR9F5MSLHEOJg7A8euPIs8WimRo-nZ9S7YwK5yHixCZMNlnPSUysFEyxwX9ZUw92XTNw127RO24LYVm/pub?gid=945734435&single=true&output=csv";

    public async UniTask<IReadOnlyList<SkillDefinition>> LoadSkillDefinitions()
    {
        string csv = await CsvLoader.DownloadCsv(_skillCsvUrl);
        if (string.IsNullOrWhiteSpace(csv)) throw new InvalidDataException("Skill CSV is empty.");
        IReadOnlyList<SkillCsvHeader> parsedRows = CsvLoader.ParseCsv<SkillCsvHeader>(csv);
        return CreateSkillList(parsedRows);
    }

    public async UniTask<IReadOnlyList<SkillNodeDefinition>> LoadSkillNodeDefinitions()
    {
        string csv = await CsvLoader.DownloadCsv(_skillNodeCsvUrl);
        if (string.IsNullOrWhiteSpace(csv)) throw new InvalidDataException("SkillNode CSV is empty.");
        IReadOnlyList<SkillNodeCsvHeader> parsedRows = CsvLoader.ParseCsv<SkillNodeCsvHeader>(csv);
        return CreateSkillNodeList(parsedRows);
    }

    public async UniTask<IReadOnlyList<SkillNodeParameterDefinition>> LoadSkillNodeParameterDefinitions()
    {
        string csv = await CsvLoader.DownloadCsv(_skillNodeParameterCsvUrl);
        if (string.IsNullOrWhiteSpace(csv)) throw new InvalidDataException("SkillNodeParameter CSV is empty.");
        IReadOnlyList<SkillNodeParameterCsvHeader> parsedRows = CsvLoader.ParseCsv<SkillNodeParameterCsvHeader>(csv);
        return CreateSkillNodeParameterList(parsedRows);
    }

    private IReadOnlyList<SkillDefinition> CreateSkillList(IReadOnlyList<SkillCsvHeader> rows)
    {
        List<SkillDefinition> skills = new();
        foreach (SkillCsvHeader row in rows)
        {
            string path = SkillIconLoadPath + row.IconName;
            Sprite icon = Resources.Load<Sprite>(path);
            // TODO: 스킬 아이콘 추가
            // if (icon == null) throw new InvalidOperationException($"{row.Id} 스킬 아이콘이 없습니다.");
            skills.Add(new SkillDefinition(row.Id, row.DisplayName, row.Description, icon));
        }

        Debug.Log($"Loaded {skills.Count} Skill definitions.");
        return skills;
    }

    private IReadOnlyList<SkillNodeDefinition> CreateSkillNodeList(IReadOnlyList<SkillNodeCsvHeader> rows)
    {
        List<SkillNodeDefinition> skillNodes = new();
        foreach (SkillNodeCsvHeader row in rows)
        {
            skillNodes.Add(new SkillNodeDefinition(row.Id, row.DisplayName, row.Description, row.SkillId,
                row.PrerequisiteNodeId, row.MaxLevel, row.UpgradeCost));
        }

        Debug.Log($"Loaded {skillNodes.Count} SkillNode definitions.");
        return skillNodes;
    }

    private IReadOnlyList<SkillNodeParameterDefinition> CreateSkillNodeParameterList(
        IReadOnlyList<SkillNodeParameterCsvHeader> rows)
    {
        List<SkillNodeParameterDefinition> skillNodeParameters = new();
        foreach (SkillNodeParameterCsvHeader row in rows)
        {
            if (!Enum.TryParse(row.ParameterType, out ParameterType parameterType))
            {
                throw new InvalidDataException($"Invalid skill parameter type: {row.ParameterType}");
            }
            skillNodeParameters.Add(new SkillNodeParameterDefinition(row.NodeId, row.Level, parameterType, row.Value));
        }

        Debug.Log($"Loaded {skillNodeParameters.Count} SkillNodeParameter definitions.");
        return skillNodeParameters;
    }
}
