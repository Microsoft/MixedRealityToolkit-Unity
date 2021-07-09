// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.

using System;
using System.Threading.Tasks;

namespace Microsoft.MixedReality.Toolkit.Utilities
{
    /// <summary>
    /// Response to a REST Call.
    /// </summary>
    public struct Response
    {
        /// <summary>
        /// Was the REST call successful?
        /// </summary>
        public bool Successful { get; }

        /// <summary>
        /// Response body from the resource.
        /// </summary>
        [Obsolete("This property is obsolete. " +
        "Use the GetResponseBody() method instead.", false)]
        public string ResponseBody => responseBody ?? (responseBody = responseBodyTask.Result);

        /// <summary>
        /// Response body from the resource.
        /// </summary>
        public async Task<string> GetResponseBody()
        {
            if (responseBody != null)
            {
                return responseBody;
            }
            return await responseBodyTask;
        }
        private string responseBody;
        private Task<string> responseBodyTask;

        /// <summary>
        /// Response data from the resource.
        /// </summary>
        public byte[] ResponseData => responseData ?? (responseData = responseDataAction?.Invoke());
        private byte[] responseData;
        private Func<byte[]> responseDataAction;

        /// <summary>
        /// Response code from the resource.
        /// </summary>
        public long ResponseCode { get; }

        /// <summary>
        /// Constructor.
        /// </summary>
        public Response(bool successful, string responseBody, byte[] responseData, long responseCode)
        {
            Successful = successful;
            responseBodyTask = null;
            this.responseBody = responseBody;
            responseDataAction = null;
            this.responseData = responseData;
            ResponseCode = responseCode;
        }

        public Response(bool successful, Task<string> responseBodyTask, Func<byte[]> responseDataAction, long responseCode)
        {
            Successful = successful;
            this.responseBodyTask = responseBodyTask;
            responseBody = null;
            this.responseDataAction = responseDataAction;
            responseData = null;
            ResponseCode = responseCode;
        }

        public Response(bool successful, Func<byte[]> responseDataAction, long responseCode)
        {
            Successful = successful;
            responseBodyTask = ResponseUtils.BytesToString(responseDataAction.Invoke());
            responseBody = null;
            this.responseDataAction = responseDataAction;
            responseData = null;
            ResponseCode = responseCode;
        }
    }
}