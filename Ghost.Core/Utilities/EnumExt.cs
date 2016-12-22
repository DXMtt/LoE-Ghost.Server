﻿namespace Ghost.Core.Utilities
{
    public enum AccessLevel : byte
    {
        Default = 0,
        Player = 1,
        TeamMember = 20,
        Implementer = 25,
        Moderator = 30,
        Admin = 255
    }

    public enum UserPermission : uint
    {
        ExecuteCommands = 1,
    }

    public static class EnumExt
    {

    }
}