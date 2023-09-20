namespace Terrasoft.Configuration.OneCBaseEntity
{
    using System.Collections.Generic;
    using System.ServiceModel;
    using System.ServiceModel.Web;
    using System.ServiceModel.Activation;
    using System.Linq;
    using System.Runtime.Serialization;
    using Terrasoft.Web.Common;
    using Web.Http.Abstractions;
    using System.Reflection;

    using System.Diagnostics;
    using System;
    using Core;
    using Core.DB;
    using Core.Entities;
    using Terrasoft.Core.Configuration;
    using Common;
    using System.Data;
    using System.Globalization;

    using Configuration.GenIntegrationLogHelper;
    using Configuration.GenOneCSvcIntegration;
    using Terrasoft.Configuration.OneCBaseEntity;
    using Terrasoft.Configuration.GenOneCIntegrationHelper;


    public class OneCQueryBuilder
    {
        private readonly UserConnection _userConnection;
        private readonly string _entityName;
        private Select selectQuery;

        public OneCQueryBuilder(UserConnection userConnection, string entityName)
        {
            _userConnection = userConnection;
            _entityName = entityName;
            this.selectQuery = new Select(_userConnection);
        }

        public void AddBaseEntityFields()
        {
            selectQuery = selectQuery.Column(_entityName, "Id")
                .Column(_entityName, "GenID1C")
                .Column(_entityName, "ModifiedOn")
                .Column(_entityName, "CreatedOn")
                .From(_entityName) as Select;
        }

        public void FindFirstEntityId()
        {
            selectQuery = new Select(_userConnection)
                .Column(_entityName, "Id").Top(1)
                .From(_entityName).As(_entityName)
                as Select;
        }

        public void AddColumn(JoinParameter parameter)
        {
            selectQuery = selectQuery.Column(parameter.TableName, parameter.ColumnName).As(parameter.ColumnAlias);
            if(parameter.JoinColumn != null)
            {
                AddLeftJoin(parameter);
            }
        }

        public void AddLeftJoin(JoinParameter parameter)
        {
            if (parameter.JoinColumn != null && !selectQuery.Joins.Exists(parameter.TableName))
            {
                selectQuery = selectQuery.LeftOuterJoin(parameter.TableName).As(parameter.TableName)
                .On(parameter.TableName, "Id").IsEqual(_entityName, parameter.JoinColumn) as Select;
            }
        }

        public void AddSearchFilters(SearchFilter searchFilters)
        {
            if (searchFilters == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(searchFilters.Id1C))
            {
                AddCondition<string>(_entityName, "GenID1C");
            }
            else if (!string.IsNullOrEmpty(searchFilters.LocalId))
            {
                AddCondition<Guid>(_entityName, new Guid(searchFilters.LocalId));
            }
            //else if (!string.IsNullOrEmpty(searchFilters.CreatedFrom) || !string.IsNullOrEmpty(searchFilters.CreatedTo))
            //{
            //    if (!string.IsNullOrEmpty(searchFilters.CreatedFrom))
            //    {
            //        selectQuery = selectQuery.Where(EntityName, "CreatedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(searchFilters.CreatedFrom))) as Select;
            //    }
            //    if (!string.IsNullOrEmpty(searchFilters.CreatedTo))
            //    {
            //        selectQuery = selectQuery.And(EntityName, "CreatedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(searchFilters.CreatedTo))) as Select;
            //    }
            //}
            //else if (!string.IsNullOrEmpty(searchFilters.ModifiedFrom) || !string.IsNullOrEmpty(searchFilters.ModifiedTo))
            //{
            //    if (!string.IsNullOrEmpty(searchFilters.ModifiedFrom))
            //    {
            //        selectQuery = selectQuery.Where(EntityName, "ModifiedOn").IsLessOrEqual(Column.Parameter(DateTime.Parse(searchFilters.ModifiedFrom))) as Select;
            //    }
            //    if (!string.IsNullOrEmpty(searchFilters.ModifiedTo))
            //    {
            //        selectQuery = selectQuery.And(EntityName, "ModifiedOn").IsGreaterOrEqual(Column.Parameter(DateTime.Parse(searchFilters.ModifiedTo))) as Select;
            //    }
            //}
        }

        public void AddCondition<T>(string equalityKey, T equalityValue, EqualityConditionType whereCondition = EqualityConditionType.IsEqual)
        {
            if (!selectQuery.HasCondition)
            {
                Where<T>(equalityKey, equalityValue, whereCondition);
            }
            else
            {
                And<T>(equalityKey, equalityValue, whereCondition);
            }
        }

        private void Where<T>(string equalityKey, T equalityValue, EqualityConditionType whereCondition = EqualityConditionType.IsEqual)
        {
            switch (whereCondition)
            {
                case EqualityConditionType.IsEqual:
                    selectQuery = selectQuery.Where(_entityName, equalityKey).IsEqual(Column.Parameter(equalityValue)) as Select;
                    break;
                case EqualityConditionType.IsLessOrEqual:
                    selectQuery = selectQuery.Where(_entityName, equalityKey).IsLessOrEqual(Column.Parameter(equalityValue)) as Select;
                    break;
                case EqualityConditionType.IsGreaterOrEqual:
                    selectQuery = selectQuery.Where(_entityName, equalityKey).IsGreaterOrEqual(Column.Parameter(equalityValue)) as Select;
                    break;
                default:
                    break;
            }
        }

        private void And<T>(string equalityKey, T equalityValue, EqualityConditionType andCondition = EqualityConditionType.IsEqual)
        {
            switch (andCondition)
            {
                case EqualityConditionType.IsEqual:
                    selectQuery = selectQuery.And(_entityName, equalityKey).IsEqual(Column.Parameter(equalityValue)) as Select;
                    break;
                case EqualityConditionType.IsLessOrEqual:
                    selectQuery = selectQuery.And(_entityName, equalityKey).IsLessOrEqual(Column.Parameter(equalityValue)) as Select;
                    break;
                case EqualityConditionType.IsGreaterOrEqual:
                    selectQuery = selectQuery.And(_entityName, equalityKey).IsGreaterOrEqual(Column.Parameter(equalityValue)) as Select;
                    break;
                default:
                    break;
            }
        }

        public IDataReader ExecuteReader(DBExecutor dBExecutor)
        {
            IDataReader dataReader = selectQuery.ExecuteReader(dBExecutor);
            Reset();
            return dataReader;
        }

        public T BuildAndExecuteScalar<T>()
        {
            var result = selectQuery.ExecuteScalar<T>();
            Reset();
            return result;
        }

        public void Reset()
        {
            selectQuery = new Select(_userConnection);
        }
    }

    public class JoinParameter
    {
        public string ColumnName { get; set; }
        public string TableName { get; set; }
        public string JoinColumn { get; set; }
        public string ColumnAlias { get; set; }

        public JoinParameter(
            string columnName, 
            string tableName, 
            string joinColumn = null, 
            string columnAlias = null)
        {
            this.JoinColumn = joinColumn;
            this.ColumnName = columnName;
            this.TableName = tableName;
            this.ColumnAlias = columnAlias != null ? columnAlias : this.ColumnName;
        }
    }

    public enum EqualityConditionType
    {
        IsEqual,
        IsLessOrEqual,
        IsGreaterOrEqual
    }
}