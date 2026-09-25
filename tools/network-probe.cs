// Read-only diagnostic. Compile with mcs inside the LXD builder, then run with
// the snap's Mono runtime and MONO_CFG_DIR/LD_LIBRARY_PATH from its launcher.
using System;
using System.IO;
using System.Net;
class NetworkProbe {
    static int Main(string[] urls) {
        int failed = 0;
        foreach (string url in urls) {
            Console.WriteLine(new Uri(url).GetLeftPart(UriPartial.Path));
            try {
                var req = (HttpWebRequest)WebRequest.Create(url);
                req.Timeout = 20000;
                req.UserAgent = "Banshee-preservation-network-diagnostic";
                using (var response = (HttpWebResponse)req.GetResponse()) {
                    Console.WriteLine("HTTP " + (int)response.StatusCode + " " + response.ContentType);
                    using(var reader = new StreamReader(response.GetResponseStream())) {
                        char[] buffer = new char[160];
                        int count = reader.Read(buffer, 0, buffer.Length);
                        Console.WriteLine(new string(buffer, 0, count).Replace('\n', ' '));
                    }
                }
            } catch (Exception e) {
                failed++;
                for (Exception inner=e;inner!=null;inner=inner.InnerException)
                    Console.WriteLine(inner.GetType().Name + ": " + inner.Message);
            }
        }
        return failed == 0 ? 0 : 1;
    }
}
