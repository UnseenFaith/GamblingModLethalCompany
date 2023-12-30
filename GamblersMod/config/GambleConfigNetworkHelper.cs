﻿using Unity.Collections;
using Unity.Netcode;
using static GamblersMod.config.GambleConstants;

namespace GamblersMod.config
{
    public class GambleConfigNetworkHelper
    {
        // Callback when host recieves client config request. It will serialize the config, and sends that config to clients
        public static void OnHostRecievesClientConfigRequest(ulong clientId, FastBufferReader _)
        {
            if (!NetworkManager.Singleton.IsHost)
            {
                return;
            }

            Plugin.mls.LogInfo("Host recieved client config request.");

            Plugin.mls.LogInfo("Serializing host config data...");
            byte[] serializedData = SerializerHelper.GetSerializedSettings(Plugin.UserConfig);

            Plugin.mls.LogInfo("Start writing host config data...");
            FastBufferWriter writer = new FastBufferWriter(serializedData.Length + 4, Allocator.Temp); // + 4 for error checking, but why is this also needed in StartClientRequestConfigFromHost
            using (writer)
            {
                Plugin.mls.LogInfo("Writing host config data");
                writer.WriteValueSafe(serializedData.Length); // This will be used for error checking when client recieves the host config message
                writer.WriteBytesSafe(serializedData); // This is the actual config data

                Plugin.mls.LogInfo($"Sending host config data to client with id of {clientId}...");
                NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage($"{Plugin.modGUID}_{ON_CLIENT_RECIEVES_HOST_CONFIG_REQUEST}", clientId, writer, NetworkDelivery.ReliableFragmentedSequenced);
            }
        }

        // Starts the process for the client requesting the configuration from the host
        public static void StartClientRequestConfigFromHost()
        {
            if (!NetworkManager.Singleton.IsClient)
            {
                return;
            }
            Plugin.mls.LogInfo("Client is requesting configuration from host");
            FastBufferWriter stream = new FastBufferWriter(4, Allocator.Temp); // The 4 is used in OnClientRecievesHostConfigRequest for error checking.. but isn't that done in OnHostRecievesClientConfigRequest? 

            // Client asking host for configuration file
            NetworkManager.Singleton.CustomMessagingManager.SendNamedMessage($"{Plugin.modGUID}_{ON_HOST_RECIEVES_CLIENT_CONFIG_REQUEST}", 0ul, stream); // 0ul cause not being used
        }

        // Callback for when client recieves a host config message. Client will set read the config and set the config as their own.
        public static void OnClientRecievesHostConfigRequest(ulong _, FastBufferReader reader)
        {
            // Host + Client will update config, so no early checks here. (Seems like HOST doesn't hit this code at all)
            Plugin.mls.LogInfo("Client recieved configuration message from host");

            // Read and pop 4 bytes?
            if (!reader.TryBeginRead(4))
            {
                Plugin.mls.LogError("Could not sync client configuration with host. The stream sent by StartClientRequestConfigFromHost was invalid.");
                return;
            }

            // Read the length of the config file in bytes, but dont pop
            reader.ReadValueSafe(out int configByteLength);

            // Read the config file in bytes and pop
            if (!reader.TryBeginRead(configByteLength))
            {
                Plugin.mls.LogError("Could not sync client configuration with host. Host could not serialize the data.");
            }

            // Now the remaining bytes in the buffer should be the config data
            byte[] configByteData = new byte[configByteLength];
            reader.ReadBytesSafe(ref configByteData, configByteLength);


            GambleConfigSettingsSerializable newUserConfig = SerializerHelper.GetDeserializedSettings<GambleConfigSettingsSerializable>(configByteData);
            newUserConfig.configGamblingMusicEnabled = Plugin.UserConfig.configGamblingMusicEnabled; // Keep this client sided
            newUserConfig.configGamblingMusicVolume = Plugin.UserConfig.configGamblingMusicVolume; // Keep this client sided
            Plugin.UserConfig = newUserConfig; // Client configuration set to the host configuration!

            var pluginLogger = Plugin.mls;

            pluginLogger.LogInfo($"Jackpot chance value from config: {Plugin.UserConfig.configJackpotChance}");
            pluginLogger.LogInfo($"Triple chance value from config: {Plugin.UserConfig.configTripleChance}");
            pluginLogger.LogInfo($"Double chance value from config: {Plugin.UserConfig.configDoubleChance}");
            pluginLogger.LogInfo($"Halve chance value from config: {Plugin.UserConfig.configHalveChance}");
            pluginLogger.LogInfo($"Zero chance value from config: {Plugin.UserConfig.configZeroChance}");

            pluginLogger.LogInfo($"Jackpot multiplier value from config: {Plugin.UserConfig.configJackpotMultiplier}");
            pluginLogger.LogInfo($"Triple multiplier value from config: {Plugin.UserConfig.configTripleMultiplier}");
            pluginLogger.LogInfo($"Double multiplier value from config: {Plugin.UserConfig.configDoubleMultiplier}");
            pluginLogger.LogInfo($"Halve multiplier value from config: {Plugin.UserConfig.configHalveMultiplier}");
            pluginLogger.LogInfo($"Zero multiplier value from config: {Plugin.UserConfig.configZeroMultiplier}");

            Plugin.mls.LogInfo("Successfully synced a client with host configuration");
        }
    }
}
