using System.Collections.Generic;

namespace AI.Core
{
    public interface ITaskScanner
    {
        void Scan(List<RobotTask> tasks);
    }
}
