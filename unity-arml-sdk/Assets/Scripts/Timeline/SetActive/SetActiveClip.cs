using UnityEngine;
using UnityEngine.Playables;

/// <summary>
/// A PlayableAsset that creates a Playable for setting the active state of a GameObject.
/// </summary>
public class SetActiveClip : PlayableAsset
{
    /// <summary>
    /// The active state to set on the GameObject when the Playable is executed.
    /// </summary>
    public bool setActive;

    /// <summary>
    /// Creates a Playable based on this asset.
    /// </summary>
    /// <param name="graph">The PlayableGraph that will own the new Playable.</param>
    /// <param name="owner">The GameObject that is requesting the Playable. Not used in this implementation.</param>
    /// <returns>Returns a Playable linked to a SetActiveBehaviour with the specified active state.</returns>
    /// <remarks>
    /// This method creates a ScriptPlayable of type SetActiveBehaviour and sets its 'setActive' property
    /// to the value defined in this asset. The created Playable can be used to control the active state
    /// of a GameObject in a PlayableGraph.
    /// </remarks>
    public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
    {
        var playable = ScriptPlayable<SetActiveBehaviour>.Create(graph);

        var behaviour = playable.GetBehaviour();
        behaviour.setActive = setActive;

        return playable;
    }
}
