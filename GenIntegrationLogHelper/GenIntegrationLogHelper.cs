 namespace Terrasoft.Configuration.GenIntegrationLogHelper
{
    using Newtonsoft.Json;
    using System;
    using Terrasoft.Core;
    using Terrasoft.Core.DB;

    public static class LogHelper
    {
        public enum LogResult
        {
            Error,
            Ok
        }

        public static class IntegrationDirection
        {
            public static readonly Guid Export = new Guid("0D8E082B-8A96-4C3F-A1B1-2B96D922ADEC");//	Экспорт
            public static readonly Guid Import = new Guid("A3F528A7-8A23-4435-8E3F-B2B84DF4EA3E");//	Импорт
        }

        public static class IntegrationResult
        {
            public static readonly Guid Ok = new Guid("00FC0D2C-6325-4ABC-AB97-90CABFB064E6");//	Успешно
            public static readonly Guid Error = new Guid("D62F29E2-A456-48D1-9E36-B01DA3C2ACDD");//	Ошибка
        }


        public static string SerializeToJson(object obj)
        {
            if (obj == null)
            {
                return string.Empty;
            }
            return JsonConvert.SerializeObject(obj);
        }



        public static void Log(UserConnection userConnection, LogResult logResult,
                string description, System.Diagnostics.Stopwatch stopWatch, Guid direction, object request)
        {
            stopWatch.Stop();
            var ts = stopWatch.Elapsed;
            var elapsedTime = String.Format("{0:00}:{1:00}.{2:00}",
                ts.Minutes, ts.Seconds, ts.Milliseconds / 10);

            if (description == null)
            {
                description = string.Empty;
            }
            /// *var needLogging = LogOnlyErrors(userConnection, operationUId);
            //if (needLogging == -1) return;
            //if (logResult == LogResult.OK && needLogging == 1) return;* /
            var resultId = Guid.Empty;
            switch (logResult)
            {
                case LogResult.Ok:
                    resultId = IntegrationResult.Ok;
                    break;
                case LogResult.Error:
                    resultId = IntegrationResult.Error;
                    break;
            }

            var inputMes = "";
            if (request != null)
            {
                inputMes = SerializeToJson(request);
            }
            var integrationLogInsert = new Insert(userConnection).Into("IntegrationLog")
                .Set("CreatedOn", Column.Parameter(DateTime.UtcNow))
                .Set("ModifiedOn", Column.Parameter(DateTime.UtcNow))
                .Set("CreatedById", Column.Parameter(userConnection.CurrentUser.ContactId))
                .Set("ModifiedById", Column.Parameter(userConnection.CurrentUser.ContactId))
                .Set("Date", Column.Parameter(DateTime.UtcNow))
                //.Set("IntegrationSystemId", systemUId == Guid.Empty ? Column.Parameter(null, "Guid") :Column.Parameter(systemUId))
                //.Set("OperationId", Column.Parameter(operationUId))
                .Set("DirectionId", Column.Parameter(direction))//IntegrationDirection.IMPORT
                .Set("ResultId", Column.Parameter(resultId))
                .Set("Description", Column.Parameter(description))
                //.Set("InputXml", Column.Parameter((_direction == IntegrationDirection.IMPORT && OperationContext.Current != null) ? OperationContext.Current.RequestContext.RequestMessage.ToString() : ""))
                .Set("GenRequestText", Column.Parameter((direction == IntegrationDirection.Import) ? inputMes : ""))
                .Set("GenElapsedTime", Column.Const(elapsedTime));

            integrationLogInsert.Execute();

        }
    }
}