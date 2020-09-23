﻿using Discord;
using System;
using System.IO;

namespace CloneDroneELO
{
    [Serializable]
    public class UserData
    {
        public ulong UserID { get; private set; }

        public uint ELO;

        public string NicknameOverride;
        public bool HasNicknameOverride => !string.IsNullOrWhiteSpace(NicknameOverride);

        UserData()
        {
        }

        public UserData(IUser user)
        {
            UserID = user.Id;
            ELO = Program.DEFAULT_ELO;
            NicknameOverride = string.Empty;
        }

        public static UserData DeserializeFrom(BinaryReader reader)
        {
            UserData userData = new UserData();
            userData.UserID = reader.ReadUInt64();
            userData.ELO = reader.ReadUInt32();
            userData.NicknameOverride = reader.ReadString();

            return userData;
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(UserID);
            writer.Write(ELO);
            writer.Write(NicknameOverride);
        }
    }
}
