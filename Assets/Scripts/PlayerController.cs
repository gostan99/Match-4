using UnityEngine;

public class PlayerController : MonoBehaviour
{
    // Start is called before the first frame update
    private void Start()
    {
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Abs(Camera.main.transform.position.z) + 1.0f, LayerMask.GetMask("Game Tile")))
            {
                hit.transform.gameObject.GetComponent<GameTile>().OnSelect();
            }
        }
    }
}