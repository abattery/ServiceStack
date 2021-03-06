using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Reflection;
using ServiceStack.Common.Utils;
using ServiceStack.ServiceInterface.ServiceModel;
using ServiceStack.Text;

namespace ServiceStack.ServiceClient.Web
{
#if !NETFX_CORE && !WINDOWS_PHONE
    [Serializable]
#endif
    public class WebServiceException
        : Exception
    {
        public WebServiceException() { }
        public WebServiceException(string message) : base(message) { }
        public WebServiceException(string message, Exception innerException) : base(message, innerException) { }
#if !NETFX_CORE && !WINDOWS_PHONE
        public WebServiceException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#endif

        public int StatusCode { get; set; }

        public string StatusDescription { get; set; }

        public object ResponseDto { get; set; }
        
        public string ResponseBody { get; set; }

        private string errorCode;

        private void ParseResponseDto()
        {
            if (ResponseDto == null)
            {
                errorCode = StatusDescription;
                return;
            }
            var jsv = TypeSerializer.SerializeToString(ResponseDto);
            var map = TypeSerializer.DeserializeFromString<Dictionary<string, string>>(jsv);
#if NETFX_CORE
            map = new Dictionary<string, string>(map, StringComparer.CurrentCultureIgnoreCase);
#else
            map = new Dictionary<string, string>(map, StringComparer.InvariantCultureIgnoreCase);
#endif
            string responseStatus;
            if (!map.TryGetValue("ResponseStatus", out responseStatus)) return;

            var rsMap = TypeSerializer.DeserializeFromString<Dictionary<string, string>>(responseStatus);
            if (rsMap == null) return;
#if NETFX_CORE
            rsMap = new Dictionary<string, string>(rsMap, StringComparer.CurrentCultureIgnoreCase);
#else
            rsMap = new Dictionary<string, string>(rsMap, StringComparer.InvariantCultureIgnoreCase);
#endif
            rsMap.TryGetValue("ErrorCode", out errorCode);
            rsMap.TryGetValue("Message", out errorMessage);
            rsMap.TryGetValue("StackTrace", out serverStackTrace);
        }

        public string ErrorCode
        {
            get
            {
                if (errorCode == null)
                {
                    ParseResponseDto();
                }
                return errorCode;
            }
        }

        private string errorMessage;
        public string ErrorMessage
        {
            get
            {
                if (errorMessage == null)
                {
                    ParseResponseDto();
                }
                return errorMessage;
            }
        }

        private string serverStackTrace;
        public string ServerStackTrace
        {
            get
            {
                if (serverStackTrace == null)
                {
                    ParseResponseDto();
                }
                return serverStackTrace;
            }
        }

        public ResponseStatus ResponseStatus
        {
            get
            {
                if (this.ResponseDto == null)
                    return null;

                var hasResponseStatus = this.ResponseDto as IHasResponseStatus;
                if (hasResponseStatus != null)
                    return hasResponseStatus.ResponseStatus;

#if NETFX_CORE
                var propertyInfo = this.ResponseDto.GetType().GetRuntimeProperty("ResponseStatus");
#else
                var propertyInfo = this.ResponseDto.GetType().GetProperty("ResponseStatus");
#endif
                if (propertyInfo == null)
                    return null;

                return ReflectionUtils.GetProperty(this.ResponseDto, propertyInfo) as ResponseStatus;
            }
        }

        public List<ResponseError> GetFieldErrors()
        {
            var responseStatus = ResponseStatus;
            if (responseStatus != null)
                return responseStatus.Errors ?? new List<ResponseError>();

            return new List<ResponseError>();
        }
    }
}
