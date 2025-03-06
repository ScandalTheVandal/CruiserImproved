﻿using System;
using CruiserImproved.Utils;
using Unity.Netcode;

namespace CruiserImproved.Network;

internal class NetworkConfig : INetworkSerializable
{
    public Version version = new(MyPluginInfo.PLUGIN_VERSION);

    //Settings as of v1.2.0 (Implementation of NetworkedSettings)
    public bool SyncSeat = false;
    public float SeatBoostScale = 0.0f;

    public bool AllowLean = false;
    public bool PreventMissileKnockback = false;
    public bool AllowPushDestroyedCar = false;
    public bool PreventPassengersEjectingDriver = false;
    public bool EntitiesAvoidCruiser = false;
    public bool SilentCollisions = false;

    public float CruiserInvulnerabilityDuration = 0;
    public float CruiserCriticalInvulnerabilityDuration = 0;
    public int MaxCriticalHitCount = 0;

    public bool AntiSideslip = false;

    //Settings as of v1.3.0
    public bool DisableRadioStatic = false;

    //Settings as of v1.4.0
    public bool HandsfreeDoors = false;
    public bool StandingKeyRemoval = false;
    public bool TurboExhaust = false;
    public ScanNodeOptions CruiserScanNode = 0;

    //Settings as of v1.5.0
    public bool ScanWhileSeated = false;

    public bool CabinLightToggle = false;

    //Initialize NetworkedSettings from local config
    public void CopyLocalConfig()
    {
        //v1.2.0
        SyncSeat = UserConfig.SyncSeat.Value;
        AllowLean = UserConfig.AllowLean.Value;
        SeatBoostScale = UserConfig.SeatBoostScale.Value;

        PreventMissileKnockback = UserConfig.PreventMissileKnockback.Value;
        AllowPushDestroyedCar = UserConfig.AllowPushDestroyedCar.Value;
        PreventPassengersEjectingDriver = UserConfig.PreventPassengersEjectingDriver.Value;
        EntitiesAvoidCruiser = UserConfig.EntitiesAvoidCruiser.Value;
        SilentCollisions = UserConfig.SilentCollisions.Value;

        CruiserInvulnerabilityDuration = UserConfig.CruiserInvulnerabilityDuration.Value;
        CruiserCriticalInvulnerabilityDuration = UserConfig.CruiserCriticalInvulnerabilityDuration.Value;
        MaxCriticalHitCount = UserConfig.MaxCriticalHitCount.Value;

        AntiSideslip = UserConfig.AntiSideslip.Value;

        //v1.3.0
        DisableRadioStatic = UserConfig.DisableRadioStatic.Value;

        //v1.4.0
        HandsfreeDoors = UserConfig.HandsfreeDoors.Value;
        StandingKeyRemoval = UserConfig.StandingKeyRemoval.Value;

        CruiserScanNode = UserConfig.CruiserScanNode.Value;

        TurboExhaust = UserConfig.TurboExhaust.Value;

        //v1.5.0
        ScanWhileSeated = UserConfig.ScanWhileSeated.Value;

        CabinLightToggle = UserConfig.CabinLightToggle.Value;
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        string versionString = "";
        if(serializer.IsWriter) versionString = version.ToString();
        serializer.SerializeValue(ref versionString);
        if (serializer.IsReader) version = new(versionString);

        //v1.2.0
        if (version < new Version(1, 2, 0)) return;

        serializer.SerializeValue(ref SyncSeat);
        if (SyncSeat)
        {
            serializer.SerializeValue(ref SeatBoostScale);
        }
        serializer.SerializeValue(ref AllowLean);
        serializer.SerializeValue(ref PreventMissileKnockback);
        serializer.SerializeValue(ref AllowPushDestroyedCar);
        serializer.SerializeValue(ref PreventPassengersEjectingDriver);
        serializer.SerializeValue(ref EntitiesAvoidCruiser);
        serializer.SerializeValue(ref SilentCollisions);

        serializer.SerializeValue(ref CruiserInvulnerabilityDuration);
        serializer.SerializeValue(ref CruiserCriticalInvulnerabilityDuration);
        serializer.SerializeValue(ref MaxCriticalHitCount);

        serializer.SerializeValue(ref AntiSideslip);

        //v1.3.0
        if (version < new Version(1, 3, 0)) return;

        serializer.SerializeValue(ref DisableRadioStatic);

        //v1.4.0
        if (version < new Version(1, 4, 0)) return;

        serializer.SerializeValue(ref HandsfreeDoors);
        serializer.SerializeValue(ref StandingKeyRemoval);
        serializer.SerializeValue(ref CruiserScanNode);
        serializer.SerializeValue(ref TurboExhaust);

        // v1.5.0
        if (version < new Version(1, 5, 0)) return;
        serializer.SerializeValue(ref ScanWhileSeated);
        serializer.SerializeValue(ref CabinLightToggle);
    }
}
