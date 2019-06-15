using System;

namespace PortalToFriend
{
    public class Utils
    {
        public static void Log(params object[] msgs) {
            var msg = "[PortalToFriend]:";
            foreach (var _msg in msgs) {
                try {
                    msg += $" {_msg}";
                } catch {
                    msg += $" {_msg.ToString()}";
                }
            }
            System.Console.WriteLine(msg);
        }
    }
}
