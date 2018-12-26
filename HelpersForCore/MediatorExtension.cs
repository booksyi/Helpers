using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;

namespace HelpersForCore
{
    public static class MediatorExtension
    {
        public static ActionResult Ok<T>(this SingleResponse<T> response)
        {
            if (response.HasError)
            {
                return response.FailedResult;
            }
            return new OkObjectResult(response);
        }

        public static ActionResult File<T>(this SingleResponse<T> response, string contentType, string fileDownloadName) where T : System.IO.Stream
        {
            if (response.HasError)
            {
                return response.FailedResult;
            }
            return new FileStreamResult(response.Result, contentType) { FileDownloadName = fileDownloadName };
        }

        /// <summary>
        /// 自訂 response 轉成 ObjectResult 的方法
        /// (理論上 T1, T2 應可自動推斷, 但實際卻無法, 導致有點難用)
        /// </summary>
        /// <typeparam name="T1">呼叫此方法的物件類型</typeparam>
        /// <typeparam name="T2">T1 源頭繼承 SingleResponse 的泛型類型</typeparam>
        /// <example>
        /// /* if response type is MultiResponse<T> */
        /// return response.Custom<MultiResponse<T>, IEnumerable<T>>(x => new ObjectResult(0));
        /// </example>
        public static ActionResult Custom<T1, T2>(this T1 response, Func<T1, ActionResult> custom) where T1 : SingleResponse<T2>
        {
            if (response.HasError)
            {
                return response.FailedResult;
            }
            return custom(response);
        }

        public static ActionResult PagedResult<T>(this MultiResponse<T> response, int pageNumber, int pageSize)
        {
            if (response.HasError)
            {
                return response.FailedResult;
            }
            if (response.IsPaged == false)
            {
                response.Page(pageNumber, pageSize);
            }
            if (response.PageNumber != pageNumber || response.PageSize != pageSize)
            {
                response.WithError(
                    StatusCodes.Status500InternalServerError,
                    "已經分頁的結果，無法再以不同的分頁參數呈現。");
            }
            return response.Ok();
        }
    }
}
