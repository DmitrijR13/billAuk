namespace Bars.KP50.Report.Handlers
{
    using System;
    using System.Web;
    using System.Web.SessionState;

    using Newtonsoft.Json;

    public abstract class JsonRequestHandler : IHttpHandler, IRequiresSessionState
    {
        public bool IsReusable
        {
            get { return false; }
        }

        public abstract void ProcessRequest(HttpContext context);

        protected virtual string GetJson(object value)
        {
            return JsonConvert.SerializeObject(value);
        }

        protected virtual void SetFailure(HttpContext context, string message)
        {
            SetResponse(context, JsonConvert.SerializeObject(new { success = false, message }));
        }

        protected virtual void SetResponse(HttpContext context, string jsonResponse)
        {
            context.Response.Cache.SetExpires(DateTime.Now);
            context.Response.ContentType = "application/json";
            context.Response.Write(jsonResponse);
            //context.Response.BufferOutput = true;
            //context.Response.Flush();
            //context.Response.Close();
           // context.Response.End();
        }
    }
}