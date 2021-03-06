﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class test : MonoBehaviour {

    public GameObject mainParent, playParent;
    //public InputField playerCountInput;
    private int playersToCreate;
    // Use this for initialization
    void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}
    public void closeGame()
    {
        Application.Quit();
    }
    public void main()
    {
        //others set false
        //main set true
        playParent.SetActive(false);
        mainParent.SetActive(true);
    }

    public void play()
    {
        mainParent.SetActive(false);
        playParent.SetActive(true);
        SceneManager.LoadScene("CosDri_4");
    }

    public void launchGame(string input)
    {
        //playersToCreate = int.Parse(playerCountInput.text);
        if (playersToCreate > 4) playersToCreate = 4;
        
        Debug.Log(playersToCreate);
        SceneManager.LoadScene("CosDri_4");
        Invoke("delayedExecution", 2);
        //SceneManager.LoadScene("MainMenu");
    }

    public void delayedExecution()
    {
        GameManager gm = FindObjectOfType<GameManager>();
        if (gm) gm.playersToCreate = 1;// playersToCreate;
    }
}
