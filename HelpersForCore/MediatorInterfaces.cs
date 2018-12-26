using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace HelpersForCore
{
    public class SingleResponse<T>
    {
        public int Code { get; set; } = StatusCodes.Status200OK;
        public T Result { get; set; }
        public string Error { get; set; }
        public string Description { get; set; }

        public bool HasError => Error != null;
        public ObjectResult FailedResult => new ObjectResult(Error)
        {
            StatusCode = Code
        };

        public SingleResponse() { }
        public SingleResponse(T result)
        {
            Result = result;
        }

        public void WithError(int code, string error)
        {
            if (string.IsNullOrWhiteSpace(error))
            {
                error = "可預期但沒有描述的錯誤。";
            }
            Code = code;
            Error = error;
        }
    }

    public class MultiResponse<T> : SingleResponse<IEnumerable<T>>
    {
        public bool IsPaged { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int Count { get; set; }

        public MultiResponse() { }
        public MultiResponse(IEnumerable<T> result)
        {
            Result = result;
            Count = Result.Count();
        }
        public MultiResponse(IEnumerable<T> result, int pageNumber, int pageSize)
        {
            Result = result;
            Page(pageNumber, pageSize);
        }
        public void Page(int pageNumber, int pageSize)
        {
            IsPaged = true;
            PageNumber = pageNumber;
            PageSize = pageSize;
            Count = Result.Count();
            Result = Result.Skip((pageNumber - 1) * pageSize).Take(pageSize);
        }
    }
}
