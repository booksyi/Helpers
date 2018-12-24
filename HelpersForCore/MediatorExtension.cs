using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HelpersForCore
{
    public static class MediatorExtension
    {
        public static ObjectResult Ok<T>(this ResponseBase<T> response)
        {
            if (response.HasError)
            {
                return response.FailedResult;
            }
            return new OkObjectResult(response);
        }

        public static ObjectResult PagedResult<T>(this MultiResponse<T> response, int pageNumber, int pageSize)
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
                response.WithError(500, "已經分頁的結果，無法再以不同的分頁參數呈現。");
            }
            return response.Ok();
        }
    }
}
