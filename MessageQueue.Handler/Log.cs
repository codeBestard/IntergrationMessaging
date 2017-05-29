using log4net;
using log4net.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MessageQueue.Handler
{
    public static class Log
    {
        private static ILog _Log;

        static Log()
        {
            XmlConfigurator.Configure();
            _Log = LogManager.GetLogger( "MessageQueue.Handler" );
        }

        public static void WriteLine( string format, params object[] args )
        {
            _Log.Debug( string.Format( format, args ) );
        }
    }
}
