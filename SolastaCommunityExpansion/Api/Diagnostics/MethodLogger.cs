﻿using System.Diagnostics;
using System.Runtime.CompilerServices;
using SolastaModApi.Infrastructure;

namespace SolastaModApi.Diagnostics
{
    /// <summary>
    /// <para>
    /// Usage
    /// public class SomeType
    /// {
    ///     public int SomeMethod(...)
    ///     {
    ///         using(var logger = new MethodLogger(nameof(SomeType)))
    ///         {
    ///             ...
    ///             logger.Log("This is a message");
    ///             ...
    ///         }
    ///     }
    /// }
    /// </para>
    /// <para>
    /// Log entries are:
    /// SomeType.SomeMethod: Enter
    /// SomeType.SomeMethod: This is a message
    /// SomeType.SomeMethod: Exit
    /// </para>
    /// </summary>
    public class MethodLogger : SetResetDisposable
    {
        private readonly string methodName;

        // Very annoying there's no CallerTypeNameAttribute
        private readonly string typeName;

        public MethodLogger(string typeName, [CallerMemberName] string methodName = null) :
            base(() => SolastaCommunityExpansion.Main.Log($"{typeName}.{methodName}: Enter"), () => SolastaCommunityExpansion.Main.Log($"{typeName}.{methodName}: Leave"))
        {
            this.methodName = methodName;
            this.typeName = typeName;
        }

        [Conditional("DEBUG")]
        public void Log(string message)
        {
            SolastaCommunityExpansion.Main.Log($"{typeName}.{methodName}: {message}");
        }
    }
}
