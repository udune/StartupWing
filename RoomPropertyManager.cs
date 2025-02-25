using System.Collections;
using System.Collections.Generic;
using System;
using Photon.Pun;
using Hashtable =  ExitGames.Client.Photon.Hashtable;
public class RoomPropertyManager 
{
    public void SetProperty(string key, object value)
    {
        Hashtable data = PhotonNetwork.CurrentRoom.CustomProperties;
       
        if (data == null)
        {
            data = new Hashtable();
        }
       
        if (data.ContainsKey(key))
        {
            data[key] = value;
        }
        else
        {
            data.Add(key, value);
        }

        PhotonNetwork.CurrentRoom.SetCustomProperties(data);
    }

    public object GetProperty(string key)
    {
        Hashtable data = PhotonNetwork.CurrentRoom.CustomProperties;

        if (data.ContainsKey(key))
        {
            return data[key];
        }
        else
        {
            return null;
        }
    }
}
