using System;
using System.Collections.Generic;
using UnityEngine;

public enum CreditSectionType
{
    Title,
    HeaderAndNames,
    Attribution
}

[Serializable]
public struct CreditEntry
{
    public string primaryText;
    public string secondaryText;
    public string sourceText;
}

[Serializable]
public struct CreditSection
{
    public CreditSectionType sectionType;
    public string sectionTitle;
    public List<CreditEntry> entries;
}

[CreateAssetMenu(fileName = "GameCreditsData", menuName = "Credits/Credit Data")]
public class CreditData : ScriptableObject
{
    public string gameTitle;
    public List<CreditSection> sections = new List<CreditSection>();

    [Header("End Display Options")]
    public string closingMessage = "Thank You For Playing!";
    public bool showCopyright = true;
    public string copyrightText = "© 2026 NorthStar Studios. All Rights Reserved.";
}