using System;
using System.Data.SqlClient;

namespace CustomORM.EventArgsObjects
{
    public class CommandEventArgs : EventArgs
    {
        public CommandEventArgs(SqlCommand command)
        {
            Command = command;
        }

        public SqlCommand Command { get; }
    }
}