using System;
using System.Collections;
using System.Diagnostics;
using System.Net;

namespace AspNet.Mvc
{
    public class ApiResult
    {
        #region Private Variables
        private Stopwatch timer = null;
        #endregion

        #region Constructors
        public ApiResult() : this(true) { }
        public ApiResult(bool startTimer)
        {
            this.StatusCode = HttpStatusCode.OK;
            timer = new Stopwatch();
            if (startTimer) timer.Start();
        }
        #endregion

        #region Public Properties
        public HttpStatusCode StatusCode { get; set; }
        public long ElapsedTime
        {
            get
            {
                return timer.ElapsedMilliseconds;
            }
        }
        public string StatusDescription {
            get
            {
                return this.StatusCode.ToString();        
            }
        }
        public string StatusMessage { get; set; }
        public int Total { get { return Data.Count; } }
        public int CurrrentPage { get; set; }
        public int ItemsPerPage { get; set; }
        public int Paging { get; set; }
        public ICollection Data { get; set; }
        #endregion
    }
}