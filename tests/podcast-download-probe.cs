using System;
using System.Threading;
using Migo.DownloadCore;
class PodcastDownloadProbe {
    static int Main(string[] args) {
        using (var done = new ManualResetEvent(false)) {
            var task = new HttpFileDownloadTask(args[0], args[1]);
            task.Completed += (o, e) => done.Set();
            task.ExecuteAsync();
            if (!done.WaitOne(15000)) { Console.Error.WriteLine("Timed out"); return 2; }
            Console.WriteLine(task.Status + ": " + task.ErrorMessage);
            return task.Status == Migo.TaskCore.TaskStatus.Succeeded ? 0 : 1;
        }
    }
}
