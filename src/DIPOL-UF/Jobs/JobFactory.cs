using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DIPOL_UF.UserNotifications;
using Microsoft.Extensions.Logging;
using Serializers;

namespace DIPOL_UF.Jobs
{
    internal class JobFactory
    {
        private readonly IUserNotifier _notifier;
        private readonly ILoggerFactory _loggerFactory;

        public JobFactory(IUserNotifier notifier, ILoggerFactory loggerFactory)
        {
            _notifier = notifier;
            _loggerFactory = loggerFactory;
        }

        public JobManager.Job Create(ReadOnlyDictionary<string, object> input)
        {
            var job = new JobManager.Job(input, _notifier, _loggerFactory);
            return job;
        }

        public JobManager.Job Create(Stream stream)
        {
            if (!stream.CanRead)
                throw new IOException(@"Stream does not support reading.");

            ReadOnlyDictionary<string, object> json;
            using (var str = new StreamReader(stream, Encoding.ASCII, true, 512, true))
                json = JsonParser.ReadJson(str);

            var job = new JobManager.Job(json, _notifier, _loggerFactory);

            return job;
        }

        public async Task<JobManager.Job> CreateAsync(Stream stream)
        {
            if (!stream.CanRead)
                throw new IOException(@"Stream does not support reading.");

            var json = await JsonParser.ReadJsonAsync(stream, Encoding.ASCII, CancellationToken.None).ConfigureAwait(false);

            var job = new JobManager.Job(json, _notifier, _loggerFactory);

            return job;
        }

    }
}
