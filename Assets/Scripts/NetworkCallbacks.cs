﻿using System.Collections;
using UnityEngine;
using Bolt;
using Boo.Lang;
using UnityEditor;

[BoltGlobalBehaviour]
public class NetworkCallbacks : GlobalEventListener
{
    List<string> m_logMessages = new List<string>();

    int numPlayersConnected = 0;

    public override void SceneLoadLocalDone(string scene)
    {
        var spawnPos = new Vector3(Random.Range(-10f, 10f), 1.7f, -3f);

        if (BoltNetwork.IsServer)
            BoltNetwork.Instantiate(BoltPrefabs.Player1, spawnPos, Quaternion.identity);
        else
            BoltNetwork.Instantiate(BoltPrefabs.Player2, spawnPos, Quaternion.identity);

        //switch (numPlayersConnected)
        //{
        //    case 0:
        //        BoltNetwork.Instantiate(BoltPrefabs.Player1, spawnPos, Quaternion.identity);
        //        break;

        //    case 1:
        //        BoltNetwork.Instantiate(BoltPrefabs.Player2, spawnPos, Quaternion.identity);
        //        break;
        //}
        
        //numPlayersConnected++;
    }

    public override void OnEvent(LogEvent evt)
    {
        m_logMessages.Insert(0, evt.Message);
    }

    void OnGUI()
    {
        // only display max the 5 latest log messages
        int maxMessages = Mathf.Min(5, m_logMessages.Count);

        GUILayout.BeginArea(new Rect(Screen.width / 2 - 200, Screen.height - 100, 400, 100), GUI.skin.box);

        for (int i = 0; i < maxMessages; ++i)
        {
            GUILayout.Label(m_logMessages[i]);
        }

        GUILayout.EndArea();
    }
}