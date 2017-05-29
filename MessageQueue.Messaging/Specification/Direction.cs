namespace MessageQueue.Messaging.Specification
{


    public sealed class Direction
    {
        public static Direction Inbound = new Direction( "Inbound" );
        public static Direction Outbound = new Direction( "Outbound" );


        private string _name { get; }
        private Direction() { }
        private Direction( string name )
        {
            this._name = name;
        }

        public override string ToString()
        {
            return this._name;
        }
    }
}