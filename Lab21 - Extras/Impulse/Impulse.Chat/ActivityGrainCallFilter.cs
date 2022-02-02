using Orleans;
using Orleans.Runtime;
using System.Diagnostics;
using System.Reflection;

namespace Impulse.Chat
{
    public class ActivityGrainCallFilter : IIncomingGrainCallFilter
    {
        private readonly ActivitySource _source;
        private readonly Assembly _assembly = Assembly.GetExecutingAssembly();

        public ActivityGrainCallFilter(ActivitySource source)
        {
            _source = source;
        }

        public async Task Invoke(IIncomingGrainCallContext context)
        {
            var type = context.Grain.GetType();
            if (type.Assembly != _assembly)
            {
                await context.Invoke();
                return;
            }

            var identity = (context.Grain as Grain)?.GrainReference.GrainIdentity;
            var method = context.ImplementationMethod.Name;

            using var activity = _source.StartActivity($"{identity}/{method}");
            activity?.SetTag("ActivityId", RequestContext.ActivityId);
            activity?.SetTag("ClientId", RequestContext.Get("ClientId"));

            await context.Invoke();
        }
    }
}