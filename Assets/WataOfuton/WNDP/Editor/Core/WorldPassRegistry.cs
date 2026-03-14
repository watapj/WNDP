using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace WataOfuton.Tool.WNDP.Editor
{
    public static class WorldPassRegistry
    {
        private static IReadOnlyList<Type> _passTypes;

        public static IReadOnlyList<IWorldBuildPass> GetPasses()
        {
            EnsureTypes();

            return _passTypes
                .Select(CreatePass)
                .Where(pass => pass != null)
                .OrderBy(pass => pass.Phase)
                .ThenBy(pass => pass.Order)
                .ThenBy(pass => pass.GetType().FullName, StringComparer.Ordinal)
                .ToArray();
        }

        public static void Refresh()
        {
            _passTypes = null;
            EnsureTypes();
        }

        private static void EnsureTypes()
        {
            if (_passTypes != null)
            {
                return;
            }

            _passTypes = TypeCache.GetTypesDerivedFrom<IWorldBuildPass>()
                .Where(type => !type.IsAbstract && !type.IsInterface)
                .Where(type => type.GetCustomAttributes(typeof(WorldBuildPassAttribute), false).Length > 0)
                .Where(type => type.GetConstructor(Type.EmptyTypes) != null)
                .OrderBy(type => type.FullName, StringComparer.Ordinal)
                .ToArray();
        }

        private static IWorldBuildPass CreatePass(Type passType)
        {
            try
            {
                return (IWorldBuildPass)Activator.CreateInstance(passType);
            }
            catch (Exception exception)
            {
                Debug.LogError($"[WNDP] Failed to instantiate pass '{passType.FullName}': {exception}");
                return null;
            }
        }
    }
}
