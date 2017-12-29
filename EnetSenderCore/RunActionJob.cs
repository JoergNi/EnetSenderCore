using Quartz;
using System;
using System.Threading.Tasks;

namespace EnetSenderCore
{
    public class RunActionJob : IJob
    {
        public async Task Execute(IJobExecutionContext context)
        {
            var action = context.MergedJobDataMap["action"] as Action;
            await Task.Run(action);
        }
    }

}
