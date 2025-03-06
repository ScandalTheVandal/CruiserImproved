﻿using CruiserImproved.Patches;
using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

namespace CruiserImproved.Network;

internal static class NetworkSync
{
    static public NetworkConfig Config = null;
    static public bool SyncedWithHost = false;
    static public bool FinishedSync = false;

    static public List<ulong> HostSyncedList = null;

    static public void Init()
    {
        Config = new NetworkConfig();
        Config.CopyLocalConfig();
        SyncedWithHost = false;
        FinishedSync = false;

        AddAllMessageHandlers();

        if (NetworkManager.Singleton.IsHost)
        {
            CruiserImproved.LogMessage("Setup as host!");
            HostSyncedList = new();
            SyncedWithHost = true;
            SetupMessageHandler("ContactServerRpc", ContactServerRpc);

            FinishSync(true);

            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnect;
        }
        else if (NetworkManager.Singleton.IsClient)
        {
            SetupMessageHandler("SendConfigClientRpc", SendConfigClientRpc);

            string text = CruiserImproved.Version.ToString();
            FastBufferWriter fastBufferWriter = new FastBufferWriter(text.Length * sizeof(char), Allocator.Temp);
            fastBufferWriter.WriteValue(text);
            SendToHost("ContactServerRpc", fastBufferWriter, true);

            CruiserImproved.LogMessage("Setup as client!");
        }
    }

    static public void AddAllMessageHandlers()
    {
        //no need to clean these up as NetworkManager's CustomMessageHandler gets destroyed when not in a lobby

        SetupMessageHandler("SyncSteeringRpc", VehicleControllerPatches.SyncSteeringRpc);
        SetupMessageHandler("SyncRadioTimeRpc", VehicleControllerPatches.SyncRadioTimeRpc);
        SetupMessageHandler("ToggleCabLightRpc", VehicleControllerPatches.ToggleCabLightRpc);
    }

    static public void Cleanup()
    {
        Config = null;
        SyncedWithHost = false;
        HostSyncedList = null;
        FinishedSync = false;
        NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnect;
    }

    static public void OnClientDisconnect(ulong clientId)
    {
        if (HostSyncedList.Remove(clientId))
        {
            CruiserImproved.LogMessage("CruiserImproved client " + clientId + " disconnected.");
        }
    }

    static public void SetupMessageHandler(string name, CustomMessagingManager.HandleNamedMessageDelegate del)
    {
        NetworkManager.Singleton.CustomMessagingManager.RegisterNamedMessageHandler("CruiserImproved." + name, del);
    }

    static public void DeleteMessageHandler(string name)
    {
        NetworkManager.Singleton.CustomMessagingManager.UnregisterNamedMessageHandler("CruiserImproved." + name);
    }

    static public void SendClientSyncRpcs(ulong clientId)
    {
        CruiserImproved.LogMessage($"Sent sync for client {clientId}.");
        VehicleControllerPatches.SendClientSyncData(clientId);
    }

    //Send to specific clients
    static public void SendToClients(string name, IReadOnlyList<ulong> clients, ref FastBufferWriter buffer)
    {
        if (!NetworkManager.Singleton.IsHost)
        {
            CruiserImproved.LogError("SendToClients called from client!");
            return;
        }
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("CruiserImproved." + name, clients, buffer);
    }

    //Send to all CruiserImproved clients
    static public void SendToClients(string name, ref FastBufferWriter buffer) => SendToClients(name, HostSyncedList, ref buffer);

    static public void SendToHost(string name, FastBufferWriter buffer, bool forceSend = false)
    {
        if (!forceSend && !SyncedWithHost) return;
        NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage("CruiserImproved." + name, NetworkManager.ServerClientId, buffer);
    }

    static public void ContactServerRpc(ulong clientId, FastBufferReader reader)
    {
        reader.ReadValue(out string clientVersionStr);
        Version clientVersion = new(clientVersionStr);
        if (clientVersion > CruiserImproved.Version)
        {
            CruiserImproved.LogWarning("Client " + clientId + " connected with newer CruiserImproved version " + clientVersion + ". We're running outdated " + CruiserImproved.Version);
        }
        else if(clientVersion < CruiserImproved.Version)
        {
            CruiserImproved.LogWarning("Client " + clientId + " connected with outdated CruiserImproved version " + clientVersion + ". We're running " + CruiserImproved.Version);
        }
        else
        {
            CruiserImproved.LogMessage("Client " + clientId + " connected with CruiserImproved version match " + clientVersion);
        }

        HostSyncedList.Add(clientId);

        FastBufferWriter writer = new FastBufferWriter(128, Allocator.Temp, 1024);
        writer.WriteNetworkSerializable(Config);

        SendToClients("SendConfigClientRpc", [clientId], ref writer);
    }

    static public void SendConfigClientRpc(ulong clientId, FastBufferReader reader)
    {
        reader.ReadNetworkSerializableInPlace(ref Config);

        Version hostVersion = Config.version;

        if (hostVersion > CruiserImproved.Version)
        {
            CruiserImproved.LogWarning("Host successfuly synced with newer CruiserImproved version " + hostVersion + ". We're running outdated " + CruiserImproved.Version);
        }
        else if (hostVersion < CruiserImproved.Version)
        {
            CruiserImproved.LogWarning("Host successfuly synced with outdated CruiserImproved version " + hostVersion + ". We're running " + CruiserImproved.Version);
        }
        else
        {
            CruiserImproved.LogMessage("Host successfuly synced with CruiserImproved version " + hostVersion);
        }
        FinishSync(true);
    }

    static public void FinishSync(bool hostSynced)
    {
        if (FinishedSync) return;

        SyncedWithHost = hostSynced;
        FinishedSync = true;

        if (!SyncedWithHost)
        {
            CruiserImproved.LogMessage("Could not sync with host CruiserImproved instance. Only client-side effects will apply.");
        }
        VehicleControllerPatches.OnSync();
    }
}

