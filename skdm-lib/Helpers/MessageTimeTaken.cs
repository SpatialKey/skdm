using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SpatialKey.DataManager.Lib.Message;

namespace SpatialKey.DataManager.Lib.Helpers
{
    public class MessageTimeTaken : IDisposable
    {
        private readonly DateTime _startTime = DateTime.Now;
        private readonly string _msg;
        private readonly MessageLevel _level;
        private readonly BaseMessageClass.Messager _messenger;

        public MessageTimeTaken(BaseMessageClass.Messager messenger, string msg, MessageLevel level = MessageLevel.Status)
        {
            _messenger = messenger;
            _msg = msg;
            _level = level;

            _messenger(_level, string.Format("STARTED: {0}", _msg));
        }

        public void Dispose()
        {
            var nowTime = DateTime.Now;
            var totalTime = nowTime - _startTime;
            _messenger(_level, string.Format("FINISHED ({0}): {1}", totalTime, _msg));
        }
    }
}
