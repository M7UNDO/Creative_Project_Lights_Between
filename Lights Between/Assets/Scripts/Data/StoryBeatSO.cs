using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Beat_", menuName = "The Lights Between/Story/Story Beat")]
public class StoryBeatSO : ScriptableObject
{
    [SerializeField] private string beatID;

    [Header("Objectives to Activate")]
    [SerializeField] private List<ObjectiveSO> activeObjectives = new List<ObjectiveSO>();

    [Header("On Completion")]
    [SerializeField] private GameEventSO completionStoryEvent;
    [SerializeField] private StoryBeatSO nextBeat;

    public string BeatID => beatID;
    public IReadOnlyList<ObjectiveSO> ActiveObjectives => activeObjectives;
    public GameEventSO CompletionStoryEvent => completionStoryEvent;
    public StoryBeatSO NextBeat => nextBeat;
}