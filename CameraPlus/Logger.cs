#if DEBUG
using System.IO;
using System.Runtime.CompilerServices;
#endif
using IPALogger = IPA.Logging.Logger;

namespace CameraPlus
{
    internal static class Logger
    {
        internal static IPALogger log { get; set; }
#if DEBUG
        //TODO:用済みになったら消す。
        internal static void Debug(string logmsg, [CallerMemberName] string memberName = "", [CallerFilePath] string file = "", [CallerLineNumber] int lineNum = 0)
        {
            log.Debug($"[{Path.GetFileName(file)}({lineNum})] {memberName}:{logmsg}");
        }
#endif
    }
}
