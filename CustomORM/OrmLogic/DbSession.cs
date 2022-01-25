using System;
using System.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading;
using CustomORM.EventArgsObjects;
using CustomORM.Interfaces;

namespace CustomORM.OrmLogic
{
    
    public class DbSession : ISession, IDisposable
    {
        private SqlTransaction _currentTransaction;
        private SqlCommand _currentCommand;
        private readonly SqlConnection _currentConnection;
        private bool _isDisposed = false;
        public DbSession(String connectionString)
        {
            _currentConnection = new SqlConnection(connectionString);
            RegisterAllDerivedDbSets();
            _currentConnection.Open();
        }
        
        public event EventHandler<CommandEventArgs> SetCommandToDerivedDbSets;

        private object? GetDbSet(PropertyInfo property)
        {
            var dbSetTarget = property.GetValue(this);

            if (dbSetTarget is null)
            {
                var dbSetInstance = Activator.CreateInstance(property.PropertyType,_currentConnection.CreateCommand());
                property.SetValue(this, dbSetInstance);
                dbSetTarget = dbSetInstance;
            }

            return dbSetTarget;
        }

        private void RegisterAllDerivedDbSets()
        {
            var declaredProperties = this.GetType().GetTypeInfo().DeclaredProperties;
            foreach (var property in declaredProperties)
            {
                if (property.PropertyType.GetGenericTypeDefinition().IsAssignableFrom(typeof(DbEntitySet<>)))
                {
                    var dbSetEventRegistrationMethod = GetDbSetRegistrationMethod(property.PropertyType);
                    var dbSetTarget = this.GetDbSet(property);
                    this.RegisterCommandEventForMember(dbSetEventRegistrationMethod, dbSetTarget);
                }
            }
        }

        


        private MethodInfo? GetDbSetRegistrationMethod(Type dbSetType)
        {
            var declaredMethods = dbSetType.GetTypeInfo().DeclaredMethods;
            
            var foundMethodInfo = declaredMethods.ToList().Find(methodInfo =>
            {
                var methodParameters = methodInfo.GetParameters();
                return methodParameters.Length == 2 &&
                       methodParameters[0].ParameterType == typeof(Object)
                       && methodParameters[1].ParameterType == typeof(CommandEventArgs);
            });

            return foundMethodInfo;
        }

        private void OnSetCommandToDerivedDbSets(CommandEventArgs e)
        {
            var currEvent = Volatile.Read(ref SetCommandToDerivedDbSets);
            
            currEvent?.Invoke(this, e);
        }

        
        private void RegisterCommandEventForMember(MethodInfo methodToRegister,Object? target)
        {
           var delegateToRegister =
               (EventHandler<CommandEventArgs>)methodToRegister.CreateDelegate(typeof(EventHandler<CommandEventArgs>), target);


           SetCommandToDerivedDbSets += delegateToRegister;
        }
        
        public void SaveChanges()
        {
            _currentTransaction = _currentConnection.BeginTransaction();
            _currentCommand = _currentConnection.CreateCommand();
            _currentCommand.Transaction = _currentTransaction;
            var eventArgs = new CommandEventArgs(_currentCommand);
            //TODO WRITE LOGIC FOR Transaction Exception Handling
            try
            {
                this.OnSetCommandToDerivedDbSets(eventArgs);

                _currentTransaction.Commit();
            }
            catch (Exception ex)
            {
                _currentTransaction.Rollback();
                throw;
            }
        }
         
        public void Dispose()
        {
            if (!_isDisposed){
                _currentConnection?.Dispose();
                _isDisposed = true;
            }
        }
    }
}