using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Services.Vivox;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class OurRosterManager : MonoBehaviour
{
    public GameObject rosterItemPrefab;
    public Button deletionFallback;

    EventSystem m_EventSystem;

    Dictionary<string, List<OurRosterItem>> m_RosterObjects = new Dictionary<string, List<OurRosterItem>>();

    void Start()
    {
        // Fetch the current EventSystem
        m_EventSystem = EventSystem.current;
        VivoxService.Instance.ParticipantAddedToChannel += OnParticipantAdded;
        VivoxService.Instance.ParticipantRemovedFromChannel += OnParticipantRemoved;
        VivoxService.Instance.LoggedOut += OnUserLoggedOut;
        VivoxService.Instance.ChannelLeft += OnChannelDisconnected;
    }

    private void Awake()
    {
        /*VivoxService.Instance.ParticipantAddedToChannel += OnParticipantAdded;
        VivoxService.Instance.ParticipantRemovedFromChannel += OnParticipantRemoved;
        VivoxService.Instance.LoggedOut += OnUserLoggedOut;
        VivoxService.Instance.ChannelLeft += OnChannelDisconnected;*/
    }

    private void OnDestroy()
    {
        VivoxService.Instance.ParticipantAddedToChannel -= OnParticipantAdded;
        VivoxService.Instance.ParticipantRemovedFromChannel -= OnParticipantRemoved;
        VivoxService.Instance.LoggedOut -= OnUserLoggedOut;
        VivoxService.Instance.ChannelLeft -= OnChannelDisconnected;
    }

    public void ClearAllRosters()
    {
        foreach (List<OurRosterItem> rosterList in m_RosterObjects.Values)
        {
            foreach (OurRosterItem item in rosterList)
            {
                Destroy(item.gameObject);
            }
            rosterList.Clear();
        }
        m_RosterObjects.Clear();
    }

    public void ClearChannelRoster(string channelName)
    {
        List<OurRosterItem> rosterList = m_RosterObjects[channelName];
        foreach (OurRosterItem item in rosterList)
        {
            Destroy(item.gameObject);
        }
        rosterList.Clear();
        m_RosterObjects.Remove(channelName);
    }

    void CleanRoster(string channelName)
    {
        RectTransform rt = this.gameObject.GetComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, m_RosterObjects[channelName].Count * 50);
    }

    void OnChannelDisconnected(string channelName)
    {
        if (m_RosterObjects.Keys.Contains(channelName))
        {
            ClearChannelRoster(channelName);
        }
    }

    void OnUserLoggedOut()
    {
        ClearAllRosters();
    }
    
    void OnParticipantAdded(VivoxParticipant participant)
    {
        GameObject newRosterObject = GameObject.Instantiate(rosterItemPrefab, this.gameObject.transform);
        OurRosterItem newRosterItem = newRosterObject.GetComponent<OurRosterItem>();
        List<OurRosterItem> thisChannelList;

        if (m_RosterObjects.ContainsKey(participant.ChannelName))
        {
            //Add this object to an existing roster
            m_RosterObjects.TryGetValue(participant.ChannelName, out thisChannelList);
            newRosterItem.SetupRosterItem(participant);
            thisChannelList.Add(newRosterItem);
            m_RosterObjects[participant.ChannelName] = thisChannelList;
        }
        else
        {
            //Create a new roster to add this object to
            thisChannelList = new List<OurRosterItem>();
            thisChannelList.Add(newRosterItem);
            newRosterItem.SetupRosterItem(participant);
            m_RosterObjects.Add(participant.ChannelName, thisChannelList);
        }
        CleanRoster(participant.ChannelName);
    }

    void OnParticipantRemoved(VivoxParticipant participant)
    {
        if (m_RosterObjects.ContainsKey(participant.ChannelName))
        {
            OurRosterItem removedItem = m_RosterObjects[participant.ChannelName].FirstOrDefault(p => p.Participant.PlayerId == participant.PlayerId);
            if (removedItem != null)
            {
                for (GameObject go = m_EventSystem.currentSelectedGameObject; go != null; go = go?.transform?.parent?.gameObject)
                {
                    if (removedItem.gameObject == go)
                    {
                        deletionFallback?.Select();
                        break;
                    }
                }

                m_RosterObjects[participant.ChannelName].Remove(removedItem);
                Destroy(removedItem.gameObject);
                CleanRoster(participant.ChannelName);
            }
            else
            {
                Debug.LogError("Trying to remove a participant that has no roster item.");
            }
        }
    }
}
