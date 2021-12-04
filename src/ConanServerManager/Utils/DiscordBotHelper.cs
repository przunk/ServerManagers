﻿using QueryMaster;
using ServerManagerTool.Common.Utils;
using ServerManagerTool.DiscordBot.Enums;
using ServerManagerTool.Lib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using WPFSharp.Globalizer;

namespace ServerManagerTool.Utils
{
    internal static class DiscordBotHelper
    {
        private static readonly GlobalizedApplication _globalizer = GlobalizedApplication.Instance;
        private static bool _runningCommand = false;

        public static IList<string> HandleDiscordCommand(CommandType commandType, string serverId, string channelId, string profileId)
        {
            // check if incoming values are valid
            if (string.IsNullOrWhiteSpace(serverId) || string.IsNullOrWhiteSpace(channelId))
                return null;

            // check if the server ids match
            if (!serverId.Equals(Config.Default.DiscordBotServerId))
                return new List<string>();

            if (_runningCommand)
                return new List<string> { _globalizer.GetResourceString("DiscordBot_CommandRunning") };
            _runningCommand = true;

            try
            {
                switch (commandType)
                {
                    case CommandType.Info:
                        return GetServerInfo(channelId, profileId);
                    case CommandType.List:
                        return GetServerList(channelId);
                    case CommandType.Status:
                        return GetServerStatus(channelId, profileId);

                    case CommandType.Backup:
                        return BackupServer(channelId, profileId);
                    case CommandType.Shutdown:
                        return ShutdownServer(channelId, profileId);
                    case CommandType.Stop:
                        return StopServer(channelId, profileId);
                    case CommandType.Start:
                        return StartServer(channelId, profileId);
                    case CommandType.Update:
                        return UpdateServer(channelId, profileId);

                    default:
                        return new List<string> { string.Format(_globalizer.GetResourceString("DiscordBot_CommandUnknown"), commandType) };
                }
            }
            catch (Exception ex)
            {
                return new string[] { ex.Message };
            }
            finally
            {
                _runningCommand = false;
            }
        }

        public static string HandleTranslation(string translationKey)
        {
            return string.IsNullOrWhiteSpace(translationKey) ? string.Empty : _globalizer.GetResourceString(translationKey) ?? translationKey;
        }

        private static IList<string> GetServerInfo(string channelId, string profileId)
        {
            if (string.IsNullOrWhiteSpace(profileId))
            {
                return new List<string> { _globalizer.GetResourceString("DiscordBot_CommandProfileMissing") };
            }

            var serverName = string.Empty;
            var serverIp = IPAddress.Loopback;
            var queryPort = 0;

            TaskUtils.RunOnUIThreadAsync(() =>
            {
                var server = ServerManager.Instance.Servers.Where(s => Equals(channelId, s.Profile.DiscordChannelId) && Equals(profileId, s.Profile.ProfileID)).FirstOrDefault();

                if (server is null)
                {
                    throw new Exception(string.Format(_globalizer.GetResourceString("DiscordBot_CommandProfileNotFound"), profileId));
                }

                serverName = server.Profile.ServerName;
                if (!string.IsNullOrWhiteSpace(server.Profile.ServerIP))
                {
                    IPAddress.TryParse(server.Profile.ServerIP, out serverIp);
                }
                queryPort = server.Profile.QueryPort;
            }).Wait();

            List<string> response = new List<string>();

            try
            {
                using (var gameServer = ServerQuery.GetServerInstance(EngineType.Source, new IPEndPoint(serverIp, queryPort)))
                {
                    var info = gameServer?.GetInfo();
                    if (info is null)
                    {
                        response.Add(string.Format(_globalizer.GetResourceString("DiscordBot_CommandInfoFailed"), serverName));
                    }
                    else
                    {
                        var mapName = _globalizer.GetResourceString($"Map_{info.Map}") ?? info.Map;
                        response.Add($"```{info.Name}\n{_globalizer.GetResourceString("DiscordBot_MapLabel")} {mapName}\n{_globalizer.GetResourceString("ServerSettings_PlayersLabel")} {info.Players} / {info.MaxPlayers}```");
                    }
                }
            }
            catch (Exception)
            {
                response.Add(string.Format(_globalizer.GetResourceString("DiscordBot_CommandInfoFailed"), serverName));
            }

            return response;
        }

        private static IList<string> GetServerList(string channelId)
        {
            List<string> response = new List<string>();

            TaskUtils.RunOnUIThreadAsync(() =>
            {
                var serverList = ServerManager.Instance.Servers.Where(s => Equals(channelId, s.Profile.DiscordChannelId));

                response.Add($"**{_globalizer.GetResourceString("DiscordBot_CountLabel")}** {serverList.Count()}");
                foreach (var server in serverList)
                {
                    response.Add($"```{_globalizer.GetResourceString("ServerSettings_ProfileIdLabel")} {server.Profile.ProfileID}\n{_globalizer.GetResourceString("ServerSettings_ProfileLabel")} {server.Profile.ProfileName}\n{_globalizer.GetResourceString("ServerSettings_ServerNameLabel")} {server.Profile.ServerName}```");
                }
            }).Wait();

            return response;
        }

        private static IList<string> GetServerStatus(string channelId, string profileId)
        {
            List<string> response = new List<string>();

            TaskUtils.RunOnUIThreadAsync(() =>
            {
                var serverList = ServerManager.Instance.Servers.Where(s => Equals(channelId, s.Profile.DiscordChannelId) && (string.IsNullOrWhiteSpace(profileId) || Equals(profileId, s.Profile.ProfileID)));

                response.Add($"**{_globalizer.GetResourceString("DiscordBot_CountLabel")}** {serverList.Count()}");
                foreach (var server in serverList)
                {
                    response.Add($"```{_globalizer.GetResourceString("ServerSettings_ProfileLabel")} {server.Profile.ProfileName}\n{_globalizer.GetResourceString("ServerSettings_ServerNameLabel")} {server.Profile.ServerName}\n{_globalizer.GetResourceString("ServerSettings_StatusLabel")} {server.Runtime.StatusString}\n{_globalizer.GetResourceString("ServerSettings_AvailabilityLabel")} {_globalizer.GetResourceString($"ServerSettings_Availability_{server.Runtime.Availability}")}```");
                }
            }).Wait();

            return response;
        }

        private static IList<string> BackupServer(string channelId, string profileId)
        {
            return new List<string>() { string.Format(_globalizer.GetResourceString("DiscordBot_CommandUnknown"), CommandType.Backup) };
        }

        private static IList<string> ShutdownServer(string channelId, string profileId)
        {
            return new List<string>() { string.Format(_globalizer.GetResourceString("DiscordBot_CommandUnknown"), CommandType.Shutdown) };
        }

        private static IList<string> StopServer(string channelId, string profileId)
        {
            return new List<string>() { string.Format(_globalizer.GetResourceString("DiscordBot_CommandUnknown"), CommandType.Stop) };
        }

        private static IList<string> StartServer(string channelId, string profileId)
        {
            return new List<string>() { string.Format(_globalizer.GetResourceString("DiscordBot_CommandUnknown"), CommandType.Start) };
        }

        private static IList<string> UpdateServer(string channelId, string profileId)
        {
            return new List<string>() { string.Format(_globalizer.GetResourceString("DiscordBot_CommandUnknown"), CommandType.Update) };
        }
    }
}
