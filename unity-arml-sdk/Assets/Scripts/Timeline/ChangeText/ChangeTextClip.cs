using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// A custom PlayableAsset used to create playable clips for changing text in Unity's Timeline.
/// </summary>
public class ChangeTextClip : PlayableAsset
{
    public string textToChangeTo;
    public bool setEmptyAtTheEnd = true;

    /// <summary>
    /// Creates a playable for the text change behavior.
    /// </summary>
    /// <param name="graph">The PlayableGraph that the playable will be added to.</param>
    /// <param name="owner">The GameObject that is requesting the playable.</param>
    /// <returns>A new playable with the assigned ChangeTextBehaviour.</returns>
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<ChangeTextBehaviour>.Create(graph);

        var behaviour = playable.GetBehaviour();
        behaviour.textToChangeTo = textToChangeTo;
        behaviour.setEmptyAtTheEnd = setEmptyAtTheEnd;

        return playable;
    }
}
