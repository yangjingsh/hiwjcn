﻿using Lib.cache;
using Lib.core;
using Lib.extension;
using Lib.helper;
using Lib.mvc.user;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Mvc;

namespace Lib.mvc
{
    /// <summary>
    /// 阻止CSRF
    /// </summary>
    public class ValidCSRFAttribute : ActionFilterAttribute
    {
        public static string CreateCSRFFieldHtml(string session_key)
        {
            var token = Com.GetUUID();
            System.Web.HttpContext.Current.Session[session_key] = token;
            var html = new StringBuilder();
            html.Append($"<input type='hidden' name='{FORM_KEY}' value='{token}' />");
            return html.ToString();
        }

        public const string FORM_KEY = "csrf_token";

        public string _SessionKey { get; set; }

        public ValidCSRFAttribute(string SessionKey)
        {
            this._SessionKey = SessionKey;

        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var token_client = filterContext.HttpContext.Request.Form[FORM_KEY];
            var token_server = filterContext.HttpContext.Session[_SessionKey]?.ToString();
            if (token_client != token_server)
            {
                filterContext.Result = new ContentResult() { Content = "try again" };
                return;
            }

            base.OnActionExecuting(filterContext);
        }
    }

    /// <summary>
    /// 过滤不是来自本站的请求
    /// </summary>
    public class CheckReferrerUrlAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var reffer = ConvertHelper.GetString(filterContext.HttpContext.Request.UrlReferrer).ToLower();
            var allowlist = ConfigHelper.Instance.AllowDomains;
            if (ValidateHelper.IsPlumpList(allowlist))
            {
                bool find = false;
                foreach (var domain in allowlist)
                {
                    if (reffer.Contains(ConvertHelper.GetString(domain).ToLower()))
                    {
                        find = true;
                        break;
                    }
                }
                if (!find)
                {
                    filterContext.Result = new ContentResult() { Content = string.Empty };
                    return;
                }
            }
            base.OnActionExecuting(filterContext);
        }
    }

    /// <summary>
    /// 标记接口过期时间
    /// </summary>
    public class ApiExpireAtAttribute : ActionFilterAttribute
    {
        private DateTime Date { get; set; }

        public ApiExpireAtAttribute(string date)
        {
            this.Date = DateTime.Parse(date);
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            if (DateTime.Now > this.Date)
            {
                filterContext.Result = new CustomJsonResult()
                {
                    Data = new ResJson() { success = false, msg = "无法响应请求，请升级客户端" },
                    JsonRequestBehavior = JsonRequestBehavior.AllowGet
                };
                return;
            }
            base.OnActionExecuting(filterContext);
        }
    }

    /// <summary>
    /// 异常处理
    /// </summary>
    public class LogErrorAttribute : HandleErrorAttribute
    {
        /// <summary>
        /// 处理异常 404 500。。
        /// </summary>
        /// <param name="filterContext"></param>
        public override void OnException(ExceptionContext filterContext)
        {
            if (!filterContext.ExceptionHandled)
            {
                var ex = filterContext.Exception;
                if (ex != null && !filterContext.ExceptionHandled)
                {
                    ex.SaveLog("全局捕捉到错误");
                }
                filterContext.ExceptionHandled = true;
            }
            base.OnException(filterContext);
        }
    }

    /// <summary>
    /// 设置编码
    /// </summary>
    public class SetEncodingAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var config = ConfigHelper.Instance;
            filterContext.HttpContext.Response.ContentEncoding = config.SystemEncoding;
            filterContext.HttpContext.Request.ContentEncoding = config.SystemEncoding;
            base.OnActionExecuting(filterContext);
        }
    }

    /// <summary>
    /// 跨域请求
    /// </summary>
    public class CrossDomainAjaxAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuted(ActionExecutedContext filterContext)
        {
            ResponseHelper.AllowCrossDomainAjax(System.Web.HttpContext.Current);

            base.OnActionExecuted(filterContext);
        }
    }

    /// <summary>
    /// 缓存页面html
    /// 参考OutputCache
    /// </summary>
    public class HtmlCacheAttribute : ActionFilterAttribute
    {
        /// <summary>
        /// 缓存时间
        /// </summary>
        public int Minute { get; set; }

        /// <summary>
        /// 是否是从缓存中读取的数据
        /// </summary>
        private bool ReadFromCache = false;

        /// <summary>
        /// 访问页面URL，作为key使用
        /// </summary>
        private string URL { get; set; }

        /// <summary>
        /// 缓存组件
        /// </summary>
        private ICacheProvider cache { get; set; }

        /// <summary>
        /// 缓存页面html，时间是分钟
        /// </summary>
        /// <param name="minute"></param>
        public HtmlCacheAttribute(int minute = 5)
        {
            this.Minute = minute;
            cache = CacheManager.CacheProvider();
        }

        #region 执行action前，尝试读取缓存html
        /// <summary>
        /// action执行中
        /// </summary>
        /// <param name="filterContext"></param>
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            //URL作为key
            URL = ConvertHelper.GetString(filterContext.HttpContext.Request.Url);

            var data = cache.Get<string>(URL);

            if (data.Success)
            {
                ReadFromCache = true;
                filterContext.Result = new ContentResult()
                {
                    Content = data.Result,
                    ContentEncoding = Encoding.UTF8,
                    ContentType = "text/html"
                };
                return;
            }
            else
            {
                //attribute好像只实例化了一次，这里必须手动设置，不然会用上一次的值
                ReadFromCache = false;
            }
            base.OnActionExecuting(filterContext);
        }
        #endregion

        #region 执行action后，如果不是从缓存读取的数据就加入缓存
        /// <summary>
        /// 结果执行完毕
        /// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        /// 注意：这个方法在渲染view后执行，
        /// 在这个方法里将重新渲染view拿到html（因为不知道怎么拿到之前渲染好的html）
        /// 如果在前一次渲染view的时候修改viewdata中的数据
        /// 将会导致最终被缓存的内容与第一次渲染的内容不一致
        /// 
        /// 重要的事情说三遍【不要修改viewdata中的数据，要修改请重新创建对象】
        /// !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        /// </summary>
        /// <param name="filterContext"></param>
        public override void OnResultExecuted(ResultExecutedContext filterContext)
        {
            //如果不是从缓存中读取的就添加到缓存
            if (!ReadFromCache)
            {
                //获取view的名称
                string viewName = filterContext.RouteData.GetRequiredString("action");
                viewName = ConvertHelper.GetString(viewName);
                using (var sw = new StringWriter())
                {
                    //找到view
                    var view = System.Web.Mvc.ViewEngines.Engines.FindView(
                        filterContext.Controller.ControllerContext, viewName, string.Empty).View;
                    //初始化view上下文
                    var vc = new ViewContext(
                        filterContext.Controller.ControllerContext,
                        view,
                        filterContext.Controller.ViewData, filterContext.Controller.TempData,
                        sw);
                    //渲染view
                    view.Render(vc, sw);
                    //获取渲染后的html
                    var html = sw.ToString();
                    cache.Set(URL, html, Minute * 60);
                }
            }
            base.OnResultExecuted(filterContext);
        }
        #endregion

    }

    /// <summary>
    /// 验证签名
    /// </summary>
    public class ValidateSignAttribute : ActionFilterAttribute
    {
        private void AddToNameValueCollection(ref NameValueCollection col, NameValueCollection nv)
        {
            foreach (var key in nv.AllKeys)
            {
                if (ConvertHelper.GetString(key).Length > 32
                    || ConvertHelper.GetString(nv[key]).Length > 32) { continue; }

                col[key] = nv[key];
            }
        }

        public string ConfigKey { get; set; } = "sign_key";
        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var sign_key = ConfigurationManager.AppSettings[ConfigKey];
            if (!ValidateHelper.IsPlumpString(sign_key)) { throw new Exception("没有配置签名的约定key"); }

            var reqparams = new NameValueCollection();
            AddToNameValueCollection(ref reqparams, filterContext.HttpContext.Request.Form);
            AddToNameValueCollection(ref reqparams, filterContext.HttpContext.Request.QueryString);

            #region 验证时间戳
            var timespan = ConvertHelper.GetInt64(reqparams["timespan"], -1);
            if (timespan < 0)
            {
                filterContext.Result = ResultHelper.BadRequest("缺少时间戳");
                return;
            }
            var server_timestamp = DateTimeHelper.GetTimeStamp();
            if (server_timestamp - timespan > 5 || server_timestamp < timespan)
            {
                filterContext.Result = ResultHelper.BadRequest("请求内容已经过期");
                return;
            }
            #endregion

            #region 验证签名
            var signKey = "sign";
            var sign = ConvertHelper.GetString(reqparams[signKey]).ToUpper();
            if (!ValidateHelper.IsAllPlumpString(sign))
            {
                filterContext.Result = ResultHelper.BadRequest("请求被拦截，获取不到签名");
                return;
            }

            //排序的字典
            var dict = new SortedDictionary<string, string>(new MyStringComparer());

            foreach (var p in reqparams.AllKeys)
            {
                if (!ValidateHelper.IsAllPlumpString(p) || p == signKey) { continue; }
                if (p.Length > 32 || reqparams[p]?.Length > 32) { continue; }

                dict[p] = ConvertHelper.GetString(reqparams[p]);
            }

            var strdata = dict.ToUrlParam();
            strdata += sign_key;
            strdata = strdata.ToLower();

            var md5 = strdata.ToMD5().ToUpper();
            if (sign != md5)
            {
                var msg = $"服务器加密：{md5}客户端加密：{sign}服务器排序：{strdata}";
                filterContext.Result = ResultHelper.BadRequest(msg);
                return;
            }
            #endregion

            base.OnActionExecuting(filterContext);
        }
    }

    /// <summary>
    /// 防止重复提交
    /// </summary>
    public class AntiReSubmitAttribute : ActionFilterAttribute
    {
        private void AddToNameValueCollection(ref NameValueCollection col, NameValueCollection nv)
        {
            foreach (var key in nv.AllKeys)
            {
                if (ConvertHelper.GetString(key).Length > 32
                    || ConvertHelper.GetString(nv[key]).Length > 32) { continue; }

                col[key] = nv[key];
            }
        }

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            var sessionID = filterContext.HttpContext.Session.SessionID;
            var key = $"{nameof(AntiReSubmitAttribute)}:{sessionID}";

            var reqparams = new NameValueCollection();
            AddToNameValueCollection(ref reqparams, filterContext.HttpContext.Request.Form);
            AddToNameValueCollection(ref reqparams, filterContext.HttpContext.Request.QueryString);

            var dict = new SortedDictionary<string, string>();
            foreach (var k in reqparams.AllKeys)
            {
                dict[k] = reqparams[k];
            }
            var submitData = string.Join("&", dict.Select(x => $"{x.Key}={x.Value}"));
            var AreaName = ConvertHelper.GetString(filterContext.RouteData.Values["Area"]);
            var ControllerName = ConvertHelper.GetString(filterContext.RouteData.Values["Controller"]);
            var ActionName = ConvertHelper.GetString(filterContext.RouteData.Values["Action"]);
            submitData = $"{AreaName}/{ControllerName}/{ActionName}/:{submitData}";
            //读取缓存
            using (var cache = CacheManager.CacheProvider())
            {
                var data = cache.Get<string>(key);
                if (data.Success)
                {
                    if (data.Result == submitData)
                    {
                        filterContext.Result = ResultHelper.BadRequest("重复提交，请稍后再试");
                        return;
                    }
                }
                //5秒钟不能提交相同的数据
                cache.Set(key, submitData, 5);
            }
            base.OnActionExecuting(filterContext);
        }
    }
}
