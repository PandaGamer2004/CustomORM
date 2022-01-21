using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using CustomORM.Interfaces;

namespace CustomORM.OrmLogic
{
    
    
    public class SimpleCommandBuilder<T> : ICommandBuilder<T> where T : class, new()
    {
        private EntityInfo _entityInfo = EntityInfoCollector.Instance.GetEntityInfoForType(typeof(T));
        private const String DefaultSelectPattern = "{0} from [dbo].[{1}]";
        private const String DefaultSelectAllString = "SELECT " + DefaultSelectPattern;
        private const String DefaultSelectTopString = "SELECT TOP{2} " + DefaultSelectPattern;
        private const String DefualtDeleteString = "DELETE FROM [dbo].[{0}]";
        private const String DefaultInsertString = "INSERT INTO [dbo].[{0}]({1})";
        private const String WherePattern = "[{0}].[{1}] = {2}";
        private static String DefaultUpdateString = "UPDATE [dbo].[{0}]";
        
        private void AddToSbInsertTemplate(StringBuilder sb)
        {
            var declaredEntityProperties = _entityInfo.EntityProperties;
            var dbColumnNames = _entityInfo.GetDbColumnNamesFromPropertyInfos(declaredEntityProperties);
            sb.AppendFormat(DefaultInsertString,_entityInfo.TableName, String.Join(", ", dbColumnNames));
            sb.AppendLine("VALUES");
        }

        private IEnumerable<SqlParameter> AddToSbValueSection(StringBuilder sb, T entity, Int32 rowNumber = 1)
        {
            sb.AppendLine();
            var declaredEntityProperties = _entityInfo.EntityProperties;
            var dbColumnNames = _entityInfo.GetDbColumnNamesFromPropertyInfos(declaredEntityProperties);
            var paramsNames = dbColumnNames.Select(name => "@" + name + rowNumber);

            sb.AppendFormat("({0})", String.Join(",", paramsNames));

            var sqlParameters = declaredEntityProperties.Zip(paramsNames,
                (property, paramName) => new SqlParameter
                {
                    SqlDbType = _entityInfo.GetDbTypeForGivenProperty(property),
                    Value = _entityInfo.GetPropertyValueForEntity(property, entity),
                    ParameterName = paramName
                });

            return sqlParameters;
        }
        
        private QueryEntity GenerateSelectCommandIdSearch<T2>(T2 id)
        {
            var sb = new StringBuilder();
            var columnNames = _entityInfo.GetDbColumnNamesFromPropertyInfos(_entityInfo.EntityProperties);
            var pkColumnName = _entityInfo.GetDbColumnNameFromPropertyInfo(_entityInfo.PrimaryKey);
            var paramName = $"@{pkColumnName}";
            sb.AppendFormat(DefaultSelectTopString,String.Join(", ", columnNames),_entityInfo.TableName, 1);
            sb.AppendLine("WHERE ");
            sb.AppendFormat(WherePattern,
                            _entityInfo.TableName,
                            pkColumnName,
                            paramName);
            
            var resultParam = new SqlParameter()
            {
                ParameterName = paramName,
                Value = id,
                SqlDbType = _entityInfo.GetDbTypeForGivenProperty(_entityInfo.PrimaryKey)
            };
            
            return new QueryEntity(sb.ToString(), new[] {resultParam});
        }
        
        public QueryEntity GenerateSelectCommand()
        {
            var sb = new StringBuilder();
            sb.AppendFormat(DefaultSelectAllString,_entityInfo.EntityProperties, _entityInfo.TableName);
            return new QueryEntity(sb.ToString(), null);
        }

        public QueryEntity GenerateSelectCommand(Guid id)
        {
            return GenerateSelectCommandIdSearch(id);
        }

        public QueryEntity GenerateSelectCommand(int id)
        {
            return GenerateSelectCommandIdSearch(id);
        }

        
        
        
        public QueryEntity GenerateInsertCommand(T entity)
        {
            var sb = new StringBuilder();
            AddToSbInsertTemplate(sb);
            IEnumerable<SqlParameter> sqlParameters = AddToSbValueSection(sb, entity);
            return new QueryEntity(sb.ToString(), sqlParameters);
        }
        

        public QueryEntity GenerateUpdateCommand(T entity)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(DefaultUpdateString, _entityInfo.TableName);
            sb.AppendLine(" SET");
            var propertiesToUpdate = 
                _entityInfo.EntityProperties.Except(new[] {_entityInfo.PrimaryKey});
            foreach (var propertyTpUpdate in propertiesToUpdate)
            {
                sb.AppendLine($"{_entityInfo.GetDbColumnNameFromPropertyInfo(propertyTpUpdate)} = " +
                              $"{_entityInfo.GetDbColumnValueForEntity(propertyTpUpdate, entity)},");
            }

            return sb.ToString();
        }
        

        public QueryEntity GenerateDeleteCommand(T entity)
        {
            var sb = new StringBuilder();
            sb.AppendFormat(DefualtDeleteString, _entityInfo.TableName);
            sb.AppendLine("WHERE ");
            sb.AppendFormat(WherePattern, _entityInfo.TableName,
                _entityInfo.GetDbColumnNameFromPropertyInfo(_entityInfo.PrimaryKey),;
            return sb.ToString();
        }
    }
}