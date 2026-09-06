using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class NarrativePlayer : MonoBehaviour
{
    public Image panel;
    public Sprite[] panels;
    public string nextScene = "Poker";

    private int index = 0;

    private void Start()
    {
        if (panels.Length > 0) {
            panel.sprite = panels[0];
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
        {
            Next();
        }
    }

    public void Next()
    {
        index++;
        if (index >= panels.Length) {
            SceneManager.LoadScene(nextScene);
        }
        else {
            panel.sprite = panels[index];
        }
    }
}