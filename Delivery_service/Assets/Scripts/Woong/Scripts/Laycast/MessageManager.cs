using System.Collections.Generic;
using UnityEngine;

public class MessageManager : MonoBehaviour
{
    public static MessageManager Instance;

    [Header("¿¢¼¿ÆÄÀÏ")]
    public TextAsset messageCSV;
    private Dictionary<int, string> messageDictionary = new Dictionary<int, string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            LoadMessages();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadMessages()
    {
        if (messageCSV == null) return;

        string[] lines = messageCSV.text.Split('\n');

        for (int i = 1; i < lines.Length; i++)
        {
            string line = lines[i].Trim();
            if (string.IsNullOrEmpty(line)) continue;
            string[] parts = line.Split(',');
            if (parts.Length >= 2)
            {
                if (int.TryParse(parts[0], out int id))
                {
                    string text = line.Substring(line.IndexOf(',') + 1).Trim();
                    messageDictionary[id] = text;
                }
            }
        }
    }

    public string GetMessage(int id)
    {
        if (messageDictionary.TryGetValue(id, out string msg))
        {
            return msg;
        }
        return "¹øÈ£´Â 1ºÎÅÍ¿¡¿ä 0¾ÈµÅ";
    }
}