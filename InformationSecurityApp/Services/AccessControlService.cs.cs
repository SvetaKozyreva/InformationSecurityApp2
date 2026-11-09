
using System;
using InformationSecurityApp.Models;

namespace InformationSecurityApp.Services;

public class AccessControlService
{
    private static readonly string[] AllowedDirectories =
        { "A", "B", "C", "D", "E" };

    private const string AllowedRights = "RWAE";

    public bool HasPermission(
        User user,
        string directory,
        char requiredRight,
        out string message)
    {
        directory = directory.Trim().ToUpperInvariant();
        requiredRight = char.ToUpperInvariant(requiredRight);

        if (!AllowedDirectories.Contains(directory))
        {
            message = "Доступ заборонено: невідомий каталог.";
            return false;
        }

        if (!AllowedRights.Contains(requiredRight))
        {
            message = "Доступ заборонено: невідоме право.";
            return false;
        }

        if (user.Permissions == null ||
            !user.Permissions.TryGetValue(
                directory,
                out string? userRights) ||
            string.IsNullOrEmpty(userRights) ||
            !userRights.Contains(requiredRight))
        {
            message =
                $"Доступ заборонено. Користувач {user.Login} " +
                $"не має права {requiredRight} для каталогу {directory}.";

            return false;
        }

        message =
            $"Доступ дозволено: каталог {directory}, право {requiredRight}.";

        return true;
    }
}
