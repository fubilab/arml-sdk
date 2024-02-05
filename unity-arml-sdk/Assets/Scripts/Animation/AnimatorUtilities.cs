using UnityEngine;

/// <summary>
/// Provides utility functions for manipulating Animator parameters.
/// This class requires a GameObject with an Animator component attached.
/// </summary>
[RequireComponent(typeof(Animator))]
public class AnimatorUtilities : MonoBehaviour
{
    private Animator animator;

    /// <summary>
    /// Called before the first frame update. Initializes the Animator component.
    /// </summary>
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Increases the value of an integer parameter in the Animator by a specified amount.
    /// </summary>
    /// <param name="parameterName">The name of the integer parameter in the Animator.</param>
    /// <param name="increaseAmount">The amount by which to increase the parameter's value.</param>
    public void IncreaseIntParameterByAmount(string parameterName, int increaseAmount)
    {
        int newValue = animator.GetInteger(parameterName) + increaseAmount;
        animator.SetInteger(parameterName, newValue);
    }

    /// <summary>
    /// Increases the value of a float parameter in the Animator by a specified amount.
    /// </summary>
    /// <param name="parameterName">The name of the float parameter in the Animator.</param>
    /// <param name="increaseAmount">The amount by which to increase the parameter's value.</param>
    public void IncreaseFloatParameterByAmount(string parameterName, float increaseAmount)
    {
        float newValue = animator.GetFloat(parameterName) + increaseAmount;
        animator.SetFloat(parameterName, newValue);
    }
}
