
using System;
using System.Diagnostics;
using Topshelf;

namespace MessageQueue.Handler
{
    class Program
    {
        private static void Main( string[] args )
        {
            try
            {
                Debug.WriteLine( "Starting..." );
                                
                string listenOnQueueName = null;

                
                HostFactory.Run( hostConfig =>
                {
                
                    hostConfig.AddCommandLineDefinition( "listenOnQueueName", q => { listenOnQueueName = q; } );
                    hostConfig.ApplyCommandLine();

                    Debug.WriteLine( $"listenOnQueueName={ listenOnQueueName}" );

                
                    hostConfig.Service<QueueListener>(
                        sc =>
                        {
                
                            sc.ConstructUsing( () => new QueueListener() );
                
                            sc.WhenStarted( s => s.Start( listenOnQueueName ) );
                
                            sc.WhenStopped( s => s.Stop() );
                        } );

                
                    hostConfig.SetServiceName( "MessageQueue.Handler" );
                
                    hostConfig.UseLog4Net();
                
                    hostConfig.RunAsLocalSystem();
                } );
            }
            catch ( Exception ex )
            {
                Debug.WriteLine( "Error running host: {0}", ex );
            }
        }
    }
}
