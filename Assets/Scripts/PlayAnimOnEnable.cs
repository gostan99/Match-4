using UnityEngine;

public class PlayAnimOnEnable : MonoBehaviour
{
    private Animator animator;

    private void OnEnable()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
        animator.Play("StartAnimation");
    }
}