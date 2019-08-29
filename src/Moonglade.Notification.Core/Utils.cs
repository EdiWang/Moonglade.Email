using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Moonglade.Notification.Core
{
    public class Utils
    {
        public static string AppVersion =>
            Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                .InformationalVersion;
    }
}
